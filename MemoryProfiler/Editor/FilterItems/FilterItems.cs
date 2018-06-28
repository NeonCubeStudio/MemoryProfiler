using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MemoryProfilerWindow
{
	public class FilterItems
	{
		[Flags]
		enum Filters
		{
			Everything = 0,
			IgnoreUnused = 1 << 0,
            NewOnly = 1 << 1,
        }

		MemoryProfilerWindow _hostWindow;
		CrawledMemorySnapshot _unpackedCrawl;
        CrawledMemorySnapshot _previousUnpackedCrawl;
        Filters _currentFilter;

        public FilterItems(MemoryProfilerWindow hostWindow, CrawledMemorySnapshot unpackedCrawl, CrawledMemorySnapshot previousUnpackedCrawl)
        {
			this._unpackedCrawl = unpackedCrawl;
            this._previousUnpackedCrawl = previousUnpackedCrawl;
            this._hostWindow = hostWindow;
			this._currentFilter = Filters.Everything;
        }

		public void Draw()
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

		void ApplyFilters()
		{
			foreach (var thing in _unpackedCrawl.allObjects)
				thing.ignored = false;

			if ((_currentFilter & Filters.IgnoreUnused) != 0)
				FilterIgnoreUnused();
            if ((_currentFilter & Filters.NewOnly) != 0)
				FilterNewOnly();

			_hostWindow._treeMapView.RefreshCaches();
            _hostWindow._spreadsheetView.Draw();
		}

		void FilterIgnoreUnused()
		{
			HashSet<ThingInMemory> matches = new HashSet<ThingInMemory>();
			Queue<ThingInMemory> references = new Queue<ThingInMemory>();

			foreach ( var thing in _unpackedCrawl.allObjects )
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
				var thing = references.Dequeue();

				foreach (var item in thing.references)
				{
					if (!matches.Contains(item))
					{
						references.Enqueue(item);
						matches.Add(item);
					}
				}
			}

			foreach (var thing in _unpackedCrawl.allObjects)
			{
				if (!matches.Contains(thing))
					thing.ignored = true;
			}		
		}

        void FilterNewOnly()
		{
            if (_previousUnpackedCrawl == null)
                return;

            HashSet<int> oldNativeIDs = new HashSet<int>(_previousUnpackedCrawl.nativeObjects.Select(obj => obj.instanceID));
			foreach (var thing in _unpackedCrawl.nativeObjects)
			{
				if (oldNativeIDs.Contains(thing.instanceID))
					thing.ignored = true;
			}

			HashSet<UInt64> oldManagedObjects = new HashSet<UInt64>(_previousUnpackedCrawl.managedObjects.Select(obj => obj.address));
			foreach (var thing in _unpackedCrawl.managedObjects)
			{
				if (oldManagedObjects.Contains(thing.address))
					thing.ignored = true;
			}
		}
	}
}