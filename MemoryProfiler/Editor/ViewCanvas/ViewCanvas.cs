using UnityEngine;
using UnityEditor;

namespace MemoryProfilerWindow
{
    internal class ViewCanvas
    {
        private MemoryProfilerWindow _hostWindow;
        private int _selectedTab = 0;
        internal Rect _canvas;

        internal ViewCanvas(MemoryProfilerWindow hostWindow)
        {
            _hostWindow = hostWindow;
        }

        internal void Draw()
        {
            if (_hostWindow == null)
                return;

            _canvas = new Rect(0f, _hostWindow.topMargin, _hostWindow.position.width - _hostWindow._inspector.width, _hostWindow.position.height - (_hostWindow.topMargin * 2));
            GUILayout.BeginArea(_canvas);
            GUILayout.BeginHorizontal();

            _selectedTab = GUILayout.Toolbar(_selectedTab, new string[] { "TreeMap", "Spreadsheet" });
            switch (_selectedTab)
            {
                case 0:
                    if (_hostWindow._treeMapView != null)
                        _hostWindow._treeMapView.Draw();
                    break;
                case 1:
                    if (_hostWindow._spreadsheetView != null)
                        _hostWindow._spreadsheetView.Draw();
                    break;
                default:
                    break;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}