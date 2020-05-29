namespace UnhollowerBaseLib.Runtime
{
    public interface INativeClassStructHandler
    {
        INativeClassStruct CreateNewClassStruct(int vTableSlots);
        unsafe INativeClassStruct Wrap(Il2CppClass* classPointer);
    }
}