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

        private static readonly string[] MethodAccessTypeLabels = { "CompilerControlled", "Private", "FamAndAssem", "Internal", "Protected", "FamOrAssem", "Public"};
        private string UnmangleFieldNameBase(FieldDefinition field)
        {
            if (!field.Name.IsObfuscated()) return field.Name;

            var accessModString = MethodAccessTypeLabels[(int) (field.Attributes & FieldAttributes.FieldAccessMask)];
            var staticString = field.IsStatic ? "_Static" : "";
            return "field_" + accessModString + staticString + "_" + DeclaringType.AssemblyContext.RewriteTypeRef(field.FieldType).GetUnmangledName();
        }
        
        private string UnmangleFieldName(FieldDefinition field)
        {
            if (!field.Name.IsObfuscated()) return field.Name;

            return UnmangleFieldNameBase(field) + "_" +
                   field.DeclaringType.Fields.Where(it => FieldsHaveSameSignature(field, it)).TakeWhile(it => it != field).Count();
        }

        private static bool FieldsHaveSameSignature(FieldDefinition fieldA, FieldDefinition fieldB)
        {
            if ((fieldA.Attributes & FieldAttributes.FieldAccessMask) !=
                (fieldB.Attributes & FieldAttributes.FieldAccessMask))
                return false;

            if (fieldA.IsStatic != fieldB.IsStatic) return false;

            return fieldA.FieldType.FullName == fieldB.FieldType.FullName;
        }
    }
}