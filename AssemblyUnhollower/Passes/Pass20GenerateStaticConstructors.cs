using System;
using System.Diagnostics;
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
            var baseLibModule = context.GetAssemblyByName("mscorlib");
            var moduleType = baseLibModule.NewAssembly.MainModule.GetType("<Module>");
            var moduleStaticCtor = new MethodDefinition(".cctor",
                MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.SpecialName |
                MethodAttributes.HideBySig | MethodAttributes.RTSpecialName, baseLibModule.Imports.Void);
            moduleType.Methods.Add(moduleStaticCtor);
            
            
            foreach (var assemblyContext in context.Assemblies)
            foreach (var typeContext in assemblyContext.Types)
                GenerateStaticProxy(assemblyContext, typeContext, moduleStaticCtor.Body.GetILProcessor(), baseLibModule);
            
            moduleStaticCtor.Body.GetILProcessor().Emit(OpCodes.Ret);
            
            LogSupport.Trace($"\nTokenless method count: {context.Statistics.TokenLessMethods}");
        }

        private static void GenerateStaticProxy(AssemblyRewriteContext assemblyContext, TypeRewriteContext typeContext, ILProcessor systemTypeInitializer, AssemblyRewriteContext systemContext)
        {
            if (typeContext.RewriteSemantic == TypeRewriteContext.TypeRewriteSemantic.UseSystemValueType || typeContext.RewriteSemantic == TypeRewriteContext.TypeRewriteSemantic.UseSystemInterface)
            {
                GenerateSystemTypeData(systemContext, typeContext, systemTypeInitializer);
                return;
            }
            
            var oldType = typeContext.OriginalType;
            if (typeContext.RewriteSemantic != TypeRewriteContext.TypeRewriteSemantic.Default || oldType.IsEnum) 
                return;
            
            
            var newType = typeContext.NewType;

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
                    ctorBuilder.EmitLdcI4((int) token);
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