﻿using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using ProBuilder2.Common;
using Newtonsoft.Json;
using System.Linq;
using System.Text.RegularExpressions;
using ProBuilder2.EditorCommon;

namespace ProBuilder2.UpgradeKit
{
	/**
	 * 2.3.1 (lowest supported upgrade path) doesn't include serialized support for colors.
	 */
	static class BackwardsCompatibilityExtensions
	{
		public static void SetColors(this pb_Object pb, Color[] colors) { }
		public static pb_IntArray[] ToPbIntArray(this int[][] arr)
		{
			pb_IntArray[] pbint = new pb_IntArray[arr.Length];
			for(int i = 0; i < arr.Length; i++)
				pbint[i] = new pb_IntArray(arr[i]);
			return pbint;
		}
	}

	/**
 	 * Methods for storing data about ProBuilder objects that may be translated back into PB post-upgrade.
	 */
	public class pb_UpgradeBridgeEditor : Editor
	{
		const string MaterialFieldRegex = "\"material\": [\\-0-9]{2,20}";

		[MenuItem("Tools/ProBuilder/Upgrade/Prepare Scene for Upgrade")]
		static void MenuSerialize()
		{
			pb_Object[] objects = (pb_Object[])Resources.FindObjectsOfTypeAll(typeof(pb_Object));
			pb_Object[] prefabs = FindProBuilderPrefabs();

			objects = pbUtil.Concat(objects, prefabs).Distinct().ToArray();

			MakeSerializedComponent(objects);		
		}

		static void MakeSerializedComponent(pb_Object[] objects)
		{
			if(pb_Editor.instance != null)
				pb_Editor.instance.Close();

			float len = objects.Length;
			int success = 0;

			if( len < 1 )
			{
				int c = ((pb_SerializedComponent[]) Resources.FindObjectsOfTypeAll(typeof(pb_SerializedComponent))).Length;

				if( c > 0 )
					EditorUtility.DisplayDialog("Prepare Scene", "There are " + c + " serialized components ready to be rebuilt into ProBuilder components in this scene.", "Okay");
				else
					EditorUtility.DisplayDialog("Prepare Scene", "No ProBuilder components found in scene.", "Okay");
			}
			else
			{
				if( !EditorUtility.DisplayDialog("Prepare Scene", "This will safely store all ProBuilder data in a new component, and remove ProBuilder components from all objects in the scene.\n\nThis must be run for each scene in your project.", "Okay", "Cancel") )
					return;

				foreach(pb_Object pb in objects)
				{
					if(pb == null)
						continue;

					EditorUtility.DisplayProgressBar("Serialize ProBuilder Data", "Object: " + pb.name, success / len);

					try
					{
						bool isPrefabInstance = IsPrefabInstance(pb.gameObject);

						if(isPrefabInstance)
							PrefabUtility.DisconnectPrefabInstance(pb.gameObject);

						pb_SerializableObject serializedObject = new pb_SerializableObject(pb);
						pb_SerializableEntity serializedEntity = new pb_SerializableEntity(pb.GetComponent<pb_Entity>());

						string obj = JsonConvert.SerializeObject(serializedObject, Formatting.Indented);
						string entity = JsonConvert.SerializeObject(serializedEntity, Formatting.Indented);
						
						// pre-2.4.1 pb_Face would serialize material as an instance id.  past-me is an idiot.
						// this searches for material entries and tries to replace instance ids with material
						// names.
						// obj = Regex.Replace(obj, MaterialFieldRegex, delegate(Match match)
						// 	{
						// 		string material_entry = match.ToString().Replace("\"material\": ", "").Trim();
						// 		int instanceId = 0;

						// 		if(int.TryParse(material_entry, out instanceId))
						// 		{
						// 			Object mat_obj = EditorUtility.InstanceIDToObject(instanceId);

						// 			if(mat_obj != null)
						// 				return "\"material\": \"" + mat_obj.name + "\"";
						// 		}

						// 		return match.ToString();
						// 	});
						
						pb_SerializedComponent storage = pb.gameObject.AddComponent<pb_SerializedComponent>();
						storage.isPrefabInstance = isPrefabInstance;

						storage.SetObjectData(obj);
						storage.SetEntityData(entity);

						RemoveProBuilderScripts(pb);

						success++;
					}
					catch (System.Exception e)
					{
						Debug.LogError("Failed serializing: " + pb.name + "\nId: " + pb.gameObject.GetInstanceID() + "\nThis object will not be safely upgraded if you continue the process!\n" + e.ToString());
					}
				}

				EditorUtility.ClearProgressBar();

				EditorUtility.DisplayDialog("Prepare Scene", "Successfully serialized " + success + " / " + (int)len + " objects.", "Okay");
			}			
		}

