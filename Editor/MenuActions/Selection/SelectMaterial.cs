using UnityEngine;
using UnityEditor;
using UnityEditor.ProBuilder.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.ProBuilder;
using UnityEditor.ProBuilder;
using EditorGUILayout = UnityEditor.EditorGUILayout;
using EditorStyles = UnityEditor.EditorStyles;

namespace UnityEditor.ProBuilder.Actions
{
	class SelectMaterial : MenuAction
	{
		public override ToolbarGroup group { get { return ToolbarGroup.Selection; } }
		public override Texture2D icon { get { return IconUtility.GetIcon("Toolbar/Selection_SelectByMaterial", IconSkin.Pro); } }
		public override TooltipContent tooltip { get { return _tooltip; } }

		static readonly TooltipContent _tooltip = new TooltipContent
		(
			"Select by Material",
			"Selects all faces matching the selected materials."
		);

		GUIContent gc_restrictToSelection = new GUIContent("Current Selection", "Optionally restrict the matches to only those faces on currently selected objects.");

		public override bool IsEnabled()
		{
			return 	ProBuilderEditor.instance != null &&
					ProBuilderEditor.instance.editLevel != EditLevel.Top &&
					selection != null &&
					selection.Length > 0 &&
					selection.Any(x => x.SelectedFaceCount > 0);
		}

		public override bool IsHidden()
		{
			return 	editLevel != EditLevel.Geometry;
		}

		public override MenuActionState AltState()
		{
			if(	IsEnabled() &&
				ProBuilderEditor.instance.editLevel == EditLevel.Geometry &&
				ProBuilderEditor.instance.selectionMode == SelectMode.Face)
				return MenuActionState.VisibleAndEnabled;

			return MenuActionState.Visible;
		}

		public override void OnSettingsGUI()
		{
			GUILayout.Label("Select Material Options", EditorStyles.boldLabel);

			EditorGUI.BeginChangeCheck();

			bool restrictToSelection = PreferencesInternal.GetBool("pb_restrictSelectMaterialToCurrentSelection");
			restrictToSelection = EditorGUILayout.Toggle(gc_restrictToSelection, restrictToSelection);

			if( EditorGUI.EndChangeCheck() )
				PreferencesInternal.SetBool("pb_restrictSelectMaterialToCurrentSelection", restrictToSelection);

			GUILayout.FlexibleSpace();

			if(GUILayout.Button("Select Faces with Material"))
			{
				DoAction();
				SceneView.RepaintAll();
			}
		}

		public override ActionResult DoAction()
		{
			UndoUtility.RecordSelection(selection, "Select Faces with Material");

			bool restrictToSelection = PreferencesInternal.GetBool("pb_restrictSelectMaterialToCurrentSelection");

			HashSet<Material> sel = new HashSet<Material>(selection.SelectMany(x => x.SelectedFaces.Select(y => y.material).Where( z => z != null)));
			List<GameObject> newSelection = new List<GameObject>();

			foreach(pb_Object pb in restrictToSelection ? selection : Object.FindObjectsOfType<pb_Object>())
			{
				IEnumerable<pb_Face> matches = pb.faces.Where(x => sel.Contains(x.material));

				if(matches.Count() > 0)
				{
					newSelection.Add(pb.gameObject);
					pb.SetSelectedFaces(matches);
				}
			}

			Selection.objects = newSelection.ToArray();

			ProBuilderEditor.Refresh();

			return new ActionResult(Status.Success, "Select Faces with Material");
		}
	}
}


