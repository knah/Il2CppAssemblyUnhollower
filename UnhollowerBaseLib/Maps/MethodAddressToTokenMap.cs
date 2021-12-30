using System;
using System.Reflection;

#nullable enable

namespace UnhollowerBaseLib.Maps
{
    public class MethodAddressToTokenMap : MethodAddressToTokenMapBase<Assembly, MethodBase>
    {
        [Obsolete("Use the constant in MethodAddressToTokenMapBase")]
        public new const int Magic = MethodAddressToTokenMapBase<Assembly, MethodBase>.Magic;
        [Obsolete("Use the constant in MethodAddressToTokenMapBase")]
        public new const int Version = MethodAddressToTokenMapBase<Assembly, MethodBase>.Version;
        [Obsolete("Use the constant in MethodAddressToTokenMapBase")]
        public new const string FileName = MethodAddressToTokenMapBase<Assembly, MethodBase>.FileName;

        public MethodAddressToTokenMap(string filePath) : base(filePath)
        {
        }

        protected override Assembly LoadAssembly(string assemblyName) => Assembly.Load(assemblyName);

        protected override MethodBase? ResolveMethod(Assembly? assembly, int token) => assembly?.ManifestModule.ResolveMethod(token);
    }
}