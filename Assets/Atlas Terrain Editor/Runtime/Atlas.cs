using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Atlas.Unity {

    public class AtlasTerrainData {

        public Transform parent { get; private set; }
        public Vector3 size { get; private set; }
        public int tilesX { get; private set; }
        public int tilesZ { get; private set; }
        public Vector3 tileSize { get; private set; }

        public AtlasTerrainData(Transform parent, Vector3 size, Vector2Int tiles) {

            if (parent == null) {

                Debug.LogError("AtlasTerrainData: parent is null");

                return;

            }

            this.parent = parent;
            this.size = size;

            tilesX = Mathf.Max(1, tiles.x);
            tilesZ = Mathf.Max(1, tiles.y);
            tileSize = new Vector3(size.x / tilesX, size.y, size.z / tilesZ);

        }

    }

    public partial class AtlasStamper : IDisposable {

        public AtlasTerrainData atlasTerrainData { get; private set; }
        public RenderPool renderPool { get; private set; }
        public int resolution { get; private set; }
        public int splatResolution { get; private set; }
        public bool forUnityTerrain { get; private set; }
        public List<StampBase> ignoreList;
        public List<StampBase> renderAtEndList;
        public bool dirty = false;

        //for reflection purposes
        public static AtlasStamper NewAtlasStamperGeneric(AtlasTerrainData atlasTerrainData, int resolution, int splatResolution, bool forUnityTerrain, bool registerForTriggeredRendering, Component ignoreStamp, Component renderAtEndListStamp) {
            
            var ignoreList = new List<StampBase>() { (StampBase)ignoreStamp };
            var renderAtEndList = new List<StampBase>() { (StampBase)renderAtEndListStamp };

            return new AtlasStamper(atlasTerrainData, resolution, splatResolution, forUnityTerrain, registerForTriggeredRendering, ignoreList, renderAtEndList);

        }
        //

        public AtlasStamper(AtlasTerrainData atlasTerrainData, int resolution, int splatResolution, bool forUnityTerrain, bool registerForTriggeredRendering, List<StampBase> ignoreList = null, List<StampBase> renderAtEndList = null) {

            if (atlasTerrainData == null) {

                Debug.LogError("AtlasStamper: atlasTerrainData is null");

                return;

            }

            resolution = Mathf.Max(16, resolution);
            splatResolution = Mathf.Max(16, splatResolution);

            this.atlasTerrainData = atlasTerrainData;
            this.resolution = resolution;
            this.splatResolution = splatResolution;
            this.forUnityTerrain = forUnityTerrain;
            this.ignoreList = ignoreList;
            this.renderAtEndList = renderAtEndList;

            renderPool = new RenderPool(resolution, splatResolution, forUnityTerrain);

            if (registerForTriggeredRendering) {

                RegisterForTriggeredRendering(this);

            }

        }

        public void Render(bool forExport = false, int splatmapCount = 16) {

            //reset render pool

            renderPool.ResetRenderTextures();

            //find stamps

            var stamps = FindStamps();

            //triger before export

            if (forExport) {

                foreach (var i in stamps) {

                    i.OnBeforeRenderForExport();

                }

            }

            //render stamps

            foreach (var i in stamps) {

                if (ignoreList != null && ignoreList.Contains(i)) { continue; }

                i.Render(this, splatmapCount);

            }

            //trigger after export

            if (forExport) {

                foreach (var i in stamps) {

                    i.OnAfterRenderForExport();

                }

            }

            //render last stamps

            if (renderAtEndList != null) {

                foreach (var i in renderAtEndList) {

                    if (i != null) {

                        i.Render(this, splatmapCount);

                    }

                }

            }

            //render normal map

            renderPool.GetMaterial(RenderPool.MaterialType.AtlasNormal).SetVector("_Size", new Vector3(atlasTerrainData.size.x * 0.5f, atlasTerrainData.size.y, atlasTerrainData.size.z * 0.5f));
            renderPool.GetMaterial(RenderPool.MaterialType.AtlasNormal).SetVector("_Resolution", new Vector2(resolution, resolution));
            Graphics.Blit(renderPool.GetRenderTexture(RenderPool.RenderTextureType.FinalHeight), renderPool.GetRenderTexture(RenderPool.RenderTextureType.FinalNormal), renderPool.GetMaterial(RenderPool.MaterialType.AtlasNormal), 0);

            //render sum of al splatmaps to an RFloat rendertexture

            renderPool.GetMaterial(RenderPool.MaterialType.AtlasSplatSum).SetTexture("_Splat1", renderPool.GetRenderTexture(RenderPool.RenderTextureType.FinalSplat1));
            renderPool.GetMaterial(RenderPool.MaterialType.AtlasSplatSum).SetTexture("_Splat2", renderPool.GetRenderTexture(RenderPool.RenderTextureType.FinalSplat2));
            renderPool.GetMaterial(RenderPool.MaterialType.AtlasSplatSum).SetTexture("_Splat3", renderPool.GetRenderTexture(RenderPool.RenderTextureType.FinalSplat3));
            renderPool.GetMaterial(RenderPool.MaterialType.AtlasSplatSum).SetTexture("_Splat4", renderPool.GetRenderTexture(RenderPool.RenderTextureType.FinalSplat4));

            Graphics.Blit(null, renderPool.ClearAndGetRenderTexture(RenderPool.RenderTextureType.SplatSum), renderPool.GetMaterial(RenderPool.MaterialType.AtlasSplatSum));

            //devide all splatmaps using the total sum

            renderPool.GetMaterial(RenderPool.MaterialType.AtlasSplatNormalize).SetTexture("_Sum", renderPool.GetRenderTexture(RenderPool.RenderTextureType.SplatSum));

            Graphics.Blit(renderPool.GetRenderTexture(RenderPool.RenderTextureType.FinalSplat1), renderPool.ClearAndGetRenderTexture(RenderPool.RenderTextureType.PreSplat1));
            Graphics.Blit(renderPool.GetRenderTexture(RenderPool.RenderTextureType.PreSplat1), renderPool.ClearAndGetRenderTexture(RenderPool.RenderTextureType.FinalSplat1), renderPool.GetMaterial(RenderPool.MaterialType.AtlasSplatNormalize), 0);

            Graphics.Blit(renderPool.GetRenderTexture(RenderPool.RenderTextureType.FinalSplat2), renderPool.ClearAndGetRenderTexture(RenderPool.RenderTextureType.PreSplat2));
            Graphics.Blit(renderPool.GetRenderTexture(RenderPool.RenderTextureType.PreSplat2), renderPool.ClearAndGetRenderTexture(RenderPool.RenderTextureType.FinalSplat2), renderPool.GetMaterial(RenderPool.MaterialType.AtlasSplatNormalize), 0);

            Graphics.Blit(renderPool.GetRenderTexture(RenderPool.RenderTextureType.FinalSplat3), renderPool.ClearAndGetRenderTexture(RenderPool.RenderTextureType.PreSplat3));
            Graphics.Blit(renderPool.GetRenderTexture(RenderPool.RenderTextureType.PreSplat3), renderPool.ClearAndGetRenderTexture(RenderPool.RenderTextureType.FinalSplat3), renderPool.GetMaterial(RenderPool.MaterialType.AtlasSplatNormalize), 0);

            Graphics.Blit(renderPool.GetRenderTexture(RenderPool.RenderTextureType.FinalSplat4), renderPool.ClearAndGetRenderTexture(RenderPool.RenderTextureType.PreSplat4));
            Graphics.Blit(renderPool.GetRenderTexture(RenderPool.RenderTextureType.PreSplat4), renderPool.ClearAndGetRenderTexture(RenderPool.RenderTextureType.FinalSplat4), renderPool.GetMaterial(RenderPool.MaterialType.AtlasSplatNormalize), 0);

        }

        public void Dispose() {

            UnRegisterForTriggeredRendering(this);

            Dispose(true);

            GC.SuppressFinalize(this);

        }

        protected virtual void Dispose(bool disposing) {

            if (disposing) {

                renderPool.Dispose();

            }

        }

        private List<StampBase> FindStamps() {

            var stamps = GameObject.FindObjectsOfType<StampBase>();

            var ret = new List<StampBase>();

            var center = atlasTerrainData.parent.position;

            if (forUnityTerrain) {

                center += new Vector3(atlasTerrainData.size.x * 0.5f, 0, atlasTerrainData.size.z * 0.5f);

            }

            var terrainBounds = new Bounds(center, new Vector3(atlasTerrainData.size.x, float.MaxValue, atlasTerrainData.size.z));

            foreach (var i in stamps) {

                var stampBounds = new Bounds(i.transform.TransformPoint(i.center), i.size * 1.71f * Mathf.Max(i.transform.lossyScale.x, i.transform.lossyScale.y, i.transform.lossyScale.z));

                if (stampBounds.Intersects(terrainBounds)) {

                    ret.Add(i);

                }

            }

            ret.Sort(new StampSorter());

            return ret;

        }

        public class StampSorter : IComparer<StampBase> {

            public int Compare(StampBase x, StampBase y) {

                if (x == y) {

                    return 0;

                } else if (y.transform.IsChildOf(x.transform)) {

                    return -1;

                } else if (x.transform.IsChildOf(y.transform)) {

                    return 1;

                }

                var xparentList = GetParents(x.transform);
                var yparentList = GetParents(y.transform);

                for (var xIndex = 0; xIndex < xparentList.Count; xIndex++) {

                    if (y.transform.IsChildOf(xparentList[xIndex])) {

                        var yIndex = yparentList.IndexOf(xparentList[xIndex]) - 1;

                        xIndex -= 1;

                        return xparentList[xIndex].GetSiblingIndex() - yparentList[yIndex].GetSiblingIndex();
                    }

                }

                return xparentList[xparentList.Count - 1].GetSiblingIndex() - yparentList[yparentList.Count - 1].GetSiblingIndex();

            }

            private List<Transform> GetParents(Transform t) {

                var parents = new List<Transform> { t };

                while (t.parent != null) {

                    parents.Add(t.parent);

                    t = t.parent;
                }

                return parents;
            }

        }

        public class RenderPool : IDisposable {

            public List<RenderTexture> renderTextures { get; private set; }
            public List<Material> materials { get; private set; }

            public RenderPool(int resolution, int splatResolution, bool forUnityTerrain) {

                renderTextures = new List<RenderTexture>();

                var renderTextureCount = Enum.GetNames(typeof(RenderTextureType)).Length;

                for (var i = 0; i < renderTextureCount; i++) {

                    var res = AtlasUtils.GetRenderTextureResolution((RenderTextureType)i, resolution, splatResolution, forUnityTerrain);

                    renderTextures.Add(new RenderTexture(res, res, 0, AtlasUtils.GetRenderTextureFormat((RenderTextureType)i), RenderTextureReadWrite.Linear) {

                        name = "altas_stamper_renderpool_" + ((RenderTextureType)i).ToString(),
                        enableRandomWrite = true,
                        anisoLevel = 1,
                        autoGenerateMips = false,
                        wrapMode = TextureWrapMode.Clamp,
                        useMipMap = false,

                    }) ;

                    ClearRenderTexture(renderTextures[i], (RenderTextureType)i);

                }

                materials = new List<Material>();

                var materialCount = Enum.GetNames(typeof(MaterialType)).Length;

                for (var i = 0; i < materialCount; i++) {

                    materials.Add(new Material(Shader.Find("Hidden/Atlas/" + ((MaterialType)i).ToString())));

                }

            }

            public List<RenderTexture> CloneResult() {

                var ret = new List<RenderTexture>();

                ret.Add(GetRenderTextureClone(RenderTextureType.FinalHeight));
                ret.Add(GetRenderTextureClone(RenderTextureType.FinalNormal));
                ret.Add(GetRenderTextureClone(RenderTextureType.FinalColor));
                ret.Add(GetRenderTextureClone(RenderTextureType.FinalSplat1));
                ret.Add(GetRenderTextureClone(RenderTextureType.FinalSplat2));
                ret.Add(GetRenderTextureClone(RenderTextureType.FinalSplat3));
                ret.Add(GetRenderTextureClone(RenderTextureType.FinalSplat4));
                ret.Add(GetRenderTextureClone(RenderTextureType.FinalHighlight));
                ret.Add(GetRenderTextureClone(RenderTextureType.FinalHole));

                return ret;

            }

            public Material GetMaterial(MaterialType materialType) {

                return materials[(int)materialType];

            }

            public RenderTexture GetRenderTexture(RenderTextureType renderTextureType) {

                return renderTextures[(int)renderTextureType];

            }

            public RenderTexture GetRenderTextureClone(RenderTextureType renderTextureType) {

                var rt = GetRenderTexture(renderTextureType);

                var clone = new RenderTexture(rt);

                Graphics.Blit(rt, clone);

                return clone;

            }

            public RenderTexture ClearAndGetRenderTexture(RenderTextureType renderTextureType) {

                var rt = GetRenderTexture(renderTextureType);

                ClearRenderTexture(rt, renderTextureType);

                return rt;

            }

            public void ResetRenderTextures() {

                if (renderTextures != null) {

                    for (var i = 0; i < renderTextures.Count; i++) {

                        ClearRenderTexture(renderTextures[i], (RenderTextureType)i);

                    }

                }

            }

            public void Dispose() {

                Dispose(true);

                GC.SuppressFinalize(this);

            }

            protected virtual void Dispose(bool disposing) {

                if (disposing) {

                    if (renderTextures != null) {

                        foreach (var i in renderTextures) {

                            if (i == null) { continue; }

                            RenderTexture.active = null;

                            i.Release();

                            GameObject.DestroyImmediate(i, false);

                        }

                        renderTextures = null;
                    }

                    if (materials != null) {

                        foreach (var i in materials) {

                            GameObject.DestroyImmediate(i, false);

                        }

                    }

                }

            }

            private void ClearRenderTexture(RenderTexture renderTexture, RenderTextureType renderTextureType) {

                RenderTexture.active = renderTexture;

                GL.Clear(true, true, AtlasUtils.GetRenderTextureClearColor(renderTextureType));

                RenderTexture.active = null;

            }

            public enum MaterialType {

                AtlasHeight, AtlasColor, AtlasMask, AtlasOtherMask, AtlasRoadMask,
                AtlasHeightMerge, AtlasColorMerge, AtlasOtherMaskMerge, AtlasRoadMaskMerge,
                AtlasNormal,
                AtlasHeightBasedAlteration,
                AtlasSplatSum, AtlasSplatNormalize,
                AtlasHighlight, AtlasHighlightMerge,
                AtlasHole, AtlasHoleMerge,

            }

            public enum RenderTextureType {

                PreHeight, Height,
                Mask,
                PreSplat1, PreSplat2, PreSplat3, PreSplat4,
                Other,
                PreColor, Color,
                FinalHeight,
                FinalSplat1, FinalSplat2, FinalSplat3, FinalSplat4,
                FinalColor,
                FinalNormal,
                RoadMask, PreRoadMask, FinalRoadMask,
                SplatSum,
                PreHighlight, Highlight, FinalHighlight,
                PreHole, Hole, FinalHole,

            }

#if UNITY_EDITOR

            public void Export(RenderTexture renderTexture, string directory, string filename, string type = "direct") {

                if (renderTexture != null) {

                    var width = renderTexture.width;
                    var height = renderTexture.height;

                    var tex = new Texture2D(width, height, renderTexture.format == RenderTextureFormat.RFloat ? TextureFormat.RFloat : TextureFormat.RGBAFloat, false);

                    Graphics.SetRenderTarget(renderTexture);

                    tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);

                    tex.Apply();

                    var bytes = tex.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);

                    if (Directory.Exists(directory) == false) {

                        Directory.CreateDirectory(directory);

                    }

                    File.WriteAllBytes(directory + "/" + filename + "_" + type + ".exr", bytes);

                    Debug.Log("Atlas exported texture: " + directory + "/" + filename + "_" + type + ".exr");

                    GameObject.DestroyImmediate(tex, false);

                } else {

                    Debug.LogError("AtlasUtils.SaveRenderTexture: renderTexture is null");

                }

            }

            public void Export(RenderTextureType renderTextureType, string directory, string filename) {

                var renderTexture = GetRenderTexture(renderTextureType);

                Export(renderTexture, directory, filename, renderTextureType.ToString());

            }

#endif

        }

    }

    public partial class AtlasStamper : IDisposable {

        public static List<AtlasStamper> atlasStampers = new List<AtlasStamper>();

        private static void RegisterForTriggeredRendering(AtlasStamper atlasStamper) {

            atlasStampers.Add(atlasStamper);

        }

        private static void UnRegisterForTriggeredRendering(AtlasStamper atlasStamper) {

            atlasStampers.RemoveAll(x => x == atlasStamper);

        }

        public static void QueRender() {

#if UNITY_EDITOR

            SceneView.RepaintAll();
#endif

            foreach (var i in atlasStampers) {

                i.dirty = true;

            }

        }

    }

    public class AtlasScaleReferenceDrawer {

        public bool isDrawing { get; private set; }
        public Transform parent { get; private set; }
        public List<GameObject> gameObjects { get; private set; }
        public HideFlags subHideFlags;
        public float spacing { get; private set; }
        public int cellCount { get; private set; }
        public GameObject prefab { get; private set; }

#if UNITY_EDITOR
        private ScaleReferenceType preScaleReferenceType;
        private Vector3 lastCameraPosition;
        private int tick = 0;
#endif

        public AtlasScaleReferenceDrawer(Transform parent) {

            this.parent = parent;

            spacing = 10f;

            cellCount = 15;

            gameObjects = new List<GameObject>();

        }

        public void ForceDraw(ScaleReferenceType scaleReferenceType = ScaleReferenceType.Human) {

            Update(true, scaleReferenceType, true);

        }

        public void Update(bool draw, ScaleReferenceType scaleReferenceType = ScaleReferenceType.Human, bool force = false) {

#if UNITY_EDITOR

            if (draw) {

                if (preScaleReferenceType != scaleReferenceType || isDrawing == false) {

                    isDrawing = true;

                    preScaleReferenceType = scaleReferenceType;

                    CreateGameObjects(scaleReferenceType);

                }

                tick++;

                if (tick > 10 || force) {

                    tick = 0;

                    if (SceneView.lastActiveSceneView != null) {

                        lastCameraPosition = SceneView.lastActiveSceneView.camera.transform.position;

                    }

                    var centerPosition = lastCameraPosition;

                    centerPosition.x = Mathf.Round(centerPosition.x / spacing) * spacing;
                    centerPosition.y = 0;
                    centerPosition.z = Mathf.Round(centerPosition.z / spacing) * spacing;

                    RaycastHit hit;

                    var cc = 0;

                    for (var x = centerPosition.x - cellCount * spacing * 0.5f; x < centerPosition.x + cellCount * spacing * 0.5f; x += spacing) {

                        for (var z = centerPosition.z - cellCount * spacing * 0.5f; z < centerPosition.z + cellCount * spacing * 0.5f; z += spacing) {

                            var position = new Vector3(x, 0, z);

                            if (Physics.Raycast(new Ray(position + Vector3.up * 10000, -Vector3.up), out hit, 20000)) {

                                position.y = hit.point.y;

                            }

                            if (cc < gameObjects.Count) {

                                gameObjects[cc].transform.position = position;
                                gameObjects[cc].transform.rotation = Quaternion.Euler(0, (position.x * 22.5f) + (position.z * 22.5f), 0);

                            }

                            cc++;

                        }

                    }

                }

            } else {

                if (isDrawing) {

                    isDrawing = false;

                    DestroyGameObjects();

                }

            }

#endif

        }

        public void OnDisable() {

            DestroyGameObjects();

            isDrawing = false;

        }

        private void CreateGameObjects(ScaleReferenceType scaleReferenceType) {

            DestroyGameObjects();

            string prefabSearchPattern;

            switch (scaleReferenceType) {

                case ScaleReferenceType.Tree:

                    prefabSearchPattern = "l:AtlasScaleReferenceTree";

                    spacing = 50;

                    break;

                case ScaleReferenceType.Bush:

                    prefabSearchPattern = "l:AtlasScaleReferenceBush";

                    spacing = 10;

                    break;

                case ScaleReferenceType.House:

                    prefabSearchPattern = "l:AtlasScaleReferenceHouse";

                    spacing = 100;

                    break;

                default:

                    prefabSearchPattern = "l:AtlasScaleReferenceMannequin";

                    spacing = 10;

                    break;

            }

            prefab = FindScaleReferencePrefab(prefabSearchPattern);

            if (prefab != null) {

                for (var i = 0; i < cellCount * cellCount; i++) {

                    var gameObject = GameObject.Instantiate(prefab);

                    gameObject.hideFlags = subHideFlags;

                    gameObject.transform.SetParent(parent);

                    gameObjects.Add(gameObject);

                }


            }

        }

        private void DestroyGameObjects() {

            foreach (var i in gameObjects) {

                GameObject.DestroyImmediate(i, false);

            }

            gameObjects.Clear();

        }

        private GameObject FindScaleReferencePrefab(string pattern) {

#if UNITY_EDITOR

            var guids = AssetDatabase.FindAssets(pattern);

            if (guids.Length > 0) {

                return AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));

            }

