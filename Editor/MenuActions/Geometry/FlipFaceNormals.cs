using UnityEngine;
using UnityEditor;
using UnityEditor.ProBuilder.UI;
using System.Linq;
using UnityEngine.ProBuilder;
using UnityEditor.ProBuilder;

namespace UnityEditor.ProBuilder.Actions
{
	class FlipFaceNormals : MenuAction
	{
		public override ToolbarGroup group { get { return ToolbarGroup.Geometry; } }
		public override Texture2D icon { get { return IconUtility.GetIcon("Toolbar/Face_FlipNormals", IconSkin.Pro); } }
		public override TooltipContent tooltip { get { return _tooltip; } }

		static readonly TooltipContent _tooltip = new TooltipContent
		(
			"Flip Face Normals",
			@"Reverses the direction of all faces in selection.",
			CMD_ALT, 'N'
		);

		public override bool IsEnabled()
		{
			return 	ProBuilderEditor.instance != null &&
					selection != null &&
					selection.Length > 0 &&
					selection.Any(x => x.SelectedFaceCount > 0);
		}

		public override bool IsHidden()
		{
			return 	ProBuilderEditor.instance == null ||
					ProBuilderEditor.instance.editLevel != EditLevel.Geometry ||
					ProBuilderEditor.instance.selectionMode != SelectMode.Face;
		}

		public override ActionResult DoAction()
		{
			return MenuCommands.MenuFlipNormals(selection);
		}
	}
}
