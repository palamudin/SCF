using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace Atlas.Unity {

    [CustomEditor(typeof(ScatterRuleAsset))]
    [CanEditMultipleObjects]
    public class ScatterRuleAssetEditor : Editor {

        private SerializedProperty targetProperty;
        private SerializedProperty maskFactorMin;
        private SerializedProperty maskFactorMax;

        private SerializedProperty blockTagLayer;
        private SerializedProperty invertTagLayer;

        private SerializedProperty slopeMin;
        private SerializedProperty slopeMax;

        private SerializedProperty heightMin;
        private SerializedProperty heightMax;

        private SerializedProperty spacing;
        private SerializedProperty seed;
        private SerializedProperty selfCulling;

        private SerializedProperty scatterStack;

        private SerializedProperty detailStack;

        private string[] targetOptions;

        private static bool maskFolded = false;
        private static bool placementFolded = false;

        public void OnEnable() {

            targetProperty = serializedObject.FindProperty("target");
            maskFactorMin = serializedObject.FindProperty("maskFactorMin");
            maskFactorMax = serializedObject.FindProperty("maskFactorMax");

            blockTagLayer = serializedObject.FindProperty("blockTagLayer");
            invertTagLayer = serializedObject.FindProperty("invertTagLayer");

            slopeMin = serializedObject.FindProperty("slopeMin");
            slopeMax = serializedObject.FindProperty("slopeMax");

            heightMin = serializedObject.FindProperty("heightMin");
            heightMax = serializedObject.FindProperty("heightMax");

            spacing = serializedObject.FindProperty("spacing");
            seed = serializedObject.FindProperty("seed");
            selfCulling = serializedObject.FindProperty("selfCulling");

            scatterStack = serializedObject.FindProperty("scatterStack");

            detailStack = serializedObject.FindProperty("detailStack");

            targetOptions = Enum.GetNames(typeof(AtlasScatterer.ScatterTargets));

        }

        public override void OnInspectorGUI() {

            Border.Start();

            maskFolded = EditorGUILayout.Foldout(maskFolded, "Masking", EditorStyles.foldoutHeader);

            if (maskFolded) {

                EditorGUI.indentLevel++;

                targetProperty.intValue = EditorGUILayout.MaskField(new GUIContent(targetProperty.displayName, targetProperty.tooltip), targetProperty.intValue, targetOptions);

                EditorGUILayout.Space(10);

                EditorGUILayout.PropertyField(maskFactorMin);
                EditorGUILayout.PropertyField(maskFactorMax);

                EditorGUILayout.Space(10);

                EditorGUILayout.PropertyField(slopeMin);
                EditorGUILayout.PropertyField(slopeMax);

                EditorGUILayout.Space(10);

                EditorGUILayout.PropertyField(heightMin);
                EditorGUILayout.PropertyField(heightMax);

                EditorGUILayout.Space(10);

                EditorGUILayout.PropertyField(blockTagLayer);
                EditorGUILayout.PropertyField(invertTagLayer);

                EditorGUI.indentLevel--;

            }

            Border.End(0);


            EditorGUILayout.Space(5);

            Border.Start();

            placementFolded = EditorGUILayout.Foldout(placementFolded, "Placement", EditorStyles.foldoutHeader);

            if (placementFolded) {

                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(spacing);
                EditorGUILayout.PropertyField(seed);
                EditorGUILayout.PropertyField(selfCulling);

                EditorGUI.indentLevel--;

            }

            Border.End(1);


            EditorGUILayout.Space(5);

            Border.Start();

            EditorGUILayout.PropertyField(scatterStack);

            EditorGUILayout.PropertyField(detailStack);

            Border.End(2);


            if (serializedObject.hasModifiedProperties) {

                serializedObject.ApplyModifiedProperties();

            }

        }

    }

}
