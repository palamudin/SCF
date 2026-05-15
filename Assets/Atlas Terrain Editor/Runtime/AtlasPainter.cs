using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
//using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
//using UnityEditor.SceneManagement;
#endif

namespace Atlas.Unity {

    public static class AtlasPainter {

        public static bool editing = false;
        public static Stamp currentStamp = null;
        public static RenderTexture currentRenderTexture = null;
        public static bool previewMask;
        public static Texture2D brush;
        public static Material brushMaterial;
        public static float size = 0.2f;
        public static float opacity = 0.1f;
        public static float rotation = 0f;

        private static Texture pre;
        private static Collider[] colliders;
        private static bool leftMouseButtonDown;
        private static AtlasPaintBrushPreview currentPaintBrushPreview;

        public static void OnEnable() {

            brush = GetDefaultBrushTexture();

            brushMaterial = CreatePaintMaterialEditor();

            colliders = GetPaintColliders();

        }

        public static void OnDisable() {

            if(currentStamp != null) {

                ApplyChange();

            }

            StopEditStampMask();

            GameObject.DestroyImmediate(brushMaterial, false);

        }

        public static void EditorUpdate() {

#if UNITY_EDITOR

            if (editing) {

                if (currentStamp == null) {

                    StopEditStampMask();

                    return;

                }

                //Selection.activeGameObject = currentStamp.gameObject;

                var e = Event.current;

                var re = Event.current.rawType;

                UpdatePaintBrushPreview(e.mousePosition, currentStamp.transform, currentStamp.size, e.shift);

                if (e.alt == false) {

                    if (e.type == EventType.Layout && leftMouseButtonDown) {

                        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

                        GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);

                        Paint(e.mousePosition, currentRenderTexture, colliders, brush, currentStamp.transform, currentStamp.center, currentStamp.size, e.shift ? Color.black : Color.white, size, opacity);

                        AtlasStamper.QueRender();

                    }

                    if (re == EventType.MouseDown && e.button == 0) {
                        leftMouseButtonDown = true;
                    }

                    if (re == EventType.MouseUp && e.button == 0) {
                        leftMouseButtonDown = false;
                    }

                    if (e.isScrollWheel && e.shift) {

                        if (e.control) {

                            rotation += e.delta.y * 5f;

                            while (rotation > 360) {

                                rotation -= 360f;

                            }

                            while (rotation < 0) {

                                rotation += 360f;

                            }

                        } else {

                            size -= e.delta.y * 0.01f;

                        }

                        size = Mathf.Clamp(size, 0.01f, 1);

                        e.Use();

                        RepaintInspector();

                    } else if (e.isScrollWheel && e.control) {

                        opacity -= e.delta.y * 0.01f;

                        opacity = Mathf.Clamp01(opacity);

                        e.Use();

                        RepaintInspector();

                    }

                }

            }

#endif

        }


        public static void EditStampMask(Stamp stamp) {

            if (currentStamp != null) {

                ApplyChange();

                StopEditStampMask();

            }

            if (stamp == null) { Debug.LogError("AtlasStamper:EditStampMask: stamp is null"); return; }

            if (stamp.stamp == null) { Debug.LogError("AtlasStamper:EditStampMask: stamp.stampAsset is null"); return; }

            if (editing == false) {

                editing = true;

                currentStamp = stamp;

                var stampMaskTexture = stamp.maskMap.GetStampTexture(stamp.stamp, stamp);

                currentRenderTexture = new RenderTexture(stampMaskTexture == null ? 1024 : stampMaskTexture.width, stampMaskTexture == null ? 1024 : stampMaskTexture.height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear) {

                    name = "altas_stamper_edit_stamp_mask",
                    enableRandomWrite = true,
                    anisoLevel = 1,
                    autoGenerateMips = false,
                    wrapMode = TextureWrapMode.Clamp,
                    useMipMap = false,

                };

                if (stampMaskTexture != null) {

                    Graphics.Blit(stampMaskTexture, currentRenderTexture);

                }

                if (stamp.maskOverride != null) {

                    pre = stamp.maskOverride;

                }

                stamp.maskOverride = currentRenderTexture;

                currentPaintBrushPreview = CreatePaintBrushPreview();

            }

        }

        public static void StopEditStampMask() {

            if (editing) {

                editing = false;

                if (currentRenderTexture != null) {

                    currentRenderTexture.Release();

                    GameObject.DestroyImmediate(currentRenderTexture, false);

                }

                if (pre != null) {

                    currentStamp.maskOverride = pre;

                }

                if (brushMaterial != null) {

                    GameObject.DestroyImmediate(brushMaterial);

                }

                DestroyPaintBrushPreview();

                currentStamp = null;

                AtlasStamper.QueRender();

            }

        }

