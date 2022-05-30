using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace UnhollowerBaseLib
{
    public class UnhollowerUtils
    {
        private const string GenericDeclaringTypeName = "MethodInfoStoreGeneric_";
        private const string GenericFieldName = "Pointer";

        private static FieldInfo GetFieldInfoFromMethod(MethodBase method, string prefix, FieldType type = FieldType.None)
        {
            var body = method.GetMethodBody();
            if (body == null) throw new ArgumentException("Target method may not be abstract");
            var methodModule = method.DeclaringType.Assembly.Modules.Single();
            foreach (var (opCode, opArg) in MiniIlParser.Decode(body.GetILAsByteArray()))
            {
                if (opCode != OpCodes.Ldsfld) continue;
                var fieldInfo = methodModule.ResolveField((int) opArg);
                if (fieldInfo?.FieldType != typeof(IntPtr))
                    continue;

                switch (type)
                {
                    case FieldType.None:
                        if (fieldInfo.Name.StartsWith(prefix))
                            return fieldInfo;

                        break;

                    case FieldType.GenericMethod:

                        if (fieldInfo.Name.Equals(GenericFieldName) &&
                            fieldInfo.DeclaringType.Name.StartsWith(GenericDeclaringTypeName))
                        {
                            var genericType = fieldInfo.DeclaringType.GetGenericTypeDefinition().MakeGenericType(method.GetGenericArguments());
                            return genericType.GetField(GenericFieldName, BindingFlags.NonPublic | BindingFlags.Static);
                        }

                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }
            }
            return null;
        }

        public static FieldInfo GetIl2CppMethodInfoPointerFieldForGeneratedMethod(MethodBase method)
        {
            const string prefix = "NativeMethodInfoPtr_";
            if (method.IsGenericMethod)
                return GetFieldInfoFromMethod(method, prefix, FieldType.GenericMethod);

            return GetFieldInfoFromMethod(method, prefix);
        }

        public static FieldInfo GetIl2CppFieldInfoPointerFieldForGeneratedFieldAccessor(MethodBase method)
        {
            return GetFieldInfoFromMethod(method, "NativeFieldInfoPtr_");
        }

        private enum FieldType
        {
            None,
            GenericMethod
        }
    }
}