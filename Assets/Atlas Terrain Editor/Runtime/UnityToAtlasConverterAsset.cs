using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace Atlas.Unity {

    [CreateAssetMenu(fileName = "AtlasConverterAsset", menuName = "Atlas/Unity/Atlas Converter Asset", order = 0)]
    public class UnityToAtlasConverterAsset : ScriptableObject {

        public static readonly string assetPath = "Assets/Atlas Terrain Editor/ConvertedStampAssets";
        public static readonly string prefabPath = "Assets/Atlas Terrain Editor/ConvertedStamps";

        [Tooltip("Assign the terrain data you want to convert to an atlas stamp asset.")]
        public TerrainData terrainData;
        [Tooltip("The atlas stamp asset that has been converted and will be overidden when converted again.")]
        public StampAsset stampAsset;
        [Tooltip("The atlas stamp prefab that has been converted and will be overidden when converted again.")]
        public GameObject stampPrefab;
        [Tooltip("Convert the height range to 0-1")]
        public bool normalizeHeight = true;
        [Tooltip("Enable masking settings.")]
        public bool useMasking;
        [Tooltip("Masking Mode.")]
        public MaskingMode mode;
        [Tooltip("Size of the mask.")]
        [Range(0.01f, 1)]
        public float size = 0.99f;
        [Tooltip("fade length.")]
        [Range(0.01f, 1)]
        public float fade = 0.1f;
        [Tooltip("More means more overall opacity")]
        [Range(0, 1)]
        public float opacity = 1f;
        [Tooltip("Minimum value used for creating the opacity mask.")]
        [Range(0, 1)]
        public float maskHeightFrom = 0.01f;
        [Tooltip("Maximum value used for creating the opacity mask.")]
        [Range(0, 1)]
        public float maskHeightTo = 0.1f;

        public Vector2 lastPreviewHeightRange;

        [ContextMenu("Convert")]
        public void Convert() {

#if UNITY_EDITOR

            //validate

            if (terrainData == null) {

                EditorUtility.DisplayDialog("Unity terrain to Atlas stamp converter", "no terraindata assigned", "ok");

                return;

            }

            var stampAssetName = terrainData.name;

            if (string.IsNullOrEmpty(stampAssetName)) {

                stampAssetName = "terrainData-noname";

            }



            try {

                //create stamp asset

                if (stampAsset == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(stampAsset))) {

                    stampAsset = ScriptableObject.CreateInstance<StampAsset>();

                    if (AssetDatabase.LoadAssetAtPath<StampAsset>(assetPath + "/" + stampAssetName + "-stamp-asset.asset") != null) {

                        if (!EditorUtility.DisplayDialog("Unity terrain to Atlas stamp converter", "override " + assetPath + "/" + stampAssetName + "-stamp-asset.asset" + "?", "ok")) {

                            return;

                        }

                    }

                    //save folder

                    if (Directory.Exists(assetPath) == false) {

                        Directory.CreateDirectory(assetPath);

                        AssetDatabase.Refresh();

                    }

                    AssetDatabase.CreateAsset(stampAsset, assetPath + "/" + stampAssetName + "-stamp-asset.asset");

                } else {

                    //ask if we want to override

                    if (!EditorUtility.DisplayDialog("Unity terrain to Atlas stamp converter", "override " + AssetDatabase.GetAssetPath(stampAsset) + "?", "ok")) {

                        return;

                    }

                    //clear assets inside stamp asset

                    var children = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(stampAsset));

                    foreach (var i in children) {

                        if (i is StampAsset) { continue; }

                        AssetDatabase.RemoveObjectFromAsset(i);

                    }

                    AssetDatabase.SaveAssets();

                }

                //start progress bar

                EditorUtility.DisplayProgressBar("Unity terrain to Atlas stamp converter", "converting...", 0.2f);


                //update height range

                var heightRange = CalculateheightRange();

                //save height

                var heightMap = ExtractHeightMap(heightRange);

                stampAsset.height = SaveTextureAndAddToStampWithProgress(heightMap, assetPath + "/Height.exr", stampAsset, 0.4f);


                //save mask

                if (useMasking) {

                    var maskMap = ExtractMaskMap(heightRange);

                    stampAsset.mask = SaveTextureAndAddToStampWithProgress(maskMap, assetPath + "/Mask.exr", stampAsset, 0.6f);

                }


                //save splats

                var splatmaps = ExtractSplatMaps();

                for (var i = 0; i < splatmaps.Length; i++) {

                    if (i == 0) {

                        stampAsset.splat1 = SaveTextureAndAddToStampWithProgress(splatmaps[i], assetPath + "/" + GetSplatMapName(0) + ".exr", stampAsset, 0.8f);

                    } else {

                        stampAsset.splat2 = SaveTextureAndAddToStampWithProgress(splatmaps[i], assetPath + "/" + GetSplatMapName(1) + ".exr", stampAsset, 0.8f);

                    }

                }


                //create prefab

                if ( stampPrefab == null || string.IsNullOrEmpty(AssetDatabase.GetAssetPath(stampAsset))) {

                    stampPrefab = new GameObject(stampAssetName);

                    var stamp = stampPrefab.AddComponent<Stamp>();

                    stamp.stamp = stampAsset;

                    stamp.size = new Vector3(terrainData.size.x + ((1f / terrainData.heightmapResolution) * terrainData.size.x), terrainData.size.y, terrainData.size.z + ((1f / terrainData.heightmapResolution) * terrainData.size.z));

                    stamp.transform.position = new Vector3(terrainData.size.x * 0.5f, 0, terrainData.size.z * 0.5f);

                    stamp.stampMaps = new List<StampBase.StampMap>();

                    for (var i = 0; i < terrainData.terrainLayers.Length; i++) {

                        stamp.stampMaps.Add(new StampBase.StampMap() {
                            input = i == 0 ? StampBase.StampMap.StampTexture.None : (StampBase.StampMap.StampTexture)Enum.Parse(typeof(StampBase.StampMap.StampTexture), "SplatMask" + (i + 1)),
                            target = (StampBase.StampMap.StampTargetType)Enum.Parse(typeof(StampBase.StampMap.StampTargetType), "Splat" + (i + 1)),
                        });

                    }

                    if (Directory.Exists(prefabPath) == false) {

                        Directory.CreateDirectory(prefabPath);

                        AssetDatabase.Refresh();

                    }

                    PrefabUtility.SaveAsPrefabAsset(stampPrefab, prefabPath + "/" + stampAssetName + "-stamp.prefab");

                    GameObject.DestroyImmediate(stampPrefab, false);

                    stampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath + "/"+ stampAssetName + "-stamp.prefab");

                } else {

                    var stamp = stampPrefab.GetComponent<Stamp>();

                    stamp.stamp = stampAsset;

                }


                //save assets

                EditorUtility.SetDirty(this);

                AssetDatabase.SaveAssets();


            } catch (Exception e) { Debug.LogError(e); }


            //stop progress bar

            EditorUtility.ClearProgressBar();

