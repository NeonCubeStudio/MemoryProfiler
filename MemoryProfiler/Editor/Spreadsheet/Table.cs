using UnityEngine;
using System.Collections;
using UnityEditor;
using System;

namespace MemoryProfilerWindow
{
	internal delegate int NumColumnsCallback();
    internal delegate int NumRowsCallback();
    internal delegate GUIContent LabelForColumnCallback(int col);
    internal delegate float WidthForColumnCallback(int col);
    internal delegate GUIContent DataForGridCallback(int id, int col);
    internal delegate int CompareDataCallback(int id1, int id2, int col);
    internal delegate void IDSelectedCallback(int id);

    internal class Table
    {
		// Profiling GUI constants
		private const float kRowHeight = 16;
        private const float kFoldoutSize = 14;
        private const float kIndent = 16;
        private const float kSmallMargin = 4;
        private const float kBaseIndent = 4;
        private const float kInstrumentationButtonWidth = 30;
        private const float kInstrumentationButtonOffset = 5;
        private const int kFirst = -999999;
        private const int kLast = 999999;
        private const float kScrollbarWidth = 16;

		protected class Styles
		{
            internal GUIStyle background = "OL Box";
            internal GUIStyle header = "OL title";
            internal GUIStyle rightHeader = "OL title TextRight";
            internal GUIStyle entryEven = "OL EntryBackEven";
            internal GUIStyle entryOdd = "OL EntryBackOdd";
            internal GUIStyle numberLabel = "OL Label";
            internal GUIStyle foldout = "IN foldout";
            internal GUIStyle miniPullDown = "MiniPullDown";
            internal GUIContent disabledSearchText = new GUIContent("Showing search results are disabled while recording with deep profiling.\nStop recording to view search results.");
            internal GUIContent notShowingAllResults = new GUIContent("...", "Narrow your search. Not all search results can be shown.");
            internal GUIContent instrumentationIcon = EditorGUIUtility.IconContent("Profiler.Record", "Record|Record profiling information");
		}

		protected static Styles ms_Styles;
		protected static Styles styles
		{
			get { return ms_Styles ?? (ms_Styles = new Styles()); }
		}

        internal NumColumnsCallback numColumnsCallback = () => 0;
        internal NumRowsCallback numRowsCallback = () => 0;
        internal LabelForColumnCallback labelForColumnCallback = c => GUIContent.none;
        internal DataForGridCallback dataForGridCallback = (r, c) => null;
        internal CompareDataCallback compareDataCallback = (id1, id2, c) => 0;
        internal IDSelectedCallback idSelectedCallback = r => { };
        internal WidthForColumnCallback widthForColumnCallback = null;

        private int m_sortIndex = 0;
        private Rect m_tableRect;
        private float m_scrollViewHeight;
        private Vector2 m_scrollPosition = Vector2.zero;
        private float[] m_columnWidths = null;
        private GUIContent[] m_columnLabels = null;
        private GUIContent[,] m_grid = null;
        private int[] m_sortedIDs = null;
        private int m_numColumns;
        private int m_numRows;
        private int m_selectedID = -1;

        internal void InvalidateData()
		{
			m_columnWidths = null;
			m_columnLabels = null;
			m_grid = null;
			m_sortedIDs = null;
			m_numColumns = 0;
			m_numRows = 0;
		}

        private void SortIDs()
		{
			if (m_sortedIDs == null)
			{
				m_sortedIDs = new int[m_numRows];
			}

			for (int r = 0; r<m_numRows; r++)
			{
				m_sortedIDs[r] = r;
			}

			Array.Sort<int>(m_sortedIDs, (id1, id2) => compareDataCallback(id1, id2, m_sortIndex));
		}

        private void ClickOutsideToDeselect()
		{
			Event evt = Event.current;
			if (evt.type == EventType.MouseDown &&
				0 < evt.mousePosition.y &&
				evt.mousePosition.y<Screen.height)
			{
				m_selectedID = -1;
				evt.Use();
			}
		}

        private void DrawColumnsHeader()
		{
			GUILayout.BeginHorizontal();

			bool setup = (m_columnWidths == null);
			if (setup)
			{
				m_columnWidths = new float[m_numColumns];
				m_columnLabels = new GUIContent[m_numColumns];
			}
			
			for (int c=0; c<numColumnsCallback(); c++)
			{
				if (setup)
				{
					m_columnLabels[c] = labelForColumnCallback(c);
				}
				DrawTitle(m_columnLabels[c], c, setup);
			}
			GUILayout.Space(kScrollbarWidth);
			GUILayout.EndHorizontal();
		}

