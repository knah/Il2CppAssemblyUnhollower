using AssemblyUnhollower.Contexts;
using AssemblyUnhollower.Extensions;
using Mono.Cecil;

namespace AssemblyUnhollower.Passes
{
    public static class Pass12FillTypedefs
    {
        public static void DoPass(RewriteGlobalContext context)
        {
            foreach (var assemblyContext in context.Assemblies)
            {
                foreach (var typeContext in assemblyContext.Types)
                {
                    foreach (var originalParameter in typeContext.OriginalType.GenericParameters)
                    {
                        var newParameter = new GenericParameter(originalParameter.Name, typeContext.NewType);
                        typeContext.NewType.GenericParameters.Add(newParameter);
                        newParameter.Attributes = originalParameter.Attributes.StripValueTypeConstraint();
                    }

                    if (typeContext.OriginalType.IsEnum)
                        typeContext.NewType.BaseType = assemblyContext.Imports.Enum;
                    else if (typeContext.ComputedTypeSpecifics == TypeRewriteContext.TypeSpecifics.BlittableStruct) 
                        typeContext.NewType.BaseType = assemblyContext.Imports.ValueType;
                    //else if (typeContext.ComputedTypeSpecifics == TypeRewriteContext.TypeSpecifics.NonBlittableStruct) //todo: uncomment
                    //    typeContext.NewType.BaseType = assemblyContext.Imports.Il2CppNonBlittableValueType;
                }
            }

            // Second pass is explicitly done after first to account for rewriting of generic base types - value-typeness is important there
            foreach (var assemblyContext in context.Assemblies)
                OldRewriteAssembly(assemblyContext); //todo: switch to new method
                //NewRewriteAssembly(assemblyContext, context);
        }

        private static void OldRewriteAssembly(AssemblyRewriteContext assemblyContext)
        {
            foreach (var typeContext in assemblyContext.Types)
                if (!typeContext.OriginalType.IsEnum && typeContext.ComputedTypeSpecifics !=
                    TypeRewriteContext.TypeSpecifics.BlittableStruct)
                    typeContext.NewType.BaseType = assemblyContext.RewriteTypeRef(typeContext.OriginalType.BaseType);
        }

        private static void NewRewriteAssembly(AssemblyRewriteContext assemblyContext, RewriteGlobalContext context)
        {
            foreach (var typeContext in assemblyContext.Types)
            {
                if (!typeContext.OriginalType.IsEnum &&
                    typeContext.ComputedTypeSpecifics != TypeRewriteContext.TypeSpecifics.BlittableStruct &&
                    typeContext.ComputedTypeSpecifics != TypeRewriteContext.TypeSpecifics.NonBlittableStruct &&
                    typeContext.RewriteSemantic == TypeRewriteContext.TypeRewriteSemantic.Default)
                {
                    if (typeContext.OriginalType.BaseType == null)
                        typeContext.NewType.BaseType = assemblyContext.Imports.Il2CppObjectBase;
                    else if (typeContext.OriginalType.BaseType.FullName == "System.Object") // normal typeref rewrite will produce real System.Object
                        typeContext.NewType.BaseType = assemblyContext.NewAssembly.MainModule.ImportReference(context.GetNewTypeForOriginal(typeContext.OriginalType.BaseType.Resolve()).NewType);
                    else
                        typeContext.NewType.BaseType = assemblyContext.RewriteTypeRef(typeContext.OriginalType.BaseType);
                }

                if (typeContext.RewriteSemantic == TypeRewriteContext.TypeRewriteSemantic.UseSystemInterface ||
                    typeContext.RewriteSemantic == TypeRewriteContext.TypeRewriteSemantic.UseSystemValueType)
                    continue;

                foreach (var iface in typeContext.OriginalType.Interfaces)
                {
                    var newInterface = assemblyContext.RewriteTypeRef(iface.InterfaceType);

                    typeContext.NewType.Interfaces.Add(new InterfaceImplementation(newInterface));
                }
            }
        }
    }
}