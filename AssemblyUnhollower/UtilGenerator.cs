using System;
using System.Linq;
using AssemblyUnhollower.Contexts;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnhollowerBaseLib;

namespace AssemblyUnhollower
{
    public static class UtilGenerator
    {
        private static readonly OpCode[] I4Constants = {
            OpCodes.Ldc_I4_M1,
            OpCodes.Ldc_I4_0,
            OpCodes.Ldc_I4_1,
            OpCodes.Ldc_I4_2,
            OpCodes.Ldc_I4_3,
            OpCodes.Ldc_I4_4,
            OpCodes.Ldc_I4_5,
            OpCodes.Ldc_I4_6,
            OpCodes.Ldc_I4_7,
            OpCodes.Ldc_I4_8,
        };

        public static void EmitLdcI4(this ILProcessor body, int constant)
        {
            if(constant >= -1 && constant <= 8)
                body.Emit(I4Constants[constant + 1]);
            else if(constant >= byte.MinValue && constant <= byte.MaxValue)
                body.Emit(OpCodes.Ldc_I4_S, (sbyte) constant);
            else
                body.Emit(OpCodes.Ldc_I4, constant);
        }

        public static void EmitObjectStore(this ILProcessor body, TypeReference originalType, TypeReference newType, TypeRewriteContext enclosingType, int argumentIndex)
        {
            // input stack: target address
            // output: nothing
            if (originalType is GenericParameter)
            {
                EmitObjectStoreGeneric(body, originalType, newType, enclosingType, argumentIndex);
                return;
            }
            
            var imports = enclosingType.AssemblyContext.Imports;
            
            if (originalType.FullName == "System.String")
            {
                body.Emit(OpCodes.Ldarg, argumentIndex);
                body.Emit(OpCodes.Call, imports.StringToNative);
                body.Emit(OpCodes.Stobj, imports.IntPtr);
            } else if (originalType.IsValueType)
            {
                var typeSpecifics =  enclosingType.AssemblyContext.GlobalContext.JudgeSpecificsByOriginalType(originalType);
                if (typeSpecifics == TypeRewriteContext.TypeSpecifics.BlittableStruct)
                {
                    body.Emit(OpCodes.Ldarg, argumentIndex);
                    body.Emit(OpCodes.Stobj, newType);
                }
                else
                {
                    body.Emit(OpCodes.Ldarg, argumentIndex);
                    body.Emit(OpCodes.Call, imports.Il2CppObjectBaseToPointer);
                    body.Emit(OpCodes.Call, imports.ObjectUnbox);
                    var classPointerTypeRef = new GenericInstanceType(imports.Il2CppClassPointerStore) { GenericArguments = { newType }};
                    var classPointerFieldRef = new FieldReference(nameof(Il2CppClassPointerStore<int>.NativeClassPtr), imports.IntPtr, classPointerTypeRef);
                    body.Emit(OpCodes.Ldsfld, enclosingType.NewType.Module.ImportReference(classPointerFieldRef));
                    body.Emit(OpCodes.Ldc_I4_0);
                    body.Emit(OpCodes.Call, imports.ValueSizeGet);
                    body.Emit(OpCodes.Cpblk);
                }
            } else {
                body.Emit(OpCodes.Ldarg, argumentIndex);
                body.Emit(OpCodes.Call, imports.Il2CppObjectBaseToPointer);
                body.Emit(OpCodes.Stobj, imports.IntPtr);
            }
        }

