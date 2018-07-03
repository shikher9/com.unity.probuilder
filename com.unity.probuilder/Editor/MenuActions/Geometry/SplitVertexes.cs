using UnityEngine;
using UnityEditor;
using UnityEditor.ProBuilder.UI;
using System.Linq;
using UnityEngine.ProBuilder;
using UnityEditor.ProBuilder;

namespace UnityEditor.ProBuilder.Actions
{
	sealed class SplitVertexes : MenuAction
	{
		public override ToolbarGroup group { get { return ToolbarGroup.Geometry; } }
		public override Texture2D icon { get { return IconUtility.GetIcon("Toolbar/Vert_Split", IconSkin.Pro); } }
		public override TooltipContent tooltip { get { return _tooltip; } }

		static readonly TooltipContent _tooltip = new TooltipContent
		(
			"Split Vertexes",
			@"Disconnects vertexes that share the same position in space so that they may be moved independently of one another.",
			keyCommandAlt, 'X'
		);

		public override bool enabled
		{
			get
			{
				return ProBuilderEditor.instance != null &&
					ProBuilderEditor.instance.editLevel == EditLevel.Geometry &&
					ProBuilderEditor.instance.componentMode == ComponentMode.Vertex &&
					MeshSelection.TopInternal().Any(x => x.selectedVertexCount > 0);
			}
		}

		public override bool hidden
		{
			get
			{
				return ProBuilderEditor.instance == null ||
					ProBuilderEditor.instance.editLevel != EditLevel.Geometry ||
					ProBuilderEditor.instance.componentMode != ComponentMode.Vertex;
			}
		}

		public override ActionResult DoAction()
		{
			return MenuCommands.MenuSplitVertexes(MeshSelection.TopInternal());
		}
	}
}
