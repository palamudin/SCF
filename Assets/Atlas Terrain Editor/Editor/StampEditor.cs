using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Atlas.Unity {

    [CustomEditor(typeof(Stamp)), CanEditMultipleObjects]
    public class StampEditor : Editor {

        private SerializedProperty size;


        private void OnEnable() {

            size = serializedObject.FindProperty("size");

        }

        public override void OnInspectorGUI() {

            EditorGUILayout.PropertyField(size);

            bool thesameStampAssetInAllTargets = true;

            var stampAsset = (target as StampBase).stamp;

            foreach (var i in targets) {

                var stampBase = i as StampBase;

                if (stampBase.stamp != stampAsset) {

                    thesameStampAssetInAllTargets = false;

                    break;

                }

            }

            if (thesameStampAssetInAllTargets) {

                StampBaseEditor.DrawBaseStamp(serializedObject, true);

            } else {

                EditorGUILayout.HelpBox("Multi edit only supported when thesame stamp asset is selected", MessageType.Info);

            }

            if (serializedObject.hasModifiedProperties) {

                serializedObject.ApplyModifiedProperties();

                AtlasStamper.QueRender();

            }

        }

        //to focus on terrain in editor

        public bool HasFrameBounds() { return true; }

        public Bounds OnGetFrameBounds() { return new Bounds((target as StampBase).transform.position, Vector3.Scale((target as StampBase).size, (target as StampBase).transform.lossyScale) * 0.5f); }

    }

}
