using UnityEngine;
using System;
using UnityEditor;

namespace MemoryProfilerWindow
{
    public class SpreadsheetView
    {
        CrawledMemorySnapshot m_unpackedCrawl;
        MemoryProfilerWindow m_hostWindow;

        private Table m_table;


        enum ThingType
        {
            NativeUnityEngineObject = 0,
            ManagedObject,
            GCHandle,
            StaticFields,
            Count
        }

        string ThingTypeToString(ThingType t)
        {
            switch (t)
            {
                case ThingType.NativeUnityEngineObject: return "Native";
                case ThingType.ManagedObject: return "Managed";
                case ThingType.GCHandle: return "GCHandle";
                case ThingType.StaticFields: return "StaticFields";
                default: return null;
            }
        }

        enum Column
        {
            Name = 0,
            Type,
            Size,
            ClassName,
            InstanceID,
            Count
        }

        string ColumnName(Column col)
        {
            switch (col)
            {
                case Column.Name: return "Name";
                case Column.Type: return "Type";
                case Column.Size: return "Size";
                case Column.ClassName: return "Class Name";
                case Column.InstanceID: return "Instance ID";
                default: return null;
            }
        }

        public SpreadsheetView()
        {
            m_table = new Table();
            m_table.numColumnsCallback = NumColumns;
            m_table.labelForColumnCallback = LabelForColumn;
            m_table.numRowsCallback = NumRows;
            m_table.idSelectedCallback = TableIDSelected;
            m_table.dataForGridCallback = DataForGrid;
            m_table.compareDataCallback = CompareData;
            m_table.widthForColumnCallback = WidthForColumn;
        }

        public void Setup(MemoryProfilerWindow hostWindow, CrawledMemorySnapshot unpackedCrawl)
        {
            this.m_unpackedCrawl = unpackedCrawl;
            this.m_hostWindow = hostWindow;
            m_table.InvalidateData();
        }

        public void Draw()
        {
            if (m_hostWindow == null)
                return;

            Rect r = new Rect(m_hostWindow._viewCanvas._canvas.x, m_hostWindow._viewCanvas._canvas.y, m_hostWindow._viewCanvas._canvas.width, m_hostWindow._viewCanvas._canvas.height - m_hostWindow.topMargin);
            GUILayout.BeginArea(r);

            m_table.DoGUI();

            GUILayout.EndArea();
        }

        ThingType indexToThingType(int id)
        {
            int lenGC = m_unpackedCrawl.gcHandles.Length;
            int lenNative = lenGC + m_unpackedCrawl.nativeObjects.Length;
            int lenStatic = lenNative + m_unpackedCrawl.staticFields.Length;

            if (id < lenGC)
                return ThingType.GCHandle;
            else if (id < lenNative)
                return ThingType.NativeUnityEngineObject;
            else if (id < lenStatic)
                return ThingType.StaticFields;
            else
                return ThingType.ManagedObject;
        }

        GUIContent DataForGrid(int id, int col)
        {
            string str = "undefined";
            ThingInMemory thing = m_unpackedCrawl.allObjects[id];
            var nativeObject = thing as NativeUnityEngineObject;

            switch ((Column)col)
            {
                case Column.Name:
                    if (nativeObject != null) str = nativeObject.name;
                    if (str.Equals("")) str = "unnamed";
                    if (thing.ignored) str = "<filtered>";
                    break;
                case Column.Type:
                    str = ThingTypeToString(indexToThingType(id));
                    if (thing.ignored) str = "<filtered>";
                    break;
                case Column.Size:
                    str = EditorUtility.FormatBytes(thing.size);
                    if (thing.ignored) str = "<filtered>";
                    break;
                case Column.ClassName:
                    if (nativeObject != null) str = nativeObject.className;
                    if (thing.ignored) str = "<filtered>";
                    break;
                case Column.InstanceID:
                    if (nativeObject != null) str = nativeObject.instanceID.ToString();
                    if (thing.ignored) str = "<filtered>";
                    break;
            }

            return new GUIContent(str);
        }

        int CompareData(int id1, int id2, int col)
        {
            ThingInMemory thing1 = m_unpackedCrawl.allObjects[id1];
            var nativeObject1 = thing1 as NativeUnityEngineObject;

            ThingInMemory thing2 = m_unpackedCrawl.allObjects[id2];
            var nativeObject2 = thing2 as NativeUnityEngineObject;

            switch ((Column)col)
            {
                case Column.Name:
                    if (nativeObject1 != null && nativeObject2 != null)
                        return nativeObject1.name.CompareTo(nativeObject2.name);
                    else if (nativeObject1 != null)
                        return -1;
                    else if (nativeObject2 != null)
                        return 1;
                    else
                        return 0;
                case Column.Type:
                    // this depends on the fact that HighLevelAPI.allGameObjects concatenates objects in the order we want
                    return id1 - id2;
                case Column.Size:
                    return -thing1.size.CompareTo(thing2.size);
                case Column.ClassName:
                    if (nativeObject1 != null && nativeObject2 != null)
                        return nativeObject1.className.CompareTo(nativeObject2.className);
                    else if (nativeObject1 != null)
                        return -1;
                    else if (nativeObject2 != null)
                        return 1;
                    else
                        return 0;
                case Column.InstanceID:
                    if (nativeObject1 != null && nativeObject2 != null)
                        return nativeObject1.instanceID.CompareTo(nativeObject2.instanceID);
                    else if (nativeObject1 != null)
                        return -1;
                    else if (nativeObject2 != null)
                        return 1;
                    else
                        return 0;
            }
            return 0;
        }

        void TableIDSelected(int id)
        {
            m_hostWindow.SelectThing(m_unpackedCrawl.allObjects[id]);
        }

        int NumColumns()
        {
            return (int)Column.Count;
        }

        int NumRows()
        {
            return m_unpackedCrawl.allObjects.Length;
        }

        GUIContent LabelForColumn(int c)
        {
            return new GUIContent(ColumnName((Column)c));
        }

        float WidthForColumn(int c)
        {
            switch ((Column)c)
            {
                case Column.Type:
                    return 80;
                case Column.Size:
                    return 80f;
                case Column.ClassName:
                    return 150f;
                case Column.InstanceID:
                    return 100f;
                default:
                    return 0f;
            }
        }
    }
}