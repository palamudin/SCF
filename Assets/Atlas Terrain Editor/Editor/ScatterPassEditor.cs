using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace Atlas.Unity {

    [CustomPropertyDrawer(typeof(ScatterRuleAsset.ScatterPass))]
    public class ScatterPassEditor : PropertyDrawer {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

            EditorGUI.BeginProperty(position, label, property);

            if (property.FindPropertyRelative("inspectorInitialized").boolValue == false) {
                property.FindPropertyRelative("inspectorInitialized").boolValue = true;
                property.FindPropertyRelative("mode").intValue = 0;
                property.FindPropertyRelative("cullRadius").floatValue = 1;
                property.FindPropertyRelative("detailCullRadius").floatValue = 1;
                property.FindPropertyRelative("spawnRadius").floatValue = 5;
                property.FindPropertyRelative("childCount").intValue = 3;
                property.FindPropertyRelative("scaleMultiplierMin").floatValue = 1;
                property.FindPropertyRelative("scaleMultiplierMax").floatValue = 1;
                property.FindPropertyRelative("widthScaleMin").floatValue = 1;
                property.FindPropertyRelative("widthScaleMax").floatValue = 1;
                property.FindPropertyRelative("heightScaleMin").floatValue = 1;
                property.FindPropertyRelative("heightScaleMax").floatValue = 1;
                property.FindPropertyRelative("distanceScaleMultiplier").floatValue = 1;
                property.FindPropertyRelative("maskScaleMultiplier").floatValue = 1;
            }

            EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label, EditorStyles.boldLabel);

            if (property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, 18), property.isExpanded, GUIContent.none, EditorStyles.foldout)) {

                EditorGUI.indentLevel++;


                var modeProperty = property.FindPropertyRelative("mode");

                modeProperty.intValue = EditorGUI.Popup(new Rect(position.x, position.y + 20, position.width, 18),new GUIContent(modeProperty.displayName,null,modeProperty.tooltip), modeProperty.intValue, new GUIContent[] { new GUIContent("TreePrototypes") , new GUIContent("GameObject") });

                //EditorGUI.BeginDisabledGroup(modeProperty.intValue == 0);
                //
                //if (GUI.Button(new Rect(position.x + 20, position.y + 20, (position.width - 20) / 2, 18), "Tree")) {
                //    modeProperty.intValue = 0;
                //}
                //
                //EditorGUI.EndDisabledGroup();
                //
                //EditorGUI.BeginDisabledGroup(modeProperty.intValue != 0);
                //
                //if (GUI.Button(new Rect(position.x + 20 + ((position.width - 20) / 2), position.y + 20, (position.width - 20) / 2, 18), "Prefab")) {
                //    modeProperty.intValue = 2;
                //}
                //
                //EditorGUI.EndDisabledGroup();

                var prefabsProperty = property.FindPropertyRelative("prefabs");
                EditorGUI.PropertyField(new Rect(position.x, position.y + 40, position.width, 18), prefabsProperty, true);

                var heightOffset = prefabsProperty.isExpanded ? (20 * 2) + (20 * Mathf.Max(1, prefabsProperty.arraySize)) : 0;

                heightOffset += 20;

                var usingCulling = property.serializedObject.FindProperty("selfCulling").boolValue;

                if (usingCulling) {

                    EditorGUI.PropertyField(new Rect(position.x, position.y + heightOffset + 60, position.width, 18), property.FindPropertyRelative("cullRadius"));
                    EditorGUI.PropertyField(new Rect(position.x, position.y + heightOffset + 80, position.width, 18), property.FindPropertyRelative("detailCullRadius"));
                    EditorGUI.PropertyField(new Rect(position.x, position.y + heightOffset + 120, position.width, 18), property.FindPropertyRelative("spawnRadius"));
                    EditorGUI.PropertyField(new Rect(position.x, position.y + heightOffset + 140, position.width, 18), property.FindPropertyRelative("childCount"));

                } else {

                    EditorGUI.PropertyField(new Rect(position.x, position.y + heightOffset + 60, position.width, 18), property.FindPropertyRelative("spawnRadius"));
                    EditorGUI.PropertyField(new Rect(position.x, position.y + heightOffset + 80, position.width, 18), property.FindPropertyRelative("childCount"));

                    heightOffset -= 3 * 20;

                }

                heightOffset += 40;

                var foldedProperty = property.FindPropertyRelative("folded");

                foldedProperty.boolValue = EditorGUI.Foldout(new Rect(position.x, position.y + heightOffset + 140, position.width, 18), foldedProperty.boolValue, "Scaling", true);

                if (foldedProperty.boolValue) {

                    EditorGUI.indentLevel++;

                    StampBaseEditor.DrawMinMaxSlider("Scale Multiplier", "Min and max scale multiplier.", new Rect(position.x, position.y + heightOffset + 160, position.width, 18), property.FindPropertyRelative("scaleMultiplierMin"), property.FindPropertyRelative("scaleMultiplierMax"), 0.1f, 10);
                    StampBaseEditor.DrawMinMaxSlider("Width Multiplier", "Min and max width multiplier.", new Rect(position.x + 20, position.y + heightOffset + 180, position.width - 20, 18), property.FindPropertyRelative("widthScaleMin"), property.FindPropertyRelative("widthScaleMax"), 0.1f, 10);
                    StampBaseEditor.DrawMinMaxSlider("Height Multiplier", "Min and max height multiplier.", new Rect(position.x + 20, position.y + heightOffset + 200, position.width - 20, 18), property.FindPropertyRelative("heightScaleMin"), property.FindPropertyRelative("heightScaleMin"), 0.1f, 10);
                    //EditorGUI.PropertyField(new Rect(position.x, position.y +heightOffset+ 160, position.width, 18), property.FindPropertyRelative("scaleMultiplierMin"));
                    //EditorGUI.PropertyField(new Rect(position.x, position.y +heightOffset+ 180, position.width, 18), property.FindPropertyRelative("scaleMultiplierMax"));
                    //EditorGUI.PropertyField(new Rect(position.x, position.y + heightOffset + 200, position.width, 18), property.FindPropertyRelative("widthScaleMin"));
                    //EditorGUI.PropertyField(new Rect(position.x, position.y + heightOffset + 220, position.width, 18), property.FindPropertyRelative("widthScaleMax"));
                    //EditorGUI.PropertyField(new Rect(position.x, position.y + heightOffset + 240, position.width, 18), property.FindPropertyRelative("heightScaleMin"));
                    //EditorGUI.PropertyField(new Rect(position.x, position.y + heightOffset + 260, position.width, 18), property.FindPropertyRelative("heightScaleMax"));
                    EditorGUI.PropertyField(new Rect(position.x, position.y + heightOffset + 240, position.width, 18), property.FindPropertyRelative("distanceScaleMultiplier"));
                    EditorGUI.PropertyField(new Rect(position.x, position.y + heightOffset + 260, position.width, 18), property.FindPropertyRelative("maskScaleMultiplier"));

                    if (modeProperty.intValue != 0) {

                        EditorGUI.PropertyField(new Rect(position.x, position.y + heightOffset + 300, position.width, 18), property.FindPropertyRelative("alignment"));
                        StampBaseEditor.DrawMinMaxSlider("Height Offset", "Min and max height offset.", new Rect(position.x, position.y + heightOffset + 320, position.width, 18), property.FindPropertyRelative("heightOffsetMin"), property.FindPropertyRelative("heightOffsetMax"), -5, 5);

                        //EditorGUI.PropertyField(new Rect(position.x, position.y + heightOffset + 260, position.width, 18), property.FindPropertyRelative("heightOffsetMin"));
                        //EditorGUI.PropertyField(new Rect(position.x, position.y + heightOffset + 280, position.width, 18), property.FindPropertyRelative("heightOffsetMax"));
                    }

                    EditorGUI.indentLevel--;

                }

                EditorGUI.indentLevel--;

            }

            EditorGUI.EndProperty();

        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {

            var totalHeight = 20;// * 3;

            if (property.isExpanded) {

                totalHeight += 20 * 2;

                var prefabsProperty = property.FindPropertyRelative("prefabs");

                if (prefabsProperty.isExpanded) {

                    totalHeight += 20 * 2;

                    totalHeight += 20 * Mathf.Max(1, prefabsProperty.arraySize);

                }

                var usingCulling = property.serializedObject.FindProperty("selfCulling").boolValue;

                if (usingCulling) {

                    totalHeight += 20 * 8;

                } else {

                    totalHeight += 20 * 5;

                }

                var foldedProperty = property.FindPropertyRelative("folded");

                if (foldedProperty.boolValue) {

                    var modeProperty = property.FindPropertyRelative("mode");

                    totalHeight += 20 * 6;

                    if (modeProperty.intValue != 0) {

                        totalHeight += 20 * 3;

                    }

                }

            }

            return totalHeight;

        }

    }

}
