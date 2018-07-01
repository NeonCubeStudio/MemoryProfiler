#if UNITY_EDITOR
using System;
using UnityEditor.MemoryProfiler;

namespace MemoryProfilerWindow
{
    internal static class ArrayTools
    {
        internal static int ReadArrayLength(MemorySection[] heap, UInt64 address, TypeDescription arrayType, VirtualMachineInformation virtualMachineInformation)
        {
            BytesAndOffset bo = heap.Find(address, virtualMachineInformation);

            UInt64 bounds = bo.Add(virtualMachineInformation.arrayBoundsOffsetInHeader).ReadPointer();

            if (bounds == 0)
#if UNITY_2017_2_OR_NEWER
                return (int)bo.Add(virtualMachineInformation.arraySizeOffsetInHeader).ReadPointer();
#else
                return bo.Add(virtualMachineInformation.arraySizeOffsetInHeader).ReadInt32();
#endif

            BytesAndOffset cursor = heap.Find(bounds, virtualMachineInformation);
            int length = 1;
            for (int i = 0; i != arrayType.arrayRank; i++)
            {
#if UNITY_2017_2_OR_NEWER
                length *= (int)cursor.ReadPointer();
                cursor = cursor.Add(virtualMachineInformation.pointerSize == 4 ? 8 : 16);
#else
                length *= cursor.ReadInt32();
                cursor = cursor.Add(8);
#endif
            }
            return length;
        }

        internal static int ReadArrayObjectSizeInBytes(MemorySection[] heap, UInt64 address, TypeDescription arrayType, TypeDescription[] typeDescriptions, VirtualMachineInformation virtualMachineInformation)
        {
            int arrayLength = ArrayTools.ReadArrayLength(heap, address, arrayType, virtualMachineInformation);
            TypeDescription elementType = typeDescriptions[arrayType.baseOrElementTypeIndex];
            int elementSize = elementType.isValueType ? elementType.size : virtualMachineInformation.pointerSize;
            return virtualMachineInformation.arrayHeaderSize + elementSize * arrayLength;
        }
    }
}
#endif