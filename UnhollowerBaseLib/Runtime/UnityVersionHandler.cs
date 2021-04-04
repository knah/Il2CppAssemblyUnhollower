using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnhollowerBaseLib.Runtime
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class UnityVersionHandlerAttribute : Attribute
    {
        public UnityVersionHandlerAttribute(string priority)
        {
            Priority = priority;
        }

        public string Priority { get; }
    }

    internal class VersionHandleInfo
    {
        public Func<Version, bool> WorksOn { get; init; }
        public Dictionary<Type, Type> StructHandlers { get; } = new();
        public Type VersionHandleType { get; init; }
    }

    public static class UnityVersionHandler
    {
        private static readonly List<VersionHandleInfo> VersionHandlers = new();

        private static readonly List<Type> StructHandlers =
            GetAllTypesSafe().Where(t => t.IsInterface && typeof(INativeStructHandler).IsAssignableFrom(t)).ToList();

        private static readonly Dictionary<Type, object> Handlers = new();

        private static Version UnityVersion = new(2018, 4, 20);

        static UnityVersionHandler()
        {
            var versionHandlerTypes =
                GetAllTypesSafe().Where(t => t.GetCustomAttribute<UnityVersionHandlerAttribute>() != null);

            foreach (var versionHandlerType in versionHandlerTypes.OrderBy(t =>
                Version.Parse(t.GetCustomAttribute<UnityVersionHandlerAttribute>().Priority)))
            {
                var worksOnCheck = versionHandlerType.GetMethods().FirstOrDefault(m => m.IsStatic &&
                    m.Name == "WorksOn" && m.ReturnType == typeof(bool) &&
                    m.GetParameters().FirstOrDefault()?.ParameterType == typeof(Version));
                var info = new VersionHandleInfo
                {
                    WorksOn = worksOnCheck != null
                        ? (Func<Version, bool>) worksOnCheck.CreateDelegate(typeof(Func<Version, bool>))
                        : _ => true,
                    VersionHandleType = versionHandlerType
                };

                foreach (var structHandler in StructHandlers)
                {
                    var handlerImplementation = versionHandlerType.GetNestedTypes().FirstOrDefault(t =>
                        !t.IsInterface && !t.IsAbstract && structHandler.IsAssignableFrom(t));
                    if (handlerImplementation != null)
                        info.StructHandlers[structHandler] = handlerImplementation;
                }

                VersionHandlers.Add(info);
            }
        }

        private static T GetHandler<T>()
        {
            if (Handlers.TryGetValue(typeof(T), out var result))
                return (T) result;

            static object InitHandler(Type handlerType)
            {
                var res = Activator.CreateInstance(handlerType);
                Handlers[typeof(T)] = res;
                return res;
            }

            Type latestValidHandler = null;
            VersionHandleInfo latestValidVersionHandleInfo = null;
            foreach (var versionHandleInfo in VersionHandlers)
            {
                if (!versionHandleInfo.StructHandlers.TryGetValue(typeof(T), out var handler)) continue;
                latestValidHandler = handler;
                latestValidVersionHandleInfo = versionHandleInfo;
                if (versionHandleInfo.WorksOn(UnityVersion))
                {
                    LogSupport.Info(
                        $"Using version handler {versionHandleInfo.VersionHandleType.FullName} for {typeof(T).FullName}");
                    return (T) InitHandler(handler);
                }
            }

            if (latestValidVersionHandleInfo == null)
                throw new NotImplementedException(
                    $"No matching {typeof(T).FullName} handler for Unity version {UnityVersion}");

            LogSupport.Warning($"No direct handler for {typeof(T).FullName} found for Unity {UnityVersion}; using best match handler {latestValidVersionHandleInfo.VersionHandleType.FullName}");

            return (T) InitHandler(latestValidHandler);
        }

        private static IEnumerable<Type> GetAllTypesSafe()
        {
            try
            {
                return typeof(UnityVersionHandler).Assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException re)
            {
                return re.Types.Where(t => t != null);
            }
        }

        /// <summary>
        ///     Initializes Unity interface for specified Unity version.
        /// </summary>
        /// <example>For Unity 2018.4.20, call <c>Initialize(2018, 4, 20)</c></example>
        public static void Initialize(int majorVersion, int minorVersion, int patchVersion)
        {
            UnityVersion = new Version(majorVersion, minorVersion, patchVersion);
            Handlers.Clear();
        }

        public static INativeClassStruct NewClass(int vTableSlots)
        {
            return GetHandler<INativeClassStructHandler>().CreateNewClassStruct(vTableSlots);
        }

        public static unsafe INativeClassStruct Wrap(Il2CppClass* classPointer)
        {
            return GetHandler<INativeClassStructHandler>().Wrap(classPointer);
        }

        public static INativeImageStruct NewImage()
        {
            return GetHandler<INativeImageStructHandler>().CreateNewImageStruct();
        }

        public static unsafe INativeImageStruct Wrap(Il2CppImage* classPointer)
        {
            return GetHandler<INativeImageStructHandler>().Wrap(classPointer);
        }

        public static INativeMethodStruct NewMethod() =>
            GetHandler<INativeMethodStructHandler>().CreateNewMethodStruct();

        public static unsafe Il2CppParameterInfo* NewMethodParameterArray(int count) =>
            GetHandler<INativeMethodStructHandler>().CreateNewParameterInfoArray(count);

        public static unsafe INativeMethodStruct Wrap(Il2CppMethodInfo* methodPointer) =>
            GetHandler<INativeMethodStructHandler>().Wrap(methodPointer);
        
        public static unsafe INativeParameterInfoStruct Wrap(Il2CppParameterInfo* parameterInfo) =>
            GetHandler<INativeMethodStructHandler>().Wrap(parameterInfo);

        public static IntPtr GetMethodFromReflection(IntPtr method) =>
            GetHandler<INativeMethodStructHandler>().GetMethodFromReflection(method);
    }
}