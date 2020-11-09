using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.EditorTools;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using Object = UnityEngine.Object;
using PBMeshUtility = UnityEngine.ProBuilder.MeshUtility;
using Math = UnityEngine.ProBuilder.Math;

namespace UnityEditor.ProBuilder
{
    [EditorTool("Free Form Box", typeof(ProBuilderMesh))]
    sealed class FreeFormBoxTool : BoxManipulationTool
    {
        class InternalModification
        {
            public Vector3[] vertices;
            public Quaternion rotation;

            public InternalModification(Vector3[] v)
            {
                vertices = v;
                rotation = Quaternion.identity;
            }
        }

        Dictionary<ProBuilderMesh, InternalModification> m_Modifications ;

        void OnEnable()
        {
            InitTool();
            m_OverlayTitle = new GUIContent("Free Form Box Tool");
            m_Modifications = new Dictionary<ProBuilderMesh, InternalModification>();

            MeshSelection.objectSelectionChanged += OnObjectSelectionChanged;
        }

        void OnDisable()
        {
            MeshSelection.objectSelectionChanged -= OnObjectSelectionChanged;
        }

        /// <summary>
        ///   <para>Invoked after this EditorTool becomes the active tool.</para>
        /// </summary>
        public override void OnActivated()
        {
            foreach(var obj in targets)
            {
                var pbmesh = obj as ProBuilderMesh;
                pbmesh.SetPivot(pbmesh.transform.position + pbmesh.mesh.bounds.center);

                m_Modifications.Add(pbmesh, new InternalModification(pbmesh.positionsInternal));
            }
        }

        /// <summary>
        ///   <para>Invoked before this EditorTool stops being the active tool.</para>
        /// </summary>
        public override void OnWillBeDeactivated()
        {
            m_Modifications.Clear();
        }

        void OnObjectSelectionChanged()
        {
            if(!ToolManager.IsActiveTool(this))
                return;

            foreach(var mesh in m_Modifications.Keys)
            {
                if(!targets.Contains((Object) mesh))
                    EditorApplication.delayCall += () => m_Modifications.Remove(mesh);
            }

            foreach(var obj in targets)
            {
                var pbmesh = obj as ProBuilderMesh;
                if(!m_Modifications.ContainsKey(pbmesh))
                {
                    pbmesh.SetPivot(pbmesh.transform.position + pbmesh.mesh.bounds.center);
                    m_Modifications.Add(pbmesh, new InternalModification(pbmesh.positionsInternal));
                }
            }
        }

        /// <summary>
        ///   <para>Use this method to implement a custom editor tool.</para>
        /// </summary>
        /// <param name="window">The window that is displaying the custom editor tool.</param>
        public override void OnToolGUI(EditorWindow window)
        {
            base.OnToolGUI(window);

            foreach (var obj in targets)
            {
                var pbmesh = obj as ProBuilderMesh;

                if (pbmesh != null)
                {
                    if(m_BoundsHandleActive && GUIUtility.hotControl == k_HotControlNone)
                        EndBoundsEditing();

                    if(Mathf.Approximately(pbmesh.transform.lossyScale.sqrMagnitude, 0f))
                        return;

                    DoManipulationGUI(pbmesh);
                }
            }
        }

        protected override void DoManipulationGUI(Object toolTarget)
        {
            ProBuilderMesh mesh = toolTarget as ProBuilderMesh;
            if(mesh == null)
                return;

            var matrix = IsEditing
                ? m_ActiveBoundsState.positionAndRotationMatrix
                : Matrix4x4.TRS(mesh.transform.position, mesh.transform.rotation, Vector3.one);

            using (new Handles.DrawingScope(matrix))
            {
                m_BoundsHandle.SetColor(Handles.s_BoundingBoxHandleColor);

                CopyColliderPropertiesToHandle(mesh.transform, mesh.mesh.bounds);

                EditorGUI.BeginChangeCheck();
                m_BoundsHandle.DrawHandle();

                if (EditorGUI.EndChangeCheck())
                {
                    BeginBoundsEditing(mesh);
                    CopyHandlePropertiesToCollider(mesh);
                }

                DoRotateHandlesGUI(toolTarget, mesh, mesh.mesh.bounds);
            }
        }

        protected override void UpdateTargetRotation(Object toolTarget, Quaternion rotation)
        {
            ProBuilderMesh mesh = toolTarget as ProBuilderMesh;
            if(mesh == null)
                return;

            if ( rotation.Equals(Quaternion.identity) )
                return;

            InternalModification currentModification = m_Modifications[mesh];
            currentModification.rotation = rotation * currentModification.rotation;

            Bounds bounds = mesh.mesh.bounds;

            var origVerts = new Vector3[mesh.vertexCount] ;
            Array.Copy(currentModification.vertices, origVerts, mesh.vertexCount);

            for (int i = 0; i < origVerts.Length; ++i)
                origVerts[i] = currentModification.rotation * origVerts[i] + bounds.center;

            mesh.mesh.vertices = origVerts;
            mesh.ReplaceVertices(origVerts);
            PBMeshUtility.FitToSize(mesh, bounds.size);
        }

        protected override void OnOverlayGUI(Object target, SceneView view)
        {
            m_snapAngle = EditorGUILayout.IntSlider(m_SnapAngleContent, m_snapAngle, 1, 90);
        }

        void CopyHandlePropertiesToCollider(ProBuilderMesh mesh)
        {
            Vector3 snappedHandleSize = ProBuilderSnapping.Snap(m_BoundsHandle.size, EditorSnapping.activeMoveSnapValue);
            //Find the scaling direction
            Vector3 centerDiffSign = ( m_BoundsHandle.center - m_ActiveBoundsState.boundsHandleValue.center ).normalized;
            Vector3 sizeDiffSign = ( m_BoundsHandle.size - m_ActiveBoundsState.boundsHandleValue.size ).normalized;
            Vector3 globalSign = Vector3.Scale(centerDiffSign,sizeDiffSign);
            //Set the center to the right position
            Vector3 center = m_ActiveBoundsState.boundsHandleValue.center + Vector3.Scale((snappedHandleSize - m_ActiveBoundsState.boundsHandleValue.size)/2f,globalSign);
            //Set new Bounding box value
            m_ActiveBoundsState.boundsHandleValue = new Bounds(center, snappedHandleSize);

            var trs = mesh.transform;
            var meshCenter = Handles.matrix.MultiplyPoint3x4(m_ActiveBoundsState.boundsHandleValue.center);
            var size = Math.Abs(Vector3.Scale(m_ActiveBoundsState.boundsHandleValue.size, Math.InvertScaleVector(trs.lossyScale)));

            PBMeshUtility.FitToSize(mesh, size);
            mesh.transform.position = meshCenter;
            ProBuilderEditor.Refresh(false);
        }
    }
}