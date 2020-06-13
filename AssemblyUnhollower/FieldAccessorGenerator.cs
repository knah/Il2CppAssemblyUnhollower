using AssemblyUnhollower.Contexts;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnhollowerBaseLib;

namespace AssemblyUnhollower
{
    public static class FieldAccessorGenerator
    {
        public static void MakeGetter(FieldDefinition field, FieldRewriteContext fieldContext, PropertyDefinition property, AssemblyKnownImports imports)
        {
            var getter = new MethodDefinition("get_" + property.Name, Field2MethodAttrs(field.Attributes) | MethodAttributes.SpecialName | MethodAttributes.HideBySig, property.PropertyType);
            
            var getterBody = getter.Body.GetILProcessor();
            property.DeclaringType.Methods.Add(getter);

            if (field.IsStatic)
            {
                var local0 = new VariableDefinition(property.PropertyType.IsValueType ? property.PropertyType : imports.IntPtr);
                getter.Body.Variables.Add(local0);

                bool localIsPointer = false;
                if (field.FieldType.IsValueType && !property.PropertyType.IsValueType)
                {
                    var pointerStore = new GenericInstanceType(imports.Il2CppClassPointerStore);
                    pointerStore.GenericArguments.Add(property.PropertyType);
                    var pointerStoreType = property.DeclaringType.Module.ImportReference(pointerStore);
                    getterBody.Emit(OpCodes.Ldsfld, new FieldReference(nameof(Il2CppClassPointerStore<int>.NativeClassPtr), imports.IntPtr, pointerStoreType));
                    getterBody.Emit(OpCodes.Ldc_I4, 0);
                    getterBody.Emit(OpCodes.Conv_U);
                    getterBody.Emit(OpCodes.Call, imports.ValueSizeGet);
                    getterBody.Emit(OpCodes.Conv_U);
                    getterBody.Emit(OpCodes.Localloc);
                    getterBody.Emit(OpCodes.Stloc, local0);
                    localIsPointer = true;
                }
                
                getterBody.Emit(OpCodes.Ldsfld, fieldContext.PointerField);
                if (localIsPointer)
                    getterBody.Emit(OpCodes.Ldloc, local0);
                else
                    getterBody.Emit(OpCodes.Ldloca_S, local0);
                getterBody.Emit(OpCodes.Conv_U);
                getterBody.Emit(OpCodes.Call, imports.FieldStaticGet);

                if (property.PropertyType.IsValueType)
                {
                    getterBody.Emit(OpCodes.Ldloc, local0);
                    getterBody.Emit(OpCodes.Ret);

                    property.GetMethod = getter;
                    return;
                }
            }
            else
            {
                var local0 = new VariableDefinition(imports.IntPtr);
                getter.Body.Variables.Add(local0);
                
                getterBody.EmitObjectToPointer(fieldContext.DeclaringType.OriginalType, fieldContext.DeclaringType.NewType, fieldContext.DeclaringType, 0, false, false, false, out _);
                getterBody.Emit(OpCodes.Ldsfld, fieldContext.PointerField);
                getterBody.Emit(OpCodes.Call, imports.FieldGetOffset);
                getterBody.Emit(OpCodes.Add);
                
                getterBody.Emit(OpCodes.Stloc_0);
            }

            getterBody.EmitPointerToObject(fieldContext.OriginalField.FieldType, property.PropertyType, fieldContext.DeclaringType, getterBody.Create(OpCodes.Ldloc_0), !field.IsStatic, false);

            getterBody.Emit(OpCodes.Ret);

            property.GetMethod = getter;
        }
        
        public static void MakeSetter(FieldDefinition field, FieldRewriteContext fieldContext, PropertyDefinition property, AssemblyKnownImports imports)
        {
            var setter = new MethodDefinition("set_" + property.Name, Field2MethodAttrs(field.Attributes) | MethodAttributes.SpecialName | MethodAttributes.HideBySig, imports.Void);
            setter.Parameters.Add(new ParameterDefinition(property.PropertyType));
            property.DeclaringType.Methods.Add(setter);
            var setterBody = setter.Body.GetILProcessor();

            if (field.IsStatic)
            {
                setterBody.Emit(OpCodes.Ldsfld, fieldContext.PointerField);
                setterBody.EmitObjectToPointer(field.FieldType, property.PropertyType, fieldContext.DeclaringType, 0, false, true, true, out _);
                setterBody.Emit(OpCodes.Call, imports.FieldStaticSet);
            }
            else
            {
                setterBody.EmitObjectToPointer(fieldContext.DeclaringType.OriginalType, fieldContext.DeclaringType.NewType, fieldContext.DeclaringType, 0, false, false, false, out _);
                setterBody.Emit(OpCodes.Ldsfld, fieldContext.PointerField);
                setterBody.Emit(OpCodes.Call, imports.FieldGetOffset);
                setterBody.Emit(OpCodes.Add);
                setterBody.EmitObjectStore(field.FieldType, property.PropertyType, fieldContext.DeclaringType, 1);
            }
            
            setterBody.Emit(OpCodes.Ret);

            property.SetMethod = setter;
        }
        
        private static MethodAttributes Field2MethodAttrs(FieldAttributes fieldAttributes)
        {
            if ((fieldAttributes & FieldAttributes.Static) != 0)
                return MethodAttributes.Public | MethodAttributes.Static;
            return MethodAttributes.Public;
        }
    }
}