        private static void EmitObjectStoreGeneric(ILProcessor body, TypeReference originalType, TypeReference newType, TypeRewriteContext enclosingType, int argumentIndex)
        {
            // input stack: target address
            // output: nothing
            
            var imports = enclosingType.AssemblyContext.Imports;
            
            body.Emit(OpCodes.Ldtoken, newType);
            body.Emit(OpCodes.Call, enclosingType.NewType.Module.ImportReference(imports.Type.Methods.Single(it => it.Name == nameof(Type.GetTypeFromHandle))));
            body.Emit(OpCodes.Dup);
            body.Emit(OpCodes.Callvirt, enclosingType.NewType.Module.ImportReference(imports.Type.Methods.Single(it => it.Name == typeof(Type).GetProperty(nameof(Type.IsValueType))!.GetMethod!.Name)));

            var finalNop = body.Create(OpCodes.Nop);
            var stringNop = body.Create(OpCodes.Nop);
            var valueTypeNop = body.Create(OpCodes.Nop); // todo: non-blittable structs

            body.Emit(OpCodes.Brtrue, valueTypeNop);
            
            body.Emit(OpCodes.Callvirt, enclosingType.NewType.Module.ImportReference(imports.Type.Methods.Single(it => it.Name == typeof(Type).GetProperty(nameof(Type.FullName))!.GetMethod!.Name)));
            body.Emit(OpCodes.Ldstr, "System.String");
            body.Emit(OpCodes.Call, enclosingType.NewType.Module.ImportReference(TargetTypeSystemHandler.String.Methods.Single(it => it.Name == nameof(String.Equals) && it.IsStatic && it.Parameters.Count == 2)));
            body.Emit(OpCodes.Brtrue_S, stringNop);
            
            body.Emit(OpCodes.Ldarg, argumentIndex);
            body.Emit(OpCodes.Box, newType);
            body.Emit(OpCodes.Isinst, imports.Il2CppObjectBase);
            body.Emit(OpCodes.Call, imports.Il2CppObjectBaseToPointer);
            body.Emit(OpCodes.Stind_I);
            body.Emit(OpCodes.Br_S, finalNop);
            
            body.Append(stringNop);
            body.Emit(OpCodes.Ldarg, argumentIndex);
            body.Emit(OpCodes.Box, newType);
            body.Emit(OpCodes.Isinst, imports.String);
            body.Emit(OpCodes.Call, imports.StringToNative);
            body.Emit(OpCodes.Stind_I);
            body.Emit(OpCodes.Br_S, finalNop);
            
            body.Append(valueTypeNop);
            body.Emit(OpCodes.Pop); // pop extra typeof(T)
            body.Emit(OpCodes.Ldarg, argumentIndex);
            body.Emit(OpCodes.Stobj, newType);
            
            body.Append(finalNop);
        }

        public static void EmitObjectToPointer(this ILProcessor body, TypeReference originalType, TypeReference newType, TypeRewriteContext enclosingType, int argumentIndex, bool valueTypeArgument0IsAPointer, bool allowNullable, bool unboxNonBlittableType)
        {
            // input stack: not used
            // output stack: IntPtr to either Il2CppObject or IL2CPP value type

            if (originalType is GenericParameter)
            {
                EmitObjectToPointerGeneric(body, originalType, newType, enclosingType, argumentIndex, valueTypeArgument0IsAPointer, allowNullable, unboxNonBlittableType);
                return;
            }

            var imports = enclosingType.AssemblyContext.Imports;
            if (originalType.IsValueType)
            {
                var typeSpecifics =  enclosingType.AssemblyContext.GlobalContext.JudgeSpecificsByOriginalType(originalType);
                if (typeSpecifics == TypeRewriteContext.TypeSpecifics.BlittableStruct)
                {
                    if (argumentIndex == 0 && valueTypeArgument0IsAPointer)
                        body.Emit(OpCodes.Ldarg_0);
                    else
                        body.Emit(OpCodes.Ldarga, argumentIndex);
                }
                else
                {
                    body.Emit(OpCodes.Ldarg, argumentIndex);
                    body.Emit(OpCodes.Call, imports.Il2CppObjectBaseToPointerNotNull);
                    if (unboxNonBlittableType)
                        body.Emit(OpCodes.Call, imports.ObjectUnbox);
                }
            }
            else if (originalType.FullName == "System.String")
            {
                body.Emit(OpCodes.Ldarg, argumentIndex);
                body.Emit(OpCodes.Call, imports.StringToNative);
            } 
            else 
            {
                body.Emit(OpCodes.Ldarg, argumentIndex);
                body.Emit(OpCodes.Call, allowNullable ? imports.Il2CppObjectBaseToPointer : imports.Il2CppObjectBaseToPointerNotNull);
            }
        }

