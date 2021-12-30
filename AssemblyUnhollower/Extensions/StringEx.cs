using System.Text;
using Mono.Cecil;

namespace AssemblyUnhollower.Extensions
{
    public static class StringEx
    {
        public static string UnSystemify(this string str, UnhollowerOptions options)
        {
            foreach (var prefix in options.NamespacesAndAssembliesToPrefix)
                if (str.StartsWith(prefix))
                    return "Il2Cpp" + str;

            return str;
        }

        public static string FilterInvalidInSourceChars(this string str)
        {
            var chars = str.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                var it = chars[i];
                if (!char.IsDigit(it) && !(it >= 'a' && it <= 'z' || it >= 'A' && it <= 'Z') && it != '_' &&
                    it != '`') chars[i] = '_';
            }

            return new string(chars);
        }
        
        public static bool IsInvalidInSource(this string str)
        {
            for (var i = 0; i < str.Length; i++)
            {
                var it = str[i];
                if (!char.IsDigit(it) && !(it >= 'a' && it <= 'z' || it >= 'A' && it <= 'Z') && it != '_' &&
                    it != '`') return true;
            }

            return false;
        }

        public static bool IsObfuscated(this string str, UnhollowerOptions options)
        {
            if (options.ObfuscatedNamesRegex != null)
                return options.ObfuscatedNamesRegex.IsMatch(str);

            foreach (var it in str)
            {
                if (!char.IsDigit(it) && !(it >= 'a' && it <= 'z' || it >= 'A' && it <= 'Z') && it != '_' && it != '`' && it != '.' && it != '<' && it != '>') return true;
            }

            return false;
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
            } else if (typeRef is ByReferenceType byRef)
            {
                builder.Append("byref_");
                builder.Append(byRef.ElementType.GetUnmangledName());
            } else if (typeRef is PointerType pointer)
            {
                builder.Append("ptr_");
                builder.Append(pointer.ElementType.GetUnmangledName());
            }
            else
            {
                if (typeRef.Namespace == nameof(UnhollowerBaseLib) && typeRef.Name.StartsWith("Il2Cpp") && typeRef.Name.Contains("Array"))
                {
                    builder.Append("ArrayOf");
                } else
                    builder.Append(typeRef.Name.Replace('`', '_'));
            }

            return builder.ToString();
        }
    }
}