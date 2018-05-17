﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace ProBuilder.AssetUtility
{
	class AssetIdRemapBuilderTreeView : TreeView
	{
		AssetIdRemapObject m_RemapObject = null;
		const float k_RowHeight = 20f;
		const float k_RowHeightSearching = 76f;

		public bool isDirty = false;

		public AssetIdRemapObject remapObject
		{
			get { return m_RemapObject; }
			set { m_RemapObject = value; }
		}

		public AssetIdRemapBuilderTreeView(TreeViewState state, MultiColumnHeader header)
			: base(state, header)
		{
			rowHeight = 20f;
			showAlternatingRowBackgrounds = true;
			showBorder = true;
			extraSpaceBeforeIconAndLabel = 18f;
		}

		protected override TreeViewItem BuildRoot()
		{
			StringTupleTreeElement root = new StringTupleTreeElement(0, -1, -1, "Root", "", "");

			var all = new List<TreeViewItem>();

			int index = 1;

			for (int i = 0, c = remapObject.map.Count; i < c; i++)
			{
				all.Add(new StringTupleTreeElement(index++, 0, i, "Remap Entry", remapObject[i].source.name, remapObject[i].destination.name));
				all.Add(new StringTupleTreeElement(index++, 1, i, "Local Path", remapObject[i].source.localPath, remapObject[i].destination.localPath));
				all.Add(new StringTupleTreeElement(index++, 1, i, "GUID", remapObject[i].source.guid, remapObject[i].destination.guid));
				all.Add(new StringTupleTreeElement(index++, 1, i, "File ID", remapObject[i].source.fileId, remapObject[i].destination.fileId));
				all.Add(new StringTupleTreeElement(index++, 1, i, "Type", remapObject[i].source.type, remapObject[i].destination.type));
			}

			SetupParentsAndChildrenFromDepths(root, all);
			return root;
		}

		public void SetRowHeight()
		{
			rowHeight = hasSearch ? k_RowHeightSearching : k_RowHeight;
		}

		protected override void RowGUI(RowGUIArgs args)
		{
			StringTupleTreeElement item = args.item as StringTupleTreeElement;

			for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
			{
				CellGUI(args.GetCellRect(i), item, i, ref args);
			}
		}

		GUIContent m_CellContents = new GUIContent();

		void CellGUI(Rect rect, StringTupleTreeElement item, int visibleColumn, ref RowGUIArgs args)
		{
			if (hasSearch)
			{
				AssetId id = visibleColumn == 0 ? m_RemapObject[item.index].source : m_RemapObject[item.index].destination;

				m_CellContents.text = "<b>Name: </b>" + id.name +
					"\n<b>Path: </b>" + id.localPath +
					"\n<b>Guid: </b>" + id.guid +
					"\n<b>FileId: </b>" + id.fileId +
					"\n<b>Type: </b>" + id.type + (string.IsNullOrEmpty(id.assetType) ? "" : " (" + id.assetType + ")");
			}
			else
			{
				m_CellContents.text = item.GetLabel(visibleColumn);
			}

			rect.x += foldoutWidth + 4;
			rect.width -= (foldoutWidth + 4);

			if (hasSearch)
			{
				float textHeight = GUI.skin.label.CalcHeight(m_CellContents, rect.width);
				rect.y += (rect.height - textHeight) * .5f;
				rect.height = textHeight;
			}
			else
			{
				CenterRectUsingSingleLineHeight(ref rect);
			}

			GUI.skin.label.richText = true;
			GUI.Label(rect, m_CellContents);
		}

		protected override bool DoesItemMatchSearch(TreeViewItem element, string search)
		{
			StringTupleTreeElement tup = element as StringTupleTreeElement;

			if (tup == null || tup.depth > 0)
				return false;

			var o = m_RemapObject[tup.index];

			try
			{
				Regex pattern = new Regex(search);

				return pattern.IsMatch(o.source.localPath) ||
					pattern.IsMatch(o.source.guid) ||
					pattern.IsMatch(o.source.fileId) ||
					pattern.IsMatch(o.source.type) ||
					pattern.IsMatch(o.destination.localPath) ||
					pattern.IsMatch(o.destination.guid) ||
					pattern.IsMatch(o.destination.fileId) ||
					pattern.IsMatch(o.destination.type);
			}
			catch
			{
				return o.source.localPath.Contains(search) ||
					o.source.guid.Contains(search) ||
					o.source.fileId.Contains(search) ||
					o.source.type.Contains(search) ||
					o.destination.localPath.Contains(search) ||
					o.destination.guid.Contains(search) ||
					o.destination.fileId.Contains(search) ||
					o.destination.type.Contains(search);
			}
		}

		protected override void ContextClicked()
		{
			GenericMenu menu = new GenericMenu();

			menu.AddItem(new GUIContent("Combine", ""), false, () =>
			{
				m_RemapObject.Merge(GetAssetIdSelection());
				isDirty = true;
				Reload();
				SetSelection(new int[] {});
			});

			menu.AddSeparator("");

			menu.AddItem(new GUIContent("Compare", ""), false, () =>
			{
				IList<int> selected = GetSelection();
				if (selected.Count == 2)
				{
					StringTupleTreeElement a = FindItem(selected[0], rootItem) as StringTupleTreeElement;
					StringTupleTreeElement b = FindItem(selected[1], rootItem) as StringTupleTreeElement;

					if (a != null && b != null)
					{
						AssetId left = m_RemapObject[a.index].source;
						AssetId right = m_RemapObject[b.index].destination;
						Debug.Log(left.AssetEquals2(right).ToString());
						return;
					}
				}

				Debug.Log("Compare requires exactly two items be selected.");
			});

			menu.ShowAsContext();
		}

		IEnumerable<AssetIdentifierTuple> GetAssetIdSelection()
		{
			return GetSelection().Select(x => m_RemapObject[((StringTupleTreeElement)FindItem(x, rootItem)).index]);
		}

		protected override void CommandEventHandling()
		{
			var evt = Event.current;

			var selected = GetSelection();

			if (selected.Count > 0)
			{
				if (evt.type == EventType.ValidateCommand)
				{
					if (evt.commandName.Equals("SoftDelete"))
						evt.Use();

				}
				else if (evt.type == EventType.ExecuteCommand)
				{
					if (evt.commandName.Equals("SoftDelete")
						&& EditorUtility.DisplayDialog("Delete", "Delete Selected Entries?", "Delete", "Cancel"))
					{
						m_RemapObject.Delete(GetAssetIdSelection());
						evt.Use();
						Reload();
						isDirty = true;
						SetSelection(new int[] {});
					}
				}
			}

			base.CommandEventHandling();
		}
	}

	class StringTupleTreeElement : TreeViewItem
	{
		public string item1;
		public string item2;
		public int index;

		public StringTupleTreeElement(int id, int depth, int sourceIndex, string displayName, string key, string value) : base(id, depth, displayName)
		{
			item1 = key;
			item2 = value;
			index = sourceIndex;
		}

		public string GetLabel(int column)
		{
			return column < 1 ? item1 : item2;
		}
	}
}
