using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Atlas.Unity {

    [CustomPropertyDrawer(typeof(ScatterRuleAsset.DetailPass))]
    public class DetailPassEditor : PropertyDrawer {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

            EditorGUI.BeginProperty(position, label, property);

            if (property.FindPropertyRelative("inspectorInitialized").boolValue == false) {
                property.FindPropertyRelative("inspectorInitialized").boolValue = true;
                property.FindPropertyRelative("useMesh").boolValue = false;
                property.FindPropertyRelative("density").intValue = 10;
                property.FindPropertyRelative("minHeight").floatValue = 1;
                property.FindPropertyRelative("maxHeight").floatValue = 5;
                property.FindPropertyRelative("minWidth").floatValue = 1;
                property.FindPropertyRelative("maxWidth").floatValue = 3;
                property.FindPropertyRelative("holeEdgePadding").floatValue = 1;
                property.FindPropertyRelative("useInstancing").boolValue = true;
            }

            EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label, EditorStyles.boldLabel);

            if (property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, 18), property.isExpanded, GUIContent.none, EditorStyles.foldout)) {

                EditorGUI.indentLevel++;

                var useMeshProperty = property.FindPropertyRelative("useMesh");

                EditorGUI.BeginDisabledGroup(useMeshProperty.boolValue == false);

                if (GUI.Button(new Rect(position.x + 20, position.y + 20, (position.width - 20) / 2, 18), "Texture")) {
                    useMeshProperty.boolValue = false;
                }

                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(useMeshProperty.boolValue);

                if (GUI.Button(new Rect(position.x + 20 + ((position.width - 20) / 2), position.y + 20, (position.width - 20) / 2, 18), "Prefab")) {
                    useMeshProperty.boolValue = true;
                }

                EditorGUI.EndDisabledGroup();

                float heightOffset;

                if (useMeshProperty.boolValue == false) {

                    var texturesProperty = property.FindPropertyRelative("textures");
                    EditorGUI.PropertyField(new Rect(position.x, position.y + 40, position.width, 18), texturesProperty, true);
                    heightOffset = texturesProperty.isExpanded ? (20 * 2) + (20 * Mathf.Max(1, texturesProperty.arraySize)) : 0;

                } else {

                    var prefabsProperty = property.FindPropertyRelative("prefabs");
                    EditorGUI.PropertyField(new Rect(position.x, position.y + 40, position.width, 18), prefabsProperty, true);
                    heightOffset = prefabsProperty.isExpanded ? (20 * 2) + (20 * Mathf.Max(1, prefabsProperty.arraySize)) : 0;

                }

                EditorGUI.PropertyField(new Rect(position.x, position.y + heightOffset + 60, position.width, 18), property.FindPropertyRelative("density"));

                var foldedProperty = property.FindPropertyRelative("folded");

                foldedProperty.boolValue = EditorGUI.Foldout(new Rect(position.x, position.y + heightOffset + 80, position.width, 18), foldedProperty.boolValue, "Properties", true);

                if (foldedProperty.boolValue) {

                    EditorGUI.indentLevel++;
                    StampBaseEditor.DrawMinMaxSlider("Height Multiplier", "Min and Max height multiplier.", new Rect(position.x, position.y + heightOffset + 100, position.width, 18), property.FindPropertyRelative("minHeight"), property.FindPropertyRelative("maxHeight"), 0.1f, 10);
                    StampBaseEditor.DrawMinMaxSlider("Width Multiplier", "Min and Max width multiplier.", new Rect(position.x, position.y + heightOffset + 120, position.width, 18), property.FindPropertyRelative("minWidth"), property.FindPropertyRelative("maxWidth"), 0.1f, 10);

                    //EditorGUI.PropertyField(new Rect(position.x, position.y+ heightOffset + 100, position.width, 18), property.FindPropertyRelative("minHeight"));
                    //EditorGUI.PropertyField(new Rect(position.x, position.y+ heightOffset + 120, position.width, 18), property.FindPropertyRelative("maxHeight"));
                    //EditorGUI.PropertyField(new Rect(position.x, position.y+ heightOffset + 140, position.width, 18), property.FindPropertyRelative("minWidth"));
                    //EditorGUI.PropertyField(new Rect(position.x, position.y+ heightOffset + 160, position.width, 18), property.FindPropertyRelative("maxWidth"));
                    EditorGUI.PropertyField(new Rect(position.x, position.y + heightOffset + 140, position.width, 18), property.FindPropertyRelative("holeEdgePadding"));
                    EditorGUI.PropertyField(new Rect(position.x, position.y + heightOffset + 160, position.width, 18), property.FindPropertyRelative("useInstancing"));

                    if (useMeshProperty.boolValue == false) {

                        EditorGUI.PropertyField(new Rect(position.x, position.y + heightOffset + 180, position.width, 18), property.FindPropertyRelative("renderMode"));

                    }

                    EditorGUI.indentLevel--;

                }

                EditorGUI.indentLevel--;

            }

            EditorGUI.EndProperty();

        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {

            var totalHeight = 20 * 1;

            if (property.isExpanded) {

                var useMeshProperty = property.FindPropertyRelative("useMesh");

                if (useMeshProperty.boolValue == false) {

                    var texturesProperty = property.FindPropertyRelative("textures");

                    if (texturesProperty.isExpanded) {

                        totalHeight += 20 * 2;

                        totalHeight += 20 * Mathf.Max(1, texturesProperty.arraySize);

                    }

                } else {

                    var prefabsProperty = property.FindPropertyRelative("prefabs");

                    if (prefabsProperty.isExpanded) {

                        totalHeight += 20 * 2;

                        totalHeight += 20 * Mathf.Max(1, prefabsProperty.arraySize);

                    }

                }

                totalHeight += 20 * 4;

                var foldedProperty = property.FindPropertyRelative("folded");

                if (foldedProperty.boolValue) {

                    totalHeight += 20 * 4;

                    if (useMeshProperty.boolValue == false) {

                        totalHeight += 20 * 1;

                    }

                }

            }

            return totalHeight;

        }

    }

}
