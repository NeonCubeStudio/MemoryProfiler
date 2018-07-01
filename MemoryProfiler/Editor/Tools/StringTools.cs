#if UNITY_EDITOR
using UnityEditor.MemoryProfiler;

namespace MemoryProfilerWindow
{
    internal static class StringTools
    {
        internal static string ReadString(BytesAndOffset bo, VirtualMachineInformation virtualMachineInformation)
        {
            BytesAndOffset lengthPointer = bo.Add(virtualMachineInformation.objectHeaderSize);
            int length = lengthPointer.ReadInt32();
            BytesAndOffset firstChar = lengthPointer.Add(4);

            return System.Text.Encoding.Unicode.GetString(firstChar.bytes, firstChar.offset, length * 2);
        }

        internal static int ReadStringObjectSizeInBytes(BytesAndOffset bo, VirtualMachineInformation virtualMachineInformation)
        {
            BytesAndOffset lengthPointer = bo.Add(virtualMachineInformation.objectHeaderSize);
            int length = lengthPointer.ReadInt32();

            return virtualMachineInformation.objectHeaderSize + /*lengthfield*/ 1 + (length * /*utf16=2bytes per char*/ 2) + /*2 zero terminators*/ 2;
        }
    }
}
#endif