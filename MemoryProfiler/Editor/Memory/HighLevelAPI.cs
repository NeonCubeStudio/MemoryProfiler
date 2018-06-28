using System;
using UnityEditor.MemoryProfiler;
using UnityEngine;

namespace MemoryProfilerWindow
{
    using System.Linq;

    //this is the highest level dataformat. it can be unpacked from the PackedCrawledMemorySnapshot, which contains all the interesting information we want. The Packed format
    //however is designed to be serializable and relatively storage compact.  This dataformat is designed to give a nice c# api experience. so while the packed version uses typeIndex,
    //this version has TypeReferences,  and also uses references to ThingInObject, instead of the more obscure object indexing pattern that the packed format uses.
    internal class CrawledMemorySnapshot
    {
        internal NativeUnityEngineObject[] nativeObjects;
        internal GCHandle[] gcHandles;
        internal ManagedObject[] managedObjects;
        internal StaticFields[] staticFields;

        //contains concatenation of nativeObjects, gchandles, managedobjects and staticfields
        internal ThingInMemory[] allObjects { get; private set; }
        internal long totalSize { get; private set; }

        internal MemorySection[] managedHeap;
        internal TypeDescription[] typeDescriptions;
        internal PackedNativeType[] nativeTypes;
        internal VirtualMachineInformation virtualMachineInformation;

        internal void FinishSnapshot()
        {
            allObjects = new ThingInMemory[0].Concat(gcHandles).Concat(nativeObjects).Concat(staticFields).Concat(managedObjects).ToArray();
            totalSize = allObjects != null ? allObjects.Sum(o => o.size) : 0;
        }
    }

    internal class ThingInMemory
    {
        internal long size;
        internal string caption;
        internal ThingInMemory[] references;
        internal ThingInMemory[] referencedBy;
        internal bool ignored;
    }

    internal class ManagedObject : ThingInMemory
    {
        internal UInt64 address;
        internal TypeDescription typeDescription;
    }

    internal class NativeUnityEngineObject : ThingInMemory
    {
        internal int instanceID;
        internal int classID;
        internal string className;
        internal string name;
        internal bool isPersistent;
        internal bool isDontDestroyOnLoad;
        internal bool isManager;
        internal HideFlags hideFlags;
    }

    internal class GCHandle : ThingInMemory
    {
    }

    internal class StaticFields : ThingInMemory
    {
        public TypeDescription typeDescription;
        public byte[] storage;
    }
}
