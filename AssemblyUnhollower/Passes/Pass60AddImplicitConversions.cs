using System.Linq;
using AssemblyUnhollower.Contexts;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnhollowerRuntimeLib;

namespace AssemblyUnhollower.Passes
{
    public static class Pass60AddImplicitConversions
    {
        public static void DoPass(RewriteGlobalContext context)
        {
            var assemblyContext = context.GetAssemblyByName("mscorlib");
            var typeContext = assemblyContext.GetTypeByName("System.String");
            var objectTypeContext = assemblyContext.GetTypeByName("System.Object");
            
            var methodFromMonoString = new MethodDefinition("op_Implicit", MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeContext.NewType);
            methodFromMonoString.Parameters.Add(new ParameterDefinition(assemblyContext.Imports.String));
            typeContext.NewType.Methods.Add(methodFromMonoString);
            var fromBuilder = methodFromMonoString.Body.GetILProcessor();

            var createIl2CppStringNop = fromBuilder.Create(OpCodes.Nop);
            
            fromBuilder.Emit(OpCodes.Ldarg_0);
            fromBuilder.Emit(OpCodes.Dup);
            fromBuilder.Emit(OpCodes.Brtrue_S, createIl2CppStringNop);
            fromBuilder.Emit(OpCodes.Ret);
            
            fromBuilder.Append(createIl2CppStringNop);
            fromBuilder.Emit(OpCodes.Call, assemblyContext.Imports.StringToNative);
            fromBuilder.Emit(OpCodes.Newobj,
                new MethodReference(".ctor", assemblyContext.Imports.Void, typeContext.NewType)
                {
                    HasThis = true, Parameters = {new ParameterDefinition(assemblyContext.Imports.IntPtr)}
                });
            fromBuilder.Emit(OpCodes.Ret);
            
            var methodToObject = new MethodDefinition("op_Implicit", MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, objectTypeContext.NewType);
            methodToObject.Parameters.Add(new ParameterDefinition(assemblyContext.Imports.String));
            objectTypeContext.NewType.Methods.Add(methodToObject);
            var toObjectBuilder = methodToObject.Body.GetILProcessor();
            toObjectBuilder.Emit(OpCodes.Ldarg_0);
            toObjectBuilder.Emit(OpCodes.Call, methodFromMonoString);
            toObjectBuilder.Emit(OpCodes.Ret);
            
            var methodToMonoString = new MethodDefinition("op_Implicit", MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, assemblyContext.Imports.String);
            methodToMonoString.Parameters.Add(new ParameterDefinition(typeContext.NewType));
            typeContext.NewType.Methods.Add(methodToMonoString);
            var toBuilder = methodToMonoString.Body.GetILProcessor();

            var createStringNop = toBuilder.Create(OpCodes.Nop);
            
            toBuilder.Emit(OpCodes.Ldarg_0);
            toBuilder.Emit(OpCodes.Call, assemblyContext.Imports.Il2CppObjectBaseToPointer);
            toBuilder.Emit(OpCodes.Dup);
            toBuilder.Emit(OpCodes.Brtrue_S, createStringNop);
            toBuilder.Emit(OpCodes.Pop);
            toBuilder.Emit(OpCodes.Ldnull);
            toBuilder.Emit(OpCodes.Ret);
            
            toBuilder.Append(createStringNop);
            toBuilder.Emit(OpCodes.Call, assemblyContext.Imports.StringFromNative);
            toBuilder.Emit(OpCodes.Ret);

            AddDelegateConversions(context);
        }

