using UnityEngine;
using UnityEditor;
using UnityEditor.ProBuilder.UI;
using System.Linq;
using UnityEngine.ProBuilder;
using UnityEditor.ProBuilder;

namespace UnityEditor.ProBuilder.Actions
{
	class SelectEdgeRing : MenuAction
	{
		public override ToolbarGroup group { get { return ToolbarGroup.Selection; } }
		public override Texture2D icon { get { return IconUtility.GetIcon("Toolbar/Selection_Ring", IconSkin.Pro); } }
		public override TooltipContent tooltip { get { return m_Tooltip; } }
		public override int toolbarPriority { get { return 2; } }
		public override bool hasFileMenuEntry { get { return false; } }

		private static readonly TooltipContent m_Tooltip = new TooltipContent
		(
			"Select Edge Ring",
			"Selects a ring of edges.  Ringed edges are opposite the selected edge.\n\n<b>Shortcut</b>: Shift + Double-Click on Edge",
			CMD_ALT, 'R'
		);

		public override bool IsEnabled()
		{
			return 	ProBuilderEditor.instance != null &&
					editLevel == EditLevel.Geometry &&
					selectionMode == SelectMode.Edge &&
					selection != null &&
					selection.Length > 0 &&
					selection.Sum(x => x.SelectedEdgeCount) > 0;
		}

		public override bool IsHidden()
		{
			return 	ProBuilderEditor.instance == null ||
					editLevel != EditLevel.Geometry ||
					selectionMode != SelectMode.Edge;
		}

		public override ActionResult DoAction()
		{
			return MenuCommands.MenuRingSelection(selection);
		}
	}
}
