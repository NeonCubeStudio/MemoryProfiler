using UnityEngine;
using System.Collections;
using UnityEditor;
using System;

namespace MemoryProfilerWindow
{
	public delegate int NumColumnsCallback();
	public delegate int NumRowsCallback();
	public delegate GUIContent LabelForColumnCallback(int col);
	public delegate float WidthForColumnCallback(int col);
	public delegate GUIContent DataForGridCallback(int id, int col);
	public delegate int CompareDataCallback(int id1, int id2, int col);
	public delegate void IDSelectedCallback(int id);

    public class Table
    {
		// Profiling GUI constants
		const float kRowHeight = 16;
		const float kFoldoutSize = 14;
		const float kIndent = 16;
		const float kSmallMargin = 4;
		const float kBaseIndent = 4;
		const float kInstrumentationButtonWidth = 30;
		const float kInstrumentationButtonOffset = 5;
		const int kFirst = -999999;
		const int kLast = 999999;
		const float kScrollbarWidth = 16;

		protected class Styles
		{
			public GUIStyle background = "OL Box";
			public GUIStyle header = "OL title";
			public GUIStyle rightHeader = "OL title TextRight";
			public GUIStyle entryEven = "OL EntryBackEven";
			public GUIStyle entryOdd = "OL EntryBackOdd";
			public GUIStyle numberLabel = "OL Label";
			public GUIStyle foldout = "IN foldout";
			public GUIStyle miniPullDown = "MiniPullDown";
			public GUIContent disabledSearchText = new GUIContent("Showing search results are disabled while recording with deep profiling.\nStop recording to view search results.");
			public GUIContent notShowingAllResults = new GUIContent("...", "Narrow your search. Not all search results can be shown.");
			public GUIContent instrumentationIcon = EditorGUIUtility.IconContent("Profiler.Record", "Record|Record profiling information");
		}

		protected static Styles ms_Styles;
		protected static Styles styles
		{
			get { return ms_Styles ?? (ms_Styles = new Styles()); }
		}

		public NumColumnsCallback numColumnsCallback = () => 0;
		public NumRowsCallback numRowsCallback = () => 0;
		public LabelForColumnCallback labelForColumnCallback = c => GUIContent.none;
		public DataForGridCallback dataForGridCallback = (r, c) => null;
		public CompareDataCallback compareDataCallback = (id1, id2, c) => 0;
		public IDSelectedCallback idSelectedCallback = r => { };
		public WidthForColumnCallback widthForColumnCallback = null;

		int m_sortIndex = 0;
		Rect m_tableRect;
		float m_scrollViewHeight;
		Vector2 m_scrollPosition = Vector2.zero;
		float[] m_columnWidths = null;
		GUIContent[] m_columnLabels = null;
		GUIContent[,] m_grid = null;
		int[] m_sortedIDs = null;
		int m_numColumns;
		int m_numRows;
		int m_selectedID = -1;

		public void InvalidateData()
		{
			m_columnWidths = null;
			m_columnLabels = null;
			m_grid = null;
			m_sortedIDs = null;
			m_numColumns = 0;
			m_numRows = 0;
		}

		void SortIDs()
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

		void ClickOutsideToDeselect()
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

		void DrawColumnsHeader()
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

		float TextDimensionsForColumnHeader(int col)
		{
			if (widthForColumnCallback != null)
				return widthForColumnCallback(col);
			else
				return GUI.skin.label.CalcSize(labelForColumnCallback(col)).x + 20f;
		}

		void DrawTitle(GUIContent content, int index, bool setup)
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

		Rect GetRowRect(int rowIndex)
		{
			return new Rect(1, kRowHeight* rowIndex, m_tableRect.width, kRowHeight);
		}

		GUIStyle GetRowBackgroundStyle(int rowIndex)
		{
			return (rowIndex % 2 == 0 ? styles.entryEven : styles.entryOdd);
		}

		GUIContent dataForGrid(int id, int col)
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
		
		void DrawRow(int row, bool selected)
		{
			Event evt = Event.current;

			var rowRect = GetRowRect(row);
			var curRect = rowRect;

			var background = GetRowBackgroundStyle(row);

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

		bool isRowVisible(int row)
		{
			if (Event.current.type != EventType.Layout && m_scrollViewHeight == 0f)
				return true;
			float yPos = row * kRowHeight;
			return m_scrollPosition.y - kRowHeight <= yPos && yPos <= m_scrollPosition.y + m_scrollViewHeight;
		}

		void DrawRows()
		{
			m_tableRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.MinHeight(kRowHeight* m_numRows));
			for (int r=0; r<m_numRows; r++)
			{
				bool selected = m_selectedID == m_sortedIDs[r];
				if (isRowVisible(r))
					DrawRow(r, selected);
			}
		}

		public void DoGUI()
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