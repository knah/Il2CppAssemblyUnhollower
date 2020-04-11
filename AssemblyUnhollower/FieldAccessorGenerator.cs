using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AssemblyUnhollower
{
    public static class FieldAccessorGenerator
    {
        public static MethodDefinition MakeGetter(FieldDefinition field, FieldReference fieldInfoPointer, PropertyDefinition property)
        {
            var imports = AssemblyKnownImports.For(property);

            var getter = new MethodDefinition("get_" + property.Name, Field2MethodAttrs(field.Attributes) | MethodAttributes.SpecialName | MethodAttributes.HideBySig, property.PropertyType);
            var propertyTypeIsValueType = field.FieldType.IsValueType || field.FieldType.IsPrimitive;
            var local0 = new VariableDefinition(propertyTypeIsValueType ? property.PropertyType : imports.IntPtr);
            getter.Body.Variables.Add(local0);
            var getterBody = getter.Body.GetILProcessor();

            if (field.IsStatic)
            {
                getterBody.Emit(OpCodes.Ldsfld, fieldInfoPointer);
                getterBody.Emit(OpCodes.Ldloca_S, local0);
                getterBody.Emit(OpCodes.Conv_U);
                getterBody.Emit(OpCodes.Call, imports.FieldStaticGet);
            }
            else
            {
                getterBody.Emit(OpCodes.Ldarg_0);
                getterBody.Emit(OpCodes.Call, imports.Il2CppObjectBaseToPointerNotNull);
                getterBody.Emit(OpCodes.Ldsfld, fieldInfoPointer);
                getterBody.Emit(OpCodes.Call, imports.FieldGetOffset);
                getterBody.Emit(OpCodes.Add);
                getterBody.Emit(OpCodes.Ldobj, local0.VariableType);
                getterBody.Emit(OpCodes.Stloc_0);
            }
            
            getterBody.Emit(OpCodes.Ldloc_0);

            if (field.FieldType.FullName == "System.String")
            {
                getterBody.Emit(OpCodes.Call, imports.StringFromNative);
            } else if (!propertyTypeIsValueType)
            {
                var createRealObject = getterBody.Create(OpCodes.Newobj,
                    new MethodReference(".ctor", imports.Void, property.PropertyType)
                        {Parameters = {new ParameterDefinition(imports.IntPtr)}, HasThis = true});
                
                getterBody.Emit(OpCodes.Dup);
                getterBody.Emit(OpCodes.Brtrue_S, createRealObject);
                getterBody.Emit(OpCodes.Pop);
                getterBody.Emit(OpCodes.Ldnull);
                getterBody.Emit(OpCodes.Ret);
                
                getterBody.Append(createRealObject);
            }

            getterBody.Emit(OpCodes.Ret);

            property.GetMethod = getter;

            return getter;
        }
        
        public static MethodDefinition MakeSetter(FieldDefinition field, FieldReference fieldDefinition, PropertyDefinition property)
        {
            var imports = AssemblyKnownImports.For(property);

            var propertyTypeIsValueType = field.FieldType.IsValueType || field.FieldType.IsPrimitive;
            var actualFieldType = propertyTypeIsValueType ? property.PropertyType : imports.IntPtr;

            var setter = new MethodDefinition("set_" + property.Name, Field2MethodAttrs(field.Attributes) | MethodAttributes.SpecialName | MethodAttributes.HideBySig, imports.Void);
            setter.Parameters.Add(new ParameterDefinition(property.PropertyType));
            var setterBody = setter.Body.GetILProcessor();

            
            if (field.IsStatic)
            {
                setterBody.Emit(OpCodes.Ldsfld, fieldDefinition);

                if (field.FieldType.FullName == "System.String")
                {
                    setterBody.Emit(OpCodes.Ldarg_0);
                    setterBody.Emit(OpCodes.Call, imports.StringToNative);
                } else if (!propertyTypeIsValueType)
                {
                    setterBody.Emit(OpCodes.Ldarg_0);
                    setterBody.Emit(OpCodes.Call, imports.Il2CppObjectBaseToPointer);
                }
                else
                {
                    setterBody.Emit(OpCodes.Ldarga_S, (byte) 0);
                    setterBody.Emit(OpCodes.Conv_U);
                }

                setterBody.Emit(OpCodes.Call, imports.FieldStaticSet);
            }
            else
            {
                setterBody.Emit(OpCodes.Ldarg_0);
                setterBody.Emit(OpCodes.Call, imports.Il2CppObjectBaseToPointerNotNull);
                setterBody.Emit(OpCodes.Ldsfld, fieldDefinition);
                setterBody.Emit(OpCodes.Call, imports.FieldGetOffset);
                setterBody.Emit(OpCodes.Add);
                
                if (field.FieldType.FullName == "System.String")
                {
                    setterBody.Emit(OpCodes.Ldarg_1);
                    setterBody.Emit(OpCodes.Call, imports.StringToNative);
                } else if (!propertyTypeIsValueType)
                {
                    setterBody.Emit(OpCodes.Ldarg_1);
                    setterBody.Emit(OpCodes.Call, imports.Il2CppObjectBaseToPointer);
                }
                else
                {
                    setterBody.Emit(OpCodes.Ldarg_1);
                }

                setterBody.Emit(OpCodes.Stobj, actualFieldType);
            }
            
            setterBody.Emit(OpCodes.Ret);

            property.SetMethod = setter;

            return setter;
        }
        
        private static MethodAttributes Field2MethodAttrs(FieldAttributes fieldAttributes)
        {
            if ((fieldAttributes & FieldAttributes.Static) != 0)
                return MethodAttributes.Public | MethodAttributes.Static;
            return MethodAttributes.Public;
        }
    }
}