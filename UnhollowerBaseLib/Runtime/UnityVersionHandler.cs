using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnhollowerBaseLib.Runtime.VersionSpecific.Assembly;
using UnhollowerBaseLib.Runtime.VersionSpecific.Class;
using UnhollowerBaseLib.Runtime.VersionSpecific.Image;
using UnhollowerBaseLib.Runtime.VersionSpecific.MethodInfo;
using UnhollowerBaseLib.Runtime.VersionSpecific.ParameterInfo;
using UnhollowerBaseLib.Runtime.VersionSpecific.Type;

namespace UnhollowerBaseLib.Runtime
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    internal class ApplicableToUnityVersionsSinceAttribute : Attribute
    {
        public string StartVersion { get; }

        public ApplicableToUnityVersionsSinceAttribute(string startVersion)
        {
            StartVersion = startVersion;
        }
    }

    public static class UnityVersionHandler
    {
        private static readonly Type[] InterfacesOfInterest;
        private static readonly Dictionary<Type, List<(Version Version, object Handler)>> VersionedHandlers = new();
        private static readonly Dictionary<Type, object> Handlers = new();

        private static Version UnityVersion = new(2018, 4, 20);

        static UnityVersionHandler()
        {
            var allTypes = GetAllTypesSafe();
            var interfacesOfInterest = allTypes.Where(t => t.IsInterface && typeof(INativeStructHandler).IsAssignableFrom(t) && t != typeof(INativeStructHandler)).ToArray();
            InterfacesOfInterest = interfacesOfInterest;

            foreach (var i in interfacesOfInterest) VersionedHandlers[i] = new();

            foreach (var handlerImpl in allTypes.Where(t => !t.IsAbstract && interfacesOfInterest.Any(i => i.IsAssignableFrom(t))))
                foreach (var startVersion in handlerImpl.GetCustomAttributes<ApplicableToUnityVersionsSinceAttribute>())
                {
                    var instance = Activator.CreateInstance(handlerImpl);
                    foreach (var i in handlerImpl.GetInterfaces())
                        if (interfacesOfInterest.Contains(i))
                            VersionedHandlers[i].Add((Version.Parse(startVersion.StartVersion), instance));
                }

            foreach (var handlerList in VersionedHandlers.Values)
                handlerList.Sort((a, b) => -a.Version.CompareTo(b.Version));

            RecalculateHandlers();
        }

        private static void RecalculateHandlers()
        {
            Handlers.Clear();
            foreach (var type in InterfacesOfInterest)
            {
                foreach (var valueTuple in VersionedHandlers[type])
                {
                    if (valueTuple.Version > UnityVersion) continue;

                    Handlers[type] = valueTuple.Handler;
                    break;
                }
            }
        }

        private static T GetHandler<T>()
        {
            if (Handlers.TryGetValue(typeof(T), out var result))
                return (T) result;

            LogSupport.Error($"No direct for {typeof(T).FullName} found for Unity {UnityVersion}; this likely indicates a severe error somewhere");

            throw new ApplicationException("No handler");
        }

        private static Type GetMethodInfoStructType()
        {
            return GetHandler<INativeMethodStructHandler>().StructType;
        }

        public static IntPtr CopyMethodInfoStruct(IntPtr origMethodInfo)
        {
            int sizeOfMethodInfo = Marshal.SizeOf(GetMethodInfoStructType());
            IntPtr copiedMethodInfo = Marshal.AllocHGlobal(sizeOfMethodInfo);

            object temp = Marshal.PtrToStructure(origMethodInfo, GetMethodInfoStructType());
            Marshal.StructureToPtr(temp, copiedMethodInfo, false);

            return copiedMethodInfo;
        }

        private static Type[] GetAllTypesSafe()
        {
            try
            {
                return typeof(UnityVersionHandler).Assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException re)
            {
                return re.Types.Where(t => t != null).ToArray();
            }
        }

        /// <summary>
        ///     Initializes Unity interface for specified Unity version.
        /// </summary>
        /// <example>For Unity 2018.4.20, call <c>Initialize(2018, 4, 20)</c></example>
        public static void Initialize(int majorVersion, int minorVersion, int patchVersion)
        {
            UnityVersion = new Version(majorVersion, minorVersion, patchVersion);
            RecalculateHandlers();
        }


        public static IntPtr GetMethodFromReflection(IntPtr method) =>
            GetHandler<INativeMethodStructHandler>().GetMethodFromReflection(method);


        public static INativeClassStruct NewClass(int vTableSlots) =>
            GetHandler<INativeClassStructHandler>().CreateNewClassStruct(vTableSlots);

        public static unsafe INativeClassStruct Wrap(Il2CppClass* classPointer) =>
            GetHandler<INativeClassStructHandler>().Wrap(classPointer);
        

        public static INativeImageStruct NewImage() =>
            GetHandler<INativeImageStructHandler>().CreateNewImageStruct();
        
        public static unsafe INativeImageStruct Wrap(Il2CppImage* imagePointer) =>
            GetHandler<INativeImageStructHandler>().Wrap(imagePointer);
        

        public static INativeAssemblyStruct NewAssembly() =>
            GetHandler<INativeAssemblyStructHandler>().CreateNewAssemblyStruct();

        public static unsafe INativeAssemblyStruct Wrap(Il2CppAssembly* assemblyPointer) =>
            GetHandler<INativeAssemblyStructHandler>().Wrap(assemblyPointer);


        public static INativeTypeStruct NewType() =>
            GetHandler<INativeTypeStructHandler>().CreateNewTypeStruct();
        
        public static unsafe INativeTypeStruct Wrap(Il2CppTypeStruct* typePointer) =>
            GetHandler<INativeTypeStructHandler>().Wrap(typePointer);
        

        public static INativeMethodStruct NewMethod() =>
            GetHandler<INativeMethodStructHandler>().CreateNewMethodStruct();

        public static unsafe INativeMethodStruct Wrap(Il2CppMethodInfo* methodPointer) =>
            GetHandler<INativeMethodStructHandler>().Wrap(methodPointer);


        public static unsafe Il2CppParameterInfo*[] NewMethodParameterArray(int count) =>
            GetHandler<INativeParameterInfoStructHandler>().CreateNewParameterInfoArray(count);

        public static unsafe INativeParameterInfoStruct Wrap(Il2CppParameterInfo* parameterInfo) =>
            GetHandler<INativeParameterInfoStructHandler>().Wrap(parameterInfo);
    }
}