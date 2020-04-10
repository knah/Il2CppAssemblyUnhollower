using System.Linq;
using Mono.Cecil;

namespace AssemblyUnhollower.Contexts
{
    public class FieldRewriteContext
    {
        public readonly TypeRewriteContext DeclaringType;
        public readonly FieldDefinition OriginalField;
        public readonly string UnmangledName;

        public readonly FieldReference PointerField;
        public readonly FieldReference OffsetField;

        public FieldRewriteContext(TypeRewriteContext declaringType, FieldDefinition originalField)
        {
            DeclaringType = declaringType;
            OriginalField = originalField;

            UnmangledName = UnmangleFieldName(originalField);
            var pointerField = new FieldDefinition("NativeFieldInfoPtr_" + UnmangledName, FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly, declaringType.AssemblyContext.Imports.IntPtr);
            var offsetField = new FieldDefinition("NativeFieldOffset_" + UnmangledName, FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly, declaringType.AssemblyContext.Imports.IntPtr);
            
            declaringType.NewType.Fields.Add(pointerField);
            declaringType.NewType.Fields.Add(offsetField);
            
            PointerField = new FieldReference(pointerField.Name, pointerField.FieldType, DeclaringType.SelfSubstitutedRef);
            OffsetField = new FieldReference(offsetField.Name, offsetField.FieldType, DeclaringType.SelfSubstitutedRef);
        }

        private string UnmangleFieldName(FieldDefinition field)
        {
            if (!field.Name.IsObfuscated()) return field.Name;

            return "field_" +
                   DeclaringType.AssemblyContext.RewriteTypeRef(field.FieldType).GetUnmangledName() + "_" +
                   field.DeclaringType.Fields.Where(it => it.FieldType.GetUnmangledName() == field.FieldType.GetUnmangledName()).ToList()
                       .IndexOf(field);
        }
    }
}