using System.Linq;
using System.Text;
using Mono.Cecil;

namespace AssemblyUnhollower
{
    public static class StringEx
    {
        public static string UnSystemify(this string str)
        {
            if (str.StartsWith("System") || str.StartsWith("mscorlib"))
                return "Il2Cpp" + str;

            return str;
        }

        public static bool IsObfuscated(this string str)
        {
            return str.Any(it => !char.IsDigit(it) && !(it >= 'a' && it <= 'z' || it >= 'A' && it <= 'Z') && it != '_' && it != '`');
        }

        public static ulong StableHash(this string str)
        {
            ulong hash = 0;
            for (var i = 0; i < str.Length; i++) 
                hash = hash * 37 + str[i];

            return hash;
        }

        public static string GetUnmangledName(this TypeReference typeRef)
        {
            StringBuilder builder = new StringBuilder();
            if (typeRef is GenericInstanceType genericInstance)
            {
                builder.Append(genericInstance.ElementType.GetUnmangledName());
                foreach (var genericArgument in genericInstance.GenericArguments)
                {
                    builder.Append("_");
                    builder.Append(genericArgument.GetUnmangledName());
                }
            }
            else
            {
                builder.Append(typeRef.Name.Replace('`', '_'));
            }

            return builder.ToString();
        }
    }
}