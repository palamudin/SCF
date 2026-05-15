using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;

namespace Atlas.Unity {

    [CustomEditor(typeof(StampAsset))]
    [CanEditMultipleObjects]
    public class StampAssetEditor : Editor {

        private RenderTexture previewRT;
        private Material previewMat;

        private SerializedProperty height;
        private SerializedProperty mask;
        private SerializedProperty roadMask;


        private GUIContent[] toolbarContents;
        private ToolBarMode toolbarMode = ToolBarMode.FormatConvert;

        private static Texture2D splatChannel1;
        private static Texture2D splatChannel2;
        private static Texture2D splatChannel3;
        private static Texture2D splatChannel4;

        public void OnEnable() {

            height = serializedObject.FindProperty("height");
            mask = serializedObject.FindProperty("mask");
            roadMask = serializedObject.FindProperty("roadMask");

            toolbarContents = new GUIContent[] {
                new GUIContent(AtlasUtils.LoadIcon("Packages/com.atlas.atlas-terrain-editor/Runtime/Icons/Atlas_convert_16_32_bit_icon.png", "l:Atlas_convert_16_32_bit_icon"), "Set height and mask texture format to 16Bit or 32Bit."),
                new GUIContent(AtlasUtils.LoadIcon("Packages/com.atlas.atlas-terrain-editor/Runtime/Icons/Atlas_combine_splat_icon.png", "l:Atlas_combine_splat_icon"), "Combine 4 grayscale textures into 1 splat texture for the stamp."),
                new GUIContent(AtlasUtils.LoadIcon("Packages/com.atlas.atlas-terrain-editor/Runtime/Icons/Atlas_pack_unpack_icon.png", "l:Atlas_pack_unpack_icon"), "Texture Pack and Unpack."),
            };

        }

        public void OnDisable() {

            if (previewRT != null) {

                previewRT.Release();

                GameObject.DestroyImmediate(previewRT, false);

            }

            if (previewMat != null) {

                GameObject.DestroyImmediate(previewMat, false);

            }

        }

