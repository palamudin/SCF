using UnityEngine;
using UnityEditor;

namespace Atlas.Unity {

    public static class StampBaseEditor {

        private static bool tilingFolded = false;

        public static void DrawBaseStamp(SerializedObject serializedObject, bool allowMaskEditing = false) {

            var stampProperty = serializedObject.FindProperty("stamp");

            EditorGUILayout.PropertyField(stampProperty);

            if (stampProperty.objectReferenceValue == null) {

                EditorGUILayout.HelpBox("No stamp asset assigned.\nStamp will not render.", MessageType.Info);

            }

            EditorGUILayout.Space(10);


            var maskMapProperty = serializedObject.FindProperty("maskMap");
            maskMapProperty.FindPropertyRelative("mapType").enumValueIndex = (int)StampBase.StampMap.StampMapType.Mask;

            Border.Start();
            EditorGUILayout.PropertyField(maskMapProperty, new GUIContent("Mask"));

            if (serializedObject.targetObjects.Length <= 1) {

                if (allowMaskEditing && stampProperty.objectReferenceValue != null) {

                    if (maskMapProperty.isExpanded) {

                        DrawMaskEditor(serializedObject);

                    }

                }

            }

            Border.End(GetMapTypeColor(StampBase.StampMap.StampMapType.Mask));


            EditorGUILayout.Space(5);
            var heightMapProperty = serializedObject.FindProperty("heightMap");
            heightMapProperty.FindPropertyRelative("mapType").enumValueIndex = (int)StampBase.StampMap.StampMapType.Height;

            Border.Start();
            EditorGUILayout.PropertyField(heightMapProperty, new GUIContent("Height"));
            Border.End(GetMapTypeColor(StampBase.StampMap.StampMapType.Height));

            EditorGUILayout.Space(5);

            var colorMapProperty = serializedObject.FindProperty("colorMap");
            colorMapProperty.FindPropertyRelative("mapType").enumValueIndex = (int)StampBase.StampMap.StampMapType.Color;

            Border.Start();
            EditorGUILayout.PropertyField(colorMapProperty, new GUIContent("Color"));
            Border.End(GetMapTypeColor(StampBase.StampMap.StampMapType.Color));

            EditorGUILayout.Space(5);

            var stampMapsProperty = serializedObject.FindProperty("stampMaps");

            for (var i = 0; i < stampMapsProperty.arraySize; i++) {

                var propertyAtIndex = stampMapsProperty.GetArrayElementAtIndex(i);

                if (propertyAtIndex != null) {

                    propertyAtIndex.FindPropertyRelative("selfIndex").intValue = i;
                    propertyAtIndex.FindPropertyRelative("mapType").enumValueIndex = (int)StampBase.StampMap.StampMapType.Other;

                }

            }

            Border.Start();
            EditorGUILayout.PropertyField(stampMapsProperty, new GUIContent("Splat Layers", "Splatmap layers:\n\nAdd layers to write to the splatmap of your terrain."));
            Border.End(GetMapTypeColor(StampBase.StampMap.StampMapType.Other));

            EditorGUILayout.Space(5);

            var holeMapProperty = serializedObject.FindProperty("holeMap");
            holeMapProperty.FindPropertyRelative("mapType").enumValueIndex = (int)StampBase.StampMap.StampMapType.Hole;

            Border.Start();
            EditorGUILayout.PropertyField(holeMapProperty, new GUIContent("hole"));
            Border.End(GetMapTypeColor(StampBase.StampMap.StampMapType.Hole));

            EditorGUILayout.Space(5);

            Border.Start();

            tilingFolded = EditorGUILayout.Foldout(tilingFolded, new GUIContent("Tiling", "How mush the stamp texture will repeat."), EditorStyles.foldoutHeader);

            if (tilingFolded) {

                EditorGUILayout.PropertyField(serializedObject.FindProperty("tileMask"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stampTiling"));

            }

            Border.End(new Color(0, 0.5f, 1));

        }

        private static void DrawMaskEditor(SerializedObject serializedObject) {

            EditorGUILayout.Space(10);

            if (AtlasPainter.editing && AtlasPainter.currentStamp == serializedObject.targetObject) {

                //if (AtlasPainter.editing) {

                //if (AtlasPainter.currentStamp == serializedObject.targetObject) {

                EditorGUILayout.HelpBox("Click and drag to paint.\nHold [shift] to remove.\nHold [shift] and scroll to change size.\nHold [ctrl] and scroll to change opacity.\nHold [shift] + [ctrl] and scroll to rotate.", MessageType.Info);

                AtlasPainter.brush = (Texture2D)EditorGUILayout.ObjectField("brush", AtlasPainter.brush, typeof(Texture2D), false);

                AtlasPainter.size = EditorGUILayout.Slider("size", AtlasPainter.size, 0.01f, 1f);

                AtlasPainter.opacity = EditorGUILayout.Slider("opacity", AtlasPainter.opacity, 0.0f, 1f);

                AtlasPainter.rotation = EditorGUILayout.Slider("rotation", AtlasPainter.rotation, 0.0f, 360f);

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("apply")) {

                    AtlasPainter.ApplyChange();

                    AtlasPainter.StopEditStampMask();

                }

                if (GUILayout.Button("clear")) {

                    AtlasPainter.Clear();

                }

                if (GUILayout.Button("reset")) {

                    AtlasPainter.Reset();

                    AtlasPainter.StopEditStampMask();

                }

                if (GUILayout.Button("cancel")) {

                    AtlasPainter.StopEditStampMask();

                }

                GUILayout.EndHorizontal();

                //} else {
                //
                //    EditorGUILayout.HelpBox("Currently editing another stamp mask.", MessageType.Info);
                //
                //}

            } else {

                GUILayout.BeginHorizontal();

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(new GUIContent("Paint mask", "Enable manual mask painting."), GUILayout.Width(90))) {

                    AtlasPainter.EditStampMask((Stamp)serializedObject.targetObject);

                    AtlasStamper.QueRender();

                }

                GUILayout.FlexibleSpace();

                GUILayout.EndHorizontal();

                EditorGUILayout.Space(10);

            }

        }

