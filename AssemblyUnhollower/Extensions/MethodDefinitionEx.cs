using System.Globalization;
using System.Linq;
using Mono.Cecil;

namespace AssemblyUnhollower.Extensions
{
    public static class MethodDefinitionEx
    {
        public static long ExtractOffset(this MethodDefinition originalMethod) => ExtractAddress(originalMethod, "Offset");
        public static long ExtractRva(this MethodDefinition originalMethod) => ExtractAddress(originalMethod, "RVA");

        private static long ExtractAddress(this MethodDefinition originalMethod, string attributeName)
        {
            var addressAttribute = originalMethod.CustomAttributes.SingleOrDefault(it => it.AttributeType.Name == "AddressAttribute");
            var rvaField = addressAttribute?.Fields.SingleOrDefault(it => it.Name == attributeName);

            if (rvaField?.Name == null) return 0;

            var addressString = (string) rvaField.Value.Argument.Value;
            long.TryParse(addressString.Substring(2), NumberStyles.HexNumber, null, out var address);
            return address;
        }
    }
}