        private static void EmitObjectToPointerGeneric(ILProcessor body, TypeReference originalType,
            TypeReference newType, TypeRewriteContext enclosingType, int argumentIndex,
            bool valueTypeArgument0IsAPointer, bool allowNullable, bool unboxNonBlittableType)
        {
            var imports = enclosingType.AssemblyContext.Imports;
            
            body.Emit(OpCodes.Ldtoken, newType);
            body.Emit(OpCodes.Call, enclosingType.NewType.Module.ImportReference(imports.Type.Methods.Single(it => it.Name == nameof(Type.GetTypeFromHandle))));
            body.Emit(OpCodes.Callvirt, enclosingType.NewType.Module.ImportReference(imports.Type.Methods.Single(it => it.Name == typeof(Type).GetProperty(nameof(Type.IsValueType))!.GetMethod!.Name)));

            var finalNop = body.Create(OpCodes.Nop);
            var valueTypeNop = body.Create(OpCodes.Nop);
            var stringNop = body.Create(OpCodes.Nop);
            
            body.Emit(OpCodes.Brtrue, valueTypeNop);

            body.Emit(OpCodes.Ldarg, argumentIndex);
            body.Emit(OpCodes.Box, newType);
            body.Emit(OpCodes.Dup);
            body.Emit(OpCodes.Isinst, imports.String);
            body.Emit(OpCodes.Brtrue_S, stringNop);

            body.Emit(OpCodes.Isinst, imports.Il2CppObjectBase);
            body.Emit(OpCodes.Call, allowNullable ? imports.Il2CppObjectBaseToPointer : imports.Il2CppObjectBaseToPointerNotNull);
            if (unboxNonBlittableType)
            {
                body.Emit(OpCodes.Dup);
                body.Emit(OpCodes.Brfalse_S, finalNop); // return null immediately
                body.Emit(OpCodes.Dup);
                body.Emit(OpCodes.Call, imports.ObjectGetClass);
                body.Emit(OpCodes.Call, imports.ClassIsValueType);
                body.Emit(OpCodes.Brfalse_S, finalNop); // return reference types immediately
                body.Emit(OpCodes.Call, imports.ObjectUnbox);
            }

            body.Emit(OpCodes.Br, finalNop);
            
            body.Append(stringNop);
            body.Emit(OpCodes.Isinst, imports.String);
            body.Emit(OpCodes.Call, imports.StringToNative);
            body.Emit(OpCodes.Br_S, finalNop);
            
            body.Append(valueTypeNop);
            body.Emit(OpCodes.Ldarga, argumentIndex);
            
            body.Append(finalNop);
        }

        public static void EmitPointerToObject(this ILProcessor body, TypeReference originalReturnType, TypeReference convertedReturnType, TypeRewriteContext enclosingType, Instruction loadPointer, bool extraDerefForNonValueTypes, bool unboxValueType)
        {
            // input stack: not used
            // output stack: converted result
            
            if (originalReturnType is GenericParameter)
            {
                EmitPointerToObjectGeneric(body, originalReturnType, convertedReturnType, enclosingType, loadPointer, extraDerefForNonValueTypes, unboxValueType);
                return;
            }

            var imports = enclosingType.AssemblyContext.Imports;
            if (originalReturnType.FullName == "System.Void")
            {
                // do nothing
            }
            else if (originalReturnType.IsValueType)
            {
                var typeSpecifics =  enclosingType.AssemblyContext.GlobalContext.JudgeSpecificsByOriginalType(originalReturnType);
                if (typeSpecifics == TypeRewriteContext.TypeSpecifics.BlittableStruct)
                {
                    body.Append(loadPointer);
                    if (unboxValueType) body.Emit(OpCodes.Call, imports.ObjectUnbox);
                    body.Emit(OpCodes.Ldobj, convertedReturnType);
                }
                else
                {
                    var classPointerTypeRef = new GenericInstanceType(imports.Il2CppClassPointerStore) { GenericArguments = { convertedReturnType }};
                    var classPointerFieldRef = new FieldReference(nameof(Il2CppClassPointerStore<int>.NativeClassPtr), imports.IntPtr, classPointerTypeRef);
                    body.Emit(OpCodes.Ldsfld, enclosingType.NewType.Module.ImportReference(classPointerFieldRef));
                    body.Append(loadPointer);
                    body.Emit(OpCodes.Call, imports.ObjectBox);
                    body.Emit(OpCodes.Newobj,
                        new MethodReference(".ctor", imports.Void, convertedReturnType)
                            {Parameters = {new ParameterDefinition(imports.IntPtr)}, HasThis = true});
                }
            } else if (originalReturnType.FullName == "System.String")
            {
                body.Append(loadPointer);
                if (extraDerefForNonValueTypes) body.Emit(OpCodes.Ldind_I);
                body.Emit(OpCodes.Call, imports.StringFromNative);
            }
            else
            {
                var createRealObject = body.Create(OpCodes.Newobj,
                    new MethodReference(".ctor", imports.Void, convertedReturnType)
                        {Parameters = {new ParameterDefinition(imports.IntPtr)}, HasThis = true});
                var endNop = body.Create(OpCodes.Nop);
                
                body.Append(loadPointer);
                if (extraDerefForNonValueTypes) body.Emit(OpCodes.Ldind_I);
                body.Emit(OpCodes.Dup);
                body.Emit(OpCodes.Brtrue_S, createRealObject);
                body.Emit(OpCodes.Pop);
                body.Emit(OpCodes.Ldnull);
                body.Emit(OpCodes.Br, endNop);
                
                body.Append(createRealObject);
                body.Append(endNop);
            }
        }

