using Mono.Cecil;

namespace AssemblyUnhollower.Extensions
{
    public static class TypeReferenceEx
    {
        public static bool UnmangledNamesMatch(this TypeReference typeRefA, TypeReference typeRefB)
        {
            var aIsDefOrRef = typeRefA.GetType() == typeof(TypeReference) || typeRefA.GetType() == typeof(TypeDefinition);
            var bIsDefOrRef = typeRefB.GetType() == typeof(TypeReference) || typeRefB.GetType() == typeof(TypeDefinition);
            if (!(aIsDefOrRef && bIsDefOrRef) && typeRefA.GetType() != typeRefB.GetType())
                return false;
            
            switch (typeRefA)
            {
                case PointerType pointer:
                    return pointer.ElementType.UnmangledNamesMatch(((PointerType) typeRefB).ElementType);
                case ByReferenceType byRef:
                    return byRef.ElementType.UnmangledNamesMatch(((ByReferenceType) typeRefB).ElementType);
                case ArrayType array:
                    return array.ElementType.UnmangledNamesMatch(((ArrayType) typeRefB).ElementType);
                case GenericInstanceType genericInstance:
                {
                    var elementA = genericInstance.ElementType;
                    var genericInstanceB = (GenericInstanceType) typeRefB;
                    var elementB = genericInstanceB.ElementType;
                    if (!elementA.UnmangledNamesMatch(elementB))
                        return false;
                    if (genericInstance.GenericArguments.Count != genericInstanceB.GenericArguments.Count)
                        return false;
                    
                    for (var i = 0; i < genericInstance.GenericArguments.Count; i++)
                    {
                        if (!genericInstance.GenericArguments[i].UnmangledNamesMatch(genericInstanceB.GenericArguments[i]))
                            return false;
                    }

                    return true;
                }
                default:
                    return typeRefA.Name == typeRefB.Name;
            }
        }

        public static bool IsNullable(this TypeReference a)
        {
            return a.Name == "Nullable`1";
        }
        
        public static bool MayRequireScratchSpace(this TypeReference a)
        {
            return a is GenericParameter || a.IsNullable();
        }
        
        public static string GetNamespacePrefix(this TypeReference type)
        {
            if (!type.IsNested) 
                return type.Namespace;
            
            return GetFullNameWithNesting(type.DeclaringType);
        }

        public static string GetFullNameWithNesting(this TypeReference type)
        {
            if (type.IsNested)
            {
                var parentPrefix = GetFullNameWithNesting(type.DeclaringType);
                return $"{parentPrefix}+{type.DeclaringType.Name}";
            }

            if (string.IsNullOrEmpty(type.Namespace))
                return type.Name;

            return $"{type.Namespace}.{type.Name}";
        }
    }
}