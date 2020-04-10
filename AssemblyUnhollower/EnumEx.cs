using Mono.Cecil;

namespace AssemblyUnhollower
{
    public static class EnumEx
    {
        public static FieldAttributes ForcePublic(this FieldAttributes fieldAttributes)
        {
            return fieldAttributes & ~FieldAttributes.FieldAccessMask | FieldAttributes.Public;
        }
    }
}