        private static void EmitPointerToObjectGeneric(ILProcessor body, TypeReference originalReturnType,
            TypeReference newReturnType,
            TypeRewriteContext enclosingType, Instruction loadPointer, bool extraDerefForNonValueTypes,
            bool unboxValueType)
        {
            var imports = enclosingType.AssemblyContext.Imports;
            
            body.Append(loadPointer);
            
            body.Emit(OpCodes.Ldtoken, newReturnType);
            body.Emit(OpCodes.Call, enclosingType.NewType.Module.ImportReference(imports.Type.Methods.Single(it => it.Name == nameof(Type.GetTypeFromHandle))));
            body.Emit(OpCodes.Dup);
            body.Emit(OpCodes.Callvirt, enclosingType.NewType.Module.ImportReference(imports.Type.Methods.Single(it => it.Name == typeof(Type).GetProperty(nameof(Type.IsValueType))!.GetMethod!.Name)));

            var finalNop = body.Create(OpCodes.Nop);
            var valueTypeNop = body.Create(OpCodes.Nop);
            var stringNop = body.Create(OpCodes.Nop);
            
            body.Emit(OpCodes.Brtrue, valueTypeNop); // todo: non-blittable value types
            
            body.Emit(OpCodes.Callvirt, enclosingType.NewType.Module.ImportReference(imports.Type.Methods.Single(it => it.Name == typeof(Type).GetProperty(nameof(Type.FullName))!.GetMethod!.Name)));
            body.Emit(OpCodes.Ldstr, "System.String");
            body.Emit(OpCodes.Call, enclosingType.NewType.Module.ImportReference(TargetTypeSystemHandler.String.Methods.Single(it => it.Name == nameof(String.Equals) && it.IsStatic && it.Parameters.Count == 2)));
            body.Emit(OpCodes.Brtrue_S, stringNop);
            
            var createRealObject = body.Create(OpCodes.Newobj,
                new MethodReference(".ctor", imports.Void, imports.Il2CppObjectBase)
                    {Parameters = {new ParameterDefinition(imports.IntPtr)}, HasThis = true});

            body.Emit(OpCodes.Dup);
            body.Emit(OpCodes.Brtrue_S, createRealObject);
            body.Emit(OpCodes.Pop);
            body.Emit(OpCodes.Ldnull);
            body.Emit(OpCodes.Br, finalNop);

            body.Append(createRealObject);
            body.Emit(OpCodes.Call, enclosingType.NewType.Module.ImportReference(new GenericInstanceMethod(imports.Il2CppObjectCast) { GenericArguments = { newReturnType }}));
            body.Emit(OpCodes.Br, finalNop);
            
            body.Append(stringNop);
            body.Emit(OpCodes.Call, imports.StringFromNative);
            body.Emit(OpCodes.Isinst, newReturnType); // satisfy the verifier
            body.Emit(OpCodes.Br_S, finalNop);
            
            body.Append(valueTypeNop);
            body.Emit(OpCodes.Pop); // pop extra typeof(T)
            if(unboxValueType) body.Emit(OpCodes.Call, imports.ObjectUnbox);
            body.Emit(OpCodes.Ldobj, newReturnType);
            
            body.Append(finalNop);
        }

        public static void GenerateBoxMethod(TypeDefinition targetType, FieldReference classHandle, TypeReference il2CppObjectTypeDef)
        {
            var method = new MethodDefinition("BoxIl2CppObject", MethodAttributes.Public | MethodAttributes.HideBySig, targetType.Module.ImportReference(il2CppObjectTypeDef));
            targetType.Methods.Add(method);

            var methodBody = method.Body.GetILProcessor();
            methodBody.Emit(OpCodes.Ldsfld, classHandle);
            methodBody.Emit(OpCodes.Ldarg_0);
            methodBody.Emit(OpCodes.Call, targetType.Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_value_box")));

            methodBody.Emit(OpCodes.Newobj, new MethodReference(".ctor", targetType.Module.ImportReference(TargetTypeSystemHandler.Void), il2CppObjectTypeDef) { Parameters = { new ParameterDefinition(targetType.Module.ImportReference(TargetTypeSystemHandler.IntPtr))}, HasThis = true});
            
            methodBody.Emit(OpCodes.Ret);
        }
    }
}