using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AssemblyUnhollower.Extensions
{
    public static class WriterEx
    {
        [ThreadStatic]
        private static byte[]? ourBuffer;
        
        public static unsafe void Write<T>(this BinaryWriter writer, T value) where T : unmanaged
        {
            var structSize = Marshal.SizeOf<T>();
            
            if (ourBuffer == null || ourBuffer.Length < structSize) 
                ourBuffer = new byte[structSize];

            fixed (byte* bytes = ourBuffer) 
                *(T*) bytes = value;

            writer.Write(ourBuffer, 0, structSize);
        }
    }
}