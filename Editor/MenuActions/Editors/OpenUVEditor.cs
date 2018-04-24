using UnityEngine.ProBuilder;
using UnityEditor.ProBuilder;
using UnityEngine;
using UnityEditor;
using UnityEditor.ProBuilder.UI;

namespace UnityEditor.ProBuilder.Actions
{
	class OpenUVEditor : MenuAction
	{
		public override ToolbarGroup group { get { return ToolbarGroup.Tool; } }
		public override Texture2D icon { get { return IconUtility.GetIcon("Toolbar/Panel_UVEditor", IconSkin.Pro); } }
		public override TooltipContent tooltip { get { return _tooltip; } }
		public override bool isProOnly { get { return true; } }

		static readonly TooltipContent _tooltip = new TooltipContent
		(
			"UV Editor",
			"Opens the UV Editor window.\n\nThe UV Editor allows you to change how textures are rendered on this mesh."
		);

		public override bool IsEnabled()
		{
			return 	ProBuilderEditor.instance != null;
		}

		public override ActionResult DoAction()
		{
			UVEditor.MenuOpenUVEditor();
			return new ActionResult(Status.Success, "Open UV Window");
		}
	}
}