#endif

            return null;

        }

        public enum ScaleReferenceType {

            Human,
            Tree,
            Bush,
            House,

        }

    }

    public static class AtlasUtils {

        public static bool enableHighlightRendering = false;

        public static bool previewColor = false;
        public static StampBase previewColorStamp = null;

        public static bool previewMask = false;
        public static StampBase previewMaskStamp = null;
        public static int previewMaskStampIndex = -1;

        public static string[] terrainLayerNamesOverride;

        public static void OverrideTerrainLayerNames(string[] names) {

            terrainLayerNamesOverride = names;

        }

        public static void ClearTerrainLayerNamesOverride() {

            terrainLayerNamesOverride = null;

        }

        public static void ClearPreview() {

            previewColor = false;
            previewColorStamp = null;

            previewMask = false;
            previewMaskStamp = null;
            previewMaskStampIndex = -1;

        }

        public static int SimpleResolutionToInt(SimpleResolution resolution) {

            var r = 4096 / 16;

            switch (resolution) {
                case SimpleResolution.Full: r = 4096; break;
                case SimpleResolution.Half: r = 4096 / 2; break;
                case SimpleResolution.Quarter: r = 4096 / 4; break;
                case SimpleResolution.Eight: r = 4096 / 8; break;
            }

            return r;

        }

        public static int WResolutionToInt(WResolution resolution, bool plusone = false) {

            var r = 0;

            switch (resolution) {

                case WResolution.R4096_13: r = 4096; break;
                case WResolution.R2048_12: r = 2048; break;
                case WResolution.R1024_11: r = 1024; break;
                case WResolution.R512_10: r = 512; break;
                case WResolution.R256_9: r = 256; break;
                case WResolution.R128_8: r = 128; break;
                case WResolution.R64_7: r = 64; break;
                case WResolution.R32_6: r = 32; break;
                case WResolution.R16_5: r = 16; break;
                case WResolution.R8_4: r = 8; break;
                case WResolution.R4_3: r = 4; break;
                case WResolution.R2_2: r = 2; break;
                case WResolution.R1_1: r = 1; break;
                case WResolution.R0_0: r = 0; break;

            }

            return r + (plusone ? 1 : 0);

        }

        public static Vector3 LocalPointToTerrainRelativePoint(StampBase stampBase, Vector3 point, AtlasStamper atlasStamper) {

            var target = stampBase.transform;
            var source = atlasStamper == null ? stampBase.transform : atlasStamper.atlasTerrainData.parent;
            var sourceSize = atlasStamper == null ? stampBase.size : atlasStamper.atlasTerrainData.size;
            var isUnityTerrain = atlasStamper == null ? false : atlasStamper.forUnityTerrain;
            var resolution = atlasStamper == null ? 4096 : atlasStamper.resolution;

            if (atlasStamper != null && atlasStamper.forUnityTerrain) {

                point = source.InverseTransformPoint(target.TransformPoint(point)) - new Vector3(atlasStamper.atlasTerrainData.size.x * 0.5f, 0, atlasStamper.atlasTerrainData.size.z * 0.5f);

            } else {

                point = source.InverseTransformPoint(target.TransformPoint(point));

            }

            var s = sourceSize;

            if (isUnityTerrain) {

                s += new Vector3(s.x, 0, s.z) * (1f / resolution);

            }

            point.x /= s.x;
            point.y /= s.y;
            point.z /= s.z;

            point += new Vector3(1, 0, 1) * 0.5f;

            return point;

        }

        public static Color GetRenderTextureClearColor(AtlasStamper.RenderPool.RenderTextureType renderTextureType) {

            switch (renderTextureType) {

                case AtlasStamper.RenderPool.RenderTextureType.Color:
                case AtlasStamper.RenderPool.RenderTextureType.PreColor:
                case AtlasStamper.RenderPool.RenderTextureType.FinalColor:

                    return Color.gray;

                case AtlasStamper.RenderPool.RenderTextureType.FinalSplat1:
                case AtlasStamper.RenderPool.RenderTextureType.PreSplat1:
                case AtlasStamper.RenderPool.RenderTextureType.FinalHole:
                case AtlasStamper.RenderPool.RenderTextureType.PreHole:
                case AtlasStamper.RenderPool.RenderTextureType.Hole:

                    return new Color(1, 0, 0, 0);

                default:

                    return new Color(0, 0, 0, 0);

            }

            //var color = Color.black;
            //
            //switch (renderTextureType) {
            //
            //    case AtlasStamper.RenderPool.RenderTextureType.Color:
            //    case AtlasStamper.RenderPool.RenderTextureType.PreColor:
            //    case AtlasStamper.RenderPool.RenderTextureType.FinalColor:
            //
            //        color = Color.gray;
            //
            //        break;
            //
            //    case AtlasStamper.RenderPool.RenderTextureType.FinalSplat1:
            //    case AtlasStamper.RenderPool.RenderTextureType.PreSplat1:
            //    case AtlasStamper.RenderPool.RenderTextureType.FinalHole:
            //    case AtlasStamper.RenderPool.RenderTextureType.PreHole:
            //    case AtlasStamper.RenderPool.RenderTextureType.Hole:
            //
            //        color = new Color(1, 0, 0, 0);
            //
            //        break;
            //
            //    case AtlasStamper.RenderPool.RenderTextureType.FinalSplat2:
            //    case AtlasStamper.RenderPool.RenderTextureType.FinalSplat3:
            //    case AtlasStamper.RenderPool.RenderTextureType.FinalSplat4:
            //    case AtlasStamper.RenderPool.RenderTextureType.PreSplat2:
            //    case AtlasStamper.RenderPool.RenderTextureType.PreSplat3:
            //    case AtlasStamper.RenderPool.RenderTextureType.PreSplat4:
            //    case AtlasStamper.RenderPool.RenderTextureType.Highlight:
            //    case AtlasStamper.RenderPool.RenderTextureType.PreHighlight:
            //    case AtlasStamper.RenderPool.RenderTextureType.FinalHighlight:
            //
            //        color = new Color(0, 0, 0, 0);
            //
            //        break;
            //
            //}
            //
            //return color;

        }

        public static int GetRenderTextureResolution(AtlasStamper.RenderPool.RenderTextureType renderTextureType, int resolution, int splatResolution, bool forUnityTerrain) {

            switch (renderTextureType) {

                default: return resolution;


                case AtlasStamper.RenderPool.RenderTextureType.PreHeight:
                case AtlasStamper.RenderPool.RenderTextureType.Height:
                case AtlasStamper.RenderPool.RenderTextureType.FinalHeight:

                    return resolution + 1;

                case AtlasStamper.RenderPool.RenderTextureType.FinalSplat1:
                case AtlasStamper.RenderPool.RenderTextureType.FinalSplat2:
                case AtlasStamper.RenderPool.RenderTextureType.FinalSplat3:
                case AtlasStamper.RenderPool.RenderTextureType.FinalSplat4:
                case AtlasStamper.RenderPool.RenderTextureType.PreSplat1:
                case AtlasStamper.RenderPool.RenderTextureType.PreSplat2:
                case AtlasStamper.RenderPool.RenderTextureType.PreSplat3:
                case AtlasStamper.RenderPool.RenderTextureType.PreSplat4:
                case AtlasStamper.RenderPool.RenderTextureType.Other:
                case AtlasStamper.RenderPool.RenderTextureType.SplatSum:

                    return splatResolution;

            }

            //var res = resolution;
            //
            //if (forUnityTerrain) {
            //
            //    switch (renderTextureType) {
            //
            //        case AtlasStamper.RenderPool.RenderTextureType.FinalSplat1:
            //        case AtlasStamper.RenderPool.RenderTextureType.FinalSplat2:
            //        case AtlasStamper.RenderPool.RenderTextureType.FinalSplat3:
            //        case AtlasStamper.RenderPool.RenderTextureType.FinalSplat4:
            //        case AtlasStamper.RenderPool.RenderTextureType.PreSplat1:
            //        case AtlasStamper.RenderPool.RenderTextureType.PreSplat2:
            //        case AtlasStamper.RenderPool.RenderTextureType.PreSplat3:
            //        case AtlasStamper.RenderPool.RenderTextureType.PreSplat4:
            //        case AtlasStamper.RenderPool.RenderTextureType.Other:
            //        case AtlasStamper.RenderPool.RenderTextureType.SplatSum:
            //
            //            res = splatResolution;
            //
            //            break;
            //
            //    }
            //
            //    switch (renderTextureType) {
            //
            //        case AtlasStamper.RenderPool.RenderTextureType.PreHeight:
            //        case AtlasStamper.RenderPool.RenderTextureType.Height:
            //        case AtlasStamper.RenderPool.RenderTextureType.FinalHeight:
            //
            //            res = resolution + 1;
            //
            //            break;
            //
            //    }
            //
            //}
            //
            //return res;

        }

        public static RenderTextureFormat GetRenderTextureFormat(AtlasStamper.RenderPool.RenderTextureType renderTextureType) {

            switch (renderTextureType) {

                default:

                    return RenderTextureFormat.ARGB32;

                case AtlasStamper.RenderPool.RenderTextureType.PreHeight:
                case AtlasStamper.RenderPool.RenderTextureType.Height:
                case AtlasStamper.RenderPool.RenderTextureType.FinalHeight:
                case AtlasStamper.RenderPool.RenderTextureType.Mask:
                case AtlasStamper.RenderPool.RenderTextureType.RoadMask:
                case AtlasStamper.RenderPool.RenderTextureType.PreRoadMask:
                case AtlasStamper.RenderPool.RenderTextureType.FinalRoadMask:
                case AtlasStamper.RenderPool.RenderTextureType.SplatSum:
                case AtlasStamper.RenderPool.RenderTextureType.Hole:
                case AtlasStamper.RenderPool.RenderTextureType.PreHole:
                case AtlasStamper.RenderPool.RenderTextureType.FinalHole:

                    return RenderTextureFormat.RFloat;

            }

            //var format = RenderTextureFormat.ARGB32;
            //
            //switch (renderTextureType) {
            //
            //    case AtlasStamper.RenderPool.RenderTextureType.PreHeight:
            //    case AtlasStamper.RenderPool.RenderTextureType.Height:
            //    case AtlasStamper.RenderPool.RenderTextureType.FinalHeight:
            //    case AtlasStamper.RenderPool.RenderTextureType.Mask:
            //    case AtlasStamper.RenderPool.RenderTextureType.RoadMask:
            //    case AtlasStamper.RenderPool.RenderTextureType.PreRoadMask:
            //    case AtlasStamper.RenderPool.RenderTextureType.FinalRoadMask:
            //    case AtlasStamper.RenderPool.RenderTextureType.SplatSum:
            //
            //        format = RenderTextureFormat.RFloat;
            //
            //        break;
            //
            //}
            //
            //return format;

        }

        public static void CenterAndSizeFromIndex(int index1, int index2, int count1, int count2, Vector3 center, Vector3 size, out Vector3 outCenter, out Vector3 outSize) {

            outSize.x = size.x / count1;
            outSize.y = size.y;
            outSize.z = size.z / count2;

            outCenter.x = center.x - (size.x * 0.5f) + (index1 * outSize.x) + (outSize.x * 0.5f);
            outCenter.y = center.y;
            outCenter.z = center.z - (size.z * 0.5f) + (index2 * outSize.z) + (outSize.z * 0.5f);

        }

        public static int XYToIndex(int x, int y, int county) {

            return y + (x * county);

        }

        public static Color IndexToColor(int index) {

            var color = new Color(0.0f, 0.3f, 0.6f, 1f);

            switch (index % 3) {

                case 1: color = Color.black; break;

                case 2: color = new Color(1f, 0.6f, 0.0f, 1f); break;

            }

            return color;

        }

        public static bool IsHDRP() {

            if (GraphicsSettings.currentRenderPipeline) {
                if (GraphicsSettings.currentRenderPipeline.GetType().ToString().Contains("HighDefinition")) {
                    return true;
                }
            }

            return false;

        }

        public static bool IsURP() {

            if (GraphicsSettings.currentRenderPipeline) {
                if (!GraphicsSettings.currentRenderPipeline.GetType().ToString().Contains("HighDefinition")) {
                    return true;
                }
            }

            return false;

        }

        public static bool IsValidPath(string path, out string error) {

            error = "";

            if (string.IsNullOrEmpty(path)) {

                error = "null or empty";

                return false;

            }

            if (path.ToLower().StartsWith("assets/") == false) {

                error = "needs to start with 'Assets/' : " + path;

                return false;

            }

            //if (path.Contains(Path.DirectorySeparatorChar)) {
            //
            //    error = "needs forward slashes '/' : " + path;
            //
            //    return false;
            //
            //}

            if (path.Contains("..")) {

                error = "'..' not allowed in path : " + path;

                return false;

            }

            foreach (var i in Path.GetInvalidPathChars()) {

                if (path.Contains(i)) {

                    error = "'" + i + "' not allowed in path : " + path;

                    return false;

                }

            }

            return true;

        }


        public static Texture2D LoadIcon(string path, string pattern = null) {

#if UNITY_EDITOR

            var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (asset == null && pattern != null) {

                var guids = AssetDatabase.FindAssets(pattern);

                if (guids.Length > 0) {

                    path = AssetDatabase.GUIDToAssetPath(guids[0]);

                }

            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);

#else

            return null;

#endif

        }

        public static void RememberTerrainDrawStates(bool goToEditMode) {

            if (goToEditMode) {



#if UNITY_2020_1_OR_NEWER

                var atlasUnityTerrainRenderers = GameObject.FindObjectsOfType<AtlasUnityTerrainRenderer>(true);

#else

                var atlasUnityTerrainRenderers = (AtlasUnityTerrainRenderer[])Resources.FindObjectsOfTypeAll(typeof(AtlasUnityTerrainRenderer));

#endif

                foreach (var i in atlasUnityTerrainRenderers) {

                    var t = i.GetComponent<Terrain>();

                    if (t != null) {

                        i.preDrawState = t.drawHeightmap;
                        i.preDrawTreesAndFoliageState = t.drawTreesAndFoliage;

                    }

                }

            }

        }

        public static void RenderStampAssetPreview(RenderTexture renderTexture, StampAsset stampAsset) {

            if( stampAsset != null) {

                var shader = Shader.Find("Hidden/Atlas/AtlasThumbnail");

                if( shader != null) {

                    var mat = new Material(shader);

                    mat.SetTexture("_MainTex", stampAsset.color);
                    mat.SetTexture("_Mask", stampAsset.mask);
                    mat.SetTexture("_Height", stampAsset.height);

                    Graphics.Blit(null, renderTexture, mat);

                    GameObject.DestroyImmediate(mat,false);

                }

            }

        }

    }

    public class AtlasScatterer : IDisposable {

        public AtlasStamper atlasStamper { get; private set; }
        public Terrain terrain { get; private set; }
        public List<ScatterRuleAsset> scatterRuleAssets;
        public List<List<ScatterInstance>> nodes { get; private set; }
        public List<int[,]> details { get; private set; }

        private Texture2D tempMask;

        private List<ScatterInstance>[,] gridLookup;
        private readonly Vector3 down = -Vector3.up;
        private RaycastHit hit;

        public AtlasScatterer(AtlasStamper atlasStamper, Terrain terrain, List<ScatterRuleAsset> scatterRuleAssets) {

            this.atlasStamper = atlasStamper;
            this.terrain = terrain;
            this.scatterRuleAssets = scatterRuleAssets;

        }

        public void Render() {

            InitTreePrototypes();

            InitDetailPrototypes();

            InitGridLookup();

            nodes = new List<List<ScatterInstance>>();

            details = new List<int[,]>();

            for (var i = 0; i < terrain.terrainData.detailPrototypes.Length; i++) {

                details.Add(terrain.terrainData.GetDetailLayer(0, 0, terrain.terrainData.detailWidth, terrain.terrainData.detailHeight, i));

            }

            foreach (var i in scatterRuleAssets) {

                RenderScatterRuleAsset(i);

            }

            Apply();

        }

        public List<int> GetTreePrototypeIndexesToScatter() {

            var ret = new List<int>();

            foreach (var scatterRuleAsset in scatterRuleAssets) {

                if (scatterRuleAsset != null) {

                    foreach (var scatterStack in scatterRuleAsset.scatterStack) {

                        if (scatterStack.mode == 0) {

                            foreach (var treePrototypeIndex in scatterStack.treePrototypeIndexes) {

                                if (treePrototypeIndex > -1 && ret.Contains(treePrototypeIndex) == false) {

                                    ret.Add(treePrototypeIndex);

                                }

                            }

                        }

                    }

                }

            }

            return ret;

        }

        public List<int> GetDetailPrototypeIndexesToScatter() { //always clear all detail
        
            var ret = new List<int>();
        
            foreach (var scatterRuleAsset in scatterRuleAssets) {
        
                if (scatterRuleAsset != null) {
        
                    foreach (var detailStack in scatterRuleAsset.detailStack) {
        
                        foreach (var detailPrototypeIndex in detailStack.detailPrototypeIndexes) {
        
                            if (detailPrototypeIndex > -1 && ret.Contains(detailPrototypeIndex) == false) {
        
                                ret.Add(detailPrototypeIndex);
        
                            }
        
                        }
        
                    }
        
                }
        
            }
        
            return ret;
        
        }

        public void ClearTreeInstances(List<int> treePrototypeIndexes) {

            var treeInstances = terrain.terrainData.treeInstances.ToList();

            for (var i = treeInstances.Count - 1; i >= 0; i--) {

                if (treePrototypeIndexes.Contains(treeInstances[i].prototypeIndex)) {

                    treeInstances.RemoveAt(i);

                }

            }

            terrain.terrainData.treeInstances = treeInstances.ToArray();

        }

        public void ClearDetails(List<int> detailPrototypeIndexes) {

            var emptyDetails = new int[terrain.terrainData.detailWidth, terrain.terrainData.detailHeight];

            foreach (var i in detailPrototypeIndexes) {

                if (i >= 0 && i < terrain.terrainData.detailPrototypes.Length) {

                    terrain.terrainData.SetDetailLayer(0, 0, i, emptyDetails);

                }

            }

        }


        private void InitTreePrototypes() {

            var treePrototypes = terrain.terrainData.treePrototypes.ToList();

            foreach (var scatterRuleAsset in scatterRuleAssets) {

                if (scatterRuleAsset != null) {

                    foreach (var scatterStack in scatterRuleAsset.scatterStack) {

                        if (scatterStack.mode == 0) {

                            if (scatterStack.prefabs != null) {

                                scatterStack.treePrototypeIndexes = new int[scatterStack.prefabs.Length];

                                for (var i = 0; i < scatterStack.prefabs.Length; i++) {

                                    var prefab = scatterStack.prefabs[i];

                                    if (prefab != null) {

                                        var found = false;

                                        foreach (var treePrototype in treePrototypes) {

                                            if (treePrototype.prefab == prefab) {

                                                found = true;

                                                break;

                                            }

                                        }

                                        if (found == false) {

                                            treePrototypes.Add(new TreePrototype() {
                                                prefab = prefab,
                                            });

                                        }

                                        scatterStack.treePrototypeIndexes[i] = treePrototypes.FindIndex(x => x.prefab == prefab);

                                    } else {

                                        scatterStack.treePrototypeIndexes[i] = -1;

                                    }

                                }

                            }

                        }

                    }

                }

            }

            treePrototypes.RemoveAll(x => x.prefab == null);

            terrain.terrainData.treePrototypes = treePrototypes.ToArray();

        }

        private void InitDetailPrototypes() {

            var detailPrototypes = terrain.terrainData.detailPrototypes.ToList(); //always clear all detail

            var addedDetailPrototypes = new List<DetailPrototype>();

            foreach (var scatterRuleAsset in scatterRuleAssets) {

                if (scatterRuleAsset != null) {

                    foreach (var detailStack in scatterRuleAsset.detailStack) {

                        if (detailStack.useMesh) {

                            if (detailStack.prefabs != null) {

                                detailStack.detailPrototypeIndexes = new int[detailStack.prefabs.Length];

                                for (var i = 0; i < detailStack.prefabs.Length; i++) {

                                    var prefab = detailStack.prefabs[i];

                                    if (prefab != null) {

                                        var found = false;

                                        foreach (var deltailPrototype in detailPrototypes) {

                                            if (deltailPrototype.usePrototypeMesh && deltailPrototype.prototype.name == prefab.name) {

                                                found = true;

                                                break;

                                            }

                                        }

                                        if (found == false) {

                                            var detailPrototype = new DetailPrototype() {
                                                usePrototypeMesh = detailStack.useMesh,
#if UNITY_2021_2_OR_NEWER
                                                useInstancing = detailStack.useInstancing,
#endif
                                                minHeight = detailStack.minHeight,
                                                maxHeight = detailStack.maxHeight,
                                                minWidth = detailStack.minWidth,
                                                maxWidth = detailStack.maxWidth,
                                                prototype = prefab,
                                            };

                                            detailPrototypes.Add(detailPrototype);

                                            addedDetailPrototypes.Add(detailPrototype);

                                        }

                                        detailStack.detailPrototypeIndexes[i] = detailPrototypes.FindIndex(x => x.prototype.name == prefab.name);

                                    } else {

                                        detailStack.detailPrototypeIndexes[i] = -1;

                                    }

                                }

                            }

                        }

                    }

                }

            }


            foreach (var scatterRuleAsset in scatterRuleAssets) {

                if (scatterRuleAsset != null) {

                    foreach (var detailStack in scatterRuleAsset.detailStack) {

                        if (detailStack.useMesh == false) {

                            if (detailStack.textures != null) {

                                detailStack.detailPrototypeIndexes = new int[detailStack.textures.Length];

                                for (var i = 0; i < detailStack.textures.Length; i++) {

                                    var texture = detailStack.textures[i];

                                    if (texture != null) {

                                        var found = false;

                                        foreach (var deltailPrototype in detailPrototypes) {

                                            if (deltailPrototype.usePrototypeMesh == false && deltailPrototype.prototypeTexture.name == texture.name) {

                                                found = true;

                                                break;

                                            }

                                        }

                                        if (found == false) {

                                            var detailPrototype = new DetailPrototype() {
                                                usePrototypeMesh = detailStack.useMesh,
#if UNITY_2021_2_OR_NEWER
                                                useInstancing = detailStack.useInstancing,
#endif
                                                minHeight = detailStack.minHeight,
                                                maxHeight = detailStack.maxHeight,
                                                minWidth = detailStack.minWidth,
                                                maxWidth = detailStack.maxWidth,
                                                prototypeTexture = texture,
                                            };

                                            detailPrototypes.Add(detailPrototype);

                                            addedDetailPrototypes.Add(detailPrototype);

                                        }

                                        detailStack.detailPrototypeIndexes[i] = detailPrototypes.FindIndex(x => x.prototypeTexture.name == texture.name);

                                    } else {

                                        detailStack.detailPrototypeIndexes[i] = -1;

                                    }

                                }

                            }

                        }

                    }

                }

            }

            foreach (var scatterRuleAsset in scatterRuleAssets) {

                if (scatterRuleAsset != null) {

                    foreach (var detailStack in scatterRuleAsset.detailStack) {

                        foreach (var detailPrototypeIndex in detailStack.detailPrototypeIndexes) {

                            var detailPrototype = detailPrototypes[detailPrototypeIndex];

                            detailPrototype.minHeight = detailStack.minHeight;
                            detailPrototype.maxHeight = detailStack.maxHeight;
                            detailPrototype.minWidth = detailStack.minWidth;
                            detailPrototype.maxWidth = detailStack.maxWidth;

#if UNITY_2020_2_OR_NEWER
                            detailPrototype.holeEdgePadding = detailStack.holeEdgePadding;
#endif

                            if (detailStack.useMesh) {

                                detailPrototype.renderMode = DetailRenderMode.VertexLit;

                            } else {

                                if (detailStack.renderMode == DetailRenderMode.VertexLit) {

                                    detailPrototype.renderMode = DetailRenderMode.Grass;

                                } else {

                                    detailPrototype.renderMode = detailStack.renderMode;

                                }

                            }

                        }

                    }

                }

            }


#if UNITY_EDITOR

            if (AtlasUtils.IsHDRP()) {

                var gotBillboards = false;

                foreach (var i in addedDetailPrototypes) {

                    if (i.usePrototypeMesh == false) {

                        gotBillboards = true;

                        break;

                    }

                }

                if (gotBillboards) {

                    if (UnityEditor.EditorUtility.DisplayDialog("HDRP grass billboard warning", "Atlas is about to assign billboard details to your terrain.\nThis can cause a crash when billboard details arn't supported in your HDRP project.\nWould you like to add the billboards?\n\nYou can always save your project and try it later.", "add them", "leave them out")) {

                    } else {

                        foreach (var i in addedDetailPrototypes) {

                            if (i.usePrototypeMesh == false) {

                                detailPrototypes.Remove(i);

                            }

                        }

                    }

                }

            }

#endif
#if UNITY_2022_2_OR_NEWER

            detailPrototypes.RemoveAll(x => x.Validate() == false);
#endif
            terrain.terrainData.detailPrototypes = detailPrototypes.ToArray();

        }

        private void RenderScatterRuleAsset(ScatterRuleAsset scatterRuleAsset) {

            if (scatterRuleAsset != null) {

                UnityEngine.Random.InitState(scatterRuleAsset.seed);

                RenderTempMask(scatterRuleAsset);

                if (scatterRuleAsset.scatterStack != null) {

                    for (var passIndex = 0; passIndex < scatterRuleAsset.scatterStack.Count; passIndex++) {

                        nodes.Add(new List<ScatterInstance>());

                        var pass = scatterRuleAsset.scatterStack[passIndex];

                        if (passIndex == 0) {

                            var xCount = Mathf.FloorToInt(terrain.terrainData.size.x / scatterRuleAsset.spacing);
                            var zCount = Mathf.FloorToInt(terrain.terrainData.size.z / scatterRuleAsset.spacing);
                            var xQuanta = 1f / xCount;
                            var zQuanta = 1f / zCount;

                            for (var x = 0f; x < 1f; x += xQuanta) {

                                for (var z = 0f; z < 1f; z += zQuanta) {

                                    var randomizedPosition = new Vector3(UnityEngine.Random.Range(-xQuanta * 0.5f, xQuanta * 0.5f), 0, UnityEngine.Random.Range(-zQuanta * 0.5f, zQuanta * 0.5f));

                                    var position = new Vector3(x, 0, z) + randomizedPosition;

                                    if (ValidPosition(scatterRuleAsset, terrain, position, null, false, scatterRuleAsset.selfCulling)) {

                                        var maskFactor = GetMaskFactor(position);

                                        var scaleMultiplier = UnityEngine.Random.Range(pass.scaleMultiplierMin, pass.scaleMultiplierMax) * Mathf.Lerp(pass.maskScaleMultiplier, 1, maskFactor); ;

                                        var node = new ScatterInstance() {
                                            mode = pass.mode,
                                            position = position,
                                            cullRadius = pass.cullRadius,
                                            detailCullRadius = pass.detailCullRadius,
                                            heightScale = UnityEngine.Random.Range(pass.heightScaleMin, pass.heightScaleMax) * scaleMultiplier,
                                            widthScale = UnityEngine.Random.Range(pass.widthScaleMin, pass.widthScaleMax) * scaleMultiplier,
                                            rotation = UnityEngine.Random.Range(0, Mathf.PI * 2),
                                            alignment = pass.alignment,
                                            heightOffset = UnityEngine.Random.Range(pass.heightOffsetMin, pass.heightOffsetMax),
                                        };

                                        if (pass.treePrototypeIndexes != null && pass.treePrototypeIndexes.Length != 0) {

                                            if (pass.treePrototypeIndexes.Length == 1) {

                                                node.treePrototypeIndex = pass.treePrototypeIndexes[0];

                                            } else {

                                                node.treePrototypeIndex = pass.treePrototypeIndexes[UnityEngine.Random.Range(0, pass.treePrototypeIndexes.Length)];

                                            }

                                        }

                                        if (pass.prefabs != null && pass.prefabs.Length != 0) {

                                            if (pass.prefabs.Length == 1) {

                                                node.prefab = pass.prefabs[0];

                                            } else {

                                                node.prefab = pass.prefabs[UnityEngine.Random.Range(0, pass.prefabs.Length)];

                                            }

                                        }

                                        nodes[passIndex].Add(node);

                                        var xlookupIndex = Mathf.FloorToInt(position.x * gridLookup.GetLongLength(0));
                                        var zlookupIndex = Mathf.FloorToInt(position.z * gridLookup.GetLongLength(1));

                                        if (gridLookup[xlookupIndex, zlookupIndex] == null) {

                                            gridLookup[xlookupIndex, zlookupIndex] = new List<ScatterInstance>();

                                        }

                                        gridLookup[xlookupIndex, zlookupIndex].Add(node);

                                    }

                                }

                            }

                        } else {

                            var prePass = scatterRuleAsset.scatterStack[passIndex - 1];

                            foreach (var parentNode in nodes[passIndex - 1]) {

                                for (var i = 0; i < prePass.childCount; i++) {

                                    var randomCircle = UnityEngine.Random.insideUnitCircle;

                                    var randomizedPosition = new Vector3(randomCircle.x * (prePass.spawnRadius / terrain.terrainData.size.x), 0, randomCircle.y * (prePass.spawnRadius / terrain.terrainData.size.z));

                                    var position = parentNode.position + randomizedPosition;

                                    if (ValidPosition(scatterRuleAsset, terrain, position, gridLookup, false, scatterRuleAsset.selfCulling)) {

                                        var distanceFactor = Vector3.Distance(ToWorldPosition(terrain, parentNode.position), ToWorldPosition(terrain, position)) / prePass.spawnRadius;

                                        var maskFactor = GetMaskFactor(position);

                                        var scaleMultiplier = UnityEngine.Random.Range(pass.scaleMultiplierMin, pass.scaleMultiplierMax) * Mathf.Lerp(1, pass.distanceScaleMultiplier, distanceFactor) * Mathf.Lerp(pass.maskScaleMultiplier,1, maskFactor);

                                        var node = new ScatterInstance() {
                                            mode = pass.mode,
                                            position = position,
                                            cullRadius = pass.cullRadius,
                                            detailCullRadius = pass.detailCullRadius,
                                            heightScale = UnityEngine.Random.Range(pass.heightScaleMin, pass.heightScaleMax) * scaleMultiplier,
                                            widthScale = UnityEngine.Random.Range(pass.widthScaleMin, pass.widthScaleMax) * scaleMultiplier,
                                            rotation = UnityEngine.Random.Range(0, Mathf.PI * 2),
                                            alignment = pass.alignment,
                                            heightOffset = UnityEngine.Random.Range(pass.heightOffsetMin, pass.heightOffsetMax),
                                        };

                                        if (pass.treePrototypeIndexes != null && pass.treePrototypeIndexes.Length != 0) {

                                            if (pass.treePrototypeIndexes.Length == 1) {

                                                node.treePrototypeIndex = pass.treePrototypeIndexes[0];

                                            } else {

                                                node.treePrototypeIndex = pass.treePrototypeIndexes[UnityEngine.Random.Range(0, pass.treePrototypeIndexes.Length)];

                                            }

                                        }

                                        if (pass.prefabs != null && pass.prefabs.Length != 0) {

                                            if (pass.prefabs.Length == 1) {

                                                node.prefab = pass.prefabs[0];

                                            } else {

                                                node.prefab = pass.prefabs[UnityEngine.Random.Range(0, pass.prefabs.Length)];

                                            }

                                        }

                                        nodes[passIndex].Add(node);

                                        var xlookupIndex = Mathf.FloorToInt(position.x * gridLookup.GetLongLength(0));
                                        var zlookupIndex = Mathf.FloorToInt(position.z * gridLookup.GetLongLength(1));

                                        if (gridLookup[xlookupIndex, zlookupIndex] == null) {

                                            gridLookup[xlookupIndex, zlookupIndex] = new List<ScatterInstance>();

                                        }

                                        gridLookup[xlookupIndex, zlookupIndex].Add(node);

                                    }

                                }

                            }

                        }

                    }

                }

                if (scatterRuleAsset.detailStack != null) {

                    var xCount = terrain.terrainData.detailWidth;
                    var zCount = terrain.terrainData.detailHeight;
                    var xQuanta = 1f / xCount;
                    var zQuanta = 1f / zCount;
                    var centerQuantaOffset = new Vector3(xQuanta * 0.5f, 0, zQuanta * 0.5f);

                    for (var x = 0; x < xCount; x++) {

                        for (var z = 0; z < zCount; z++) {

                            var position = new Vector3(x * xQuanta, 0, z * zQuanta) + centerQuantaOffset;

                            for (var passIndex = 0; passIndex < scatterRuleAsset.detailStack.Count; passIndex++) {

                                var pass = scatterRuleAsset.detailStack[passIndex];

                                if (pass.detailPrototypeIndexes != null && pass.detailPrototypeIndexes.Length != 0) {

                                    var detailPrototypeIndex = pass.detailPrototypeIndexes[UnityEngine.Random.Range(0, pass.detailPrototypeIndexes.Length)];

                                    if (detailPrototypeIndex > -1 && detailPrototypeIndex < details.Count) {

                                        if (ValidPosition(scatterRuleAsset, terrain, position, gridLookup, true, scatterRuleAsset.selfCulling)) {

                                            details[detailPrototypeIndex][z, x] += Mathf.CeilToInt((float)pass.density * GetMaskFactor(position));

                                        }

                                    }

                                }

                            }

                        }

                    }

                }

                GameObject.DestroyImmediate(tempMask, false);

            }

        }

        private void Apply() {

            if (nodes != null) {

                var treeInstances = terrain.terrainData.treeInstances.ToList();

                var hasAnyPrefabs = false;

                foreach (var nodeList in nodes) {

                    foreach (var i in nodeList) {

                        if (i.mode != 0 && i.prefab != null) {

                            hasAnyPrefabs = true;

                            break;

                        }

                    }

                    if (hasAnyPrefabs) {

                        break;

                    }

                }

                Transform parent = terrain.transform.Find("ScatteredGameObjects");

                if (parent != null) {

                    GameObject.DestroyImmediate(parent.gameObject, false);

                }

                if (hasAnyPrefabs) {

                    var parentGameObject = new GameObject("ScatteredGameObjects");

                    parent = parentGameObject.transform;

                    parent.SetParent(terrain.transform);

                }

                foreach (var nodeList in nodes) {

                    foreach (var i in nodeList) {

                        if (i.mode == 0) {

                            treeInstances.Add(new TreeInstance() {
                                position = i.position,
                                prototypeIndex = i.treePrototypeIndex,
                                heightScale = i.heightScale,
                                widthScale = i.widthScale,
                                rotation = i.rotation,
                            });

                        } else {

                            if (i.prefab != null) {
#if UNITY_EDITOR
                                var o = (GameObject)PrefabUtility.InstantiatePrefab(i.prefab);
#else
                                var o = GameObject.Instantiate(i.prefab);

#endif
                                o.transform.localScale = new Vector3(i.widthScale, i.heightScale, i.widthScale);
                                o.transform.rotation = Quaternion.LookRotation(Vector3.Lerp(Vector3.up, terrain.terrainData.GetInterpolatedNormal(i.position.x, i.position.z), i.alignment)) * Quaternion.Euler(90, 0, 0) * Quaternion.Euler(0, i.rotation * Mathf.Rad2Deg, 0);
                                o.transform.position = ToWorldPosition(terrain, i.position) - (o.transform.up * i.heightOffset);
                                o.transform.SetParent(parent);

                            }

                        }

                    }

                }

                terrain.terrainData.SetTreeInstances(treeInstances.ToArray(), true);

                for (var i = 0; i < terrain.terrainData.detailPrototypes.Length; i++) {

                    terrain.terrainData.SetDetailLayer(0, 0, i, details[i]);

                }

                terrain.Flush();

            }

        }

        private bool ValidPosition(ScatterRuleAsset scatterRuleAsset, Terrain terrain, Vector3 position, List<ScatterInstance>[,] gridLookup = null, bool forDetail = false, bool selfCulling = false) {

            if (position.x < 0 || position.x >= 1 || position.z < 0 || position.z >= 1) {

                return false;

            }

            var height = ToWorldPosition(terrain, position).y;

            if (height < scatterRuleAsset.heightMin || height > scatterRuleAsset.heightMax) {

                return false;

            }

            var steepness = terrain.terrainData.GetSteepness(position.x, position.z);

            if (steepness < scatterRuleAsset.slopeMin || steepness > scatterRuleAsset.slopeMax) {

                return false;

            }

            var maskFactor = GetMaskFactor(position);

            if (maskFactor < scatterRuleAsset.maskFactorMin || maskFactor > scatterRuleAsset.maskFactorMax) {

                return false;

            }

            var worldPosition = ToWorldPosition(terrain, position);

            if (string.IsNullOrEmpty(scatterRuleAsset.blockTagLayer) == false) {

                if (scatterRuleAsset.invertTagLayer == false) {

                    if (Physics.Raycast(worldPosition - down * 10000, down, out hit, 10000)) {

                        if (hit.collider.gameObject != terrain.gameObject && hit.collider.gameObject.tag == scatterRuleAsset.blockTagLayer) {

                            return false;

                        }

                    }

                } else {

                    if (Physics.Raycast(worldPosition - down * 10000, down, out hit, 10000)) {

                        if (hit.collider.gameObject.tag != scatterRuleAsset.blockTagLayer) {

                            return false;

                        }

                    } else {

                        return false;

                    }

                }

            }

            if (selfCulling && gridLookup != null) {

                var xlookupIndex = Mathf.FloorToInt(position.x * gridLookup.GetLongLength(0));
                var zlookupIndex = Mathf.FloorToInt(position.z * gridLookup.GetLongLength(1));

                for (var x = xlookupIndex - 1; x < xlookupIndex + 1; x++) {

                    if (x < 0 || x >= gridLookup.GetLongLength(0)) { continue; }

                    for (var z = zlookupIndex - 1; z < zlookupIndex + 1; z++) {

                        if (z < 0 || z >= gridLookup.GetLongLength(1)) { continue; }

                        if (gridLookup[x, z] != null) {

                            foreach (var i in gridLookup[x, z]) {

                                var distance = Vector3.Distance(Vector3.Scale(position, terrain.terrainData.size), Vector3.Scale(i.position, terrain.terrainData.size));

                                if (distance < (forDetail ? i.detailCullRadius : i.cullRadius)) {

                                    return false;

                                }

                            }

                        }

                    }

                }

            }

            return true;

        }

        private float GetMaskFactor(Vector3 position) {

            if (tempMask != null) {

                return tempMask.GetPixelBilinear(position.x, position.z).r;

                //var color = tempMask.GetPixelBilinear(position.x, position.z);
                //
                //switch (tempMaskChannelIndex) {
                //
                //    case 0: return color.r;
                //    case 1: return color.g;
                //    case 2: return color.b;
                //    case 3: return color.a;
                //
                //}

            }

            return 1f;

        }

        private Vector3 ToWorldPosition(Terrain terrain, Vector3 position) {

            var p = terrain.transform.TransformPoint(Vector3.Scale(position, terrain.terrainData.size));

            p.y += terrain.SampleHeight(p);

            return p;

        }

        private void RenderTempMask(ScatterRuleAsset scatterRuleAsset) {

            var renderTexture = new RenderTexture(atlasStamper.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalSplat1).width, atlasStamper.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalSplat1).height, 0, RenderTextureFormat.ARGBFloat);

            renderTexture.name = "Atlas_scatter_temp_mask_rt";

            var mergeMaterial = new Material(Shader.Find("Hidden/Atlas/AtlasScatterChannelMerge"));

            for (int i = 0; i <= 16; ++i) {

                if ((scatterRuleAsset.target & 1 << i) == 1 << i) {

                    StampTargetTypeToRenderTextureTypeAndChannel((ScatterTargets)i, out var renderTextureType, out var channelIndex);

                    mergeMaterial.SetInt("_ChannelIndex", channelIndex);

                    Graphics.Blit(atlasStamper.renderPool.GetRenderTexture(renderTextureType), renderTexture, mergeMaterial);

                }

            }

            RenderTexture.active = renderTexture;

            tempMask = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false, true) {
                name = "Scatter_Temp_Mask",
            };

            tempMask.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);

            tempMask.Apply();

            RenderTexture.active = null;

            renderTexture.Release();

            GameObject.DestroyImmediate(renderTexture, false);

            GameObject.DestroyImmediate(mergeMaterial, false);

        }

        private void InitGridLookup() {

            var biggestCullRadius = float.MinValue;

            foreach (var i in scatterRuleAssets) {

                if (i.scatterStack != null) {

                    foreach (var ii in i.scatterStack) {

                        if (ii.cullRadius > biggestCullRadius) {

                            biggestCullRadius = ii.cullRadius;

                        }

                    }

                }

            }

            gridLookup = new List<ScatterInstance>[Mathf.Max(1, Mathf.FloorToInt(terrain.terrainData.size.x / (biggestCullRadius * 2))), Mathf.Max(1, Mathf.FloorToInt(terrain.terrainData.size.z / (biggestCullRadius * 2)))];

        }

        private void StampTargetTypeToRenderTextureTypeAndChannel(ScatterTargets scatterTargetType, out AtlasStamper.RenderPool.RenderTextureType renderTextureType, out int channelIndex) {

            renderTextureType = AtlasStamper.RenderPool.RenderTextureType.FinalSplat1;
            channelIndex = 0;

            switch (scatterTargetType) {

                case ScatterTargets.Splat5:
                case ScatterTargets.Splat6:
                case ScatterTargets.Splat7:
                case ScatterTargets.Splat8:
                    renderTextureType = AtlasStamper.RenderPool.RenderTextureType.FinalSplat2;
                    break;
                case ScatterTargets.Splat9:
                case ScatterTargets.Splat10:
                case ScatterTargets.Splat11:
                case ScatterTargets.Splat12:
                    renderTextureType = AtlasStamper.RenderPool.RenderTextureType.FinalSplat3;
                    break;
                case ScatterTargets.Splat13:
                case ScatterTargets.Splat14:
                case ScatterTargets.Splat15:
                case ScatterTargets.Splat16:
                    renderTextureType = AtlasStamper.RenderPool.RenderTextureType.FinalSplat4;
                    break;

            }

            switch (scatterTargetType) {

                case ScatterTargets.Splat2:
                case ScatterTargets.Splat6:
                case ScatterTargets.Splat10:
                case ScatterTargets.Splat14:
                    channelIndex = 1;
                    break;
                case ScatterTargets.Splat3:
                case ScatterTargets.Splat7:
                case ScatterTargets.Splat11:
                case ScatterTargets.Splat15:
                    channelIndex = 2;
                    break;
                case ScatterTargets.Splat4:
                case ScatterTargets.Splat8:
                case ScatterTargets.Splat12:
                case ScatterTargets.Splat16:
                    channelIndex = 3;
                    break;

            }

        }


        public void Dispose() {

            Dispose(true);

            GC.SuppressFinalize(this);

        }

        protected virtual void Dispose(bool disposing) {

            if (disposing) {

                GameObject.DestroyImmediate(tempMask, false);

            }

        }


        public class ScatterInstance {

            public Vector3 position;

            public int mode;
            public float cullRadius;
            public float detailCullRadius;
            public int treePrototypeIndex;
            public GameObject prefab;
            public float heightScale;
            public float widthScale;
            public float rotation;
            public float alignment;
            public float heightOffset;

        }

        public enum ScatterTargets {
            Splat1,
            Splat2,
            Splat3,
            Splat4,
            Splat5,
            Splat6,
            Splat7,
            Splat8,
            Splat9,
            Splat10,
            Splat11,
            Splat12,
            Splat13,
            Splat14,
            Splat15,
            Splat16,
        }

    }

    public enum PreviewRenderMode {

        Color, Splat1, Splat2, Splat3, Splat4, SolidColor,

    }

    public enum WResolution {

        R4096_13, R2048_12, R1024_11, R512_10, R256_9, R128_8, R64_7, R32_6, R16_5, R8_4, R4_3, R2_2, R1_1, R0_0,

    }

    public enum SimpleResolution {

        Full, Half, Quarter, Eight, Sixteenth,

    }

}