        public static void ApplyChange() {

            var dir = "Assets/Atlas Terrain Editor/PaintedMasks";

            if (!AtlasUtils.IsValidPath(dir, out var error)) {

                Debug.LogError("Atlas.Unity.AtlasPainter.ApplyChange: invalid path: " + error);

                return;

            }

            if (editing) {

                if (currentRenderTexture != null) {

                    RenderTexture.active = currentRenderTexture;

                    var tex = new Texture2D(currentRenderTexture.width, currentRenderTexture.height, TextureFormat.RFloat, false);

                    tex.ReadPixels(new Rect(0, 0, currentRenderTexture.width, currentRenderTexture.height), 0, 0);

                    RenderTexture.active = null;

                    var bytes = tex.EncodeToEXR(Texture2D.EXRFlags.None);

                    if (Directory.Exists(dir) == false) { Directory.CreateDirectory(dir); }

                    var path = dir + "/" + currentStamp.gameObject.name + "-" + currentStamp.gameObject.GetInstanceID() + ".exr";

                    System.IO.File.WriteAllBytes(path, bytes);

                    Debug.Log("Painted Mask Saved @:" + path);

#if UNITY_EDITOR

                    AssetDatabase.ImportAsset(path);

                    var importer = (TextureImporter)TextureImporter.GetAtPath(path);

                    var tips = new TextureImporterPlatformSettings {
                        format = TextureImporterFormat.RFloat,
                        textureCompression = TextureImporterCompression.Uncompressed,
                        crunchedCompression = false,
                    };

                    importer.SetPlatformTextureSettings(tips);
                    importer.textureType = TextureImporterType.SingleChannel;

                    importer.isReadable = true;
                    importer.mipmapEnabled = false; 
                    importer.sRGBTexture = false; //SRGB FIX

                    AssetDatabase.ImportAsset(path);

                    Undo.RecordObject(currentStamp.maskOverride, "Stamp mask changed");

                    currentStamp.maskOverride = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));

                    PrefabUtility.RecordPrefabInstancePropertyModifications(currentStamp);

                    //EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

#endif

                    pre = null;

                }

            }

        }

        public static void Reset() {

            if (editing) {

#if UNITY_EDITOR

                Undo.RecordObject(currentStamp.maskOverride, "Stamp mask changed");

#endif
                currentStamp.maskOverride = null;

#if UNITY_EDITOR

                PrefabUtility.RecordPrefabInstancePropertyModifications(currentStamp);

                //EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

#endif
                pre = null;

            }

        }

        public static void Clear() {

            if (editing) {

                RenderTexture.active = currentRenderTexture;

                GL.Clear(false, true, Color.black);

                RenderTexture.active = null;

                AtlasStamper.QueRender();

            }

        }

        public static void OverrideColliders(Collider[] colliderOverrides) {

            colliders = colliderOverrides;

        }


        private static AtlasPaintBrushPreview CreatePaintBrushPreview() {

            var brushGameObject = new GameObject("Atlas_paint_brush_preview");

            brushGameObject.hideFlags = HideFlags.HideAndDontSave;

            var paintBrushPreview = brushGameObject.AddComponent<AtlasPaintBrushPreview>();

            return paintBrushPreview;

        }

        private static void DestroyPaintBrushPreview() {

            var paintBrushes = Resources.FindObjectsOfTypeAll<AtlasPaintBrushPreview>();

            foreach (var i in paintBrushes) {

                GameObject.DestroyImmediate(i.gameObject, false);

            }

        }

        private static void UpdatePaintBrushPreview(Vector2 mousePosition, Transform localTransform, Vector3 stampSize, bool invert) {

            if (currentPaintBrushPreview == null) {

                currentPaintBrushPreview = CreatePaintBrushPreview();

            }

            if (RaycastColliders(colliders, mousePosition, out var point)) {

                currentPaintBrushPreview.transform.position = point;

                currentPaintBrushPreview.transform.SetParent(localTransform);

                currentPaintBrushPreview.transform.localRotation = Quaternion.identity;

                currentPaintBrushPreview.transform.localScale = new Vector3(stampSize.x * size, 1, stampSize.z * size);

                currentPaintBrushPreview.transform.SetParent(null);

                currentPaintBrushPreview.transform.rotation = Quaternion.LookRotation(currentPaintBrushPreview.transform.forward, Vector3.up);

                currentPaintBrushPreview.SetTexture(brush);

                currentPaintBrushPreview.SetOpacity(opacity);

                currentPaintBrushPreview.SetInvert(invert);

                currentPaintBrushPreview.SetRotation(rotation);

            } else {

                currentPaintBrushPreview.SetOpacity(0);

            }

        }


        public static void Paint(Vector2 mousePosition, RenderTexture renderTexture, Collider[] colliders, Texture2D brush, Transform localTransform, Vector3 stampCenter, Vector3 stampSize, Color color, float size, float opacity) {

            if (renderTexture == null) { Debug.LogError("PaintBrush:Paint: renderTexture is null"); return; }

            if (localTransform == null) { Debug.LogError("PaintBrush:Paint: localTransform is null"); return; }

            if (brush == null) { brush = GetDefaultBrushTexture(); }

            if (RaycastColliders(colliders, mousePosition, out var point)) {

                var uv = PointToUV(point, localTransform, stampCenter, stampSize);

                Draw(uv, renderTexture, brush, brushMaterial, color, size, opacity);

            }

        }

        private static bool RaycastColliders(Collider[] colliders, Vector2 mousePosition, out Vector3 point) {

            point = Vector3.zero;

#if UNITY_EDITOR

            var view = SceneView.currentDrawingSceneView;

            if (view != null) {

                var ray = HandleUtility.GUIPointToWorldRay(mousePosition);

                var nearest = float.MaxValue;

                if (colliders != null) {

                    foreach (var i in colliders) {

                        if (i.Raycast(ray, out var hit, 1000000)) {

                            if (hit.distance < nearest) {

                                nearest = hit.distance;

                                point = hit.point;

                            }

                        }

                    }

                }

                if (nearest == float.MaxValue) {

                    var plane = new Plane(Vector3.up, currentStamp.transform.position);

                    if (plane.Raycast(ray, out var distance)) {

                        nearest = distance;

                        point = ray.origin + (ray.direction * distance);

                    }

                }

                return nearest < float.MaxValue;

            }

#endif

            return false;

        }

        private static Vector2 PointToUV(Vector3 point, Transform localTransform, Vector3 center, Vector3 size) {

            point = localTransform.InverseTransformPoint(point);

            point -= center;

            point.x /= size.x;
            point.y /= size.y;
            point.z /= size.z;

            point += Vector3.one * 0.5f;

            return new Vector2(point.z, point.x);

        }

        private static void Draw(Vector2 uv, RenderTexture renderTexture, Texture2D brush, Material brushMaterial, Color color, float size, float opacity) {

            if (brushMaterial == null) { brushMaterial = CreatePaintMaterialEditor(); }

            if (brushMaterial == null) {

                Debug.LogError("AtlasPainter:Draw: brushMaterial is null");

                return;

            }

            RenderTexture.active = renderTexture;

            GL.PushMatrix();

            GL.LoadPixelMatrix(0, renderTexture.width, renderTexture.height, 0);

            var rect = new Rect((uv.x * renderTexture.width) - (renderTexture.width * size * 0.5f), (uv.y * renderTexture.height) - (renderTexture.height * size * 0.5f), renderTexture.width * size, renderTexture.height * size);

            brushMaterial.SetTexture("_MainTex", brush);
            brushMaterial.SetFloat("_Opacity", opacity);
            brushMaterial.SetColor("_Color", color);
            brushMaterial.SetFloat("_Rotation", rotation);

            Graphics.DrawTexture(rect, brush, brushMaterial);

            GL.PopMatrix();

            RenderTexture.active = null;

        }


        private static Collider[] GetPaintColliders() {

            var previewVolume = GameObject.FindObjectOfType<AtlasUnityPreviewVolume>();

            if (previewVolume != null) {

                var colliders = new List<Collider>();

                previewVolume.GetComponentsInChildren(true, colliders);

                return colliders.ToArray();

            }

            return null;

        }

        private static Material CreatePaintMaterialEditor() {

            return new Material(Shader.Find("Hidden/Atlas/AtlasBrush"));

        }

        private static Texture2D GetDefaultBrushTexture() {

#if UNITY_EDITOR

            var path = "Packages/com.atlas.atlas-terrain-editor/Runtime/Brushes/AtlasUnityTerrainDefaultBrush.tga";

            var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (asset == null) {

                var guids = AssetDatabase.FindAssets("l:AtlasUnityTerrainDefaultBrush");

                if (guids.Length > 0) {

                    path = AssetDatabase.GUIDToAssetPath(guids[0]);

                }

            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);

#else

            return null;

#endif
        }

        private static void RepaintInspector() {

#if UNITY_EDITOR

            var ed = Resources.FindObjectsOfTypeAll<Editor>();

            if (ed.Length > 0) {

                ed[0].Repaint();

            }

#endif

        }

    }

}
