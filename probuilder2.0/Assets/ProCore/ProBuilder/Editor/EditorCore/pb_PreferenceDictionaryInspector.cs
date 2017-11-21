using ProBuilder.Core;
using UnityEngine;
using UnityEditor;

namespace ProBuilder.EditorCore
{
	[CustomEditor(typeof(pb_PreferenceDictionary))]
	class pb_PreferenceDictionaryInspector : UnityEditor.Editor
	{
		private pb_PreferenceDictionary m_Preferences = null;
		private Vector2 m_Scroll = Vector2.zero;

		void OnEnable()
		{
			m_Preferences = target as pb_PreferenceDictionary;
		}

		public override void OnInspectorGUI()
		{
			GUILayout.Label("Key Value Pairs", EditorStyles.boldLabel);

			m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);

			foreach(var typeDic in m_Preferences)
			{
				foreach(var kvp in typeDic)
					GUILayout.Label(kvp.ToString());
			}

			EditorGUILayout.EndScrollView();
		}
	}
}
