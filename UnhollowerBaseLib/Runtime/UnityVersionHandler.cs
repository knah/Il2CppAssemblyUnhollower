using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnhollowerBaseLib.Runtime
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class UnityVersionHandlerAttribute : Attribute
    {
        public string Priority { get; }

        public UnityVersionHandlerAttribute(string priority)
        {
            Priority = priority;
        }
    }
    
    internal class VersionHandleInfo
    {
        public Func<Version, bool> WorksOn { get; init; }
        public Dictionary<Type, Type> StructHandlers { get; } = new();
    }
    
    public static class UnityVersionHandler
    {
        private static readonly List<VersionHandleInfo> VersionHandlers = new();

        private static readonly List<Type> StructHandlers = new() {typeof(INativeClassStructHandler)};

        private static readonly Dictionary<Type, object> Handlers = new();

        private static Version UnityVersion = new(2018, 4, 20);

        static UnityVersionHandler()
        {
            var versionHandlerTypes =
                GetAllTypesSafe().Where(t => t.GetCustomAttribute<UnityVersionHandlerAttribute>() != null);

            foreach (var versionHandlerType in versionHandlerTypes.OrderBy(t => Version.Parse(t.GetCustomAttribute<UnityVersionHandlerAttribute>().Priority)))
            {
                var worksOnCheck = versionHandlerType.GetMethods().FirstOrDefault(m => m.IsStatic &&
                    m.Name == "WorksOn" && m.ReturnType == typeof(bool) &&
                    m.GetParameters().FirstOrDefault()?.ParameterType == typeof(Version));
                var info = new VersionHandleInfo
                {
                    WorksOn = worksOnCheck != null ? (Func<Version, bool>) worksOnCheck.CreateDelegate(typeof(Func<Version, bool>)) : _ => true
                };
                
                foreach (var structHandler in StructHandlers)
                {
                    var handlerImplementation = versionHandlerType.GetNestedTypes().FirstOrDefault(t =>
                        !t.IsInterface && !t.IsAbstract && structHandler.IsAssignableFrom(t));
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
            foreach (var versionHandleInfo in VersionHandlers)
            {
                if (!versionHandleInfo.StructHandlers.TryGetValue(typeof(T), out var handler)) continue;
                latestValidHandler = handler;
                if (versionHandleInfo.WorksOn(UnityVersion))
                    return (T) InitHandler(handler);
            }

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
        /// Initializes Unity interface for specified Unity version.
        /// </summary>
        /// <example>For Unity 2018.4.20, call <c>Initialize(2018, 4, 20)</c></example>
        public static void Initialize(int majorVersion, int minorVersion, int patchVersion)
        {
            UnityVersion = new Version(majorVersion, minorVersion, patchVersion);
            Handlers.Clear();
            //
            // if (majorVersion == 2018 && minorVersion == 4)
            //     ourHandler = new Unity2018_4NativeClassStructHandler();
            // else if (majorVersion <= 2018)
            //     ourHandler = new Unity2018_0NativeClassStructHandler();
            // else if (majorVersion == 2020 && minorVersion == 2 && patchVersion >= 4)
            //     ourHandler = new Unity2020_2_4.Unity2020_2_4NativeClassStructHandler();
            // else
            //     ourHandler = new Unity2019NativeClassStructHandler();
        }

        public static INativeClassStruct NewClass(int vTableSlots) => GetHandler<INativeClassStructHandler>().CreateNewClassStruct(vTableSlots);
        public static unsafe INativeClassStruct Wrap(Il2CppClass* classPointer) => GetHandler<INativeClassStructHandler>().Wrap(classPointer);
        
        public static INativeImageStruct NewImage() => GetHandler<INativeImageStructHandler>().CreateNewImageStruct();
        public static unsafe INativeImageStruct Wrap(Il2CppImage* classPointer) => GetHandler<INativeImageStructHandler>().Wrap(classPointer);
    }
}