        private static Color GetMapTypeColor(StampBase.StampMap.StampMapType mapType) {

            switch (mapType) {

                case StampBase.StampMap.StampMapType.Height: return Color.red;
                case StampBase.StampMap.StampMapType.Color: return Color.cyan;
                case StampBase.StampMap.StampMapType.Mask: return Color.black;
                case StampBase.StampMap.StampMapType.Hole: return new Color(1, 0.5f, 0, 1);
                default: return Color.white;

            }

        }

        public static void DrawMinMaxSlider(string name, string tooltip, Rect rect, SerializedProperty min, SerializedProperty max, float minLimit, float maxLimit) {

            EditorGUI.BeginChangeCheck();

            var minValue = min.floatValue;
            var maxValue = max.floatValue;

            minValue = EditorGUI.FloatField(new Rect(rect.x + rect.width - 105 - 45, rect.y, 50 + 45, rect.height), Mathf.Clamp(Mathf.Floor(minValue * 100) / 100, minLimit, maxLimit), EditorStyles.miniTextField);
            maxValue = EditorGUI.FloatField(new Rect(rect.x + rect.width - 50 - 45, rect.y, 50 + 45, rect.height), Mathf.Clamp(Mathf.Floor(maxValue * 100) / 100, minLimit, maxLimit), EditorStyles.miniTextField);

            if (minValue > maxValue) {

                var preMinValue = minValue;
                minValue = maxValue;
                maxValue = preMinValue;

            }

            EditorGUI.MinMaxSlider(new Rect(rect.x, rect.y, rect.width - 120, rect.height), new GUIContent(name, tooltip), ref minValue, ref maxValue, minLimit, maxLimit);

            if (EditorGUI.EndChangeCheck()) {

                min.floatValue = minValue;
                max.floatValue = maxValue;

                min.serializedObject.ApplyModifiedProperties();
                max.serializedObject.ApplyModifiedProperties();

            }

        }

    }

}

