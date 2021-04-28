using System.Diagnostics;
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

            // todo: for non-generic fields, call the appropriate method directly
            if (field.IsStatic)
            {
                getterBody.Emit(OpCodes.Ldsfld, fieldContext.PointerField);
                getterBody.Emit(OpCodes.Call, imports.Module.ImportReference(new GenericInstanceMethod(imports.ReadStaticFieldGeneric) { GenericArguments = { property.PropertyType } }));
            }
            else
            {
                Debug.Assert(!fieldContext.DeclaringType.NewType.IsValueType);
                
                getterBody.Emit(OpCodes.Ldarg_0);
                getterBody.Emit(OpCodes.Call, imports.Il2CppObjectBaseToPointerNotNull);
                
                getterBody.Emit(OpCodes.Ldsfld, fieldContext.PointerField);
                getterBody.Emit(OpCodes.Call, imports.FieldGetOffset);
                getterBody.Emit(OpCodes.Add);
                getterBody.Emit(OpCodes.Call, imports.Module.ImportReference(new GenericInstanceMethod(imports.ReadFieldGeneric) { GenericArguments = { property.PropertyType } }));
            }

            getterBody.Emit(OpCodes.Ret);

            property.GetMethod = getter;
        }
        
        public static void MakeSetter(FieldDefinition field, FieldRewriteContext fieldContext, PropertyDefinition property, AssemblyKnownImports imports)
        {
            var setter = new MethodDefinition("set_" + property.Name, Field2MethodAttrs(field.Attributes) | MethodAttributes.SpecialName | MethodAttributes.HideBySig, imports.Void);
            setter.Parameters.Add(new ParameterDefinition(property.PropertyType));
            property.DeclaringType.Methods.Add(setter);
            var setterBody = setter.Body.GetILProcessor();

            // todo: for non-generic fields, call the appropriate method directly
            if (field.IsStatic)
            {
                setterBody.Emit(OpCodes.Ldsfld, fieldContext.PointerField);
                setterBody.Emit(OpCodes.Ldarg_0);
                setterBody.Emit(OpCodes.Call, imports.Module.ImportReference(new GenericInstanceMethod(imports.WriteStaticFieldGeneric) { GenericArguments = { property.PropertyType } }));
            }
            else
            {
                Debug.Assert(!fieldContext.DeclaringType.NewType.IsValueType);
                
                setterBody.Emit(OpCodes.Ldarg_0);
                setterBody.Emit(OpCodes.Call, imports.Il2CppObjectBaseToPointerNotNull);
                
                setterBody.Emit(OpCodes.Ldsfld, fieldContext.PointerField);
                setterBody.Emit(OpCodes.Call, imports.FieldGetOffset);
                setterBody.Emit(OpCodes.Add);
                setterBody.Emit(OpCodes.Ldarg_0);
                setterBody.Emit(OpCodes.Call, imports.Module.ImportReference(new GenericInstanceMethod(imports.WriteFieldGeneric) { GenericArguments = { property.PropertyType } }));
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