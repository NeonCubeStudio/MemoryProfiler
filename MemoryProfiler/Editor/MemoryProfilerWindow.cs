using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Assets.Editor.Treemap;
using Treemap;
using UnityEditor;
using UnityEngine;
using System;
using System.Net;
using NUnit.Framework.Constraints;
using UnityEditor.MemoryProfiler;
using Object = UnityEngine.Object;
using System.IO;

namespace MemoryProfilerWindow
{
    public class MemoryProfilerWindow : EditorWindow
    {
        [NonSerialized]
        UnityEditor.MemoryProfiler.PackedMemorySnapshot _snapshot;

        [SerializeField]
        PackedCrawlerData _packedCrawled;

        [NonSerialized]
        CrawledMemorySnapshot _unpackedCrawl;

        Vector2 _scrollPosition;

        [NonSerialized]
        private bool _registered = false;
        internal float topMargin = 50f;
        private int _selectedTab = 0;
        public Inspector _inspector;
        TreeMapView _treeMapView;
        SpreadsheetView _spreadsheetView;

        [MenuItem("Window/MemoryProfiler")]
        static void Create()
        {
            EditorWindow.GetWindow<MemoryProfilerWindow>();
        }

        public void OnDestroy()
        {
            UnityEditor.MemoryProfiler.MemorySnapshot.OnSnapshotReceived -= IncomingSnapshot;

            if (_treeMapView != null)
                _treeMapView.CleanupMeshes ();
        }

        public void Initialize()
        {
            if (_treeMapView == null)
                _treeMapView = new TreeMapView ();
            if (_spreadsheetView == null)
                _spreadsheetView = new SpreadsheetView();

            if (!_registered)
            {
                UnityEditor.MemoryProfiler.MemorySnapshot.OnSnapshotReceived += IncomingSnapshot;
                _registered = true;
            }

            if (_unpackedCrawl == null && _packedCrawled != null && _packedCrawled.valid)
                Unpack();


        }

        void OnGUI()
        {
            Initialize();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Take Snapshot"))
            {
                UnityEditor.EditorUtility.DisplayProgressBar("Take Snapshot", "Downloading Snapshot...", 0.0f);
                try
                {
                    UnityEditor.MemoryProfiler.MemorySnapshot.RequestNewSnapshot();
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }

            EditorGUI.BeginDisabledGroup(_snapshot == null);
            if (GUILayout.Button("Save Snapshot..."))
            {
                PackedMemorySnapshotUtility.SaveToFile(_snapshot);
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Load Snapshot..."))
            {
                PackedMemorySnapshot packedSnapshot = PackedMemorySnapshotUtility.LoadFromFile();
                if(packedSnapshot != null)
                    IncomingSnapshot(packedSnapshot);
            }

            GUILayout.EndHorizontal();

            _selectedTab = GUILayout.Toolbar(_selectedTab, new string[] { "TreeMap", "Spreadsheet" } );
            switch (_selectedTab)
            {
                case 0:
                    if (_treeMapView != null)
                        _treeMapView.Draw();
                    break;
                case 1:
                    if (_spreadsheetView != null)
                        _spreadsheetView.Draw();
                    break;
                default:
                    break;
            }

            if (_inspector != null)
                _inspector.Draw();
        }

        public void SelectThing(ThingInMemory thing)
        {
            _inspector.SelectThing(thing);
            _treeMapView.SelectThing(thing);
        }

        public void SelectGroup(Group group)
        {
            _treeMapView.SelectGroup(group);
        }

        void Unpack()
        {
            _unpackedCrawl = CrawlDataUnpacker.Unpack(_packedCrawled);
            _inspector = new Inspector(this, _unpackedCrawl, _snapshot);

            if(_treeMapView != null)
                _treeMapView.Setup(this, _unpackedCrawl);
            if (_spreadsheetView != null)
                _spreadsheetView.Setup(this, _unpackedCrawl);
        }

        void IncomingSnapshot(PackedMemorySnapshot snapshot)
        {
            _snapshot = snapshot;

            UnityEditor.EditorUtility.DisplayProgressBar("Take Snapshot", "Crawling Snapshot...", 0.33f);
            try
            {
                _packedCrawled = new Crawler().Crawl(_snapshot);

                UnityEditor.EditorUtility.DisplayProgressBar("Take Snapshot", "Unpacking Snapshot...", 0.67f);

                Unpack();
            }
            finally
            {
                UnityEditor.EditorUtility.ClearProgressBar();
            }
        }
    }
}
