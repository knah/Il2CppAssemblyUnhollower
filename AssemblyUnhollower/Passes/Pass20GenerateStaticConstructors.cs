using System;
using AssemblyUnhollower.Contexts;
using AssemblyUnhollower.Extensions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnhollowerBaseLib;

namespace AssemblyUnhollower.Passes
{
    public static class Pass20GenerateStaticConstructors
    {
        public static void DoPass(RewriteGlobalContext context)
        {
            OldDoPass(context);//todo: replace with new method
            //NewDoPass(context);
        }

        private static void OldDoPass(RewriteGlobalContext context)
        {
            foreach (var assemblyContext in context.Assemblies)
            foreach (var typeContext in assemblyContext.Types)
                GenerateStaticProxyOld(assemblyContext, typeContext);
            
            LogSupport.Trace($"\nTokenless method count: {context.Statistics.TokenLessMethods}");
        }

        private static void NewDoPass(RewriteGlobalContext context)
        {
            var baseLibModule = context.GetAssemblyByName("mscorlib");
            var moduleType = baseLibModule.NewAssembly.MainModule.GetType("<Module>");
            var moduleStaticCtor = new MethodDefinition(".cctor",
                MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.SpecialName |
                MethodAttributes.HideBySig | MethodAttributes.RTSpecialName, baseLibModule.Imports.Void);
            moduleType.Methods.Add(moduleStaticCtor);

            foreach (var assemblyContext in context.Assemblies)
            foreach (var typeContext in assemblyContext.Types)
                GenerateStaticProxyNew(assemblyContext, typeContext, moduleStaticCtor.Body.GetILProcessor(), baseLibModule);

            moduleStaticCtor.Body.GetILProcessor().Emit(OpCodes.Ret);

            LogSupport.Trace($"\nTokenless method count: {context.Statistics.TokenLessMethods}");
        }