        private float TextDimensionsForColumnHeader(int col)
		{
			if (widthForColumnCallback != null)
				return widthForColumnCallback(col);
			else
				return GUI.skin.label.CalcSize(labelForColumnCallback(col)).x + 20f;
		}

        private void DrawTitle(GUIContent content, int index, bool setup)
		{
			bool isSelected = (m_sortIndex == index);
			float width = 0f;
			if (index != 0)
			{
				if (setup)
				{
					width = TextDimensionsForColumnHeader(index);
					m_columnWidths[index] = width;
				} else
				{
					width = m_columnWidths[index];
				}
			}
			bool didSelectColumn = (index == 0) ? GUILayout.Toggle(isSelected, content, styles.header) : GUILayout.Toggle(isSelected, content, styles.rightHeader, GUILayout.Width(width));
			if (index == 0 && Event.current.type == EventType.Repaint)
			{
				float firstColWidth = GUILayoutUtility.GetLastRect().width;
				m_columnWidths[0] = firstColWidth;
			}


			if (didSelectColumn && m_sortIndex != index)
			{
				m_sortIndex = index;
				SortIDs();
			}
		}

        private Rect GetRowRect(int rowIndex)
		{
			return new Rect(1, kRowHeight* rowIndex, m_tableRect.width, kRowHeight);
		}

        private GUIStyle GetRowBackgroundStyle(int rowIndex)
		{
			return (rowIndex % 2 == 0 ? styles.entryEven : styles.entryOdd);
		}

        private GUIContent dataForGrid(int id, int col)
		{
			if (m_grid == null)
				m_grid = new GUIContent[m_numRows, m_numColumns];
			GUIContent content = m_grid[id, col];
			if (content != null)
				return content;
			else
			{
				content = dataForGridCallback(id, col);
				m_grid[id, col] = content;
				return content;
			}
		}

        private void DrawRow(int row, bool selected)
		{
			Event evt = Event.current;

			Rect rowRect = GetRowRect(row);
			Rect curRect = rowRect;

			GUIStyle background = GetRowBackgroundStyle(row);

			if (evt.type == EventType.Repaint)
			{
				background.Draw(rowRect, GUIContent.none, false, false, selected, false);

				int id = m_sortedIDs[row];

				styles.numberLabel.alignment = TextAnchor.MiddleRight;
				curRect.x += m_columnWidths[0];
				for (int c=1; c<m_numColumns; c++)
				{
					curRect.width = m_columnWidths[c];
					if (c == m_numColumns - 1)
						curRect.width -= kSmallMargin;
					styles.numberLabel.Draw(curRect, dataForGrid(id, c), false, false, false, false);
					curRect.x += m_columnWidths[c];
				}

				curRect.x = kSmallMargin;
				curRect.width = m_columnWidths[0] - kSmallMargin;
				styles.numberLabel.alignment = TextAnchor.MiddleLeft;
				styles.numberLabel.Draw(curRect, dataForGrid(id, 0), false, false, false, false);
			}

			if (evt.type == EventType.MouseDown && rowRect.Contains(evt.mousePosition))
			{
				m_selectedID = m_sortedIDs[row];
				if (idSelectedCallback != null)
				{
					idSelectedCallback(m_selectedID);
				}
				evt.Use();
			}
		}

        private bool isRowVisible(int row)
		{
			if (Event.current.type != EventType.Layout && m_scrollViewHeight == 0f)
				return true;
			float yPos = row * kRowHeight;
			return m_scrollPosition.y - kRowHeight <= yPos && yPos <= m_scrollPosition.y + m_scrollViewHeight;
		}

        private void DrawRows()
		{
			m_tableRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.MinHeight(kRowHeight* m_numRows));
			for (int r=0; r<m_numRows; r++)
			{
				bool selected = m_selectedID == m_sortedIDs[r];
				if (isRowVisible(r))
					DrawRow(r, selected);
			}
		}

        internal void DoGUI()
        {
			m_numColumns = numColumnsCallback();
			m_numRows = numRowsCallback();

			if (m_sortedIDs == null)
				SortIDs();

			DrawColumnsHeader();
			GUILayout.Space(1f);
			m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition);
			DrawRows();
			GUILayout.EndScrollView();
			if (Event.current.type == EventType.Repaint)
				m_scrollViewHeight = GUILayoutUtility.GetLastRect().height;
			ClickOutsideToDeselect();
		}
	}
}