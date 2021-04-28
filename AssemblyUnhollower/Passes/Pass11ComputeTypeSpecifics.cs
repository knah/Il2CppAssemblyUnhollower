using System;
using AssemblyUnhollower.Contexts;
using AssemblyUnhollower.Extensions;

namespace AssemblyUnhollower.Passes
{
    public static class Pass11ComputeTypeSpecifics
    {
        public static void DoPass(RewriteGlobalContext context)
        {
            foreach (var assemblyContext in context.Assemblies)
            foreach (var typeContext in assemblyContext.Types)
            {
                ComputeSpecifics(typeContext);
            }
        }

        private static void ComputeSpecifics(TypeRewriteContext typeContext)
        {
            if (typeContext.ComputedTypeSpecifics != TypeRewriteContext.TypeSpecifics.NotComputed) return;
            if (typeContext.RewriteSemantic != TypeRewriteContext.TypeRewriteSemantic.Default) return;
            
            typeContext.ComputedTypeSpecifics = TypeRewriteContext.TypeSpecifics.Computing;
            
            foreach (var originalField in typeContext.OriginalType.Fields)
            {
                if(originalField.IsStatic) continue;
                
                var fieldType = originalField.FieldType;
                if (fieldType.IsPrimitive || fieldType.IsPointer) continue;
                if (fieldType.FullName == "System.String" || fieldType.FullName == "System.Object" || fieldType.IsArray || fieldType.IsByReference || fieldType.IsGenericParameter || fieldType.IsGenericInstance)
                {
                    typeContext.ComputedTypeSpecifics = TypeRewriteContext.TypeSpecifics.NonBlittableStruct;
                    return;
                }

                var fieldTypeContext = typeContext.AssemblyContext.GlobalContext.GetNewTypeForOriginal(fieldType.Resolve());
                ComputeSpecifics(fieldTypeContext);
                if (fieldTypeContext.ComputedTypeSpecifics != TypeRewriteContext.TypeSpecifics.BlittableStruct)
                {
                    typeContext.ComputedTypeSpecifics = TypeRewriteContext.TypeSpecifics.NonBlittableStruct;
                    return;
                }
            }

            typeContext.ComputedTypeSpecifics = TypeRewriteContext.TypeSpecifics.BlittableStruct;
        }
    }
}