using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Atlas.Unity {

    [AddComponentMenu("")]
    [ExecuteInEditMode]
    public class StampBase : MonoBehaviour {

        [Tooltip("Stamp asset.")]
        public StampAsset stamp;

        public Texture maskOverride;

        [Tooltip("Mask settings\n\nThe entire stamp will fade based on this mask.")]
        public StampMap maskMap = new StampMap() { mapType = StampMap.StampMapType.Mask, target = StampMap.StampTargetType.None, input = StampMap.StampTexture.Mask, modifier = new StampMap.Modifier() { modifierType = StampMap.Modifier.ModifierType.Mask } };

        [Tooltip("Heightmap settings\n\nHow the stamp will alter the height of your terrain.")]
        public StampMap heightMap = new StampMap() { mapType = StampMap.StampMapType.Height, target = StampMap.StampTargetType.Height, input = StampMap.StampTexture.Height, modifier = new StampMap.Modifier() { modifierType = StampMap.Modifier.ModifierType.Height } };

        [Tooltip("Colormap settings\n\nThe built-in unity terrain doesn't support a global colormap. If you happen to have a shader that supports this you can retrieve the global colormap through the 'AtlasUnityTerrainRenderer' component which is assigned to your terrain.")]
        public StampMap colorMap = new StampMap() { mapType = StampMap.StampMapType.Color, target = StampMap.StampTargetType.Color, input = StampMap.StampTexture.Color, modifier = new StampMap.Modifier() { modifierType = StampMap.Modifier.ModifierType.Color } };

        [Tooltip("Colormap settings\n\nThe built-in unity terrain doesn't support a global colormap. If you happen to have a shader that supports this you can retrieve the global colormap through the 'AtlasUnityTerrainRenderer' component which is assigned to your terrain.")]
        public StampMap highlightMap = new StampMap() { mapType = StampMap.StampMapType.Highlight, target = StampMap.StampTargetType.Highlight, input = StampMap.StampTexture.None, modifier = new StampMap.Modifier() { modifierType = StampMap.Modifier.ModifierType.Highlight } };

        public StampMap roadMaskMap = new StampMap() { mapType = StampMap.StampMapType.RoadMask, target = StampMap.StampTargetType.None, input = StampMap.StampTexture.RoadMask, modifier = new StampMap.Modifier() { modifierType = StampMap.Modifier.ModifierType.Mask } };

        [Tooltip("Splatmap settings\n\nChoose an input from your stamp and write to a target splatmap of your terrain.\n\nAdd multiple layers for intriguing results.")]
        public List<StampMap> stampMaps = new List<StampMap>() { new StampMap(), };

        [Tooltip("Holemap settings\n\nHow the stamp will make holes in your terrain.")]
        public StampMap holeMap = new StampMap() { mapType = StampMap.StampMapType.Hole, target = StampMap.StampTargetType.Hole, input = StampMap.StampTexture.None, modifier = new StampMap.Modifier() { modifierType = StampMap.Modifier.ModifierType.Hole, enabled = false, } };

        public Vector3 center = Vector3.zero;

        [Tooltip("Stamp size.")]
        public Vector3 size = Vector3.one;

        [Tooltip("When enabled the stamp will use its 'road mask' to make intersections possible.")]
        public bool roadBlending = false;

        [Tooltip("Should the mask also be tiled?")]
        public bool tileMask = false;

        [Tooltip("Howmush the stamp texture will repeat.")]
        public Vector2 stampTiling = Vector2.one;

        public float _selected {

            get {

#if UNITY_EDITOR

                return UnityEditor.Selection.gameObjects.Length > 0 && UnityEditor.Selection.gameObjects.Contains(gameObject) ? 0.8f : 0;

#else

                return 0;
#endif


            }
        }


        private void OnEnable() {

            AtlasStamper.QueRender();

        }

        private void OnDisable() {

            AtlasStamper.QueRender();

        }

        private void OnValidate() {

            AtlasStamper.QueRender();

        }

        private void OnDrawGizmos() {

#if UNITY_EDITOR

            DrawGizmos();

            if (MayDrawIcon(out var path)) {

                Gizmos.DrawIcon(transform.TransformPoint(center + (Vector3.up * size.y)), path, true);

            }

#endif

        }

        public virtual void DrawGizmos() {

        }

        public virtual void OnBeforeRenderForExport() {

        }

        public virtual void OnAfterRenderForExport() {

        }

        public virtual bool MayDrawIcon(out string path) {

            path = null;

            return false;

        }

        public virtual bool MayRender() {

            return false;

        }

        public virtual bool SkipMask() {

            return false;

        }

        public virtual void Render(AtlasStamper stampTerrainBase, int splatmapCount) {

            //cleanup required, almost hitting critical mass!!!

            if (!MayRender()) { return; }

            //get render textures

            var preHeight = stampTerrainBase.renderPool.ClearAndGetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.PreHeight);
            var height = stampTerrainBase.renderPool.ClearAndGetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.Height);
            var mask = stampTerrainBase.renderPool.ClearAndGetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.Mask);
            var otherMask = stampTerrainBase.renderPool.ClearAndGetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.Other);
            var roadMask = stampTerrainBase.renderPool.ClearAndGetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.RoadMask);
            var preRoadMask = stampTerrainBase.renderPool.ClearAndGetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.PreRoadMask);


            //map the final render textures onto the pre render textures

            Graphics.Blit(stampTerrainBase.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalHeight), preHeight);
            Graphics.Blit(stampTerrainBase.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalRoadMask), preRoadMask);



            //MASK ///////////////////////////////////////////////////////

            //render mask

            if (!SkipMask()) {

                //maskMap.ConfigureMaterialForRender(stamp, RenderTexturePool.maskMat, 0);

                maskMap.ConfigureMaterialForRender(stamp, this, stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasMask), 0);

                RenderTexture.active = mask;

                DrawMesh(stampTerrainBase, DrawMeshType.Mask, true);

            } else {

                RenderTexture.active = mask;

                GL.Clear(true, true, new Color(1, 1, 1, 1));

            }

            ////////////////////////////////////////////////////////////////



            //HEIGHT ///////////////////////////////////////////////////////

            //render height

            heightMap.ConfigureMaterialForRender(stamp, this, stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasHeight));

            RenderTexture.active = height;

            DrawMesh(stampTerrainBase, DrawMeshType.Height);

            //merge height

            heightMap.ConfigureMaterialForMerge(preHeight, mask, stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasHeightMerge), roadBlending ? preRoadMask : null);

            Graphics.Blit(height, stampTerrainBase.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalHeight), stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasHeightMerge), 0);

            //////////////////////////////////////////////////////////////////////



            //MIN MAX MASK ///////////////////////////////////////////////////////

            if (heightMap.modifier.blendMode == StampMap.Modifier.BlendMode.Max || heightMap.modifier.blendMode == StampMap.Modifier.BlendMode.Min) {

                stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasHeightBasedAlteration).SetTexture("_HeightTex", stampTerrainBase.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalHeight));
                stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasHeightBasedAlteration).SetTexture("_PreMainTex", preHeight);
                stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasHeightBasedAlteration).SetFloat("_EdgeBlend", heightMap.modifier.edgeBlend);
                stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasHeightBasedAlteration).SetInt("_Mode", (int)heightMap.modifier.blendMode);

                Graphics.Blit(null, mask, stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasHeightBasedAlteration), 0);

                //re render height

                heightMap.ConfigureMaterialForMerge(preHeight, mask, stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasHeightMerge), roadBlending ? preRoadMask : null);

                Graphics.Blit(height, stampTerrainBase.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalHeight), stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasHeightMerge), 0);

            }

            ///////////////////////////////////////////////////////////////



            //COLOR ///////////////////////////////////////////////////////

            if (AtlasUtils.previewMask == false) {

                if (stamp != null && stamp.color != null) {

                    var preColor = stampTerrainBase.renderPool.ClearAndGetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.PreColor);
                    var color = stampTerrainBase.renderPool.ClearAndGetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.Color);
                    Graphics.Blit(stampTerrainBase.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalColor), preColor);

                    //render color

                    colorMap.ConfigureMaterialForRender(stamp, this, stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasColor), 0);

                    RenderTexture.active = color;

                    DrawMesh(stampTerrainBase, DrawMeshType.Color);

                    //merge color 

                    colorMap.ConfigureMaterialForMerge(preColor, mask, stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasColorMerge), roadBlending ? preRoadMask : null);

                    Graphics.Blit(color, stampTerrainBase.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalColor), stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasColorMerge), 0);

                }

            }

            //////////////////////////////////////////////////////////////



            //HOLE ///////////////////////////////////////////////////////

            //render hole

            if (holeMap.modifier.enabled) {

                var hole = stampTerrainBase.renderPool.ClearAndGetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.Hole);
                var preHole = stampTerrainBase.renderPool.ClearAndGetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.PreHole);
                Graphics.Blit(stampTerrainBase.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalHole), preHole);

                holeMap.ConfigureMaterialForRender(stamp, this, stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasHole));

                RenderTexture.active = hole;

                DrawMesh(stampTerrainBase, DrawMeshType.Hole);

                //merge hole

                holeMap.ConfigureMaterialForMerge(preHole, mask, stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasHoleMerge), roadBlending ? preRoadMask : null);

                Graphics.Blit(hole, stampTerrainBase.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalHole), stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasHoleMerge), 0);

            }

            ///////////////////////////////////////////////////////////////////



            //HIGHLIGHT ///////////////////////////////////////////////////////

            if (AtlasUtils.enableHighlightRendering) {

                var preHighlight = stampTerrainBase.renderPool.ClearAndGetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.PreHighlight);
                var highlight = stampTerrainBase.renderPool.ClearAndGetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.Highlight);
                Graphics.Blit(stampTerrainBase.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalHighlight), preHighlight);

                //render highlight

                highlightMap.ConfigureMaterialForRender(stamp, this, stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasHighlight), _selected);

                RenderTexture.active = highlight;

                DrawMesh(stampTerrainBase, DrawMeshType.Highlight);

                //merge highlight 

                highlightMap.ConfigureMaterialForMerge(preHighlight, mask, stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasHighlightMerge), roadBlending ? preRoadMask : null);

                Graphics.Blit(highlight, stampTerrainBase.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalHighlight), stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasHighlightMerge), 0);

            }

            ///////////////////////////////////////////////////////////////////



            //SPLATMAPS ///////////////////////////////////////////////////////

            if (stampMaps != null) {

                foreach (var i in stampMaps) {

                    if (i.target == StampMap.StampTargetType.None) { continue; }

                    if (StampTargetTypeToSplatmapIndex(i.target) >= splatmapCount) { continue; }

                    i.ConfigureMaterialForRender(stamp, this, stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasOtherMask));

                    RenderTexture.active = otherMask;

                    DrawMesh(stampTerrainBase, DrawMeshType.Splat);

                    RenderTexture preOtherMask = null;

                    stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasOtherMaskMerge).SetTexture("_MaskTex", mask);
                    stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasOtherMaskMerge).SetTexture("_PersistantMaskTex", roadBlending ? preRoadMask : null);
                    stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasOtherMaskMerge).SetFloat("_Opacity", i.modifier.opacity);
                    stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasOtherMaskMerge).SetInt("_SplatChannel", i.GetStampTargetChannelIndex());

                    switch (i.target) {

                        case StampMap.StampTargetType.Splat1:
                        case StampMap.StampTargetType.Splat2:
                        case StampMap.StampTargetType.Splat3:
                        case StampMap.StampTargetType.Splat4:

                            stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasOtherMaskMerge).SetInt("_SplatIndex", 0);

                            break;

                        case StampMap.StampTargetType.Splat5:
                        case StampMap.StampTargetType.Splat6:
                        case StampMap.StampTargetType.Splat7:
                        case StampMap.StampTargetType.Splat8:

                            stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasOtherMaskMerge).SetInt("_SplatIndex", 1);
                            break;

                        case StampMap.StampTargetType.Splat9:
                        case StampMap.StampTargetType.Splat10:
                        case StampMap.StampTargetType.Splat11:
                        case StampMap.StampTargetType.Splat12:

                            stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasOtherMaskMerge).SetInt("_SplatIndex", 2);
                            break;

                        case StampMap.StampTargetType.Splat13:
                        case StampMap.StampTargetType.Splat14:
                        case StampMap.StampTargetType.Splat15:
                        case StampMap.StampTargetType.Splat16:

                            stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasOtherMaskMerge).SetInt("_SplatIndex", 3);
                            break;
                    }

                    preOtherMask = stampTerrainBase.renderPool.ClearAndGetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.PreSplat1);
                    Graphics.Blit(stampTerrainBase.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalSplat1), preOtherMask);
                    stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasOtherMaskMerge).SetTexture("_PreMainTex", preOtherMask);
                    stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasOtherMaskMerge).SetInt("_SplatTargetIndex", 0);
                    Graphics.Blit(otherMask, stampTerrainBase.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalSplat1), stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasOtherMaskMerge), 0);

                    preOtherMask = stampTerrainBase.renderPool.ClearAndGetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.PreSplat2);
                    Graphics.Blit(stampTerrainBase.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalSplat2), preOtherMask);
                    stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasOtherMaskMerge).SetTexture("_PreMainTex", preOtherMask);
                    stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasOtherMaskMerge).SetInt("_SplatTargetIndex", 1);
                    Graphics.Blit(otherMask, stampTerrainBase.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalSplat2), stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasOtherMaskMerge), 0);

                    preOtherMask = stampTerrainBase.renderPool.ClearAndGetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.PreSplat3);
                    Graphics.Blit(stampTerrainBase.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalSplat3), preOtherMask);
                    stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasOtherMaskMerge).SetTexture("_PreMainTex", preOtherMask);
                    stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasOtherMaskMerge).SetInt("_SplatTargetIndex", 2);
                    Graphics.Blit(otherMask, stampTerrainBase.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalSplat3), stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasOtherMaskMerge), 0);

                    preOtherMask = stampTerrainBase.renderPool.ClearAndGetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.PreSplat4);
                    Graphics.Blit(stampTerrainBase.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalSplat4), preOtherMask);
                    stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasOtherMaskMerge).SetTexture("_PreMainTex", preOtherMask);
                    stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasOtherMaskMerge).SetInt("_SplatTargetIndex", 3);
                    Graphics.Blit(otherMask, stampTerrainBase.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalSplat4), stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasOtherMaskMerge), 0);

                }

            }

            //////////////////////////////////////////////////////////////////////



            //PREVIEW MASK ///////////////////////////////////////////////////////

            if (AtlasUtils.previewMask) {

                //preview mask

                if (AtlasUtils.previewMaskStamp == this && AtlasUtils.previewMaskStampIndex < stampMaps.Count) {

                    stampMaps[AtlasUtils.previewMaskStampIndex].ConfigureMaterialForRender(stamp, this, stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasOtherMask));

                    RenderTexture.active = stampTerrainBase.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalColor);

                    DrawMesh(stampTerrainBase, DrawMeshType.Color);

                }

            }

            ///////////////////////////////////////////////////////////////////



            //ROAD MASK ///////////////////////////////////////////////////////

            if (roadBlending) {

                //render road mask

                roadMaskMap.ConfigureMaterialForRender(stamp, this, stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasRoadMask));

                RenderTexture.active = roadMask;

                DrawMesh(stampTerrainBase, DrawMeshType.RoadMask, true);

                //merge road mask

                roadMaskMap.ConfigureMaterialForMerge(preRoadMask, null, stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasRoadMaskMerge));

                Graphics.Blit(roadMask, stampTerrainBase.renderPool.GetRenderTexture(AtlasStamper.RenderPool.RenderTextureType.FinalRoadMask), stampTerrainBase.renderPool.GetMaterial(AtlasStamper.RenderPool.MaterialType.AtlasRoadMaskMerge), 0);

            }

            ///////////////////////////////////////////////////////



            //set no rendertexture as active

            RenderTexture.active = null;

        }

        public virtual void DrawMesh(AtlasStamper stampTerrainBase, DrawMeshType drawMeshType, bool forMask = false) { }

        private int StampTargetTypeToSplatmapIndex(StampMap.StampTargetType targetType) {

            switch (targetType) {

                default: return -1;

                case StampMap.StampTargetType.Splat1: return 0;
                case StampMap.StampTargetType.Splat2: return 1;
                case StampMap.StampTargetType.Splat3: return 2;
                case StampMap.StampTargetType.Splat4: return 3;
                case StampMap.StampTargetType.Splat5: return 4;
                case StampMap.StampTargetType.Splat6: return 5;
                case StampMap.StampTargetType.Splat7: return 6;
                case StampMap.StampTargetType.Splat8: return 7;
                case StampMap.StampTargetType.Splat9: return 8;
                case StampMap.StampTargetType.Splat10: return 9;
                case StampMap.StampTargetType.Splat11: return 10;
                case StampMap.StampTargetType.Splat12: return 11;
                case StampMap.StampTargetType.Splat13: return 12;
                case StampMap.StampTargetType.Splat14: return 13;
                case StampMap.StampTargetType.Splat15: return 14;
                case StampMap.StampTargetType.Splat16: return 15;

            }

        }

        [System.Serializable]
        public class StampMap {

            public StampMapType mapType = StampMapType.Height;
            [Tooltip("Select an input from your stamp asset.")]
            public StampTexture input = StampTexture.None;
            [Tooltip("Set the target splatmap to your terrain.")]
            public StampTargetType target = StampTargetType.Splat1;

            public Modifier modifier = new Modifier();

            public bool inspectorInitialized = false;

            [HideInInspector]
            public int selfIndex = 0;

            public void ConfigureMaterialForMerge(RenderTexture pre, RenderTexture mask, Material mat, RenderTexture persistantMask = null) {

                switch (mapType) {

                    case StampMapType.Height:

                        mat.SetTexture("_PreMainTex", pre);
                        mat.SetTexture("_MaskTex", mask);
                        mat.SetTexture("_PersistantMaskTex", persistantMask);
                        mat.SetInt("_Mode", (int)modifier.blendMode);
                        mat.SetFloat("_BlendRatio", modifier.blendRatio);
                        mat.SetFloat("_Opacity", modifier.opacity);

                        break;

                    case StampMapType.Color:

                        mat.SetTexture("_PreMainTex", pre);
                        mat.SetTexture("_MaskTex", mask);
                        mat.SetTexture("_PersistantMaskTex", persistantMask);
                        mat.SetFloat("_Opacity", modifier.opacity);

                        break;

                    case StampMapType.Other:

                        mat.SetTexture("_PreMainTex", pre);
                        mat.SetTexture("_MaskTex", mask);
                        mat.SetTexture("_PersistantMaskTex", persistantMask);
                        mat.SetFloat("_Opacity", modifier.opacity);

                        break;

                    case StampMapType.RoadMask:

                        mat.SetTexture("_PreMainTex", pre);

                        break;

                    case StampMapType.Highlight:

                        mat.SetTexture("_PreMainTex", pre);
                        mat.SetTexture("_MaskTex", mask);
                        mat.SetTexture("_PersistantMaskTex", persistantMask);
                        mat.SetFloat("_Opacity", modifier.opacity);

                        break;

                    case StampMapType.Hole:

                        mat.SetTexture("_PreMainTex", pre);
                        mat.SetTexture("_MaskTex", mask);
                        mat.SetTexture("_PersistantMaskTex", persistantMask);

                        break;

                }

            }

            public void ConfigureMaterialForRender(StampAsset stamp, StampBase stampBase, Material mat, float selected = 0f) {

                switch (mapType) {

                    case StampMapType.Height:

                        mat.SetTexture("_MainTex", GetStampTexture(stamp, stampBase));
                        mat.SetInt("_Mode", (int)modifier.blendMode);
                        mat.SetFloat("_Power", modifier.power);
                        mat.SetFloat("_CutoffMin", modifier.cutoffMin);
                        mat.SetFloat("_CutoffMax", modifier.cutoffMax);
                        mat.SetInt("_Invert", modifier.invert ? 1 : 0);

                        break;

                    case StampMapType.Color:

                        mat.SetTexture("_MainTex", GetStampTexture(stamp, stampBase));
                        mat.SetFloat("_Brightness", modifier.brightness);
                        mat.SetFloat("_Contrast", modifier.contrast);
                        mat.SetFloat("_Saturation", modifier.saturation);
                        mat.SetFloat("_Hue", modifier.hue);
                        mat.SetColor("_Color", new Color(1f, 0.5f, 0, 1));
                        mat.SetFloat("_Selected", selected);

                        break;

                    case StampMapType.Other:

                        mat.SetTexture("_MainTex", GetStampTexture(stamp, stampBase));

                        mat.SetInt("_InputChannelIndex", GetStampInputChannelIndex());

                        mat.SetFloat("_Power", modifier.power);
                        mat.SetFloat("_CutoffMin", modifier.cutoffMin);
                        mat.SetFloat("_CutoffMax", modifier.cutoffMax);
                        mat.SetInt("_Invert", modifier.invert ? 1 : 0);
                        mat.SetFloat("_Offset", modifier.offset);
                        mat.SetFloat("_Multiplier", modifier.multiplier);

                        break;

                    case StampMapType.Mask:

                        mat.SetTexture("_HeightTex", stamp == null ? null : stamp.height == null ? null : stamp.height);
                        mat.SetTexture("_MainTex", GetStampTexture(stamp, stampBase));
                        mat.SetFloat("_Power", modifier.power);
                        mat.SetFloat("_EdgeFade", modifier.edgeErase);
                        mat.SetInt("_Mode", (int)modifier.edgeMode);
                        mat.SetFloat("_Opacity", modifier.edgeOpacity);
                        mat.SetFloat("_Fade", modifier.edgeFade);
                        mat.SetFloat("_Size", modifier.edgeSize);
                        mat.SetFloat("_Invert", modifier.maskInvert ? 1 : 0);
                        mat.SetFloat("_FromHeight", modifier.heightSelectFrom);
                        mat.SetFloat("_ToHeight", modifier.heightSelectTo);

                        break;

                    case StampMapType.RoadMask:

                        mat.SetTexture("_MainTex", GetStampTexture(stamp, stampBase));
                        mat.SetFloat("_Power", modifier.power);

                        break;

                    case StampMapType.Highlight:

                        mat.SetTexture("_MainTex", null);
                        mat.SetColor("_Color", new Color(1f, 0.5f, 0, 1));
                        mat.SetFloat("_Selected", selected);

                        break;

                    case StampMapType.Hole:

                        mat.SetTexture("_MainTex", GetStampTexture(stamp, stampBase));
                        mat.SetInt("_InputChannelIndex", GetStampInputChannelIndex());
                        mat.SetFloat("_Offset", modifier.offset);
                        mat.SetInt("_Invert", modifier.invert ? 1 : 0);

                        break;

                }

                mat.SetPass(0);

            }

            public Texture GetStampTexture(StampAsset stamp, StampBase stampBase) {

                if (stamp != null) {

                    switch (input) {

                        case StampTexture.Height: return stamp.height;
                        case StampTexture.Color: return stamp.color;
                        case StampTexture.Mask: return stampBase.maskOverride == null ? stamp.mask : stampBase.maskOverride;
                        case StampTexture.SplatMask1: return stamp.splat1;
                        case StampTexture.SplatMask2: return stamp.splat1;
                        case StampTexture.SplatMask3: return stamp.splat1;
                        case StampTexture.SplatMask4: return stamp.splat1;
                        case StampTexture.SplatMask5: return stamp.splat2;
                        case StampTexture.SplatMask6: return stamp.splat2;
                        case StampTexture.SplatMask7: return stamp.splat2;
                        case StampTexture.SplatMask8: return stamp.splat2;
                        case StampTexture.RoadMask: return stamp.roadMask;

                    }

                }

                return null;

            }

            public int GetStampInputChannelIndex() {

                switch (input) {

                    case StampTexture.SplatMask1: return 0;
                    case StampTexture.SplatMask2: return 1;
                    case StampTexture.SplatMask3: return 2;
                    case StampTexture.SplatMask4: return 3;
                    case StampTexture.SplatMask5: return 0;
                    case StampTexture.SplatMask6: return 1;
                    case StampTexture.SplatMask7: return 2;
                    case StampTexture.SplatMask8: return 3;

                }

                return 0;

            }

            public int GetStampTargetChannelIndex() {

                switch (target) {

                    case StampTargetType.Splat1: return 0;
                    case StampTargetType.Splat2: return 1;
                    case StampTargetType.Splat3: return 2;
                    case StampTargetType.Splat4: return 3;
                    case StampTargetType.Splat5: return 0;
                    case StampTargetType.Splat6: return 1;
                    case StampTargetType.Splat7: return 2;
                    case StampTargetType.Splat8: return 3;
                    case StampTargetType.Splat9: return 0;
                    case StampTargetType.Splat10: return 1;
                    case StampTargetType.Splat11: return 2;
                    case StampTargetType.Splat12: return 3;
                    case StampTargetType.Splat13: return 0;
                    case StampTargetType.Splat14: return 1;
                    case StampTargetType.Splat15: return 2;
                    case StampTargetType.Splat16: return 3;

                }

                return 0;

            }

            [System.Serializable]
            public class Modifier {

                public ModifierType modifierType = ModifierType.Height;

                [Tooltip("How this layer will blend.\n[Blend]: value will lerp towards the target value.\n[Min]: minimum value will be applied.\n[Max]: maximum value will be applied.\n[Add]: target value will be added.\n[Sub]: target value will be subtracted.")]
                public BlendMode blendMode = BlendMode.Blend;

                [Tooltip("How much the value will be blended towards the target value.")]
                [Range(0, 1)]
                public float blendRatio = 1;

                [Tooltip("This will define how smooth the edge will blend with surroundings; a low value like '0.05' works well here.")]
                [Range(0, 1)]
                public float edgeBlend = 1;

                [Tooltip("Controls overall opacity.")]
                [Range(0, 1)]
                public float opacity = 1;

                [Tooltip("A higher value results in more steepness.")]
                [Range(0.2f, 5.0f)]
                public float power = 1;

                [Tooltip("Erases the edges.")]
                [Range(0f, 1f)]
                public float edgeErase = 0;

                [Tooltip("Controls overall offset.")]
                [Range(-1, 1)]
                public float offset = 0;

                [Tooltip("Multiplies the mask value.")]
                [Range(0, 10)]
                public float multiplier = 1f;

                [Tooltip("Cuts off minimum value.")]
                [Range(0, 1)]
                public float cutoffMin = 0;

                [Tooltip("Cuts off maximum value.")]
                [Range(0, 1)]
                public float cutoffMax = 1;

                [Tooltip("Inverts the value.")]
                public bool invert = false;

                [Tooltip("Controls brightness value.")]
                [Range(0, 3)]
                public float brightness = 1f;

                [Tooltip("Controls contrast value.")]
                [Range(0, 3)]
                public float contrast = 1f;

                [Tooltip("Controls saturation value.")]
                [Range(0, 3)]
                public float saturation = 1f;

                [Tooltip("Shifts the color.")]
                [Range(0, 1)]
                public float hue = 0;

                [Tooltip("Enable hole cutting.")]
                public bool enabled = false;

                [Tooltip("Edge mode")]
                public EdgeMode edgeMode = EdgeMode.Simple;

                [Tooltip("Edge fade.")]
                [Range(0, 1)]
                public float edgeFade = 0.1f;

                [Tooltip("Edge size.")]
                [Range(0, 1)]
                public float edgeSize = 1f;

                [Tooltip("Edge Opacity.")]
                [Range(0, 1)]
                public float edgeOpacity = 1f;

                [Tooltip("Invert the mask.")]
                public bool maskInvert = false;

                [Tooltip("lowest height where the mask begins to become visible.")]
                [Range(0, 1)]
                public float heightSelectFrom = 0f;

                [Tooltip("highest height where the mask is fully visible.")]
                [Range(0, 1)]
                public float heightSelectTo = 0.1f;

                public enum EdgeMode {
                    Simple,
                    Height,
                    Blob,
                    Edge,
                    HeightAndBlob,
                    HeightAndEdge,
                }

                public enum BlendMode {
                    Blend = 0,
                    Min = 1,
                    Max = 2,
                    Add = 3,
                    Sub = 4,
                }

                public enum ModifierType {
                    Height,
                    Color,
                    Mask,
                    Other,
                    Highlight,
                    Hole,
                }

            }

            public enum StampTexture {
                None,
                Height,
                Color,
                Mask,
                SplatMask1,
                SplatMask2,
                SplatMask3,
                SplatMask4,
                SplatMask5,
                SplatMask6,
                SplatMask7,
                SplatMask8,
                RoadMask,
            }

            public enum StampMapType {
                Height,
                Color,
                Mask,
                Other,
                RoadMask,
                Highlight,
                Hole,
            }

            public enum StampTargetType {
                None,
                Height,
                Color,
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
                Highlight,
                Hole,
            }

        }

        public enum DrawMeshType {
            Mask,
            Height,
            Color,
            Hole,
            Highlight,
            Splat,
            RoadMask
        }

    }

}