        private static void AddDelegateConversions(RewriteGlobalContext context)
        {
            foreach (var assemblyContext in context.Assemblies)
            {
                foreach (var typeContext in assemblyContext.Types)
                {
                    if (typeContext.OriginalType.BaseType?.FullName != "System.MulticastDelegate") continue;

                    var invokeMethod = typeContext.NewType.Methods.Single(it => it.Name == "Invoke");
                    if (invokeMethod.Parameters.Count > 8) continue; // mscorlib only contains delegates of up to 8 parameters

                    // Don't generate implicit conversions for pointers and byrefs, as they can't be specified in generics
                    if (invokeMethod.Parameters.Any(it => it.ParameterType.IsByReference || it.ParameterType.IsPointer))
                        continue;
                    
                    var implicitMethod = new MethodDefinition("op_Implicit", MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeContext.SelfSubstitutedRef);
                    typeContext.NewType.Methods.Add(implicitMethod);

                    var hasReturn = invokeMethod.ReturnType.FullName != "System.Void";
                    var hasParameters = invokeMethod.Parameters.Count > 0;

                    TypeReference monoDelegateType;
                    if (!hasReturn && !hasParameters)
                        monoDelegateType =
                            typeContext.NewType.Module.ImportReference(
                                assemblyContext.Imports.Type.Module.GetType("System.Action"));
                    else if (!hasReturn)
                    {
                        monoDelegateType =
                            typeContext.NewType.Module.ImportReference(
                                assemblyContext.Imports.Type.Module.GetType(
                                    "System.Action`" + invokeMethod.Parameters.Count));
                    } else 
                        monoDelegateType = 
                            typeContext.NewType.Module.ImportReference(
                                assemblyContext.Imports.Type.Module.GetType(
                                    "System.Func`" + (invokeMethod.Parameters.Count + 1)));

                    GenericInstanceType? genericInstanceType = null;
                    if (hasParameters)
                    {
                        genericInstanceType = new GenericInstanceType(monoDelegateType);
                        for (var i = 0; i < invokeMethod.Parameters.Count; i++)
                            genericInstanceType.GenericArguments.Add(invokeMethod.Parameters[i].ParameterType);
                    }

                    if (hasReturn)
                    {
                        genericInstanceType ??= new GenericInstanceType(monoDelegateType);
                        genericInstanceType.GenericArguments.Add(invokeMethod.ReturnType);
                    }

                    implicitMethod.Parameters.Add(new ParameterDefinition(genericInstanceType != null
                        ? typeContext.NewType.Module.ImportReference(genericInstanceType)
                        : monoDelegateType));

                    var bodyBuilder = implicitMethod.Body.GetILProcessor();
                    
                    bodyBuilder.Emit(OpCodes.Ldarg_0);
                    var delegateSupportTypeRef = typeContext.NewType.Module.ImportReference(typeof(DelegateSupport));
                    var genericConvertRef = new MethodReference(nameof(DelegateSupport.ConvertDelegate), assemblyContext.Imports.Void, delegateSupportTypeRef) { HasThis = false, Parameters = { new ParameterDefinition(assemblyContext.Imports.Delegate)}};
                    genericConvertRef.GenericParameters.Add(new GenericParameter(genericConvertRef));
                    genericConvertRef.ReturnType = genericConvertRef.GenericParameters[0];
                    var convertMethodRef = new GenericInstanceMethod(genericConvertRef) { GenericArguments = { typeContext.SelfSubstitutedRef }};
                    bodyBuilder.Emit(OpCodes.Call, typeContext.NewType.Module.ImportReference(convertMethodRef));
                    bodyBuilder.Emit(OpCodes.Ret);

                    // public static T operator+(T lhs, T rhs) => Il2CppSystem.Delegate.Combine(lhs, rhs).Cast<T>();
                    var addMethod = new MethodDefinition("op_Addition", MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeContext.SelfSubstitutedRef);
                    typeContext.NewType.Methods.Add(addMethod);
                    addMethod.Parameters.Add(new ParameterDefinition(typeContext.SelfSubstitutedRef));
                    addMethod.Parameters.Add(new ParameterDefinition(typeContext.SelfSubstitutedRef));
                    var addBody = addMethod.Body.GetILProcessor();
                    addBody.Emit(OpCodes.Ldarg_0);
                    addBody.Emit(OpCodes.Ldarg_1);
                    addBody.Emit(OpCodes.Call, assemblyContext.Imports.DelegateCombine);
                    addBody.Emit(OpCodes.Call, assemblyContext.Imports.Module.ImportReference(new GenericInstanceMethod(assemblyContext.Imports.Il2CppObjectCast) { GenericArguments = { typeContext.SelfSubstitutedRef }}));
                    addBody.Emit(OpCodes.Ret);

                    // public static T operator-(T lhs, T rhs) => Il2CppSystem.Delegate.Remove(lhs, rhs)?.Cast<T>();
                    var subtractMethod = new MethodDefinition("op_Subtraction", MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeContext.SelfSubstitutedRef);
                    typeContext.NewType.Methods.Add(subtractMethod);
                    subtractMethod.Parameters.Add(new ParameterDefinition(typeContext.SelfSubstitutedRef));
                    subtractMethod.Parameters.Add(new ParameterDefinition(typeContext.SelfSubstitutedRef));
                    var subtractBody = subtractMethod.Body.GetILProcessor();
                    subtractBody.Emit(OpCodes.Ldarg_0);
                    subtractBody.Emit(OpCodes.Ldarg_1);
                    subtractBody.Emit(OpCodes.Call, assemblyContext.Imports.DelegateRemove);
                    subtractBody.Emit(OpCodes.Dup);
                    var ret = subtractBody.Create(OpCodes.Ret);
                    subtractBody.Emit(OpCodes.Brfalse_S, ret);
                    subtractBody.Emit(OpCodes.Call, assemblyContext.Imports.Module.ImportReference(new GenericInstanceMethod(assemblyContext.Imports.Il2CppObjectCast) { GenericArguments = { typeContext.SelfSubstitutedRef }}));
                    subtractBody.Append(ret);
                }
            }
        }
    }
}