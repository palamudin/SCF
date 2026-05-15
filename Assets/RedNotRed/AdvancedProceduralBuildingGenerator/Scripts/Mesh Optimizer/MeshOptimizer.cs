using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public class MeshOptimizer : MonoBehaviour
{
    [Range(0.0f, 1.0f)]
    [SerializeField] float _quality = 0.5f;
    MeshFilter _renderer;
    Mesh _mesh;
    public void DecimateMesh()
    {
        _renderer = transform.GetComponent<MeshFilter>();
        _mesh = transform.GetComponent<MeshFilter>().sharedMesh;
        if (!EditorApplication.isPlaying)
        {
            var meshSimplifier = new UnityMeshSimplifier.MeshSimplifier();
            meshSimplifier.Initialize(_mesh);
            meshSimplifier.SimplifyMesh(_quality);
            var destMesh = meshSimplifier.ToMesh();
            _renderer.sharedMesh = destMesh;
        }
    }

    public void SaveMesh()
    {
        _renderer = transform.GetComponent<MeshFilter>();
        _mesh = transform.GetComponent<MeshFilter>().sharedMesh;
        if (!EditorApplication.isPlaying)
        {
            MeshSaverEditor.SaveMesh(_renderer.sharedMesh, "Optimized__" + gameObject.name, false, true);
        }
    }
    [CustomEditor(typeof(MeshOptimizer))]
    public class OptimizeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MeshOptimizer t = (MeshOptimizer)target;
            if (GUILayout.Button("Optimize Mesh!"))
            {
                t.DecimateMesh();
            }

            if (GUILayout.Button("Save Mesh!"))
            {
                t.SaveMesh();
            }
        }
    }
}
