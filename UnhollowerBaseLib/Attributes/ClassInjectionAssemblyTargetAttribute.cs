using System;
using System.Collections.Generic;

namespace UnhollowerRuntimeLib
{
    [Obsolete("UnhollowerRuntimeLib.ClassInjectionAssemblyTargetAttribute is obsolete. Use UnhollowerBaseLib.Attributes.ClassInjectionAssemblyTargetAttribute instead.")]
    [AttributeUsage(AttributeTargets.Class)]
    public class ClassInjectionAssemblyTargetAttribute : UnhollowerBaseLib.Attributes.ClassInjectionAssemblyTargetAttribute
    {
        public ClassInjectionAssemblyTargetAttribute(string assembly) : base(assembly) { }
        public ClassInjectionAssemblyTargetAttribute(string[] assemblies) : base(assemblies) { }
    }
}

namespace UnhollowerBaseLib.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ClassInjectionAssemblyTargetAttribute : Attribute
    {
        string[] assemblies;

        public ClassInjectionAssemblyTargetAttribute(string assembly)
        {
            if (string.IsNullOrWhiteSpace(assembly)) assemblies = new string[0];
            else assemblies = new string[] { assembly };
        }
        public ClassInjectionAssemblyTargetAttribute(string[] assemblies)
        {
            if (assemblies is null) this.assemblies = new string[0];
            else this.assemblies = assemblies;
        }
        internal IntPtr[] GetImagePointers()
        {
            List<IntPtr> result = new List<IntPtr>();
            foreach (string assembly in assemblies)
            {
                IntPtr intPtr = IL2CPP.GetIl2CppImage(assembly);
                if (intPtr != IntPtr.Zero) result.Add(intPtr);
            }
            return result.ToArray();
        }
    }
}
