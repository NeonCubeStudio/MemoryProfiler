#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MemoryProfilerWindow
{
	internal class FilterItems
	{
		[Flags]
		private enum Filters
		{
			Everything = 0,
			IgnoreUnused = 1 << 0,
            NewOnly = 1 << 1,
        }

		private MemoryProfilerWindow _hostWindow;
		private CrawledMemorySnapshot _unpackedCrawl;
        private CrawledMemorySnapshot _previousUnpackedCrawl;
        private Filters _currentFilter;

        internal FilterItems(MemoryProfilerWindow hostWindow, CrawledMemorySnapshot unpackedCrawl, CrawledMemorySnapshot previousUnpackedCrawl)
        {
			this._unpackedCrawl = unpackedCrawl;
            this._previousUnpackedCrawl = previousUnpackedCrawl;
            this._hostWindow = hostWindow;
			this._currentFilter = Filters.Everything;
        }

		internal void Draw()
		{
			bool refreshRequired = false;

            GUILayout.BeginArea(new Rect(_hostWindow._viewCanvas._canvas.x, _hostWindow.position.height - _hostWindow._viewCanvas._canvas.y, _hostWindow._viewCanvas._canvas.width, _hostWindow.topMargin));
            GUILayout.BeginHorizontal();
			if (GUILayout.Button("Clear filter"))
			{
				refreshRequired = true;
				_currentFilter = Filters.Everything;
			}

			if (GUILayout.Button("Ignore unused"))
			{
				refreshRequired = true;
				_currentFilter |= Filters.IgnoreUnused;
			}

            if (_previousUnpackedCrawl == null)
            {
                GUI.enabled = false;
            }
            if (GUILayout.Button("New memory"))
			{
				refreshRequired = true;
				_currentFilter |= Filters.NewOnly;
			}
            GUI.enabled = true;

            GUILayout.EndHorizontal();
			GUILayout.EndArea();

			if (refreshRequired)
				ApplyFilters();
		}

		private void ApplyFilters()
		{
			foreach (ThingInMemory thing in _unpackedCrawl.allObjects)
				thing.ignored = false;

			if ((_currentFilter & Filters.IgnoreUnused) != 0)
				FilterIgnoreUnused();
            if ((_currentFilter & Filters.NewOnly) != 0)
				FilterNewOnly();

			_hostWindow._treeMapView.RefreshCaches();
            _hostWindow._spreadsheetView.Draw();
		}

		private void FilterIgnoreUnused()
		{
			HashSet<ThingInMemory> matches = new HashSet<ThingInMemory>();
			Queue<ThingInMemory> references = new Queue<ThingInMemory>();

			foreach (ThingInMemory thing in _unpackedCrawl.allObjects )
			{
				string reason;
				if (_hostWindow._inspector._shortestPathToRootFinder.IsRoot(thing, out reason))
				{
					matches.Add(thing);
					references.Enqueue(thing);
				}
			}

			while (references.Count > 0)
			{
                ThingInMemory thing = references.Dequeue();

				foreach (ThingInMemory item in thing.references)
				{
					if (!matches.Contains(item))
					{
						references.Enqueue(item);
						matches.Add(item);
					}
				}
			}

			foreach (ThingInMemory thing in _unpackedCrawl.allObjects)
			{
				if (!matches.Contains(thing))
					thing.ignored = true;
			}		
		}

        private void FilterNewOnly()
		{
            if (_previousUnpackedCrawl == null)
                return;

            HashSet<int> oldNativeIDs = new HashSet<int>(_previousUnpackedCrawl.nativeObjects.Select(obj => obj.instanceID));
			foreach (NativeUnityEngineObject thing in _unpackedCrawl.nativeObjects)
			{
				if (oldNativeIDs.Contains(thing.instanceID))
					thing.ignored = true;
			}

			HashSet<UInt64> oldManagedObjects = new HashSet<UInt64>(_previousUnpackedCrawl.managedObjects.Select(obj => obj.address));
			foreach (ManagedObject thing in _unpackedCrawl.managedObjects)
			{
				if (oldManagedObjects.Contains(thing.address))
					thing.ignored = true;
			}
		}
	}
}
#endif