        public override void OnInspectorGUI() {

            DrawDefaultInspector();


            GUILayout.Space(10);

            Border.Start();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            toolbarMode = (ToolBarMode)GUILayout.SelectionGrid((int)toolbarMode, toolbarContents, toolbarContents.Length);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            switch (toolbarMode) {

                case ToolBarMode.FormatConvert:

                    EditorGUILayout.Space(5);

                    if ((height.objectReferenceValue != null && height.objectReferenceValue is Texture2D) || (mask.objectReferenceValue != null && mask.objectReferenceValue is Texture2D) || (roadMask.objectReferenceValue != null && roadMask.objectReferenceValue is Texture2D)) {

                        if (height.objectReferenceValue != null && height.objectReferenceValue is Texture2D) {

                            GUILayout.Space(5);

                            if (height.objectReferenceValue != null && height.objectReferenceValue is Texture2D && !TextureIs16Or32Bit(height.objectReferenceValue as Texture2D)) {

                                EditorGUILayout.HelpBox("Height texture is lower than 16bit.\n16 or 32bit is advised.\n\nTexture format: " + (height.objectReferenceValue as Texture2D).format.ToString(), MessageType.Warning);

                            }

                            GUILayout.BeginHorizontal();

                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button(new GUIContent("Height to 16Bit", "Convert height texture to 16bit Format:R16."), GUILayout.Width(130))) {

                                if (EditorUtility.DisplayDialog("Atlas - Texture Formatter", string.Format("About to change format of the height texture on {0} stamp assets to 16Bit.\n\nContinue?", targets.Length), "16Bit it")) {

                                    foreach (var i in targets) {

                                        ConfigureTextureFormat((i as StampAsset).height, TextureImporterFormat.R16);

                                    }

                                }

                            }

                            if (GUILayout.Button(new GUIContent("Height to 32Bit", "Convert height texture to 32bit Format:RFloat."), GUILayout.Width(130))) {

                                if (EditorUtility.DisplayDialog("Atlas - Texture Formatter", string.Format("About to change format of the height texture on {0} stamp assets to 32Bit.\n\nContinue?", targets.Length), "32Bit it")) {

                                    foreach (var i in targets) {

                                        ConfigureTextureFormat((i as StampAsset).height, TextureImporterFormat.RFloat);

                                    }

                                }

                            }

                            GUILayout.FlexibleSpace();

                            GUILayout.EndHorizontal();

                        }

                        if (mask.objectReferenceValue != null && mask.objectReferenceValue is Texture2D) {

                            GUILayout.Space(5);

                            if (mask.objectReferenceValue != null && mask.objectReferenceValue is Texture2D && !TextureIs16Or32Bit(mask.objectReferenceValue as Texture2D)) {

                                EditorGUILayout.HelpBox("Mask texture is lower than 16bit.\n16 or 32bit is advised.\n\nTexture format: " + (mask.objectReferenceValue as Texture2D).format.ToString(), MessageType.Warning);

                            }

                            GUILayout.BeginHorizontal();

                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button(new GUIContent("Mask to 16Bit", "Convert mask texture to 16bit Format:R16."), GUILayout.Width(130))) {

                                if (EditorUtility.DisplayDialog("Atlas - Texture Formatter", string.Format("About to change format of the mask texture on {0} stamp assets to 16Bit.\n\nContinue?", targets.Length), "16Bit it")) {

                                    foreach (var i in targets) {

                                        ConfigureTextureFormat((i as StampAsset).mask, TextureImporterFormat.R16);

                                    }

                                }

                            }

                            if (GUILayout.Button(new GUIContent("Mask to 32Bit", "Convert mask texture to 32bit Format:RFloat."), GUILayout.Width(130))) {

                                if (EditorUtility.DisplayDialog("Atlas - Texture Formatter", string.Format("About to change format of the mask texture on {0} stamp assets to 32Bit.\n\nContinue?", targets.Length), "32Bit it")) {

                                    foreach (var i in targets) {

                                        ConfigureTextureFormat((i as StampAsset).mask, TextureImporterFormat.RFloat);

                                    }

                                }

                            }

                            GUILayout.FlexibleSpace();

                            GUILayout.EndHorizontal();

                        }

                        if (roadMask.objectReferenceValue != null && roadMask.objectReferenceValue is Texture2D) {

                            GUILayout.Space(5);

                            if (roadMask.objectReferenceValue != null && roadMask.objectReferenceValue is Texture2D && !TextureIs16Or32Bit(roadMask.objectReferenceValue as Texture2D)) {

                                EditorGUILayout.HelpBox("Road Mask texture is lower than 16bit.\n16 or 32bit is advised.\n\nTexture format: " + (roadMask.objectReferenceValue as Texture2D).format.ToString(), MessageType.Warning);

                            }

                            GUILayout.BeginHorizontal();

                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button(new GUIContent("Road Mask to 16Bit", "Convert Road mask texture to 16bit Format:R16."), GUILayout.Width(130))) {

                                if (EditorUtility.DisplayDialog("Atlas - Texture Formatter", string.Format("About to change format of the road mask texture on {0} stamp assets to 16Bit.\n\nContinue?", targets.Length), "16Bit it")) {

                                    foreach (var i in targets) {

                                        ConfigureTextureFormat((i as StampAsset).roadMask, TextureImporterFormat.R16);

                                    }

                                }

                            }

                            if (GUILayout.Button(new GUIContent("Road Mask to 32Bit", "Convert Road mask texture to 32bit Format:RFloat."), GUILayout.Width(130))) {

                                if (EditorUtility.DisplayDialog("Atlas - Texture Formatter", string.Format("About to change format of the road mask texture on {0} stamp assets to 32Bit.\n\nContinue?", targets.Length), "32Bit it")) {

                                    foreach (var i in targets) {

                                        ConfigureTextureFormat((i as StampAsset).roadMask, TextureImporterFormat.RFloat);

                                    }

                                }

                            }

                            GUILayout.FlexibleSpace();

                            GUILayout.EndHorizontal();

                        }

                    } else {

                        EditorGUILayout.HelpBox("No textures assigned for converting.", MessageType.Info);

                    }

