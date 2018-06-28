using System;
using UnityEditor.MemoryProfiler;
using UnityEngine;

namespace MemoryProfilerWindow
{
    internal class PrimitiveValueReader
    {
        private readonly VirtualMachineInformation _virtualMachineInformation;
        private readonly MemorySection[] _heapSections;

        internal PrimitiveValueReader(VirtualMachineInformation virtualMachineInformation, MemorySection[] heapSections)
        {
            _virtualMachineInformation = virtualMachineInformation;
            _heapSections = heapSections;
        }

        internal System.Int32 ReadInt32(BytesAndOffset bo)
        {
            return BitConverter.ToInt32(bo.bytes, bo.offset);
        }

        internal System.UInt32 ReadUInt32(BytesAndOffset bo)
        {
            return BitConverter.ToUInt32(bo.bytes, bo.offset);
        }

        internal System.Int64 ReadInt64(BytesAndOffset bo)
        {
            return BitConverter.ToInt64(bo.bytes, bo.offset);
        }

        internal System.UInt64 ReadUInt64(BytesAndOffset bo)
        {
            return BitConverter.ToUInt64(bo.bytes, bo.offset);
        }

        internal System.Int16 ReadInt16(BytesAndOffset bo)
        {
            return BitConverter.ToInt16(bo.bytes, bo.offset);
        }

        internal System.UInt16 ReadUInt16(BytesAndOffset bo)
        {
            return BitConverter.ToUInt16(bo.bytes, bo.offset);
        }

        internal System.Byte ReadByte(BytesAndOffset bo)
        {
            return bo.bytes[bo.offset];
        }

        internal System.SByte ReadSByte(BytesAndOffset bo)
        {
            return (System.SByte)bo.bytes[bo.offset];
        }

        internal System.Boolean ReadBool(BytesAndOffset bo)
        {
            return ReadByte(bo) != 0;
        }

        internal UInt64 ReadPointer(BytesAndOffset bo)
        {
            if (_virtualMachineInformation.pointerSize == 4)
                return ReadUInt32(bo);
            else
                return ReadUInt64(bo);
        }

        internal UInt64 ReadPointer(UInt64 address)
        {
            return ReadPointer(_heapSections.Find(address, _virtualMachineInformation));
        }

        internal Char ReadChar(BytesAndOffset bytesAndOffset)
        {
            return System.Text.Encoding.Unicode.GetChars(bytesAndOffset.bytes, bytesAndOffset.offset, 2)[0];
        }

        internal System.Single ReadSingle(BytesAndOffset bytesAndOffset)
        {
            return BitConverter.ToSingle(bytesAndOffset.bytes, bytesAndOffset.offset);
        }

        internal System.Double ReadDouble(BytesAndOffset bytesAndOffset)
        {
            return BitConverter.ToDouble(bytesAndOffset.bytes, bytesAndOffset.offset);
        }
    }
}
