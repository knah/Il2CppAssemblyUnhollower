using Mono.Cecil;

namespace AssemblyUnhollower.Extensions
{
    public static class TypeReferenceEx
    {
        public static bool UnmangledNamesMatch(this TypeReference typeRefA, TypeReference typeRefB)
        {
            if (typeRefA.GetType() != typeRefB.GetType())
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
    }
}