                    break;

                case ToolBarMode.SplatCombine:

                    EditorGUILayout.Space(5);

                    if (targets.Length == 1) {

                        splatChannel1 = (Texture2D)EditorGUILayout.ObjectField(new GUIContent("splat channel R", "Enter grayscale image to be combined into a new splatmap texture at channel R."), splatChannel1, typeof(Texture2D), false, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                        splatChannel2 = (Texture2D)EditorGUILayout.ObjectField(new GUIContent("splat channel G", "Enter grayscale image to be combined into a new splatmap texture at channel G."), splatChannel2, typeof(Texture2D), false, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                        splatChannel3 = (Texture2D)EditorGUILayout.ObjectField(new GUIContent("splat channel B", "Enter grayscale image to be combined into a new splatmap texture at channel B."), splatChannel3, typeof(Texture2D), false, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                        splatChannel4 = (Texture2D)EditorGUILayout.ObjectField(new GUIContent("splat channel A", "Enter grayscale image to be combined into a new splatmap texture at channel A."), splatChannel4, typeof(Texture2D), false, GUILayout.Height(EditorGUIUtility.singleLineHeight));

                        GUILayout.Space(10);

                        GUILayout.BeginHorizontal();

                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button(new GUIContent("Combine", "Combine the grayscale textures assigned above into a new splat texture for the stamp."), GUILayout.Width(90))) {

                            if (EditorUtility.DisplayDialog("Atlas - Splatmap Combiner", "About to combine grayscale images into a single splat map texture.\n\nContinue?", "Combine it")) {

                                foreach (var i in targets) {

                                    CombineTexturesToSplatmap(i as StampAsset, new Texture2D[] { splatChannel1, splatChannel2, splatChannel3, splatChannel4 });

                                }

                            }

                        }

                        GUILayout.FlexibleSpace();

                        GUILayout.EndHorizontal();

                    } else {

                        EditorGUILayout.HelpBox("Multi edit not supported for splatmap combining.", MessageType.Info);

                    }

                    break;

                case ToolBarMode.Packing:

                    EditorGUILayout.Space(5);

                    GUILayout.BeginHorizontal();

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(new GUIContent("Pack", "Create a self contained asset by packing the textures inside."), GUILayout.Width(90))) {

                        if (EditorUtility.DisplayDialog("Atlas - Asset Packer", string.Format("About to pack {0} stamp assets.\n\nContinue?", targets.Length), "Pack it")) {

                            foreach (var i in targets) {

                                PackStampAsset(i as StampAsset);

                            }

                        }

                    }

                    if (GUILayout.Button(new GUIContent("Unpack", "Create stand-alone textures by unpacking the textures from this asset."), GUILayout.Width(90))) {

                        if (EditorUtility.DisplayDialog("Atlas - Asset Packer", string.Format("About to unpack {0} stamp assets.\n\nContinue?", targets.Length), "Unpack it")) {

                            foreach (var i in targets) {

                                UnpackStampAsset(i as StampAsset);

                            }

                        }

                    }

                    GUILayout.FlexibleSpace();

                    GUILayout.EndHorizontal();

                    break;

            }

