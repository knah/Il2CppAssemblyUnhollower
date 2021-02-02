using System;
using System.Collections.Generic;
using AssemblyUnhollower.Extensions;
using Mono.Cecil;

namespace AssemblyUnhollower.Contexts
{
    public class FieldRewriteContext
    {
        public readonly TypeRewriteContext DeclaringType;
        public readonly FieldDefinition OriginalField;
        public readonly string UnmangledName;

        public readonly FieldReference PointerField;

        public FieldRewriteContext(TypeRewriteContext declaringType, FieldDefinition originalField, Dictionary<string, int>? renamedFieldCounts = null)
        {
            DeclaringType = declaringType;
            OriginalField = originalField;

            UnmangledName = UnmangleFieldName(originalField, declaringType.AssemblyContext.GlobalContext.Options, renamedFieldCounts);
            var pointerField = new FieldDefinition("NativeFieldInfoPtr_" + UnmangledName, FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly, declaringType.AssemblyContext.Imports.IntPtr);
            
            declaringType.NewType.Fields.Add(pointerField);
            
            PointerField = new FieldReference(pointerField.Name, pointerField.FieldType, DeclaringType.SelfSubstitutedRef);
        }

        private static readonly string[] MethodAccessTypeLabels = { "CompilerControlled", "Private", "FamAndAssem", "Internal", "Protected", "FamOrAssem", "Public"};
        private string UnmangleFieldNameBase(FieldDefinition field, UnhollowerOptions options)
        {
            if (options.PassthroughNames) return field.Name;
            
            if (!field.Name.IsObfuscated(options))
            {
                if(!field.Name.IsInvalidInSource())
                    return field.Name;
                return field.Name.FilterInvalidInSourceChars();
            }

            var accessModString = MethodAccessTypeLabels[(int) (field.Attributes & FieldAttributes.FieldAccessMask)];
            var staticString = field.IsStatic ? "_Static" : "";
            return "field_" + accessModString + staticString + "_" + DeclaringType.AssemblyContext.RewriteTypeRef(field.FieldType).GetUnmangledName();
        }
        
        private string UnmangleFieldName(FieldDefinition field, UnhollowerOptions options, Dictionary<string, int>? renamedFieldCounts)
        {
            if (options.PassthroughNames) return field.Name;
            
            if (!field.Name.IsObfuscated(options))
            {
                if(!field.Name.IsInvalidInSource())
                    return field.Name;
                return field.Name.FilterInvalidInSourceChars();
            }

            if (renamedFieldCounts == null) throw new ArgumentNullException(nameof(renamedFieldCounts));

            var unmangleFieldNameBase = UnmangleFieldNameBase(field, options);

            renamedFieldCounts.TryGetValue(unmangleFieldNameBase, out var count);
            renamedFieldCounts[unmangleFieldNameBase] = count + 1;

            unmangleFieldNameBase += "_" + count;
            
            if (DeclaringType.AssemblyContext.GlobalContext.Options.RenameMap.TryGetValue(
                DeclaringType.NewType.GetNamespacePrefix() + "::" + unmangleFieldNameBase, out var newName))
                unmangleFieldNameBase = newName;
            
            return unmangleFieldNameBase;
        }
    }
}