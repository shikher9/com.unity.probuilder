using UnityEngine;
using UnityEditor;
using UnityEditor.ProBuilder.UI;
using System.Linq;
using UnityEngine.ProBuilder;
using UnityEditor.ProBuilder;
using EditorGUILayout = UnityEditor.EditorGUILayout;
using EditorStyles = UnityEditor.EditorStyles;

namespace UnityEditor.ProBuilder.Actions
{
	class ExtrudeEdges : MenuAction
	{
		public override ToolbarGroup group { get { return ToolbarGroup.Geometry; } }
		public override Texture2D icon { get { return IconUtility.GetIcon("Toolbar/Edge_Extrude", IconSkin.Pro); } }
		public override TooltipContent tooltip { get { return _tooltip; } }
		public override bool hasFileMenuEntry { get { return false; } }

		static readonly TooltipContent _tooltip = new TooltipContent
		(
			"Extrude Edges",
			@"Adds a new face extending from the currently selected edges.  Edges must have an open side to be extruded.",
			CMD_SUPER, 'E'
		);

		public override bool IsEnabled()
		{
			return 	ProBuilderEditor.instance != null &&
					ProBuilderEditor.instance.editLevel == EditLevel.Geometry &&
					ProBuilderEditor.instance.selectionMode == SelectMode.Edge &&
					selection != null &&
					selection.Length > 0 &&
					selection.Any(x => x.SelectedEdgeCount > 0);
		}

		public override bool IsHidden()
		{
			return 	ProBuilderEditor.instance == null ||
					ProBuilderEditor.instance.editLevel != EditLevel.Geometry ||
					ProBuilderEditor.instance.selectionMode != SelectMode.Edge;

		}

		public override MenuActionState AltState()
		{
			return MenuActionState.VisibleAndEnabled;
		}

		public override void OnSettingsGUI()
		{
			GUILayout.Label("Extrude Settings", EditorStyles.boldLabel);

			EditorGUILayout.HelpBox("Extrude Amount determines how far an edge will be moved along it's normal when extruding.  This value can be negative.\n\nExtrude as Group determines whether or not adjacent faces stay attached to one another when extruding.", MessageType.Info);

			float extrudeAmount = PreferencesInternal.HasKey(pb_Constant.pbExtrudeDistance) ? PreferencesInternal.GetFloat(pb_Constant.pbExtrudeDistance) : .5f;
			bool extrudeAsGroup = PreferencesInternal.GetBool(pb_Constant.pbExtrudeAsGroup);
			bool manifoldEdgeExtrusion = PreferencesInternal.GetBool(pb_Constant.pbManifoldEdgeExtrusion);

			EditorGUI.BeginChangeCheck();

			extrudeAsGroup = EditorGUILayout.Toggle("As Group", extrudeAsGroup);
			manifoldEdgeExtrusion = EditorGUILayout.Toggle(new GUIContent("Manifold Edge Extrusion", "If false, only non-manifold (edges touching two faces) edges may be extruded.  If true, you may extrude any edge you like (for those who like to live dangerously)."), manifoldEdgeExtrusion);

			extrudeAmount = EditorGUILayout.FloatField("Distance", extrudeAmount);

			if(EditorGUI.EndChangeCheck())
			{
				PreferencesInternal.SetFloat(pb_Constant.pbExtrudeDistance, extrudeAmount);
				PreferencesInternal.SetBool(pb_Constant.pbExtrudeAsGroup, extrudeAsGroup);
				PreferencesInternal.SetBool(pb_Constant.pbManifoldEdgeExtrusion, manifoldEdgeExtrusion);
			}

			GUILayout.FlexibleSpace();

			if(GUILayout.Button("Extrude Edges"))
				DoAction();
		}

		public override ActionResult DoAction()
		{
			return MenuCommands.MenuExtrude(selection, true);
		}
	}
}