        private static void GenerateStaticProxyOld(AssemblyRewriteContext assemblyContext, TypeRewriteContext typeContext)
        {
            var oldType = typeContext.OriginalType;
            var newType = typeContext.NewType;

            var staticCtorMethod = new MethodDefinition(".cctor",
                MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.SpecialName |
                MethodAttributes.HideBySig | MethodAttributes.RTSpecialName, assemblyContext.Imports.Void);
            newType.Methods.Add(staticCtorMethod);
            
            var ctorBuilder = staticCtorMethod.Body.GetILProcessor();

            if (newType.IsNested) {
                ctorBuilder.Emit(OpCodes.Ldsfld, assemblyContext.GlobalContext.GetNewTypeForOriginal(oldType.DeclaringType).ClassPointerFieldRef);
                ctorBuilder.Emit(OpCodes.Ldstr, oldType.Name);
                ctorBuilder.Emit(OpCodes.Call, assemblyContext.Imports.GetIl2CppNestedClass);
            } else {
                ctorBuilder.Emit(OpCodes.Ldstr, oldType.Module.Name);
                ctorBuilder.Emit(OpCodes.Ldstr, oldType.Namespace);
                ctorBuilder.Emit(OpCodes.Ldstr, oldType.Name);
                ctorBuilder.Emit(OpCodes.Call, assemblyContext.Imports.GetIl2CppGlobalClass);
            }

            if (oldType.HasGenericParameters)
            {
                var il2CppTypeTypeRewriteContext = assemblyContext.GlobalContext.GetAssemblyByName("mscorlib").GetTypeByName("System.Type");
                var il2CppSystemTypeRef = newType.Module.ImportReference(il2CppTypeTypeRewriteContext.NewType);
                
                var il2CppTypeHandleTypeRewriteContext = assemblyContext.GlobalContext.GetAssemblyByName("mscorlib").GetTypeByName("System.RuntimeTypeHandle");
                var il2CppSystemTypeHandleRef = newType.Module.ImportReference(il2CppTypeHandleTypeRewriteContext.NewType);
                
                ctorBuilder.Emit(OpCodes.Call, assemblyContext.Imports.GetIl2CppTypeFromClass);
                ctorBuilder.Emit(OpCodes.Call, new MethodReference("internal_from_handle", il2CppSystemTypeRef, il2CppSystemTypeRef) { Parameters = { new ParameterDefinition(assemblyContext.Imports.IntPtr) }});

                ctorBuilder.EmitLdcI4(oldType.GenericParameters.Count);
                
                ctorBuilder.Emit(OpCodes.Newarr, il2CppSystemTypeRef);
                
                for (var i = 0; i < oldType.GenericParameters.Count; i++)
                {
                    ctorBuilder.Emit(OpCodes.Dup);
                    ctorBuilder.EmitLdcI4(i);
                    
                    var param = oldType.GenericParameters[i];
                    var storeRef = new GenericInstanceType(assemblyContext.Imports.Il2CppClassPointerStore) { GenericArguments = { param }};
                    var fieldRef = new FieldReference(nameof(Il2CppClassPointerStore<object>.NativeClassPtr), assemblyContext.Imports.IntPtr, storeRef);
                    ctorBuilder.Emit(OpCodes.Ldsfld, fieldRef);
                    
                    ctorBuilder.Emit(OpCodes.Call, assemblyContext.Imports.GetIl2CppTypeFromClass);
                    
                    ctorBuilder.Emit(OpCodes.Call, new MethodReference("internal_from_handle", il2CppSystemTypeRef, il2CppSystemTypeRef) { Parameters = { new ParameterDefinition(assemblyContext.Imports.IntPtr) }});
                    ctorBuilder.Emit(OpCodes.Stelem_Ref);
                }

                var il2CppTypeArray = new GenericInstanceType(assemblyContext.Imports.Il2CppReferenceArray) { GenericArguments = { il2CppSystemTypeRef }};
                ctorBuilder.Emit(OpCodes.Newobj, new MethodReference(".ctor", assemblyContext.Imports.Void, il2CppTypeArray) {HasThis = true, Parameters = { new ParameterDefinition(new ArrayType(assemblyContext.Imports.Il2CppReferenceArray.GenericParameters[0])) }});
                ctorBuilder.Emit(OpCodes.Call, new MethodReference(nameof(Type.MakeGenericType), il2CppSystemTypeRef, il2CppSystemTypeRef) { HasThis = true, Parameters = { new ParameterDefinition(il2CppTypeArray) }});
                
                ctorBuilder.Emit(OpCodes.Call, new MethodReference(typeof(Type).GetProperty(nameof(Type.TypeHandle))!.GetMethod!.Name, il2CppSystemTypeHandleRef, il2CppSystemTypeRef) { HasThis = true });
                ctorBuilder.Emit(OpCodes.Ldfld, new FieldReference("value", assemblyContext.Imports.IntPtr, il2CppSystemTypeHandleRef));
                
                ctorBuilder.Emit(OpCodes.Call, assemblyContext.Imports.GetIl2CppTypeToClass);
            }
            
            ctorBuilder.Emit(OpCodes.Stsfld, typeContext.ClassPointerFieldRef);
            
            if (oldType.IsBeforeFieldInit)
            {
                ctorBuilder.Emit(OpCodes.Ldsfld, typeContext.ClassPointerFieldRef);
                ctorBuilder.Emit(OpCodes.Call, assemblyContext.Imports.RuntimeClassInit);
            }

            if (oldType.IsEnum)
            {
                ctorBuilder.Emit(OpCodes.Ret);
                return;
            }

            foreach (var field in typeContext.Fields)
            {
                ctorBuilder.Emit(OpCodes.Ldsfld, typeContext.ClassPointerFieldRef);
                ctorBuilder.Emit(OpCodes.Ldstr, field.OriginalField.Name);
                ctorBuilder.Emit(OpCodes.Call, assemblyContext.Imports.GetFieldPointer);
                ctorBuilder.Emit(OpCodes.Stsfld, field.PointerField);
            }

            foreach (var method in typeContext.Methods)
            {
                ctorBuilder.Emit(OpCodes.Ldsfld, typeContext.ClassPointerFieldRef);
                
                var token = method.OriginalMethod.ExtractToken();
                if (token == 0)
                {
                    typeContext.AssemblyContext.GlobalContext.Statistics.TokenLessMethods++;
                    
                    ctorBuilder.Emit(method.OriginalMethod.GenericParameters.Count > 0 ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                    ctorBuilder.Emit(OpCodes.Ldstr, method.OriginalMethod.Name);
                    ctorBuilder.EmitLoadTypeNameString(assemblyContext.Imports, method.OriginalMethod, method.OriginalMethod.ReturnType, method.NewMethod.ReturnType);
                    ctorBuilder.Emit(OpCodes.Ldc_I4, method.OriginalMethod.Parameters.Count);
                    ctorBuilder.Emit(OpCodes.Newarr, assemblyContext.Imports.String);

                    for (var i = 0; i < method.OriginalMethod.Parameters.Count; i++)
                    {
                        ctorBuilder.Emit(OpCodes.Dup);
                        ctorBuilder.EmitLdcI4(i);
                        ctorBuilder.EmitLoadTypeNameString(assemblyContext.Imports, method.OriginalMethod, method.OriginalMethod.Parameters[i].ParameterType, method.NewMethod.Parameters[i].ParameterType);
                        ctorBuilder.Emit(OpCodes.Stelem_Ref);
                    }

                    ctorBuilder.Emit(OpCodes.Call, assemblyContext.Imports.GetIl2CppMethod);
                }
                else
                {
                    ctorBuilder.EmitLdcI4((int) token);
                    ctorBuilder.Emit(OpCodes.Call, assemblyContext.Imports.GetIl2CppMethodFromToken);
                }

                ctorBuilder.Emit(OpCodes.Stsfld, method.NonGenericMethodInfoPointerField);
            }
            
            ctorBuilder.Emit(OpCodes.Ret);
        }

        private static void GenerateStaticProxyNew(AssemblyRewriteContext assemblyContext, TypeRewriteContext typeContext, ILProcessor systemTypeInitializer, AssemblyRewriteContext systemContext)
        {
            if (typeContext.RewriteSemantic == TypeRewriteContext.TypeRewriteSemantic.UseSystemValueType || typeContext.RewriteSemantic == TypeRewriteContext.TypeRewriteSemantic.UseSystemInterface)
            {
                GenerateSystemTypeData(systemContext, typeContext, systemTypeInitializer);
                return;
            }

            var oldType = typeContext.OriginalType;
            var newType = typeContext.NewType;
            if (typeContext.RewriteSemantic != TypeRewriteContext.TypeRewriteSemantic.Default || oldType.IsEnum)
                return;

            var staticCtorMethod = new MethodDefinition(".cctor",
                MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.SpecialName |
                MethodAttributes.HideBySig | MethodAttributes.RTSpecialName, assemblyContext.Imports.Void);
            newType.Methods.Add(staticCtorMethod);

            var ctorBuilder = staticCtorMethod.Body.GetILProcessor();

            if (oldType.IsBeforeFieldInit)
            {
                ctorBuilder.Emit(OpCodes.Ldsfld, typeContext.ClassPointerFieldRef);
                ctorBuilder.Emit(OpCodes.Call, assemblyContext.Imports.RuntimeClassInit);
            }

            foreach (var field in typeContext.Fields)
            {
                ctorBuilder.Emit(OpCodes.Ldsfld, typeContext.ClassPointerFieldRef);
                ctorBuilder.Emit(OpCodes.Ldstr, field.OriginalField.Name);
                ctorBuilder.Emit(OpCodes.Call, assemblyContext.Imports.GetFieldPointer);
                ctorBuilder.Emit(OpCodes.Stsfld, field.PointerField);
            }

            foreach (var method in typeContext.Methods)
            {
                ctorBuilder.Emit(OpCodes.Ldsfld, typeContext.ClassPointerFieldRef);

                var token = method.OriginalMethod.ExtractToken();
                if (token == 0)
                {
                    typeContext.AssemblyContext.GlobalContext.Statistics.TokenLessMethods++;

                    ctorBuilder.Emit(method.OriginalMethod.GenericParameters.Count > 0 ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                    ctorBuilder.Emit(OpCodes.Ldstr, method.OriginalMethod.Name);
                    ctorBuilder.EmitLoadTypeNameString(assemblyContext.Imports, method.OriginalMethod, method.OriginalMethod.ReturnType, method.NewMethod.ReturnType);
                    ctorBuilder.Emit(OpCodes.Ldc_I4, method.OriginalMethod.Parameters.Count);
                    ctorBuilder.Emit(OpCodes.Newarr, assemblyContext.Imports.String);

                    for (var i = 0; i < method.OriginalMethod.Parameters.Count; i++)
                    {
                        ctorBuilder.Emit(OpCodes.Dup);
                        ctorBuilder.EmitLdcI4(i);
                        ctorBuilder.EmitLoadTypeNameString(assemblyContext.Imports, method.OriginalMethod, method.OriginalMethod.Parameters[i].ParameterType, method.NewMethod.Parameters[i].ParameterType);
                        ctorBuilder.Emit(OpCodes.Stelem_Ref);
                    }

                    ctorBuilder.Emit(OpCodes.Call, assemblyContext.Imports.GetIl2CppMethod);
                }
                else
                {
                    ctorBuilder.EmitLdcI4((int)token);
                    ctorBuilder.Emit(OpCodes.Call, assemblyContext.Imports.GetIl2CppMethodFromToken);
                }

                ctorBuilder.Emit(OpCodes.Stsfld, method.NonGenericMethodInfoPointerField);
            }

            ctorBuilder.Emit(OpCodes.Ret);
        }

        private static void GenerateSystemTypeData(AssemblyRewriteContext systemContext, TypeRewriteContext typeContext, ILProcessor systemTypeInitializer)
        {
            systemTypeInitializer.Emit(OpCodes.Ldtoken, systemContext.NewAssembly.MainModule.ImportReference(typeContext.NewType));
            systemTypeInitializer.Emit(OpCodes.Call, systemContext.Imports.TypeFromToken);
            systemTypeInitializer.Emit(OpCodes.Ldstr, typeContext.AssemblyContext.OriginalAssembly.Name.Name);
            systemTypeInitializer.Emit(OpCodes.Ldc_I4, typeContext.Il2CppToken);
            systemTypeInitializer.Emit(OpCodes.Call, systemContext.Imports.RegisterTypeTokenExplicit);
        }

        private static void EmitLoadTypeNameString(this ILProcessor ctorBuilder, AssemblyKnownImports imports, MethodDefinition originalMethod, TypeReference originalTypeReference, TypeReference newTypeReference)
        {
            if (originalMethod.HasGenericParameters || originalTypeReference.FullName == "System.Void")
                ctorBuilder.Emit(OpCodes.Ldstr, originalTypeReference.FullName);
            else
            {
                ctorBuilder.Emit(newTypeReference.IsByReference ? OpCodes.Ldc_I4_1 :  OpCodes.Ldc_I4_0);
                ctorBuilder.Emit(OpCodes.Call, imports.Module.ImportReference(new GenericInstanceMethod(imports.Il2CppRenderTypeNameGeneric) {GenericArguments = {newTypeReference}}));
            }
        }
    }
}