            Border.End(0);

        }

        private bool TextureIs16Or32Bit(Texture2D texture) {

            var format = texture.format;

            return format == TextureFormat.R16 ||
                //format == TextureFormat.RG32 ||
                format == TextureFormat.RFloat ||
                format == TextureFormat.RGBAFloat ||
                //format == TextureFormat.RGFloat ||
                format == TextureFormat.RGBAHalf ||
                format == TextureFormat.RGB48;

            //format == TextureFormat.RGHalf ||
            //format == TextureFormat.RHalf;

        }

        public override bool HasPreviewGUI() {
            return true;
        }

        public override void DrawPreview(Rect previewArea) {

            var o = (StampAsset)target;

            if (o == null || o.color == null || o.mask == null && o.height != null) {

                return;

            }

            var resolution = (int)Mathf.Min(previewArea.width, previewArea.height);

            if (previewRT == null) {

                previewRT = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGB32);

            }

            if (previewMat == null) {

                previewMat = new Material(Shader.Find("Hidden/Atlas/AtlasThumbnail"));

                previewMat.SetTexture("_MainTex", o.color);
                previewMat.SetTexture("_Mask", o.mask);
                previewMat.SetTexture("_Height", o.height);

                Graphics.Blit(null, previewRT, previewMat);

                RenderTexture.active = null;

            }

            GUI.DrawTexture(new Rect(previewArea.x + (previewArea.width - resolution) * 0.5f, previewArea.y + (previewArea.height - resolution) * 0.5f, resolution, resolution), previewRT);

        }

        public override Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height) {

            var o = (StampAsset)target;

            if (o == null || o.color == null || o.mask == null && o.height != null) {

                return null;

            }

            var shader = Shader.Find("Hidden/Atlas/AtlasThumbnail");

            if (shader != null) {

                var rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);

                AtlasUtils.RenderStampAssetPreview(rt, o);

                RenderTexture.active = rt;

                Texture2D tex = new Texture2D(width, height, TextureFormat.ARGB32, true, true);

                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);

                tex.Apply();

                RenderTexture.active = null;

                rt.Release();

                GameObject.DestroyImmediate(rt, false);

                return tex;

            }

            return null;

        }



        public static void UnpackStampAsset(StampAsset stampAsset) {

            if (stampAsset == null) {

                Debug.LogError("StampAssetEditor.UnpackStampAsset: stampAsset is null");

                return;

            }

            //var stampAssetPath = AssetDatabase.GetAssetPath(stampAsset);
            //
            //var stampAssetDirectory = Path.GetDirectoryName(stampAssetPath);
            //
            ////unpack sub assets next to the stamp asset
            //
            //AssetDatabase.StartAssetEditing();
            //
            //var textureAssets = new List<Texture2D>();
            //
            //try {
            //
            //    var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(stampAssetPath).ToList();
            //
            //    foreach (var i in subAssets) {
            //
            //        var texture = i as Texture2D;
            //
            //        var path = stampAssetDirectory + "/" + texture.name;
            //
            //        var clone = new Texture2D(texture.width, texture.height, texture.format, texture.mipmapCount > 0, true);
            //
            //        EditorUtility.CopySerialized(texture, clone);
            //
            //        AssetDatabase.CreateAsset(clone, path + ".asset");
            //
            //        textureAssets.Add(clone);
            //
            //    }
            //
            //    AssetDatabase.SaveAssets();
            //
            //} catch (Exception e) { Debug.LogError(e); }
            //
            //AssetDatabase.StopAssetEditing();
            //
            //
            ////make them readable and uncompressed
            //
            //AssetDatabase.StartAssetEditing();
            //
            //try {
            //
            //    foreach( var texture in textureAssets) {
            //
            //        var importer = (TextureImporter)TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
            //
            //        var tips = new TextureImporterPlatformSettings {
            //            format = texture.format == TextureFormat.RFloat ? TextureImporterFormat.RFloat : TextureImporterFormat.RGBA32,
            //            textureCompression = TextureImporterCompression.Uncompressed,
            //            crunchedCompression = false,
            //        };
            //
            //        importer.textureType = texture.format == TextureFormat.RFloat ? TextureImporterType.SingleChannel : TextureImporterType.Default;
            //        importer.SetPlatformTextureSettings(tips);
            //
            //        importer.isReadable = true;
            //        importer.mipmapEnabled = false;
            //
            //        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(texture));
            //
            //    }
            //
            //    AssetDatabase.SaveAssets();
            //
            //} catch (Exception e) { Debug.LogError(e); }
            //
            //AssetDatabase.StopAssetEditing();
            //
            //
            ////convert to exr
            //
            //AssetDatabase.StartAssetEditing();
            //
            //try {
            //
            //    foreach (var texture in textureAssets) {
            //
            //        var path = stampAssetDirectory + "/" + texture.name;
            //
            //        var bytes = texture.EncodeToEXR(Texture2D.EXRFlags.None);
            //
            //        File.WriteAllBytes(path + ".ext", bytes);
            //
            //    }
            //
            //    AssetDatabase.SaveAssets();
            //
            //} catch (Exception e) { Debug.LogError(e); }
            //
            //AssetDatabase.StopAssetEditing();


            var stampAssetPath = AssetDatabase.GetAssetPath(stampAsset);
            var stampAssetDirectory = Path.GetDirectoryName(stampAssetPath);

            //var texturesToExport = new List<Texture2D>();
            //
            //texturesToExport.Add(stampAsset.height);
            //texturesToExport.Add(stampAsset.mask);
            //texturesToExport.Add(stampAsset.splat1);
            //texturesToExport.Add(stampAsset.splat2);
            //texturesToExport.Add( stampAsset.roadMask);
            //texturesToExport.Add(stampAsset.color);


            //export exr textures

            var newTexturePaths = new Dictionary<int, string>();

            AssetDatabase.StartAssetEditing();

            try {

                for (var i = 0; i < 6; i++) {

                    var texture = stampAsset.height;

                    if (i == 1) { texture = stampAsset.mask; }

                    if (i == 2) { texture = stampAsset.splat1; }

                    if (i == 3) { texture = stampAsset.splat2; }

                    if (i == 4) { texture = stampAsset.roadMask; }

                    if (i == 5) { texture = stampAsset.color; }

                    if (texture == null) {

                        continue;

                    }

                    if (AssetDatabase.IsSubAsset(texture) == false) {

                        continue;

                    }


                    var clone = new Texture2D(texture.width, texture.height, TextureFormat.RGBAFloat, texture.mipmapCount > 0 ? true : false);

                    var rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGBFloat);

                    Graphics.Blit(texture, rt);

                    RenderTexture.active = rt;

                    clone.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);

                    RenderTexture.active = null;

                    RenderTexture.ReleaseTemporary(rt);

                    var path = stampAssetDirectory + "/" + texture.name + ".exr";

                    var bytes = clone.EncodeToEXR(Texture2D.EXRFlags.None);

                    File.WriteAllBytes(path, bytes);

                    GameObject.DestroyImmediate(clone, false);

                    AssetDatabase.ImportAsset(path);


                    newTexturePaths.Add(i, path);

                }

                AssetDatabase.SaveAssets();

            } catch (Exception e) { Debug.LogError(e); }

            AssetDatabase.StopAssetEditing();

            AssetDatabase.Refresh();


            //assign new textures to stamp

            //AssetDatabase.StartAssetEditing();
            //
            //try {
            //
            //    for (var i = 0; i < 6; i++) {
            //
            //
            //        var texture = stampAsset.height;
            //
            //        if (i == 1) { texture = stampAsset.mask; }
            //
            //        if (i == 2) { texture = stampAsset.splat1; }
            //
            //        if (i == 3) { texture = stampAsset.splat2; }
            //
            //        if (i == 4) { texture = stampAsset.roadMask; }
            //
            //        if (i == 5) { texture = stampAsset.color; }
            //
            //        if (texture == null) {
            //
            //            continue;
            //
            //        }
            //
            //        if (AssetDatabase.IsSubAsset(texture) == false) {
            //
            //            continue;
            //
            //        }
            //
            //        var path = stampAssetDirectory + "/" + texture.name + ".exr";
            //
            //        var newTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            //
            //        if (i == 0) { stampAsset.height = newTexture; }
            //
            //        if (i == 1) { stampAsset.mask = newTexture; }
            //
            //        if (i == 2) { stampAsset.splat1 = newTexture; }
            //
            //        if (i == 3) { stampAsset.splat2 = newTexture; }
            //
            //        if (i == 4) { stampAsset.roadMask = newTexture; }
            //
            //        if (i == 5) { stampAsset.color = newTexture; }
            //
            //    }
            //
            //    EditorUtility.SetDirty(stampAsset);
            //
            //    AssetDatabase.SaveAssets();
            //
            //
            //} catch (Exception e) { Debug.LogError(e); }
            //
            //AssetDatabase.StopAssetEditing();
            //
            //AssetDatabase.Refresh();



            AssetDatabase.StartAssetEditing();

            try {

                for (var i = 0; i < 6; i++) {

                    var originalTexture = stampAsset.height;

                    if (i == 1) { originalTexture = stampAsset.mask; }

                    if (i == 2) { originalTexture = stampAsset.splat1; }

                    if (i == 3) { originalTexture = stampAsset.splat2; }

                    if (i == 4) { originalTexture = stampAsset.roadMask; }

                    if (i == 5) { originalTexture = stampAsset.color; }

                    if (originalTexture == null) {

                        continue;

                    }

                    if (AssetDatabase.IsSubAsset(originalTexture) == false) {

                        continue;

                    }

                    var path = newTexturePaths[i];

                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

                    var isSingleChannel = originalTexture.format == TextureFormat.RFloat || originalTexture.format == TextureFormat.RHalf || originalTexture.format == TextureFormat.R16 || originalTexture.format == TextureFormat.R8;

                    var importerFormat = originalTexture.format == TextureFormat.RFloat ? TextureImporterFormat.RFloat : originalTexture.format == TextureFormat.RHalf ? TextureImporterFormat.RHalf : originalTexture.format == TextureFormat.R16 ? TextureImporterFormat.R16 : originalTexture.format == TextureFormat.R8 ? TextureImporterFormat.R8 : originalTexture.format == TextureFormat.ARGB4444 ? TextureImporterFormat.RGBA16 : /*texture.format == TextureFormat.BC6H ? TextureImporterFormat.BC6H :*/ TextureImporterFormat.RGBA32;

                    var importer = (TextureImporter)TextureImporter.GetAtPath(path);

                    var tips = new TextureImporterPlatformSettings {
                        format = importerFormat,
                        textureCompression = isSingleChannel ? TextureImporterCompression.Uncompressed : TextureImporterCompression.Compressed,
                        crunchedCompression = true,
                    };

                    importer.textureType = isSingleChannel ? TextureImporterType.SingleChannel : TextureImporterType.Default;
                    importer.SetPlatformTextureSettings(tips);

                    importer.isReadable = false;
                    importer.mipmapEnabled = false;
                    importer.sRGBTexture = false;

                    AssetDatabase.ImportAsset(path);

                    if (i == 0) { stampAsset.height = texture; }

                    if (i == 1) { stampAsset.mask = texture; }

                    if (i == 2) { stampAsset.splat1 = texture; }

                    if (i == 3) { stampAsset.splat2 = texture; }

                    if (i == 4) { stampAsset.roadMask = texture; }

                    if (i == 5) { stampAsset.color = texture; }


                    AssetDatabase.RemoveObjectFromAsset(originalTexture);

                }

                EditorUtility.SetDirty(stampAsset);

                AssetDatabase.SaveAssets();

            } catch (Exception e) {

                Debug.LogError(e);

            }

            AssetDatabase.StopAssetEditing();


        }

        public static void PackStampAsset(StampAsset stampAsset) {

            if (stampAsset == null) {

                Debug.LogError("StampAssetEditor.PackStampAsset: stampAsset is null");

                return;

            }

            var stampAssetPath = AssetDatabase.GetAssetPath(stampAsset);

            //pack textures that are not packed already

            AssetDatabase.StartAssetEditing();

            try {

                for (var i = 0; i < 6; i++) {

                    var texture = stampAsset.height;

                    if (i == 1) { texture = stampAsset.mask; }

                    if (i == 2) { texture = stampAsset.splat1; }

                    if (i == 3) { texture = stampAsset.splat2; }

                    if (i == 4) { texture = stampAsset.roadMask; }

                    if (i == 5) { texture = stampAsset.color; }

                    if (texture == null) {

                        continue;

                    }

                    if (AssetDatabase.IsSubAsset(texture)) {

                        continue;

                    }

                    var clone = new Texture2D(texture.width, texture.height, texture.format, texture.mipmapCount > 0, true);

                    EditorUtility.CopySerialized(texture, clone);

                    AssetDatabase.AddObjectToAsset(clone, stampAsset);

                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(clone));

                    if (i == 0) { stampAsset.height = clone; }

                    if (i == 1) { stampAsset.mask = clone; }

                    if (i == 2) { stampAsset.splat1 = clone; }

                    if (i == 3) { stampAsset.splat2 = clone; }

                    if (i == 4) { stampAsset.roadMask = clone; }

                    if (i == 5) { stampAsset.color = clone; }

                }

                EditorUtility.SetDirty(stampAsset);

                AssetDatabase.SaveAssets();

            } catch (Exception e) { Debug.LogError(e); }

            AssetDatabase.StopAssetEditing();

            AssetDatabase.Refresh();


            //remove child assets that are not referenced by the stamp asset

            AssetDatabase.StartAssetEditing();

            try {

                var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(stampAssetPath).ToList();

                var stampAssetChildren = new List<Texture2D>();

                stampAssetChildren.Add(stampAsset.height);
                stampAssetChildren.Add(stampAsset.mask);
                stampAssetChildren.Add(stampAsset.splat1);
                stampAssetChildren.Add(stampAsset.splat2);
                stampAssetChildren.Add(stampAsset.roadMask);
                stampAssetChildren.Add(stampAsset.color);

                foreach (var i in subAssets) {

                    if (stampAssetChildren.Contains(i as Texture2D) == false) {

                        AssetDatabase.RemoveObjectFromAsset(i);

                    }

                }

                EditorUtility.SetDirty(stampAsset);

                AssetDatabase.SaveAssets();

            } catch (Exception e) { Debug.LogError(e); }

            AssetDatabase.StopAssetEditing();

        }

        public static void CombineTexturesToSplatmap(StampAsset stampAsset, Texture2D[] textures) {

            if (stampAsset == null) {

                Debug.LogError("StampAssetEditor.CombineTexturesToSplatmap: stampAsset is null");

                return;

            }


            //make textures readable

            AssetDatabase.StartAssetEditing();

            try {

                for (var i = 0; i < textures.Length; i++) {

                    var texture = textures[i];

                    if (texture == null) { continue; }

                    if (texture.isReadable == false) {

                        var importer = (TextureImporter)TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));

                        var tips = new TextureImporterPlatformSettings {
                            format = TextureImporterFormat.RFloat,
                            textureCompression = TextureImporterCompression.Uncompressed,
                            crunchedCompression = false,
                        };

                        importer.textureType = TextureImporterType.SingleChannel;
                        importer.SetPlatformTextureSettings(tips);

                        importer.isReadable = true;
                        importer.mipmapEnabled = false;
                        importer.sRGBTexture = false; //SRGB FIX

                        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(texture));

                    }

                }

            } catch (Exception e) {

                Debug.LogError(e);

            }

            AssetDatabase.StopAssetEditing();


            //build splatmap

            var splatmap = new Texture2D(textures[0].width, textures[0].height, TextureFormat.RGBAFloat, true);

            var totalName = "";

            var nPixels = new Color[textures[0].width * textures[0].height];

            for (var i = 0; i < textures.Length; i++) {

                var texture = textures[i];

                if (texture == null) {

                    totalName += "empty-";

                    continue;

                }

                totalName += FirstCharToUpper(texture.name).Replace("-", "_") + "-";

                var pixels = texture.GetPixels();

                for (var ii = 0; ii < pixels.Length; ii++) {

                    if (i == 0) {

                        nPixels[ii].r = pixels[ii].r;

                    } else if (i == 1) {

                        nPixels[ii].g = pixels[ii].r;

                    } else if (i == 2) {

                        nPixels[ii].b = pixels[ii].r;

                    } else {

                        nPixels[ii].a = pixels[ii].r;

                    }

                }

            }

            splatmap.name = totalName.Remove(totalName.Length - 1);

            splatmap.SetPixels(nPixels);

            splatmap.Apply();


            var stampAssetPath = AssetDatabase.GetAssetPath(stampAsset);

            var stampAssetDirectory = Path.GetDirectoryName(stampAssetPath);

            var path = stampAssetDirectory + "/" + splatmap.name + ".exr";


            //save splatmap

            AssetDatabase.StartAssetEditing();

            try {

                var bytes = splatmap.EncodeToEXR(Texture2D.EXRFlags.None);

                File.WriteAllBytes(path, bytes);

                GameObject.DestroyImmediate(splatmap, false);

                AssetDatabase.ImportAsset(path);

                var newSplatmap = AssetDatabase.LoadAssetAtPath<Texture2D>(path);


                var assignToSplat1 = true;

                if (stampAsset.splat1 != null && stampAsset.splat2 != null) {

                    assignToSplat1 = EditorUtility.DisplayDialog("Atlas Splat Combiner", "Assign to splat1 or splat2?", "splat 1", "splat 2");

                } else if (stampAsset.splat1 == null) {

                    assignToSplat1 = false;

                }

                if (assignToSplat1) {

                    stampAsset.splat1 = newSplatmap;

                } else {

                    stampAsset.splat2 = newSplatmap;

                }

                EditorUtility.SetDirty(stampAsset);

                AssetDatabase.SaveAssets();

            } catch (Exception e) { Debug.LogError(e); }

            AssetDatabase.StopAssetEditing();


            //make splatmap RGBA32

            AssetDatabase.StartAssetEditing();

            try {

                var importer = (TextureImporter)TextureImporter.GetAtPath(path);

                var tips = new TextureImporterPlatformSettings {
                    format = TextureImporterFormat.RGBA16,
                    textureCompression = TextureImporterCompression.Uncompressed,
                    crunchedCompression = false,
                };

                importer.textureType = TextureImporterType.Default;
                importer.SetPlatformTextureSettings(tips);

                //importer.isReadable = true;
                importer.mipmapEnabled = false;
                importer.sRGBTexture = false; //SRGB FIX

                AssetDatabase.ImportAsset(path);

            } catch (Exception e) {

                Debug.LogError(e);

            }

            AssetDatabase.StopAssetEditing();

        }

        public static void ConfigureTextureFormat(Texture2D texture, TextureImporterFormat textureFormat) {

            if (texture == null) {

                Debug.LogError("StampAssetEditor.ConfigureTextureForRFloat: texture is null");

                return;

            }

            AssetDatabase.StartAssetEditing();

            try {

                var path = AssetDatabase.GetAssetPath(texture);

                var importer = (TextureImporter)TextureImporter.GetAtPath(path);

                var tips = new TextureImporterPlatformSettings {

                    format = textureFormat,
                    textureCompression = TextureImporterCompression.Uncompressed,
                    crunchedCompression = false,

                };

                importer.SetPlatformTextureSettings(tips);

                importer.textureType = TextureImporterType.SingleChannel;

                importer.mipmapEnabled = false;

                importer.maxTextureSize = 4096;

                importer.sRGBTexture = false;

                AssetDatabase.ImportAsset(path);

                AssetDatabase.Refresh();

            } catch (Exception e) { Debug.LogError(e); }

            AssetDatabase.StopAssetEditing();

        }

        private static string FirstCharToUpper(string input) {
            if (String.IsNullOrEmpty(input))
                throw new ArgumentException("ARGH!");
            return input.First().ToString().ToUpper() + String.Join("", input.Skip(1));
        }


        public enum ToolBarMode {

            FormatConvert,
            SplatCombine,
            Packing,

        }

    }

}