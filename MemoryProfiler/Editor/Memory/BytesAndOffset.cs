using System;
using UnityEditor.MemoryProfiler;
using UnityEngine;

namespace MemoryProfilerWindow
{
    internal struct BytesAndOffset
    {
        internal byte[] bytes;
        internal int offset;
        internal int pointerSize;
        internal bool IsValid { get { return bytes != null; }}

        internal UInt64 ReadPointer()
        {
            if (pointerSize == 4)
                return BitConverter.ToUInt32(bytes, offset);
            if (pointerSize == 8)
                return BitConverter.ToUInt64(bytes, offset);
            throw new ArgumentException("Unexpected pointersize: " + pointerSize);
        }

        internal Int32 ReadInt32()
        {
            return BitConverter.ToInt32(bytes, offset);
        }

        internal Int64 ReadInt64()
        {
            return BitConverter.ToInt64(bytes, offset);
        }

        internal BytesAndOffset Add(int add)
        {
            return new BytesAndOffset() {bytes = bytes, offset = offset + add, pointerSize = pointerSize};
        }

        internal void WritePointer(UInt64 value)
        {
            for (int i = 0; i < pointerSize; i++)
            {
                bytes[i + offset] = (byte)value;
                value >>= 8;
            }
        }

        internal BytesAndOffset NextPointer()
        {
            return Add(pointerSize);
        }
    }
}
