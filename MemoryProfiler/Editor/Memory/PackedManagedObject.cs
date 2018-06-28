using System;

namespace MemoryProfilerWindow
{
    [Serializable]
    internal class PackedManagedObject
    {
        internal UInt64 address;
        internal int typeIndex;
        internal int size;
    }
}