#endif

        }

        public void RenderHeightMap(RenderTexture target, Vector2 heightRange) {

            var tempRT = RenderTexture.GetTemporary(target.descriptor);

            var tempRT2 = RenderTexture.GetTemporary(target.descriptor);

            if (normalizeHeight) {

                var material = new Material(Shader.Find("Hidden/Atlas/AtlasNormalizeHeight"));

                material.SetFloat("_MinHeight", heightRange.x);
                material.SetFloat("_MaxHeight", heightRange.y);

                Graphics.Blit(terrainData.heightmapTexture, tempRT, material);

                GameObject.DestroyImmediate(material, false);

            } else {

                Graphics.Blit(terrainData.heightmapTexture, tempRT);

            }


            //fix rotation

            var rotateMaterial = new Material(Shader.Find("Hidden/Atlas/AtlasRotateTexture"));

            Graphics.Blit(tempRT, tempRT2, rotateMaterial);

            GameObject.DestroyImmediate(rotateMaterial, false);


            //fix height

            var heightToUnityHeightMaterial = new Material(Shader.Find("Hidden/Atlas/AtlasHeightToUnityTerrainHeight"));

            Graphics.Blit(tempRT2, target, heightToUnityHeightMaterial);

            GameObject.DestroyImmediate(heightToUnityHeightMaterial, false);


            RenderTexture.ReleaseTemporary(tempRT);

            RenderTexture.ReleaseTemporary(tempRT2);

        }

        private Texture2D ExtractHeightMap(Vector2 heightRange) {

            var resolution = terrainData.heightmapResolution;

            var renderTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RFloat);

            RenderHeightMap(renderTexture, heightRange);

            var texture = new Texture2D(resolution, resolution, TextureFormat.RFloat, false, true);

            RenderTexture.active = renderTexture;

            texture.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);

            texture.Apply();

            RenderTexture.active = null;

            GameObject.DestroyImmediate(renderTexture, false);

            return texture;

        }

        public void RenderMaskMap(RenderTexture target, Vector2 heightRange) {

            var tempRT = RenderTexture.GetTemporary(target.descriptor);

            var material = new Material(Shader.Find("Hidden/Atlas/AtlasHeightToMask"));

            material.SetInt("_Mode", (int)mode);
            material.SetFloat("_Size", size);
            material.SetFloat("_Fade", fade);
            material.SetFloat("_FromHeight", maskHeightFrom);
            material.SetFloat("_ToHeight", maskHeightTo);
            material.SetFloat("_Opacity", opacity);
            material.SetFloat("_MinHeight", heightRange.x);
            material.SetFloat("_MaxHeight", heightRange.y);

            Graphics.Blit(terrainData.heightmapTexture, tempRT, material);

            GameObject.DestroyImmediate(material, false);


            //fix rotation

            var rotateMaterial = new Material(Shader.Find("Hidden/Atlas/AtlasRotateTexture"));

            Graphics.Blit(tempRT, target, rotateMaterial);

            GameObject.DestroyImmediate(rotateMaterial, false);


            RenderTexture.ReleaseTemporary(tempRT);

        }

        public Texture2D ExtractMaskMap(Vector2 heightRange) {

            var resolution = terrainData.heightmapResolution;

            var renderTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RFloat);

            RenderMaskMap(renderTexture, heightRange);

            var texture = new Texture2D(resolution, resolution, TextureFormat.RFloat, false, true);

            RenderTexture.active = renderTexture;

            texture.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);

            texture.Apply();

            RenderTexture.active = null;

            GameObject.DestroyImmediate(renderTexture, false);

            return texture;

        }

        public Texture2D[] ExtractSplatMaps() {

            var resolution = terrainData.alphamapResolution;

            var alphaMapCount = terrainData.terrainLayers.Length > 4 ? 2 : 1;

            var textures = new Texture2D[alphaMapCount];

            var rotateMaterial = new Material(Shader.Find("Hidden/Atlas/AtlasRotateTexture"));

            for (var i = 0; i < alphaMapCount; i++) {

                var tempRT = RenderTexture.GetTemporary(resolution, resolution, 0, RenderTextureFormat.ARGBFloat);

                Graphics.Blit(terrainData.GetAlphamapTexture(i), tempRT, rotateMaterial); //<- fix rotation

                textures[i] = new Texture2D(resolution, resolution, TextureFormat.RGBAFloat, false, true);

                RenderTexture.active = tempRT;

                textures[i].ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);

                RenderTexture.active = null;

                RenderTexture.ReleaseTemporary(tempRT);

            }

            GameObject.DestroyImmediate(rotateMaterial, false);

            return textures;

        }

        public Vector2 CalculateheightRange() {

            var resolution = terrainData.heightmapResolution;

            var heights = terrainData.GetHeights(0, 0, resolution, resolution);

            var minHeight = float.MaxValue;

            var maxHeight = float.MinValue;

            for (var y = 0; y < resolution; y++) {

                for (var x = 0; x < resolution; x++) {

                    if (heights[x, y] < minHeight) {

                        minHeight = heights[x, y];

                    }

                    if (heights[x, y] > maxHeight) {

                        maxHeight = heights[x, y];

                    }

                }

            }

            return new Vector2(minHeight, maxHeight);

        }


        private string GetSplatMapName(int splatmapIndex, bool flatIndex = false) {

            var terrainLayers = terrainData.terrainLayers;

            var name = "";

            for (var i = 0; i < 4; i++) {

                var splatIndex = (splatmapIndex * 4) + i;

                if (terrainLayers.Length > splatIndex && terrainLayers[splatIndex] != null) {

                    name += ConvertSplatMapName(terrainLayers[splatIndex].name);//

                } else {

                    name += "channel" + (splatIndex + 1);

                }

                name += "-";

            }

            return name.Remove(name.Length - 1);

        }

        private string ConvertSplatMapName(string name) {

            var newName = name;

            if( newName.Length > 15) {

                newName = newName.Remove(15);

            }

            newName = newName.Replace("-", "_");

            foreach( var i in Path.GetInvalidFileNameChars()) {

                newName = newName.Replace(i, '_');

            }

            return newName;

        }

        private Texture2D SaveTextureAndAddToStampWithProgress(Texture2D texture, string path, StampAsset stampAsset, float progress) {

#if UNITY_EDITOR

            SaveTexture(texture, path);

            var addedObjectTexture = AddToStampAsset(texture, path, stampAsset);

            AssetDatabase.DeleteAsset(path);

            EditorUtility.DisplayProgressBar("Unity terrain to Atlas stamp converter", "converting...", progress);

            return addedObjectTexture;

#else

            return null;
#endif

        }

        private Texture2D AddToStampAsset(Texture2D texture, string path, StampAsset stampAsset) {

#if UNITY_EDITOR

            var newTexture = new Texture2D(texture.width, texture.height, texture.format, false, true);

            var oldTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            EditorUtility.CopySerialized(oldTexture, newTexture);

            AssetDatabase.AddObjectToAsset(newTexture, stampAsset);

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(newTexture));

            return newTexture;

