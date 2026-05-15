using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.Unity {

    [ExecuteAlways]
    public class AtlasPaintBrushPreview : MonoBehaviour {

        private MeshRenderer meshRenderer;
        private Material material;
        private MeshFilter meshFilter;
        private Mesh mesh;

        private void OnEnable() {

            var shader = Shader.Find("Hidden/Atlas/AtlasBrushPreview");

            if (shader != null) {

                if (material == null) {

                    material = new Material(shader);

                    material.name = "atlas_paint_brush_preview_material";

                }

                if (meshRenderer == null) {

                    meshRenderer = gameObject.AddComponent<MeshRenderer>();

                    meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                    meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

                    meshRenderer.sharedMaterial = material;

                }

                if (mesh == null) {

                    mesh = CreateQuadMesh();

                }

                if (meshFilter == null) {

                    meshFilter = gameObject.AddComponent<MeshFilter>();

                    meshFilter.mesh = mesh;

                }

            }

        }

        private void OnDestroy() {

            GameObject.DestroyImmediate(material, false);

            GameObject.DestroyImmediate(mesh, false);

        }

        public void SetTexture(Texture2D texture) {

            if (material != null) {

                material.SetTexture("_MainTex", texture);

            }

        }

        public void SetOpacity(float opacity) {

            if (material != null) {

                material.SetFloat("_Opacity", opacity);

            }

        }

        public void SetInvert(bool invert) {

            if (material != null) {

                material.SetFloat("_Invert", invert ? 1 : 0);

            }

        }

        public void SetRotation(float rotation) {

            if (material != null) {

                material.SetFloat("_Rotation", rotation + 90);

            }

        }

        private Mesh CreateQuadMesh() {

            var mesh = new Mesh();

            mesh.name = "atlas_paint_brush_preview_mesh";

            var vertices = new Vector3[4]
            {
            new Vector3(-0.5f, 0, -0.5f),
            new Vector3(0.5f, 0, -0.5f),
            new Vector3(-0.5f, 0, 0.5f),
            new Vector3(0.5f, 0, 0.5f)
            };
            mesh.vertices = vertices;

            var tris = new int[6]
            {
            0, 2, 1,
            2, 3, 1
            };
            mesh.triangles = tris;

            var normals = new Vector3[4]
            {
            Vector3.up,
            Vector3.up,
            Vector3.up,
            Vector3.up
            };
            mesh.normals = normals;

            var uv = new Vector2[4]
            {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
            };
            mesh.uv = uv;

            return mesh;

        }

    }

}