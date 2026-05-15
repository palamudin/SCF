using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Atlas.Unity {

    [CustomPropertyDrawer(typeof(StampBase.StampMap))]
    public class StampMapEditor : PropertyDrawer {

        private static GUIContent[] buttonContents;
        private static bool iconsLoaded = false;

        private GUIContent[] targetOptions;
        private TerrainLayer[] targetOptionsTerrainLayers;

        private void OnEnable() {
            buttonContents = new GUIContent[] {
                new GUIContent(EditorGUIUtility.FindTexture("animationvisibilitytoggleon@2x"),"preview mask"),
                new GUIContent(EditorGUIUtility.FindTexture("animationvisibilitytoggleoff@2x"),"stop preview mask"),
            };
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

            if (iconsLoaded == false) {

                iconsLoaded = true;

                OnEnable();
            }

            EditorGUI.BeginProperty(position, label, property);

            if (property.FindPropertyRelative("inspectorInitialized").boolValue == false) {
                property.FindPropertyRelative("inspectorInitialized").boolValue = true;

                var modifierProperty = property.FindPropertyRelative("modifier");

                modifierProperty.FindPropertyRelative("power").floatValue = 1;
                modifierProperty.FindPropertyRelative("edgeErase").floatValue = 0;
                modifierProperty.FindPropertyRelative("opacity").floatValue = 1;
                modifierProperty.FindPropertyRelative("brightness").floatValue = 1;
                modifierProperty.FindPropertyRelative("contrast").floatValue = 1;
                modifierProperty.FindPropertyRelative("saturation").floatValue = 1;
                modifierProperty.FindPropertyRelative("hue").floatValue = 0;
                modifierProperty.FindPropertyRelative("blendRatio").floatValue = 1;
                modifierProperty.FindPropertyRelative("edgeBlend").floatValue = 1;
                modifierProperty.FindPropertyRelative("cutoffMin").floatValue = 0;
                modifierProperty.FindPropertyRelative("cutoffMax").floatValue = 1;
                modifierProperty.FindPropertyRelative("multiplier").floatValue = 1;
                modifierProperty.FindPropertyRelative("offset").floatValue = 0;
                modifierProperty.FindPropertyRelative("invert").boolValue = false;
                modifierProperty.FindPropertyRelative("enabled").boolValue = false;
            }

            var stampMapTypeProperty = property.FindPropertyRelative("mapType");

            var labelText = ((StampBase.StampMap.StampMapType)stampMapTypeProperty.enumValueIndex).ToString();

            if ((StampBase.StampMap.StampMapType)stampMapTypeProperty.enumValueIndex == StampBase.StampMap.StampMapType.Other) {

                UpdateTargetOptions();

                labelText = property.FindPropertyRelative("target").hasMultipleDifferentValues ? "" : targetOptions[TargetEnumIndexToOptionsIndex(property.FindPropertyRelative("target").enumValueIndex)].text;

            }

            EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(labelText, property.tooltip), EditorStyles.boldLabel);

            if (property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, 18), property.isExpanded, GUIContent.none, EditorStyles.foldout)) {

                var modifierProperty = property.FindPropertyRelative("modifier");

                EditorGUI.indentLevel++;

                switch ((StampBase.StampMap.StampMapType)stampMapTypeProperty.enumValueIndex) {

                    case StampBase.StampMap.StampMapType.Mask:

                        EditorGUI.PropertyField(new Rect(position.x, position.y + 20, position.width, 18), modifierProperty.FindPropertyRelative("power"));
                        EditorGUI.PropertyField(new Rect(position.x, position.y + 40, position.width, 18), modifierProperty.FindPropertyRelative("maskInvert"));

                        var edgeModeProperty = modifierProperty.FindPropertyRelative("edgeMode");

                        EditorGUI.PropertyField(new Rect(position.x, position.y + 60, position.width, 18), edgeModeProperty);

                        if (edgeModeProperty.hasMultipleDifferentValues == false) {

                            if (edgeModeProperty.enumValueIndex == 0) {

                                //simple

                                EditorGUI.PropertyField(new Rect(position.x, position.y + 80, position.width, 18), modifierProperty.FindPropertyRelative("edgeErase"));

                            } else if (edgeModeProperty.enumValueIndex == 1) {

                                //height

                                EditorGUI.PropertyField(new Rect(position.x, position.y + 80, position.width, 18), modifierProperty.FindPropertyRelative("heightSelectFrom"));
                                EditorGUI.PropertyField(new Rect(position.x, position.y + 100, position.width, 18), modifierProperty.FindPropertyRelative("heightSelectTo"));

                            } else if (edgeModeProperty.enumValueIndex == 2) {

                                //blob

                                EditorGUI.PropertyField(new Rect(position.x, position.y + 80, position.width, 18), modifierProperty.FindPropertyRelative("edgeOpacity"));
                                EditorGUI.PropertyField(new Rect(position.x, position.y + 100, position.width, 18), modifierProperty.FindPropertyRelative("edgeFade"));
                                EditorGUI.PropertyField(new Rect(position.x, position.y + 120, position.width, 18), modifierProperty.FindPropertyRelative("edgeSize"));

                            } else if (edgeModeProperty.enumValueIndex == 3) {

                                //edge

                                EditorGUI.PropertyField(new Rect(position.x, position.y + 80, position.width, 18), modifierProperty.FindPropertyRelative("edgeOpacity"));
                                EditorGUI.PropertyField(new Rect(position.x, position.y + 100, position.width, 18), modifierProperty.FindPropertyRelative("edgeFade"));
                                EditorGUI.PropertyField(new Rect(position.x, position.y + 120, position.width, 18), modifierProperty.FindPropertyRelative("edgeSize"));

                            } else if (edgeModeProperty.enumValueIndex == 4) {

                                //height and blob

                                EditorGUI.PropertyField(new Rect(position.x, position.y + 80, position.width, 18), modifierProperty.FindPropertyRelative("heightSelectFrom"));
                                EditorGUI.PropertyField(new Rect(position.x, position.y + 100, position.width, 18), modifierProperty.FindPropertyRelative("heightSelectTo"));
                                EditorGUI.PropertyField(new Rect(position.x, position.y + 120, position.width, 18), modifierProperty.FindPropertyRelative("edgeOpacity"));
                                EditorGUI.PropertyField(new Rect(position.x, position.y + 140, position.width, 18), modifierProperty.FindPropertyRelative("edgeFade"));
                                EditorGUI.PropertyField(new Rect(position.x, position.y + 160, position.width, 18), modifierProperty.FindPropertyRelative("edgeSize"));

                            } else if (edgeModeProperty.enumValueIndex == 5) {

                                //heihgt and edge

                                EditorGUI.PropertyField(new Rect(position.x, position.y + 80, position.width, 18), modifierProperty.FindPropertyRelative("heightSelectFrom"));
                                EditorGUI.PropertyField(new Rect(position.x, position.y + 100, position.width, 18), modifierProperty.FindPropertyRelative("heightSelectTo"));
                                EditorGUI.PropertyField(new Rect(position.x, position.y + 120, position.width, 18), modifierProperty.FindPropertyRelative("edgeOpacity"));
                                EditorGUI.PropertyField(new Rect(position.x, position.y + 140, position.width, 18), modifierProperty.FindPropertyRelative("edgeFade"));
                                EditorGUI.PropertyField(new Rect(position.x, position.y + 160, position.width, 18), modifierProperty.FindPropertyRelative("edgeSize"));

                            }

                        }

                        break;
                    case StampBase.StampMap.StampMapType.Color:

                        EditorGUI.PropertyField(new Rect(position.x, position.y + 20, position.width, 18), modifierProperty.FindPropertyRelative("opacity"));
                        EditorGUI.PropertyField(new Rect(position.x, position.y + 40, position.width, 18), modifierProperty.FindPropertyRelative("brightness"));
                        EditorGUI.PropertyField(new Rect(position.x, position.y + 60, position.width, 18), modifierProperty.FindPropertyRelative("contrast"));
                        EditorGUI.PropertyField(new Rect(position.x, position.y + 80, position.width, 18), modifierProperty.FindPropertyRelative("saturation"));
                        EditorGUI.PropertyField(new Rect(position.x, position.y + 100, position.width, 18), modifierProperty.FindPropertyRelative("hue"));

                        if (property.serializedObject.targetObjects.Length == 1) {

                            if (GUI.Button(new Rect(position.x + (position.width - 27), position.y + 120, 27, 18), AtlasUtils.previewColor ? buttonContents[1] : buttonContents[0])) {

                                if (AtlasUtils.previewColor) {

                                    AtlasUtils.ClearPreview();

                                } else {

                                    AtlasUtils.previewColor = true;

                                }

                                AtlasStamper.QueRender();

                            }

                        }

                        break;
                    case StampBase.StampMap.StampMapType.Hole:

                        EditorGUI.PropertyField(new Rect(position.x, position.y + 20, position.width, 18), modifierProperty.FindPropertyRelative("enabled"));

                        if (property.serializedObject.targetObjects.Length == 1) {

                            //this only works when 1 target is selected

                            var input = property.FindPropertyRelative("input");

                            if (input.hasMultipleDifferentValues == false) {

                                input.enumValueIndex = InputOptionsIndexToEnumIndex(EditorGUI.Popup(new Rect(position.x, position.y + 40, position.width, 18), new GUIContent(input.displayName, input.tooltip), InputEnumIndexToOptionsIndex(input.enumValueIndex), InputOptions(property.serializedObject)));

                            }

                        } else {

                            var input = property.FindPropertyRelative("input");
                            input.enumValueIndex = (int)(StampBase.StampMap.StampTexture)EditorGUI.EnumPopup(new Rect(position.x, position.y + 40, position.width, 18), new GUIContent(input.displayName, input.tooltip), (StampBase.StampMap.StampTexture)input.enumValueIndex, (e) => { return CheckInputAvailability((StampBase.StampMap.StampTexture)e); }, false);

                        }

                        EditorGUI.PropertyField(new Rect(position.x, position.y + 60, position.width, 18), modifierProperty.FindPropertyRelative("offset"));
                        EditorGUI.PropertyField(new Rect(position.x, position.y + 80, position.width, 18), modifierProperty.FindPropertyRelative("invert"));

                        break;
                    case StampBase.StampMap.StampMapType.Height:

                        var blendModeProperty = modifierProperty.FindPropertyRelative("blendMode");

                        EditorGUI.PropertyField(new Rect(position.x, position.y + 20, position.width, 18), blendModeProperty);

                        switch ((StampBase.StampMap.Modifier.BlendMode)blendModeProperty.enumValueIndex) {

                            case StampBase.StampMap.Modifier.BlendMode.Blend:

                                EditorGUI.PropertyField(new Rect(position.x, position.y + 40, position.width, 18), modifierProperty.FindPropertyRelative("blendRatio"));

                                break;

                            case StampBase.StampMap.Modifier.BlendMode.Min:
                            case StampBase.StampMap.Modifier.BlendMode.Max:

                                EditorGUI.PropertyField(new Rect(position.x, position.y + 40, position.width, 18), modifierProperty.FindPropertyRelative("edgeBlend"));

                                break;

                            default:

                                break;

                        }

                        EditorGUI.PropertyField(new Rect(position.x, position.y + 60, position.width, 18), modifierProperty.FindPropertyRelative("opacity"));
                        EditorGUI.PropertyField(new Rect(position.x, position.y + 80, position.width, 18), modifierProperty.FindPropertyRelative("power"));

                        EditorGUI.PropertyField(new Rect(position.x, position.y + 100, position.width, 18), modifierProperty.FindPropertyRelative("cutoffMin"));
                        EditorGUI.PropertyField(new Rect(position.x, position.y + 120, position.width, 18), modifierProperty.FindPropertyRelative("cutoffMax"));

                        EditorGUI.PropertyField(new Rect(position.x, position.y + 140, position.width, 18), modifierProperty.FindPropertyRelative("invert"));

                        break;
                    case StampBase.StampMap.StampMapType.Other:

                        if (property.serializedObject.targetObjects.Length == 1) {

                            //this only works when 1 target is selected

                            var input = property.FindPropertyRelative("input");

                            if (input.hasMultipleDifferentValues == false) {

                                input.enumValueIndex = InputOptionsIndexToEnumIndex(EditorGUI.Popup(new Rect(position.x, position.y + 20, position.width, 18), new GUIContent(input.displayName, input.tooltip), InputEnumIndexToOptionsIndex(input.enumValueIndex), InputOptions(property.serializedObject)));

                            }

                        } else {

                            var input = property.FindPropertyRelative("input");
                            input.enumValueIndex = (int)(StampBase.StampMap.StampTexture)EditorGUI.EnumPopup(new Rect(position.x, position.y + 20, position.width, 18), new GUIContent(input.displayName, input.tooltip), (StampBase.StampMap.StampTexture)input.enumValueIndex, (e) => { return CheckInputAvailability((StampBase.StampMap.StampTexture)e); }, false);

                        }

                        if (property.serializedObject.targetObjects.Length == 1) {

                            var target = property.FindPropertyRelative("target");

                            if (target.hasMultipleDifferentValues == false) {

                                target.enumValueIndex = TargetOptionsIndexToEnumIndex(EditorGUI.Popup(new Rect(position.x, position.y + 40, position.width, 18), new GUIContent(target.displayName, target.tooltip), TargetEnumIndexToOptionsIndex(target.enumValueIndex), targetOptions));

                            }

                        } else {

                            var target = property.FindPropertyRelative("target");
                            target.enumValueIndex = (int)(StampBase.StampMap.StampTargetType)EditorGUI.EnumPopup(new Rect(position.x, position.y + 40, position.width, 18), new GUIContent(target.displayName, target.tooltip), (StampBase.StampMap.StampTargetType)target.enumValueIndex, (e) => { return CheckTargetAvailability(property, (StampBase.StampMap.StampTargetType)e); }, false);

                        }


                        EditorGUI.PropertyField(new Rect(position.x, position.y + 60, position.width, 18), modifierProperty.FindPropertyRelative("opacity"));
                        EditorGUI.PropertyField(new Rect(position.x, position.y + 80, position.width, 18), modifierProperty.FindPropertyRelative("power"));

                        EditorGUI.PropertyField(new Rect(position.x, position.y + 100, position.width, 18), modifierProperty.FindPropertyRelative("cutoffMin"));
                        EditorGUI.PropertyField(new Rect(position.x, position.y + 120, position.width, 18), modifierProperty.FindPropertyRelative("cutoffMax"));

                        EditorGUI.PropertyField(new Rect(position.x, position.y + 140, position.width, 18), modifierProperty.FindPropertyRelative("multiplier"));
                        EditorGUI.PropertyField(new Rect(position.x, position.y + 160, position.width, 18), modifierProperty.FindPropertyRelative("offset"));
                        EditorGUI.PropertyField(new Rect(position.x, position.y + 180, position.width, 18), modifierProperty.FindPropertyRelative("invert"));

                        if (property.serializedObject.targetObjects.Length == 1) {

                            var index = property.FindPropertyRelative("selfIndex").intValue;

                            if (((AtlasUtils.previewMask) ||
                                (AtlasUtils.previewMask == false)) &&
                                GUI.Button(new Rect(position.x + (position.width - 27), position.y + 200, 27, 18), AtlasUtils.previewMask ? buttonContents[1] : buttonContents[0])) {

                                AtlasUtils.previewMask = !AtlasUtils.previewMask;

                                if (AtlasUtils.previewMask == true) {

                                    AtlasUtils.previewMaskStamp = property.serializedObject.targetObject as StampBase;
                                    AtlasUtils.previewMaskStampIndex = index;

                                } else {

                                    AtlasUtils.ClearPreview();

                                }

                                AtlasStamper.QueRender();

                            }

                        }

                        break;

                }

                EditorGUI.indentLevel--;

            }

            EditorGUI.EndProperty();

        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {

            var totalHeight = 20 * 1;

            if (property.isExpanded) {

                var stampMapTypeProperty = property.FindPropertyRelative("mapType");

                var modifierProperty = property.FindPropertyRelative("modifier");

                switch ((StampBase.StampMap.StampMapType)stampMapTypeProperty.enumValueIndex) {

                    case StampBase.StampMap.StampMapType.Mask:


                        var edgeModeProperty = modifierProperty.FindPropertyRelative("edgeMode");

                        if (edgeModeProperty.hasMultipleDifferentValues) {

                            totalHeight += 20 * 3;

                        } else {

                            if (edgeModeProperty.enumValueIndex == 0) {

                                totalHeight += 20 * 4;

                            } else if (edgeModeProperty.enumValueIndex == 1) {

                                totalHeight += 20 * 5;

                            } else if (edgeModeProperty.enumValueIndex == 2) {

                                totalHeight += 20 * 6;

                            } else if (edgeModeProperty.enumValueIndex == 3) {

                                totalHeight += 20 * 6;

                            } else if (edgeModeProperty.enumValueIndex == 4) {

                                totalHeight += 20 * 8;

                            } else if (edgeModeProperty.enumValueIndex == 5) {

                                totalHeight += 20 * 8;

                            }

                        }

                        break;
                    case StampBase.StampMap.StampMapType.Color:

                        totalHeight += 20 * 6;

                        break;
                    case StampBase.StampMap.StampMapType.Hole:


                        totalHeight += 20 * 4;


                        break;
                    case StampBase.StampMap.StampMapType.Height:

                        var blendModeProperty = modifierProperty.FindPropertyRelative("blendMode");

                        switch ((StampBase.StampMap.Modifier.BlendMode)blendModeProperty.enumValueIndex) {

                            case StampBase.StampMap.Modifier.BlendMode.Blend:

                                totalHeight += 20 * 1;

                                break;

                            case StampBase.StampMap.Modifier.BlendMode.Min:
                            case StampBase.StampMap.Modifier.BlendMode.Max:
                            case StampBase.StampMap.Modifier.BlendMode.Add:
                            case StampBase.StampMap.Modifier.BlendMode.Sub:

                                totalHeight += 20 * 1;

                                break;

                            default:

                                break;

                        }

                        totalHeight += 20 * 6;

                        break;
                    case StampBase.StampMap.StampMapType.Other:

                        totalHeight += 20 * 10;

                        break;
                }

            }

            return totalHeight;

        }

        private static bool CheckInputAvailability(StampBase.StampMap.StampTexture stampTexture) {

            return !(stampTexture == StampBase.StampMap.StampTexture.Height ||
                stampTexture == StampBase.StampMap.StampTexture.Color ||
                stampTexture == StampBase.StampMap.StampTexture.Mask);

        }

        private static bool CheckTargetAvailability(SerializedProperty property, StampBase.StampMap.StampTargetType targetType) {

            if (targetType == StampBase.StampMap.StampTargetType.Height) { return false; }
            if (targetType == StampBase.StampMap.StampTargetType.Color) { return false; }

            return true;

        }

        private GUIContent[] InputOptions(SerializedObject serializedObject) {

            var options = new GUIContent[] {
                new GUIContent("None"),
                new GUIContent("__"),
                new GUIContent("__"),
                new GUIContent("__"),
                new GUIContent("__"),
                new GUIContent("__"),
                new GUIContent("__"),
                new GUIContent("__"),
                new GUIContent("__"),
            };

            var stampProperty = serializedObject.FindProperty("stamp");

            if (stampProperty.hasMultipleDifferentValues == false && stampProperty.objectReferenceValue != null) {

                var splat1Name = "";

                var splat1Property = new SerializedObject(stampProperty.objectReferenceValue).FindProperty("splat1");

                if (splat1Property.hasMultipleDifferentValues == false && splat1Property.objectReferenceValue != null) {

                    splat1Name = (splat1Property.objectReferenceValue as Texture2D).name;

                }

                var splat2Name = "";

                var splat2Property = new SerializedObject(stampProperty.objectReferenceValue).FindProperty("splat2");

                if (splat2Property.hasMultipleDifferentValues == false && splat2Property.objectReferenceValue != null) {

                    splat2Name = (splat2Property.objectReferenceValue as Texture2D).name;

                }

                var splat1Names = splat1Name.Split('-');

                var splat2Names = splat2Name.Split('-');

                for (var i = 0; i < 8; i++) {

                    var name = "SplatMask" + (i + 1);

                    if (i < 4) {

                        if (i < splat1Names.Length && string.IsNullOrEmpty(splat1Names[i]) == false) {

                            name = splat1Names[i];

                        }

                    } else {

                        if ((i - 4) < splat2Names.Length && string.IsNullOrEmpty(splat2Names[i - 4]) == false) {

                            name = splat2Names[i - 4];

                        }

                    }

                    options[i + 1] = new GUIContent(name);

                }

            }

            return options;

        }

        private int InputOptionsIndexToEnumIndex(int index) {

            switch (index) {

                case 1: return (int)StampBase.StampMap.StampTexture.SplatMask1;
                case 2: return (int)StampBase.StampMap.StampTexture.SplatMask2;
                case 3: return (int)StampBase.StampMap.StampTexture.SplatMask3;
                case 4: return (int)StampBase.StampMap.StampTexture.SplatMask4;
                case 5: return (int)StampBase.StampMap.StampTexture.SplatMask5;
                case 6: return (int)StampBase.StampMap.StampTexture.SplatMask6;
                case 7: return (int)StampBase.StampMap.StampTexture.SplatMask7;
                case 8: return (int)StampBase.StampMap.StampTexture.SplatMask8;

                default: return (int)StampBase.StampMap.StampTexture.None;

            }

        }

        private int InputEnumIndexToOptionsIndex(int index) {

            switch (index) {

                case (int)StampBase.StampMap.StampTexture.SplatMask1: return 1;
                case (int)StampBase.StampMap.StampTexture.SplatMask2: return 2;
                case (int)StampBase.StampMap.StampTexture.SplatMask3: return 3;
                case (int)StampBase.StampMap.StampTexture.SplatMask4: return 4;
                case (int)StampBase.StampMap.StampTexture.SplatMask5: return 5;
                case (int)StampBase.StampMap.StampTexture.SplatMask6: return 6;
                case (int)StampBase.StampMap.StampTexture.SplatMask7: return 7;
                case (int)StampBase.StampMap.StampTexture.SplatMask8: return 8;

                default: return 0;

            }

        }

        private void UpdateTargetOptions() {

            var previewVolume = AtlasUnityPreviewVolume.previewVolume;

            TerrainLayer[] terrainLayers = null;

            if (previewVolume != null) {

                var terrain = previewVolume.terrainSource;

                if (terrain != null && terrain.terrainData.terrainLayers != null) {

                    terrainLayers = terrain.terrainData.terrainLayers;

                }

            }

            if (targetOptionsTerrainLayers != terrainLayers || targetOptions == null || targetOptions.Length != 17) {

                targetOptionsTerrainLayers = terrainLayers;

                targetOptions = new GUIContent[] {
                    new GUIContent("None"),
                    new GUIContent("1: Terrain Layer 1"),
                    new GUIContent("2: Terrain Layer 2"),
                    new GUIContent("3: Terrain Layer 3"),
                    new GUIContent("4: Terrain Layer 4"),
                    new GUIContent("5: Terrain Layer 5"),
                    new GUIContent("6: Terrain Layer 6"),
                    new GUIContent("7: Terrain Layer 7"),
                    new GUIContent("8: Terrain Layer 8"),
                    new GUIContent("9: Terrain Layer 9"),
                    new GUIContent("10: Terrain Layer 10"),
                    new GUIContent("11: Terrain Layer 11"),
                    new GUIContent("12: Terrain Layer 12"),
                    new GUIContent("13: Terrain Layer 13"),
                    new GUIContent("14: Terrain Layer 14"),
                    new GUIContent("15: Terrain Layer 15"),
                    new GUIContent("16: Terrain Layer 16"),
                };

                if (targetOptionsTerrainLayers != null) {

                    targetOptions = new GUIContent[] {
                        new GUIContent("None"),
                        new GUIContent("1:  Unassigned"),
                        new GUIContent("2:  Unassigned"),
                        new GUIContent("3:  Unassigned"),
                        new GUIContent("4:  Unassigned"),
                        new GUIContent("5:  Unassigned"),
                        new GUIContent("6:  Unassigned"),
                        new GUIContent("7:  Unassigned"),
                        new GUIContent("8:  Unassigned"),
                        new GUIContent("9:  Unassigned"),
                        new GUIContent("10: Unassigned"),
                        new GUIContent("11: Unassigned"),
                        new GUIContent("12: Unassigned"),
                        new GUIContent("13: Unassigned"),
                        new GUIContent("14: Unassigned"),
                        new GUIContent("15: Unassigned"),
                        new GUIContent("16: Unassigned"),
                    };

                    for (var i = 0; i < targetOptionsTerrainLayers.Length; i++) {

                        var name = (i + 1) + ": missing layer";

                        if (targetOptionsTerrainLayers[i] != null) {

                            name = (i + 1) + ": " + targetOptionsTerrainLayers[i].name;

                        }

                        targetOptions[i + 1] = new GUIContent(name);

                    }

                }


                if (AtlasUtils.terrainLayerNamesOverride != null) {

                    for (var i = 0; i < Mathf.Min(targetOptions.Length, AtlasUtils.terrainLayerNamesOverride.Length); i++) {

                        if (!string.IsNullOrEmpty(AtlasUtils.terrainLayerNamesOverride[i])) {

                            targetOptions[i + 1] = new GUIContent(AtlasUtils.terrainLayerNamesOverride[i]);

                        }

                    }

                }

            }

        }

        private int TargetOptionsIndexToEnumIndex(int index) {

            switch (index) {

                case 1: return (int)StampBase.StampMap.StampTargetType.Splat1;
                case 2: return (int)StampBase.StampMap.StampTargetType.Splat2;
                case 3: return (int)StampBase.StampMap.StampTargetType.Splat3;
                case 4: return (int)StampBase.StampMap.StampTargetType.Splat4;
                case 5: return (int)StampBase.StampMap.StampTargetType.Splat5;
                case 6: return (int)StampBase.StampMap.StampTargetType.Splat6;
                case 7: return (int)StampBase.StampMap.StampTargetType.Splat7;
                case 8: return (int)StampBase.StampMap.StampTargetType.Splat8;
                case 9: return (int)StampBase.StampMap.StampTargetType.Splat9;
                case 10: return (int)StampBase.StampMap.StampTargetType.Splat10;
                case 11: return (int)StampBase.StampMap.StampTargetType.Splat11;
                case 12: return (int)StampBase.StampMap.StampTargetType.Splat12;
                case 13: return (int)StampBase.StampMap.StampTargetType.Splat13;
                case 14: return (int)StampBase.StampMap.StampTargetType.Splat14;
                case 15: return (int)StampBase.StampMap.StampTargetType.Splat15;
                case 16: return (int)StampBase.StampMap.StampTargetType.Splat16;

                default: return (int)StampBase.StampMap.StampTargetType.None;

            }

        }

        private int TargetEnumIndexToOptionsIndex(int index) {

            switch (index) {

                case (int)StampBase.StampMap.StampTargetType.Splat1: return 1;
                case (int)StampBase.StampMap.StampTargetType.Splat2: return 2;
                case (int)StampBase.StampMap.StampTargetType.Splat3: return 3;
                case (int)StampBase.StampMap.StampTargetType.Splat4: return 4;
                case (int)StampBase.StampMap.StampTargetType.Splat5: return 5;
                case (int)StampBase.StampMap.StampTargetType.Splat6: return 6;
                case (int)StampBase.StampMap.StampTargetType.Splat7: return 7;
                case (int)StampBase.StampMap.StampTargetType.Splat8: return 8;
                case (int)StampBase.StampMap.StampTargetType.Splat9: return 9;
                case (int)StampBase.StampMap.StampTargetType.Splat10: return 10;
                case (int)StampBase.StampMap.StampTargetType.Splat11: return 11;
                case (int)StampBase.StampMap.StampTargetType.Splat12: return 12;
                case (int)StampBase.StampMap.StampTargetType.Splat13: return 13;
                case (int)StampBase.StampMap.StampTargetType.Splat14: return 14;
                case (int)StampBase.StampMap.StampTargetType.Splat15: return 15;
                case (int)StampBase.StampMap.StampTargetType.Splat16: return 16;

                default: return 0;

            }

        }

    }

}
