using Treemap;
using UnityEditor;
using UnityEngine;
using System;
using UnityEditor.MemoryProfiler;

namespace MemoryProfilerWindow
{
    public class MemoryProfilerWindow : EditorWindow
    {
        [NonSerialized]
        private UnityEditor.MemoryProfiler.PackedMemorySnapshot _snapshot;

        [SerializeField]
        private PackedCrawlerData _packedCrawled;

        [NonSerialized]
        private CrawledMemorySnapshot _unpackedCrawl;

        [NonSerialized]
        private CrawledMemorySnapshot _previousUnpackedCrawl;

        private Vector2 _scrollPosition;

        [NonSerialized]
        private bool _registered = false;
        internal float topMargin = 25f;
        internal Inspector _inspector;
        internal TreeMapView _treeMapView;
        internal SpreadsheetView _spreadsheetView;
        internal ViewCanvas _viewCanvas;
        private FilterItems _filterItems;

        [MenuItem("Window/MemoryProfiler")]
        private static void Create()
        {
            EditorWindow.GetWindow<MemoryProfilerWindow>();
        }

        internal void OnDestroy()
        {
            UnityEditor.MemoryProfiler.MemorySnapshot.OnSnapshotReceived -= IncomingSnapshot;

            if (_treeMapView != null)
                _treeMapView.CleanupMeshes ();
        }

        internal void Initialize()
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

        private void OnGUI()
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

            if (_viewCanvas != null)
                _viewCanvas.Draw();

            if (_filterItems != null)
                _filterItems.Draw();

            if (_inspector != null)
                _inspector.Draw();
        }

        internal void SelectThing(ThingInMemory thing)
        {
            _inspector.SelectThing(thing);
            _treeMapView.SelectThing(thing);
        }

        internal void SelectGroup(Group group)
        {
            _treeMapView.SelectGroup(group);
        }

        private void Unpack()
        {
            _previousUnpackedCrawl = _unpackedCrawl;
            _unpackedCrawl = CrawlDataUnpacker.Unpack(_packedCrawled);
            _inspector = new Inspector(this, _unpackedCrawl, _snapshot);
            _filterItems = new FilterItems(this, _unpackedCrawl, _previousUnpackedCrawl);

            if (_treeMapView != null)
                _treeMapView.Setup(this, _unpackedCrawl);
            if (_spreadsheetView != null)
                _spreadsheetView.Setup(this, _unpackedCrawl);
            if (_viewCanvas == null)
                _viewCanvas = new ViewCanvas(this);
        }

        private void IncomingSnapshot(PackedMemorySnapshot snapshot)
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
