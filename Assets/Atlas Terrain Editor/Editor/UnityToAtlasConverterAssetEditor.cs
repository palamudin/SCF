using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Atlas.Unity {

    [CustomEditor(typeof(UnityToAtlasConverterAsset))]
    [CanEditMultipleObjects]
    public class UnityToAtlasConverterAssetEditor : Editor {

        private SerializedProperty terrainData;
        private SerializedProperty normalizeHeight;
        private SerializedProperty maskHeightFrom;
        private SerializedProperty maskHeightTo;
        private SerializedProperty stampAsset;
        private SerializedProperty stampPrefab;
        private SerializedProperty useMasking;
        private SerializedProperty mode;
        private SerializedProperty size;
        private SerializedProperty fade;
        private SerializedProperty opacity;
        private SerializedProperty lastPreviewHeightRange;

        private RenderTexture previewRT;
        private RenderTexture previewHeightRT;
        private RenderTexture previewMaskRT;
        private Material previewMaterial;
        private bool previewRTDirty = false;

        public void OnEnable() {

            terrainData = serializedObject.FindProperty("terrainData");
            normalizeHeight = serializedObject.FindProperty("normalizeHeight");
            maskHeightFrom = serializedObject.FindProperty("maskHeightFrom");
            maskHeightTo = serializedObject.FindProperty("maskHeightTo");
            stampAsset = serializedObject.FindProperty("stampAsset");
            stampPrefab = serializedObject.FindProperty("stampPrefab");
            useMasking = serializedObject.FindProperty("useMasking");
            mode = serializedObject.FindProperty("mode");
            size = serializedObject.FindProperty("size");
            fade = serializedObject.FindProperty("fade");
            opacity = serializedObject.FindProperty("opacity");
            lastPreviewHeightRange = serializedObject.FindProperty("lastPreviewHeightRange");

            previewMaterial = new Material(Shader.Find("Hidden/Atlas/AtlasUnityToAtlasConverterAssetPreview"));
            previewRT = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGB32);
            previewRT.Create();
            previewHeightRT = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGB32);
            previewHeightRT.Create();
            previewMaskRT = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGB32);
            previewMaskRT.Create();
            previewRTDirty = false;

            if (targets.Length == 1) {

                var asset = target as UnityToAtlasConverterAsset;

                if (asset.terrainData != null) {

                    RenderPreview();

                }

            }

        }

        public void OnDisable() {

            if (previewRT != null) {

                previewRT.Release();

                GameObject.DestroyImmediate(previewRT, false);

            }

            if (previewHeightRT != null) {

                previewHeightRT.Release();

                GameObject.DestroyImmediate(previewHeightRT, false);

            }

            if (previewMaskRT != null) {

                previewMaskRT.Release();

                GameObject.DestroyImmediate(previewMaskRT, false);

            }

            if (previewMaterial != null) {

                GameObject.DestroyImmediate(previewMaterial, false);

            }

        }

        public override void OnInspectorGUI() {

            //assign terrain data

            Border.Start();

            EditorGUI.BeginChangeCheck();

            if (targets.Length == 1) {

                EditorGUILayout.PropertyField(terrainData);

                if (EditorGUI.EndChangeCheck()) {

                    serializedObject.ApplyModifiedProperties();

                    lastPreviewHeightRange.vector2Value = (target as UnityToAtlasConverterAsset).CalculateheightRange();

                    stampAsset.objectReferenceValue = null;

                    stampPrefab.objectReferenceValue = null;

                    serializedObject.ApplyModifiedProperties();

                    previewRTDirty = true;

                }

            } else {

                EditorGUILayout.HelpBox("multi select not supported for assigning terrainData", MessageType.Info);

            }

            Border.End(0);


            //expand when we have terrain data

            if (terrainData.objectReferenceValue != null) {


                // settings

                GUILayout.Space(5);

                Border.Start();

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(useMasking);

                if (useMasking.boolValue) {

                    EditorGUILayout.PropertyField(mode);

                    switch ((UnityToAtlasConverterAsset.MaskingMode)mode.enumValueIndex) {

                        case UnityToAtlasConverterAsset.MaskingMode.Blob:
                        case UnityToAtlasConverterAsset.MaskingMode.Edge:

                            EditorGUILayout.PropertyField(size);
                            EditorGUILayout.PropertyField(fade);
                            EditorGUILayout.PropertyField(opacity);

                            break;

                        case UnityToAtlasConverterAsset.MaskingMode.BlobAndHeight:
                        case UnityToAtlasConverterAsset.MaskingMode.EdgeAndHeight:

                            EditorGUILayout.PropertyField(size);
                            EditorGUILayout.PropertyField(fade);
                            EditorGUILayout.PropertyField(opacity);
                            EditorGUILayout.PropertyField(maskHeightFrom);
                            EditorGUILayout.PropertyField(maskHeightTo);

                            break;
                        default:

                            EditorGUILayout.PropertyField(maskHeightFrom);
                            EditorGUILayout.PropertyField(maskHeightTo);

                            break;

                    }

                }

                EditorGUILayout.PropertyField(normalizeHeight);

                if (EditorGUI.EndChangeCheck()) {

                    previewRTDirty = true;

                }

                Border.End(1);


                //stamp asset

                GUILayout.Space(5);

                Border.Start();

                EditorGUILayout.PropertyField(stampAsset);
                EditorGUILayout.PropertyField(stampPrefab);

                Border.End(2);


                //convert button

                GUILayout.Space(10);

                GUILayout.BeginHorizontal();

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Convert", GUILayout.MaxWidth(100), GUILayout.MinWidth(100))) {

                    foreach (var i in targets) {

                        (i as UnityToAtlasConverterAsset).Convert();

                    }

                }

                GUILayout.EndHorizontal();

            }

            serializedObject.ApplyModifiedProperties();

            if (previewRTDirty && targets.Length == 1 && (target as UnityToAtlasConverterAsset).terrainData != null) {

                previewRTDirty = false;

                RenderPreview();

            }

        }

        private void RenderPreview() {

            var asset = (target as UnityToAtlasConverterAsset);

            asset.RenderHeightMap(previewHeightRT, asset.lastPreviewHeightRange);

            if (asset.useMasking) {

                asset.RenderMaskMap(previewMaskRT, asset.lastPreviewHeightRange);

            } else {

                RenderTexture.active = previewMaskRT;

                GL.Clear(false, true, new Color(0, 0, 0, 1));

                RenderTexture.active = null;

            }

            previewMaterial.SetTexture("_HeightMap", previewHeightRT);
            previewMaterial.SetTexture("_MaskMap", previewMaskRT);
            previewMaterial.SetFloat("_StrayationOpacity", asset.useMasking ? 1f : 0);

            Graphics.Blit(null, previewRT, previewMaterial);

        }

        public override bool HasPreviewGUI() {

            if (targets.Length == 1) {

                var asset = target as UnityToAtlasConverterAsset;

                if (asset.terrainData != null) {

                    return true;

                }

            }

            return false;

        }

        public override void DrawPreview(Rect previewArea) {

            var resolution = (int)Mathf.Min(previewArea.width, previewArea.height);

            GUI.DrawTexture(new Rect(previewArea.x + (previewArea.width - resolution) * 0.5f, previewArea.y + (previewArea.height - resolution) * 0.5f, resolution, resolution), previewRT);

        }

    }

}