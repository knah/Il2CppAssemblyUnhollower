using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace UnhollowerBaseLib
{
    public class UnhollowerUtils
    {
        public static FieldInfo GetIl2CppMethodInfoPointerFieldForGeneratedMethod(MethodBase method)
        {
            var body = method.GetMethodBody();
            if (body == null) throw new ArgumentException("Target method may not be abstract");
            var methodModule = method.DeclaringType.Assembly.Modules.Single();
            foreach (var (opCode, opArg) in MiniIlParser.Decode(body.GetILAsByteArray()))
            {
                if (opCode != OpCodes.Ldsfld) continue;
                var fieldInfo = methodModule.ResolveField((int) opArg);
                if (fieldInfo?.FieldType != typeof(IntPtr) || !fieldInfo.Name.StartsWith("NativeMethodInfo")) continue;
                return fieldInfo;
            }
            return null;
        }
    }
}