		[MenuItem("Tools/ProBuilder/Upgrade/Re-attach ProBuilder Scripts")]
		static void MenuDeserialize()
		{
			pb_SerializedComponent[] serializedComponents = (pb_SerializedComponent[])Resources.FindObjectsOfTypeAll(typeof(pb_SerializedComponent));

			if( serializedComponents.Length < 1 )
			{
				EditorUtility.DisplayDialog("Deserialize ProBuilder Data", "No serialized ProBuilder components found in this scene.", "Okay");
			}
			else
			{
				int success = 0, c = 0;
				float len = serializedComponents.Length;

				for(int i = 0; i < serializedComponents.Length; i++)
				{
					pb_SerializedComponent ser = serializedComponents[i];

					EditorUtility.DisplayProgressBar("Deserialize ProBuilder Data", "Object: " + ser.gameObject.name, c++ / len);

					try
					{
						pb_SerializableObject serializedObject = JsonConvert.DeserializeObject<pb_SerializableObject>(ser.GetObjectData());
						pb_SerializableEntity serializedEntity = JsonConvert.DeserializeObject<pb_SerializableEntity>(ser.GetEntityData());

						// if( ser.isPrefabInstance )
						// 	PrefabUtility.ReconnectToLastPrefab(ser.gameObject);

						pb_Object pb = ser.gameObject.GetComponent<pb_Object>() ?? ser.gameObject.AddComponent<pb_Object>();
						InitObjectWithSerializedObject(pb, serializedObject);

						pb_Entity ent = ser.gameObject.GetComponent<pb_Entity>() ?? ser.gameObject.AddComponent<pb_Entity>();
						InitEntityWithSerializedObject(ent, serializedEntity);

						PrefabUtility.RecordPrefabInstancePropertyModifications(pb);

						success++;
					}
					catch(System.Exception e)
					{
						if(ser != null)
							Debug.LogError("Failed deserializing object: " + ser.gameObject.name + "\nObject ID: " + ser.gameObject.GetInstanceID() + "\n" + e.ToString());
						else
							Debug.LogError("Failed deserializing object\n" + e.ToString());

						continue;
					}

					DestroyImmediate( ser, true );
				}

				EditorUtility.ClearProgressBar();

				EditorUtility.DisplayDialog("Deserialize ProBuilder Data", "Successfully deserialized " + success + " / " + (int)len + " objects.", "Okay");
			}
		}

		/**
		 * Initialize a pb_Object component with data from a pb_SerializableObject.
		 * If you are initializing a completely new pb_Object, use pb_Object.InitWithSerializableObject() instead.
		 */
		static void InitObjectWithSerializedObject(pb_Object pb, pb_SerializableObject serialized)
		{
			pb.SetVertices( serialized.GetVertices() );
			pb.SetUV( serialized.GetUVs() );
			pb.SetColors( serialized.GetColors() );

			pb.SetSharedIndices( serialized.GetSharedIndices().ToPbIntArray() );
			pb.SetSharedIndicesUV( serialized.GetSharedIndicesUV().ToPbIntArray() );

			pb.SetFaces( serialized.GetFaces() );

			pb.ToMesh();
			pb.Refresh();
			pb.GenerateUV2(true);

			pb.GetComponent<pb_Entity>().SetEntity(EntityType.Detail);
		}

		static void InitEntityWithSerializedObject(pb_Entity entity, pb_SerializableEntity serialized)
		{
			// SetEntityType is an extension method (editor-only) that also sets the static flags to 
			// match the entity use.
			entity.SetEntityType(serialized.entityType);
		}

		static void RemoveProBuilderScripts(pb_Object pb)
		{
			GameObject go = pb.gameObject;

			pb.Verify();

			if(pb.msh == null)
				return;

			// Copy the mesh (since destroying pb_Object will take the mesh reference with it)
			Mesh m = pbUtil.DeepCopyMesh(pb.msh);

			// Destroy pb_Object first, then entity.  Order is important.
			DestroyImmediate(pb, true);
			
			if(go.GetComponent<pb_Entity>())
				DestroyImmediate(go.GetComponent<pb_Entity>(), true);

			// Set the mesh back.
			go.GetComponent<MeshFilter>().sharedMesh = m;

			if(go.GetComponent<MeshCollider>())
				go.GetComponent<MeshCollider>().sharedMesh = m;
		}

		/**
		 * Returns all prefabs that reference pb_Object
		 */
		static pb_Object[] FindProBuilderPrefabs()
		{
			List<pb_Object> pbObjects = new List<pb_Object>();

			// t:pb_Object doesn't return anything, presumably because the top level asset is a gameObject.
			foreach(string cheese in AssetDatabase.FindAssets("t:GameObject"))
			{
				Object[] prefabs = (Object[])AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(cheese));

				foreach(GameObject go in prefabs.Where(x => x is GameObject))
				{
					pb_Object pb = go.GetComponent<pb_Object>();

					if(pb != null) pbObjects.Add(pb);

					pb_Object[] all = go.GetComponentsInChildren<pb_Object>();

					foreach(pb_Object i in all)
					{
						pbObjects.Add(i);
					}
				}
			}

			return pbObjects.ToArray();
		}

		static bool IsPrefabInstance(GameObject go)
		{
			return PrefabUtility.GetPrefabType(go) == PrefabType.PrefabInstance;
			// return 	PrefabUtility.GetPrefabType(go) == PrefabType.PrefabInstance ||
			// 		PrefabUtility.GetPrefabType(go) == PrefabType.Prefab;

		}
	}
}