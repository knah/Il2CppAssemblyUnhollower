using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnhollowerBaseLib.Attributes;
using UnhollowerRuntimeLib;

namespace UnhollowerBaseLib
{
    public static class Il2CppClassPointerStore<T>
    {
        public static IntPtr NativeClassPtr;
        public static Type CreatedTypeRedirect;

        static Il2CppClassPointerStore()
        {
            var targetType = typeof(T);
            //NativeClassPtr = Il2CppClassPointerStore.CalculateClassPointerForType(targetType);//todo: uncomment
            Il2CppClassPointerStore.InitializeForType(targetType);
        }
    }

    public static class Il2CppClassPointerStore
    {
        private static readonly ConcurrentDictionary<Type, IntPtr> ourClassPointers = new();
        private static readonly ConcurrentDictionary<Type, IntPtr> ourCreatedTypeRedirects = new();

        internal static void InitializeForType(Type targetType)
        {
            RuntimeHelpers.RunClassConstructor(targetType.TypeHandle);
            if (targetType.IsPrimitive || targetType == typeof(string))
            {
                RuntimeHelpers.RunClassConstructor(AppDomain.CurrentDomain.GetAssemblies()
                    .Single(it => it.GetName().Name == "Il2Cppmscorlib").GetType("Il2Cpp" + targetType.FullName)
                    .TypeHandle);
            }
            
            foreach (var customAttribute in targetType.CustomAttributes)
            {
                if (customAttribute.AttributeType != typeof(AlsoInitializeAttribute)) continue;
                
                var linkedType = (Type) customAttribute.ConstructorArguments[0].Value;
                RuntimeHelpers.RunClassConstructor(linkedType.TypeHandle);
            }
        }

        public static void RegisterClassPointerForType(Type type, IntPtr clazz)
        {
            ourClassPointers[type] = clazz;
        }

        public static void RegisterTypeWithExplicitTokenInfo(Type type, string assemblyName, uint token)
        {
            ourClassPointers[type] = IL2CPP.GetClassPointerByToken(assemblyName, token, type);
        }

        public static IntPtr GetClassPointerForType(Type type)
        {
            if (type == typeof(void)) return Il2CppClassPointerStore<Il2CppSystem.Void>.NativeClassPtr;
            return (IntPtr)typeof(Il2CppClassPointerStore<>).MakeGenericType(type)
                .GetField(nameof(Il2CppClassPointerStore<int>.NativeClassPtr)).GetValue(null);
            //return Il2CppClassPointerStore.CalculateClassPointerForType(type); //todo: replace method contents with CalculateClassPointerForType
        }

        internal static void SetClassPointerForType(Type type, IntPtr value)
        {
            RegisterClassPointerForType(type, value);
            typeof(Il2CppClassPointerStore<>).MakeGenericType(type)
                .GetField(nameof(Il2CppClassPointerStore<int>.NativeClassPtr)).SetValue(null, value);
        }

        /// <summary>
        /// Don't call yet. Suspected to have runtime bugs
        /// </summary>
        private static IntPtr CalculateClassPointerForType(Type type)
        {
            return ourClassPointers.GetOrAdd(type, type =>
            {
                if (type.IsConstructedGenericType)
                {
                    var genericTypeDefinition = type.GetGenericTypeDefinition();
                    if (genericTypeDefinition == typeof(Il2CppReferenceArray<>) || genericTypeDefinition == typeof(Il2CppStructArray<>))
                    {
                        var elementNativeType = RuntimeReflectionHelper.GetTypeForClass(CalculateClassPointerForType(type.GenericTypeArguments[0]));
                        return IL2CPP.il2cpp_class_from_type(elementNativeType.MakeArrayType(1).TypeHandle.value);
                    }

                    var baseNativeType = CalculateClassPointerForType(genericTypeDefinition);
                    return IL2CPP.il2cpp_array_class_get(baseNativeType, 1);
                }

                var tokenAttribute = (NativeTypeTokenAttribute)type.GetCustomAttribute(typeof(NativeTypeTokenAttribute));
                if (tokenAttribute == null)
                {
                    LogSupport.Error($"Type {type} is not a valid type for use in IL2CPP (it's missing native token info)");
                    return IntPtr.Zero;
                }

                return IL2CPP.GetClassPointerByToken(tokenAttribute.AssemblyName, tokenAttribute.Token, type);
            });
        }
    }
}