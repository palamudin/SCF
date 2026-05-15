using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace Atlas.Unity {

    [SelectionBase]
    [ExecuteInEditMode]
    public partial class AtlasUnityPreviewVolume : MonoBehaviour {

        public void OnEnable() {

#if UNITY_EDITOR

            if (!Application.isPlaying) {

                EditorEnable();

            }

#endif

        }

        public void OnDisable() {

#if UNITY_EDITOR

            if (!Application.isPlaying) {

                EditorDisable();

            }

#endif

        }

    }


#if UNITY_EDITOR

    public partial class AtlasUnityPreviewVolume : MonoBehaviour {

        public static AtlasUnityPreviewVolume previewVolume {

            get {

                if (_previewVolume == null) {

                    _previewVolume = GameObject.FindObjectOfType<AtlasUnityPreviewVolume>();

                }

                return _previewVolume;

            }

        }

        private static AtlasUnityPreviewVolume _previewVolume;

        [Tooltip("The preview volume will copy stats like splatmap layers and resolution properties from this source terrain.")]
        public Terrain terrainSource = null;

        [Tooltip("Center the full resolution square to your view.")]
        public bool autoCenter = true;
        public bool editing = false;
        public bool editedOnce = false;
        public bool baked = false;

        [Tooltip("Size of the preview volume, this doesn't have to be the same as your underlying terrain.")]
        public Vector3 volumeSize = new Vector3(1000, 300, 1000);
        [Tooltip("Resolution while editing the terrain, used to determine realtime performance.")]
        public SimpleResolution previewRenderResolution = SimpleResolution.Eight;
        [Tooltip("Mode used when updating the terrain.\n\n'None' changes the visual height but not the collision.\n\n'HeightOnly' changes the visual height and collision but not the LOD stages.\n\n'HeightAndLOD' updates the terrain fully.\n\nThis setting only has meaning while editing, it does not change your final terrain outside of this preview volume box.")]
        public TerrainHeightmapSyncControl terrainUpdateMode = TerrainHeightmapSyncControl.HeightAndLod;
        public HideFlags subHideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy | HideFlags.NotEditable;
        [Tooltip("Copy resolution properties from source terrain.")]
        public bool previewAccuracyFromSource = true;
        public Accuracy previewAccuracy = new Accuracy();
        public Vector3 lastSavedEyePosition = Vector3.zero;
        [Tooltip("Enable this to get a sense of scale.")]
        public bool drawScaleReferences = false;
        [Tooltip("Choose wich type of scale reference to draw.")]
        public AtlasScaleReferenceDrawer.ScaleReferenceType scaleReferenceType = AtlasScaleReferenceDrawer.ScaleReferenceType.Human;

        public AtlasScaleReferenceDrawer atlasScaleReferenceDrawer { get; private set; }
        public AtlasTerrainData atlasTerrainData { get; private set; }
        public GameObject eyeGameObject { get; private set; }
        public Terrain[] editorTerrains { get; private set; }
        public AtlasTerrainData[] editorAtlasTerrainDatas { get; private set; }
        public AtlasStamper[] editorAtlasStampers { get; private set; }
        public Material editorMaskPreviewMaterial { get; private set; }
        public Material[] editorMaskPreviewMaterials { get; private set; }
        public Material[] editorPrevMaterials { get; private set; }
        public Action onBeforeEdit;
        public Action onAfterEdit;
        public Action onBeforeStopEdit;
        public Action onAfterStopEdit;

        public Vector3 size { get { return new Vector3(volumeSize.x, volumeSize.y * 2f, volumeSize.z); } }
        public Vector3 eyeLocalPosition = Vector3.zero;

        private Matrix4x4 preTransformationMatrix;
        private bool drawInstancedWasOnWhenEnteredPreviewMode = true;
        private bool drawInstancedWasOnWhenEnteredPreviewModeRecorded = false;

        public bool needEye {

            get { return eyeSize.x < eyeMaxSize && eyeSize.z < eyeMaxSize; }

        }

        public float eyeMaxSize {

            get { return Mathf.Min(size.x, size.z); }

        }

        public Vector3 eyeSize {

            get { return new Vector3(Mathf.Min(eyeMaxSize, AtlasUtils.SimpleResolutionToInt(previewRenderResolution) * previewAccuracy._heightMapAccuracy), size.y, Mathf.Min(eyeMaxSize, AtlasUtils.SimpleResolutionToInt(previewRenderResolution) * previewAccuracy.heightMapAccuracy)); }

        }

        public float splatResolutionRatio {

            get { return previewAccuracy._heightMapAccuracy / previewAccuracy._splatMapAccuracy; }

        }

        private void EditorEnable() {

            AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;

            Selection.selectionChanged += AtlasStamper.QueRender;

            EditorSceneManager.sceneSaved += SceneChanged;

            SceneView.beforeSceneGui += EditorUpdate;

            EditorApplication.playModeStateChanged += PlayModeStateChanged;

            if (editing) {

                TriggerCallback(CallbackType.OnBeforeEdit);

                baked = false;

                SetAtlasRenderersActiveState(false);

                ConfigureFromSource();

                atlasTerrainData = new AtlasTerrainData(transform, size, new Vector2Int(1, 1));

                if (editorMaskPreviewMaterial == null) {

                    editorMaskPreviewMaterial = LoadMaskPreviewMaterialEditor("Packages/com.atlas.atlas-terrain-editor/Runtime/Materials/AtlasUnityTerrainMaskPreview.mat", "l:AtlasUnityTerrainMaskPreview");

                }

                if (needEye) {

                    eyeGameObject = new GameObject("eye");

                    eyeGameObject.hideFlags = subHideFlags;

                    eyeGameObject.transform.SetParent(transform);

                    eyeGameObject.transform.localPosition = lastSavedEyePosition;

                    var eyeStampGameObject = new GameObject("eye-stamp");

                    eyeStampGameObject.transform.SetParent(eyeGameObject.transform);

                    eyeStampGameObject.transform.localPosition = new Vector3(eyeSize.x * 0.5f, 0, eyeSize.z * 0.5f);

                    eyeStampGameObject.transform.localScale = Vector3.one * 0.9f;

                    var eyeStamp = eyeStampGameObject.AddComponent<FlattenStamp>();

                    eyeStamp.size = new Vector3(eyeSize.x, 0, eyeSize.z);

                    editorAtlasTerrainDatas = new AtlasTerrainData[] {
                        new AtlasTerrainData(transform,size,new Vector2Int(1,1)),
                        new AtlasTerrainData(eyeGameObject.transform,eyeSize,new Vector2Int(1,1)),
                    };

                    var resolution = AtlasUtils.SimpleResolutionToInt(previewRenderResolution);
                    var splatResolution = (int)(resolution * splatResolutionRatio);

                    resolution = Mathf.Max(32, resolution);
                    splatResolution = Mathf.Max(32, splatResolution);

                    var outerResolution = previewRenderResolution == SimpleResolution.Eight || previewRenderResolution == SimpleResolution.Sixteenth ? resolution : resolution / 2;
                    var outerSplatResolution = previewRenderResolution == SimpleResolution.Eight || previewRenderResolution == SimpleResolution.Sixteenth ? splatResolution : splatResolution / 2;

                    editorAtlasStampers = new AtlasStamper[] {
                        new AtlasStamper(editorAtlasTerrainDatas[0],outerResolution,outerSplatResolution,true,true,null,new List<StampBase>(){ eyeStamp }),
                        new AtlasStamper(editorAtlasTerrainDatas[1],resolution,splatResolution,true,true,new List<StampBase>(){ eyeStamp },null),
                    };

                    if (terrainSource != null) {

                        editorTerrains = new Terrain[] {
                            Terrain.CreateTerrainGameObject(GameObject.Instantiate(terrainSource.terrainData)).GetComponent<Terrain>(),
                            Terrain.CreateTerrainGameObject(GameObject.Instantiate(terrainSource.terrainData)).GetComponent<Terrain>(),
                        };

                        editorTerrains[0].materialTemplate = terrainSource.materialTemplate;
                        editorTerrains[0].terrainData.heightmapResolution = (outerResolution) + 1;
                        editorTerrains[0].terrainData.alphamapResolution = outerSplatResolution;
                        editorTerrains[0].terrainData.baseMapResolution = outerSplatResolution;
                        editorTerrains[0].terrainData.size = new Vector3(size.x, size.y * 0.5f, size.z);

                        editorTerrains[1].materialTemplate = terrainSource.materialTemplate;
                        editorTerrains[1].terrainData.heightmapResolution = resolution + 1;
                        editorTerrains[1].terrainData.alphamapResolution = splatResolution;
                        editorTerrains[1].terrainData.baseMapResolution = splatResolution;
                        editorTerrains[1].terrainData.size = new Vector3(eyeSize.x, size.y * 0.5f, eyeSize.z);

                    } else {

                        editorTerrains = new Terrain[] {
                            Terrain.CreateTerrainGameObject(new TerrainData() {
                                heightmapResolution = (outerResolution)+1,
                                alphamapResolution = outerSplatResolution,
                                baseMapResolution = outerSplatResolution,
                                size =  new Vector3(size.x, size.y * 0.5f, size.z),
                            }).GetComponent<Terrain>(),
                            Terrain.CreateTerrainGameObject(new TerrainData() {
                                heightmapResolution = resolution+1,
                                alphamapResolution = splatResolution,
                                baseMapResolution = splatResolution,
                                size = new Vector3(eyeSize.x, size.y * 0.5f, eyeSize.z),
                            }).GetComponent<Terrain>(),
                        };

                    }

                    editorTerrains[0].transform.SetParent(transform);
                    editorTerrains[0].transform.localPosition = Vector3.zero;
                    editorTerrains[0].groupingID = editorTerrains[0].GetInstanceID();
                    editorTerrains[0].heightmapPixelError = terrainSource == null ? 5 : terrainSource.heightmapPixelError;
                    //editorTerrains[0].drawInstanced = terrainSource == null ? true : terrainSource.drawInstanced;
                    //editorTerrains[0].basemapDistance = 20000;

                    editorTerrains[1].transform.SetParent(eyeGameObject.transform);
                    editorTerrains[1].transform.localPosition = Vector3.zero;
                    editorTerrains[1].groupingID = editorTerrains[1].GetInstanceID();
                    editorTerrains[1].heightmapPixelError = terrainSource == null ? 5 : terrainSource.heightmapPixelError;
                    //editorTerrains[1].drawInstanced = terrainSource == null ? true : terrainSource.drawInstanced;
                    //editorTerrains[1].basemapDistance = 20000;


                    editorMaskPreviewMaterials = new Material[] {
                        editorMaskPreviewMaterial == null? null:GameObject.Instantiate(editorMaskPreviewMaterial),
                        editorMaskPreviewMaterial == null? null:GameObject.Instantiate(editorMaskPreviewMaterial),
                    };

                    editorPrevMaterials = new Material[] {
                        editorTerrains[0].materialTemplate,
                        editorTerrains[1].materialTemplate,
                    };

                } else {

                    editorAtlasTerrainDatas = new AtlasTerrainData[] {
                        new AtlasTerrainData(transform,size,new Vector2Int(1,1)),
                    };

                    var resolution = AtlasUtils.SimpleResolutionToInt(previewRenderResolution);
                    var splatResolution = (int)(resolution * splatResolutionRatio);

                    resolution = Mathf.Max(32, resolution);
                    splatResolution = Mathf.Max(32, splatResolution);

                    editorAtlasStampers = new AtlasStamper[] {
                        new AtlasStamper(editorAtlasTerrainDatas[0],resolution,splatResolution,true,true),
                    };

                    if (terrainSource != null) {

                        editorTerrains = new Terrain[] {
                            Terrain.CreateTerrainGameObject(GameObject.Instantiate(terrainSource.terrainData)).GetComponent<Terrain>(),
                        };

                        editorTerrains[0].materialTemplate = terrainSource.materialTemplate;
                        editorTerrains[0].terrainData.heightmapResolution = resolution + 1;
                        editorTerrains[0].terrainData.alphamapResolution = splatResolution;
                        editorTerrains[0].terrainData.baseMapResolution = splatResolution;
                        editorTerrains[0].terrainData.size = new Vector3(size.x, size.y * 0.5f, size.z);


                    } else {

                        editorTerrains = new Terrain[] {
                            Terrain.CreateTerrainGameObject(new TerrainData() {
                                heightmapResolution = resolution+1,
                                alphamapResolution = splatResolution,
                                baseMapResolution = splatResolution,
                                size = new Vector3(size.x,size.y * 0.5f,size.z),
                            }).GetComponent<Terrain>(),
                        };

                    }

                    editorTerrains[0].transform.SetParent(transform);
                    editorTerrains[0].transform.localPosition = Vector3.zero;
                    editorTerrains[0].groupingID = editorTerrains[0].GetInstanceID();
                    editorTerrains[0].heightmapPixelError = terrainSource == null ? 5 : terrainSource.heightmapPixelError;
                    //editorTerrains[0].drawInstanced = terrainSource == null ? true : terrainSource.drawInstanced;
                    //editorTerrains[0].basemapDistance = 20000;

                    editorMaskPreviewMaterials = new Material[] {
                        editorMaskPreviewMaterial == null? null:GameObject.Instantiate(editorMaskPreviewMaterial),
                    };

                    editorPrevMaterials = new Material[] {
                        editorTerrains[0].materialTemplate,
                    };

                }

                foreach (var i in editorTerrains) {

                    i.gameObject.hideFlags = subHideFlags;
                    i.drawTreesAndFoliage = false;
                    i.gameObject.isStatic = false;
                    i.groupingID = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                    i.allowAutoConnect = false;
                    i.basemapDistance = 2000;

                    if (terrainSource != null && terrainSource.drawInstanced) {
                        i.drawInstanced = true;
                    }
                }

                if (AtlasUtils.IsHDRP()) {

                    foreach (var i in editorMaskPreviewMaterials) {

                        if (i != null) {

                            i.shader = Shader.Find("HDRP/Unlit");

                        }

                    }

                }

                AtlasStamper.QueRender();

                CenterEye(true);

                AtlasPainter.OnEnable();

                TriggerCallback(CallbackType.OnAfterEdit);

            } else {

                TriggerCallback(CallbackType.OnBeforeStopEdit);

                AtlasUtils.ClearPreview();

                SetAtlasRenderersActiveState(true);

                Bake();

                AtlasPainter.OnDisable();

                TriggerCallback(CallbackType.OnAfterStopEdit);

            }

            if (atlasScaleReferenceDrawer == null) {

                atlasScaleReferenceDrawer = new AtlasScaleReferenceDrawer(transform);

            }

        }

        private void EditorDisable() {

            AssemblyReloadEvents.beforeAssemblyReload -= BeforeAssemblyReload;

            Selection.selectionChanged -= AtlasStamper.QueRender;

            EditorSceneManager.sceneSaved -= SceneChanged;

            SceneView.beforeSceneGui -= EditorUpdate;

            EditorApplication.playModeStateChanged -= PlayModeStateChanged;

            if (editing) {

                if (editorTerrains != null) {

                    foreach (var i in editorTerrains) {

                        if (i != null) {

                            GameObject.DestroyImmediate(i.terrainData, false);

                            GameObject.DestroyImmediate(i.gameObject, false);

                        }

                    }

                }

                if (eyeGameObject != null) {

                    lastSavedEyePosition = eyeGameObject.transform.localPosition;

                    GameObject.DestroyImmediate(eyeGameObject, false);

                }

                if (editorAtlasStampers != null) {

                    foreach (var i in editorAtlasStampers) {

                        if (i != null) {

                            i.Dispose();

                        }

                    }

                }

                if (editorMaskPreviewMaterials != null) {

                    foreach (var i in editorMaskPreviewMaterials) {

                        GameObject.DestroyImmediate(i, false);

                    }

                }

            }

            AtlasPainter.OnDisable();

            if (atlasScaleReferenceDrawer != null) {

                atlasScaleReferenceDrawer.OnDisable();

            }

        }

        private void EditorUpdate(SceneView sceneView) {

            if (editing) {

                if (autoCenter) {

                    CenterEye();

                }

                if (Event.current.type == EventType.Repaint) {

                    transform.rotation = Quaternion.identity;

                    for (var i = 0; i < (needEye ? 2 : 1); i++) {

                        if (editorTerrains != null && i < editorTerrains.Length) {

                            var terrain = editorTerrains[i];

                            if (terrain != null) {

                                var stamper = editorAtlasStampers[i];

                                if (stamper.dirty) {

                                    stamper.dirty = false;

                                    FlushEye();

                                    stamper.Render(false, terrainSource == null ? 16 : terrainSource.terrainData.alphamapLayers);

                                    var heightMap = stamper.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalHeight);

                                    RenderTexture.active = heightMap;

                                    terrain.terrainData.CopyActiveRenderTextureToHeightmap(new RectInt(0, 0, heightMap.width, heightMap.height), new Vector2Int(0, 0), terrainUpdateMode);

                                    if (terrain.terrainData.alphamapLayers > 0) {

                                        var splat1Map = stamper.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalSplat1);

                                        RenderTexture.active = splat1Map;

                                        if (terrain.terrainData.alphamapResolution != splat1Map.width) {

                                            terrain.terrainData.alphamapResolution = splat1Map.width;

                                        }

                                        terrain.terrainData.CopyActiveRenderTextureToTexture("alphamap", 0, new RectInt(0, 0, terrain.terrainData.alphamapWidth, terrain.terrainData.alphamapHeight), new Vector2Int(0, 0), false);

                                        if (terrain.terrainData.alphamapLayers > 4) {

                                            var splat2Map = stamper.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalSplat2);

                                            RenderTexture.active = splat2Map;

                                            terrain.terrainData.CopyActiveRenderTextureToTexture("alphamap", 1, new RectInt(0, 0, terrain.terrainData.alphamapWidth, terrain.terrainData.alphamapHeight), new Vector2Int(0, 0), false);

                                            if (terrain.terrainData.alphamapLayers > 8) {

                                                var splat3Map = stamper.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalSplat3);

                                                RenderTexture.active = splat3Map;

                                                terrain.terrainData.CopyActiveRenderTextureToTexture("alphamap", 2, new RectInt(0, 0, terrain.terrainData.alphamapWidth, terrain.terrainData.alphamapHeight), new Vector2Int(0, 0), false);

                                                if (terrain.terrainData.alphamapLayers > 12) {

                                                    var splat4Map = stamper.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalSplat4);

                                                    RenderTexture.active = splat4Map;

                                                    terrain.terrainData.CopyActiveRenderTextureToTexture("alphamap", 3, new RectInt(0, 0, terrain.terrainData.alphamapWidth, terrain.terrainData.alphamapHeight), new Vector2Int(0, 0), false);

                                                }

                                            }

                                        }

                                    }

                                    var holeMap = stamper.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalHole);

                                    RenderTexture.active = holeMap;

                                    terrain.terrainData.CopyActiveRenderTextureToTexture(TerrainData.HolesTextureName, 0, new RectInt(0, 0, holeMap.width, holeMap.height), new Vector2Int(0, 0), false);

                                    terrain.terrainData.SetBaseMapDirty();

                                    terrain.Flush();

                                    RenderTexture.active = null;

                                    if (AtlasUtils.previewMask || AtlasUtils.previewColor || (AtlasPainter.editing && AtlasPainter.previewMask)) {

                                        //terrain.drawInstanced = false;

                                        var maskPreviewMaterial = editorMaskPreviewMaterials[i];

                                        if (maskPreviewMaterial != null) {

                                            maskPreviewMaterial.SetTexture("_MainTex", stamper.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalColor));

                                            maskPreviewMaterial.SetTexture("_UnlitColorMap", stamper.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalColor));

                                            terrain.materialTemplate = maskPreviewMaterial;

                                        }

                                    } else {

                                        //terrain.drawInstanced = terrainSource == null ? true : terrainSource.drawInstanced;

                                        var prevMaterial = editorPrevMaterials[i];

                                        if (prevMaterial != null) {

                                            terrain.materialTemplate = prevMaterial;

                                        }

                                    }

                                }

                            }

                        }

                    }

                    RenderOnTransformation();

                }

                AtlasPainter.EditorUpdate();

                HandleDrawInstanced();

            }

            if (Event.current.type == EventType.Repaint) {

                if (atlasScaleReferenceDrawer != null) {

                    atlasScaleReferenceDrawer.subHideFlags = subHideFlags;

                    atlasScaleReferenceDrawer.Update(drawScaleReferences, scaleReferenceType);

                }

            }

        }


        private void RenderOnTransformation() {

            var o = Selection.activeGameObject;

            if (o != null && (o.GetComponentInChildren<StampBase>() != null || o.GetComponentInChildren<AtlasUnityPreviewVolume>() != null) && preTransformationMatrix != o.transform.localToWorldMatrix) {

                preTransformationMatrix = o.transform.localToWorldMatrix;

                AtlasStamper.QueRender();

            }

            if (o != null && o.GetComponentInChildren<AtlasUnityPreviewVolume>() != null) {

                o.GetComponentInChildren<AtlasUnityPreviewVolume>().transform.localScale = Vector3.one;

            }

        }

        private void SceneChanged(Scene scene) {

            AtlasStamper.QueRender();

        }

        private void PlayModeStateChanged(PlayModeStateChange state) {

            if (editing && state == PlayModeStateChange.ExitingEditMode) {

                editing = false;

                OnDisable();

                OnEnable();

            }

        }

        private void BeforeAssemblyReload() {

            OnDisable();

        }

        private void HandleDrawInstanced() {

            if (AtlasUtils.previewMask || AtlasUtils.previewColor || (AtlasPainter.editing && AtlasPainter.previewMask)) {

                if (drawInstancedWasOnWhenEnteredPreviewModeRecorded == false) {

                    drawInstancedWasOnWhenEnteredPreviewModeRecorded = true;

                    drawInstancedWasOnWhenEnteredPreviewMode = editorTerrains[0].drawInstanced;

                    editorTerrains[0].drawInstanced = false;

                }

            } else {
                if (drawInstancedWasOnWhenEnteredPreviewModeRecorded) {
                    drawInstancedWasOnWhenEnteredPreviewModeRecorded = false;

                    editorTerrains[0].drawInstanced = drawInstancedWasOnWhenEnteredPreviewMode;

                }

            }

        }

        public void Bake() {

            if (editedOnce) {

                if (baked == false) {

                    baked = true;

#if UNITY_2020_1_OR_NEWER

                    var atlasUnityTerrainRenderers = GameObject.FindObjectsOfType<AtlasUnityTerrainRenderer>(true);

#else

                    var atlasUnityTerrainRenderers = (AtlasUnityTerrainRenderer[])Resources.FindObjectsOfTypeAll(typeof(AtlasUnityTerrainRenderer));

#endif

                    foreach (var i in atlasUnityTerrainRenderers) {

                        var t = i.GetComponent<Terrain>();

                        if (t != null) {

                            t.terrainData.size = new Vector3(t.terrainData.size.x, size.y * 0.5f, t.terrainData.size.z);

                        }

                        i.Render();

                    }

                }

            }

        }

        public void ConfigureFromSource() {

            if (terrainSource != null) {

                gameObject.layer = terrainSource.gameObject.layer;

                if (previewAccuracyFromSource) {

                    previewAccuracy.heightMapAccuracy = terrainSource.terrainData.size.x / terrainSource.terrainData.heightmapResolution;
                    previewAccuracy.splatMapAccuracy = terrainSource.terrainData.size.x / terrainSource.terrainData.alphamapResolution;
                    previewAccuracy.baseMapAccuracy = terrainSource.terrainData.size.x / terrainSource.terrainData.baseMapResolution;
                    previewAccuracy.detailMapAccuracy = terrainSource.terrainData.size.x / terrainSource.terrainData.detailResolution;

                }

            }

        }

        public Material LoadMaskPreviewMaterialEditor(string path, string pattern) {

            var asset = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (asset == null && pattern != null) {

                var guids = AssetDatabase.FindAssets(pattern);

                if (guids.Length > 0) {

                    path = AssetDatabase.GUIDToAssetPath(guids[0]);

                }

            }

            return AssetDatabase.LoadAssetAtPath<Material>(path);

        }

        public void SetAtlasRenderersActiveState(bool active) {

#if UNITY_2020_1_OR_NEWER

            var atlasUnityTerrainRenderers = GameObject.FindObjectsOfType<AtlasUnityTerrainRenderer>(true);

#else

            var atlasUnityTerrainRenderers = (AtlasUnityTerrainRenderer[])Resources.FindObjectsOfTypeAll(typeof(AtlasUnityTerrainRenderer));

#endif

            if (active == false) {

                foreach (var i in atlasUnityTerrainRenderers) {

                    var t = i.GetComponent<Terrain>();

                    if (t != null) {

                        t.drawHeightmap = false;
                        t.drawTreesAndFoliage = false;

                    }

                }

            } else {

                foreach (var i in atlasUnityTerrainRenderers) {

                    var t = i.GetComponent<Terrain>();

                    if (t != null) {

                        t.drawHeightmap = true;// i.preDrawState;
                        t.drawTreesAndFoliage = true;// i.preDrawTreesAndFoliageState;

                    }

                }

            }

        }

        public bool FirstTimeRenderProceed() {

#if UNITY_2020_1_OR_NEWER

            var atlasUnityTerrainRenderers = GameObject.FindObjectsOfType<AtlasUnityTerrainRenderer>(true);

#else

            var atlasUnityTerrainRenderers = (AtlasUnityTerrainRenderer[])Resources.FindObjectsOfTypeAll(typeof(AtlasUnityTerrainRenderer));

#endif

            bool displayFirstRenderMessage = false;

            foreach (var i in atlasUnityTerrainRenderers) {

                if (i.firstRender) {

                    displayFirstRenderMessage = true;

                    break;

                }

            }

            if (displayFirstRenderMessage) {

                if (!EditorUtility.DisplayDialog("Atlas Terrain Renderer", "Atlas is about to override your unity terrain, do you wish to proceed?\n\nCancel if you want to backup your terrain.", "proceed", "cancel")) {

                    return false;

                }

                foreach (var i in atlasUnityTerrainRenderers) {

                    i.firstRender = false;

                }

            }

            return true;

        }


        public void CenterEye(bool force = false) {

            if (needEye && eyeGameObject != null && SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null) {

                if ((Event.current != null && Event.current.type == EventType.MouseUp) || force) {


                    var camera = SceneView.lastActiveSceneView.camera;

                    var plane = new Plane(transform.up, transform.position);

                    var cameraPosition = camera.transform.position;

                    var cameraForward = camera.transform.forward;

                    var ray = new Ray(cameraPosition, cameraForward);

                    var point = Vector3.zero;


                    if (editorTerrains != null) {

                        var nearestPoint = float.MaxValue;

                        foreach (var i in editorTerrains) {

                            var collider = i.GetComponent<TerrainCollider>();

                            if (collider != null) {

                                if (collider.Raycast(ray, out var raycastHit, 100000)) {

                                    if (raycastHit.distance < nearestPoint) {

                                        nearestPoint = raycastHit.distance;

                                        point = raycastHit.point;


                                    }

                                }

                            }

                        }

                    }


                    if (point == Vector3.zero) {

                        if (plane.Raycast(ray, out var hit)) {

                            point = cameraPosition + (cameraForward * hit);

                        }

                    }


                    if (point != Vector3.zero) {

                        SetEyeLocalPosition(transform.InverseTransformPoint(point));

                        SceneView.RepaintAll();

                        AtlasStamper.QueRender();

                    }

                }

            }

        }

        public void SetEyeLocalPosition(Vector3 localPosition) {

            eyeLocalPosition = ClampEye(atlasTerrainData.size, eyeSize, localPosition);

        }

        private Vector3 ClampEye(Vector3 size, Vector3 eyeSize, Vector3 point) {

            var halfEyeSize = new Vector3(eyeSize.x * 0.5f, 0, eyeSize.z * 0.5f);

            point -= halfEyeSize;

            point.y = 0;

            if (point.x < 0) {

                point.x = 0;

            }

            if (point.z < 0) {

                point.z = 0;

            }

            if (point.x > size.x - eyeSize.x) {

                point.x = size.x - eyeSize.x;

            }

            if (point.z > size.z - eyeSize.z) {

                point.z = size.z - eyeSize.z;

            }

            return point + halfEyeSize;

        }

        private void FlushEye() {

            var halfEyeSize = new Vector3(eyeSize.x * 0.5f, 0, eyeSize.z * 0.5f);

            eyeGameObject.transform.localPosition = eyeLocalPosition - halfEyeSize;

        }



        private void TriggerCallback(CallbackType callBackType) {

            try {

                switch (callBackType) {

                    case CallbackType.OnBeforeEdit:

                        if (onBeforeEdit != null) {

                            onBeforeEdit();

                        }

                        break;

                    case CallbackType.OnAfterEdit:

                        if (onAfterEdit != null) {

                            onAfterEdit();

                        }

                        break;

                    case CallbackType.OnBeforeStopEdit:

                        if (onBeforeStopEdit != null) {

                            onBeforeStopEdit();

                        }

                        break;

                    case CallbackType.OnAfterStopEdit:

                        if (onAfterStopEdit != null) {

                            onAfterStopEdit();

                        }

                        break;

                }

            } catch (Exception e) {

                Debug.Log(e);

            }

        }

        private void OnDrawGizmosSelected() {

            Gizmos.DrawWireCube(transform.position + (new Vector3(size.x, 0, size.z) * 0.5f), new Vector3(size.x, 0, size.z));

        }

        private void OnDrawGizmos() {

            Gizmos.color = new Color(1, 1, 1, 0.5f);

            if (!editing && editedOnce == false) {

                Gizmos.DrawCube(transform.position + (new Vector3(size.x, 0, size.z) * 0.5f), new Vector3(size.x, 0, size.z));

                Gizmos.DrawWireCube(transform.position + (size * 0.5f), size);

            }

            if (editing && needEye && eyeGameObject != null) {

                Gizmos.DrawWireCube(transform.TransformPoint(eyeLocalPosition), new Vector3(eyeSize.x, 0, eyeSize.z));

            }

        }

        [Serializable]
        public class Accuracy {

            [Tooltip("Accuracy of the heightmap 1 = 1 Unity unit.")]
            public float heightMapAccuracy = 1;
            [Tooltip("Accuracy of the splatmaps 1 = 1 Unity unit.")]
            public float splatMapAccuracy = 1;
            [Tooltip("Accuracy of the basemap 1 = 1 Unity unit.")]
            public float baseMapAccuracy = 4;
            [Tooltip("Accuracy of the detail map 1 = 1 Unity unit.")]
            public float detailMapAccuracy = 4;

            public float _heightMapAccuracy { get { return Mathf.Max(0.01f, heightMapAccuracy); } }
            public float _splatMapAccuracy { get { return Mathf.Max(0.01f, splatMapAccuracy); } }
            public float _baseMapAccuracy { get { return Mathf.Max(0.01f, baseMapAccuracy); } }
            public float _detailMapAccuracy { get { return Mathf.Max(0.01f, detailMapAccuracy); } }

        }

        public enum CallbackType {
            OnBeforeEdit,
            OnAfterEdit,
            OnBeforeStopEdit,
            OnAfterStopEdit,
        }

    }

#endif

}