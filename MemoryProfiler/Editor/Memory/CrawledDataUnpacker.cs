using System.Collections.Generic;
using System.Linq;
using UnityEditor.MemoryProfiler;
using UnityEngine;

namespace MemoryProfilerWindow
{
    internal class CrawlDataUnpacker
    {
        internal static CrawledMemorySnapshot Unpack(PackedCrawlerData packedCrawlerData)
        {
            var packedSnapshot = packedCrawlerData.packedMemorySnapshot;

            var result = new CrawledMemorySnapshot
            {
                nativeObjects = packedSnapshot.nativeObjects.Select(packedNativeUnityEngineObject => UnpackNativeUnityEngineObject(packedSnapshot, packedNativeUnityEngineObject)).ToArray(),
                managedObjects = packedCrawlerData.managedObjects.Select(pm => UnpackManagedObject(packedSnapshot, pm)).ToArray(),
                gcHandles = packedSnapshot.gcHandles.Select(pgc => UnpackGCHandle(packedSnapshot)).ToArray(),
                staticFields = packedSnapshot.typeDescriptions.Where(t => t.staticFieldBytes != null & t.staticFieldBytes.Length > 0).Select(t => UnpackStaticFields(t)).ToArray(),
                typeDescriptions = packedSnapshot.typeDescriptions,
                managedHeap = packedSnapshot.managedHeapSections,
                nativeTypes = packedSnapshot.nativeTypes,
                virtualMachineInformation = packedSnapshot.virtualMachineInformation
            };

            result.FinishSnapshot();

            var referencesLists = MakeTempLists(result.allObjects);
            var referencedByLists = MakeTempLists(result.allObjects);

            foreach (var connection in packedCrawlerData.connections)
            {
                referencesLists[connection.@from].Add(result.allObjects[connection.to]);
                referencedByLists[connection.to].Add(result.allObjects[connection.@from]);
            }

            for (var i = 0; i != result.allObjects.Length; i++)
            {
                result.allObjects[i].references = referencesLists[i].ToArray();
                result.allObjects[i].referencedBy = referencedByLists[i].ToArray();
            }

            return result;
        }

        private static List<ThingInMemory>[] MakeTempLists(ThingInMemory[] combined)
        {
            var referencesLists = new List<ThingInMemory>[combined.Length];
            for (int i = 0; i != referencesLists.Length; i++)
                referencesLists[i] = new List<ThingInMemory>(4);
            return referencesLists;
        }

        private static StaticFields UnpackStaticFields(TypeDescription typeDescription)
        {
            return new StaticFields()
                   {
                       typeDescription = typeDescription,
                       caption = "static fields of " + typeDescription.name,
                       size = typeDescription.staticFieldBytes.Length
                   };
        }

        private static GCHandle UnpackGCHandle(PackedMemorySnapshot packedSnapshot)
        {
            return new GCHandle() { size = packedSnapshot.virtualMachineInformation.pointerSize, caption = "gchandle" };
        }

        private static ManagedObject UnpackManagedObject(PackedMemorySnapshot packedSnapshot, PackedManagedObject pm)
        {
            var typeDescription = packedSnapshot.typeDescriptions[pm.typeIndex];
            return new ManagedObject() { address = pm.address, size = pm.size, typeDescription = typeDescription, caption = typeDescription.name };
        }

        private static NativeUnityEngineObject UnpackNativeUnityEngineObject(PackedMemorySnapshot packedSnapshot, PackedNativeUnityEngineObject packedNativeUnityEngineObject)
        {
#if UNITY_5_6_OR_NEWER
            var classId = packedNativeUnityEngineObject.nativeTypeArrayIndex;
#else
            var classId = packedNativeUnityEngineObject.classId;
#endif
            var className = packedSnapshot.nativeTypes[classId].name;

            return new NativeUnityEngineObject()
                   {
                       instanceID = packedNativeUnityEngineObject.instanceId,
                       classID = classId,
                       className = className,
                       name = packedNativeUnityEngineObject.name,
                       caption = packedNativeUnityEngineObject.name + "(" + className + ")",
                       size = packedNativeUnityEngineObject.size,
                       isPersistent = packedNativeUnityEngineObject.isPersistent,
                       isDontDestroyOnLoad = packedNativeUnityEngineObject.isDontDestroyOnLoad,
                       isManager = packedNativeUnityEngineObject.isManager,
                       hideFlags = packedNativeUnityEngineObject.hideFlags
                   };
        }
    }

    [System.Serializable]
    internal class PackedCrawlerData
    {
        internal bool valid;
        internal PackedMemorySnapshot packedMemorySnapshot;
        internal StartIndices startIndices;
        internal PackedManagedObject[] managedObjects;
        internal TypeDescription[] typesWithStaticFields;
        internal Connection[] connections;

        internal PackedCrawlerData(PackedMemorySnapshot packedMemorySnapshot)
        {
            this.packedMemorySnapshot = packedMemorySnapshot;
            typesWithStaticFields = packedMemorySnapshot.typeDescriptions.Where(t => t.staticFieldBytes != null && t.staticFieldBytes.Length > 0).ToArray();
            startIndices = new StartIndices(this.packedMemorySnapshot.gcHandles.Length, this.packedMemorySnapshot.nativeObjects.Length, typesWithStaticFields.Length);
            valid = true;
        }
    }

    [System.Serializable]
    internal class StartIndices
    {
        [SerializeField]
        private int _gcHandleCount;
        [SerializeField]
        private int _nativeObjectCount;
        [SerializeField]
        private int _staticFieldsCount;

        internal StartIndices(int gcHandleCount, int nativeObjectCount, int staticFieldsCount)
        {
            _gcHandleCount = gcHandleCount;
            _nativeObjectCount = nativeObjectCount;
            _staticFieldsCount = staticFieldsCount;
        }

        internal int OfFirstGCHandle { get { return 0; } }
        internal int OfFirstNativeObject { get { return OfFirstGCHandle + _gcHandleCount; } }
        internal int OfFirstStaticFields { get { return OfFirstNativeObject + _nativeObjectCount; } }
        internal int OfFirstManagedObject { get { return OfFirstStaticFields + _staticFieldsCount; } }
    }
}
