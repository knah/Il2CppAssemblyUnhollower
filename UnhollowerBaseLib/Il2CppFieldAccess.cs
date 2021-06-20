using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib
{
    public enum TestEnum
    {
        One,
        Two,
        Three
    }
    public static class Il2CppFieldAccess
    {
        public static IntPtr GetOriginalFieldPointer<T>(string fieldName)
        {
            return IL2CPP.GetIl2CppField(Il2CppClassPointerStore<T>.NativeClassPtr, fieldName);
        }

        public static IntPtr GetObjectFieldPointer<T>(T il2CppObject, IntPtr originalFieldPointer) where T : Il2CppObjectBase
        {
            return IL2CPP.Il2CppObjectBaseToPtrNotNull(il2CppObject) + (int)IL2CPP.il2cpp_field_get_offset(originalFieldPointer);
        }

        //public static void SetObjectField<T,>

        public static void Test()
        {
            GetOriginalFieldPointer<int>("dfdes");
            GetInjectedFieldValue_Struct<Il2CppObjectBase, int>(null, "");
            GetInjectedFieldValue_Struct<Il2CppObjectBase, TestEnum>(null, "");
            GetInjectedFieldValue_Object<Il2CppObjectBase, Il2CppSystem.Collections.Generic.List<int>>(null, "");
        }
        
        public static string GetInjectedFieldValue_String<T>(T il2CppObject, string fieldName) where T : Il2CppObjectBase
        {
            return GetInjectedFieldValue_String<T>(il2CppObject, GetOriginalFieldPointer<T>(fieldName));
        }
        public static string GetInjectedFieldValue_String<T>(T il2CppObject, IntPtr fieldPointer) where T : Il2CppObjectBase
        {
            return IL2CPP.Il2CppStringToManaged(GetObjectFieldPointer<T>(il2CppObject, fieldPointer));
        }
        public static void SetInjectedFieldValue_String<T>(T il2CppObject, string fieldName, string value) where T : Il2CppObjectBase
        {
            SetInjectedFieldValue_String<T>(il2CppObject, GetOriginalFieldPointer<T>(fieldName), value);
        }
        public static void SetInjectedFieldValue_String<T>(T il2CppObject, IntPtr fieldPointer, string value) where T : Il2CppObjectBase
        {
            IntPtr intPtr = IL2CPP.Il2CppObjectBaseToPtrNotNull(il2CppObject);
            IL2CPP.il2cpp_gc_wbarrier_set_field(intPtr, intPtr + (int)IL2CPP.il2cpp_field_get_offset(fieldPointer), IL2CPP.ManagedStringToIl2Cpp(value));
        }

        public static S GetInjectedFieldValue_Struct<T, S>(T il2CppObject, string fieldName) where T : Il2CppObjectBase where S : unmanaged
        {
            return GetInjectedFieldValue_Struct<T, S>(il2CppObject, GetOriginalFieldPointer<T>(fieldName));
        }
        public static S GetInjectedFieldValue_Struct<T, S>(T il2CppObject, IntPtr fieldPointer) where T : Il2CppObjectBase where S : unmanaged
        {
            return Marshal.PtrToStructure<S>(GetObjectFieldPointer<T>(il2CppObject, fieldPointer));
        }
        
        public static S GetInjectedFieldValue_Object<T, S>(T il2CppObject, string fieldName) where T : Il2CppObjectBase where S : Il2CppObjectBase
        {
            return GetInjectedFieldValue_Object<T, S>(il2CppObject, GetOriginalFieldPointer<T>(fieldName));
        }
        public static S GetInjectedFieldValue_Object<T, S>(T il2CppObject, IntPtr fieldPointer) where T : Il2CppObjectBase where S : Il2CppObjectBase
        {
            IntPtr intPtr = GetObjectFieldPointer<T>(il2CppObject, fieldPointer);
            return (intPtr != IntPtr.Zero) ? (new Il2CppObjectBase(intPtr)).TryCast<S>() : null;
        }
        public static void SetInjectedFieldValue_Object<T, S>(T il2CppObject, string fieldName, S value) where T : Il2CppObjectBase where S : Il2CppObjectBase
        {
            SetInjectedFieldValue_Object<T, S>(il2CppObject, GetOriginalFieldPointer<T>(fieldName), value);
        }
        public unsafe static void SetInjectedFieldValue_Object<T, S>(T il2CppObject, IntPtr fieldPointer, S value) where T : Il2CppObjectBase where S : Il2CppObjectBase
        {
            IntPtr intPtr = IL2CPP.Il2CppObjectBaseToPtrNotNull(il2CppObject);
            IL2CPP.il2cpp_gc_wbarrier_set_field(intPtr, intPtr + (int)IL2CPP.il2cpp_field_get_offset(fieldPointer), IL2CPP.Il2CppObjectBaseToPtr(value));
        }
    }
}