#else

            return null;
#endif

        }

        private void SaveTexture(Texture2D texture, string path) {

#if UNITY_EDITOR

            texture.name = Path.GetFileNameWithoutExtension(path);

            byte[] data;

            if (texture.format == TextureFormat.RFloat) {

                data = texture.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);

            } else {

                data = texture.EncodeToEXR(Texture2D.EXRFlags.None);

            }

            File.WriteAllBytes(path, data);

            AssetDatabase.Refresh();

            var importer = (TextureImporter)TextureImporter.GetAtPath(path);

            if (texture.format == TextureFormat.RFloat) {

                importer.textureType = TextureImporterType.SingleChannel;

                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings {
                    format = TextureImporterFormat.RFloat,
                    textureCompression = TextureImporterCompression.Uncompressed,
                    crunchedCompression = false,
                });

            } else {

                importer.textureType = TextureImporterType.Default;

                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings {
                    format = TextureImporterFormat.ARGB32,
                    textureCompression = TextureImporterCompression.Uncompressed,
                    crunchedCompression = false,
                });

            }

            importer.mipmapEnabled = false;
            importer.sRGBTexture = false; //SRGB FIX

            importer.maxTextureSize = 4096;

            AssetDatabase.ImportAsset(path);

            AssetDatabase.Refresh();

#endif

        }


        public enum MaskingMode {

            Height = 0,
            Blob = 1,
            Edge = 2,
            BlobAndHeight = 3,
            EdgeAndHeight = 4,

        }

    }

}