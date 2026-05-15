using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Atlas.Unity {

    public static class AtlasMenuItems {

        [MenuItem("GameObject/Atlas/Unity/Preview Volume", false, 0)]
        public static void CreateAtlasUnityPreview() {

            var o = new GameObject("Atlas Preview Volume");

            o.transform.position = new Vector3(0, 0, 0);

            var oo = o.AddComponent<AtlasUnityPreviewVolume>();

            oo.terrainSource = GameObject.FindObjectOfType<Terrain>();

            Selection.activeGameObject = o;

        }

        [MenuItem("GameObject/Atlas/Unity/Stamp", false, 0)]
        public static void CreateStampQuad() {

            var o = new GameObject("Stamp");

            var stampQuad = o.AddComponent<Stamp>();

            stampQuad.size = new Vector3(512, 64, 512);

            if (Selection.activeGameObject != null) {

                o.transform.SetParent(Selection.activeGameObject.transform);
                o.transform.localPosition = Vector3.zero;
                o.transform.localRotation = Quaternion.identity;
                o.transform.localScale = Vector3.one;

            }

            Selection.activeGameObject = o;

        }

        [MenuItem("GameObject/Atlas/Unity/Spline Stamp", false, 0)]
        public static void CreateStampSpline() {

            var o = new GameObject("Spline Stamp");

            var stampSpline = o.AddComponent<SplineStamp>();

            stampSpline.size = new Vector3(1, 8, 1);

            stampSpline.points = new List<SplineStamp.SplinePoint>() {
                new SplineStamp.SplinePoint(new Vector3(0,0,0.5f) *100,1),
                new SplineStamp.SplinePoint(new Vector3(0,0,-0.5f)*100,1),
            };

            stampSpline.width = 20;

            if (Selection.activeGameObject != null) {

                o.transform.SetParent(Selection.activeGameObject.transform);
                o.transform.localPosition = Vector3.zero;
                o.transform.localRotation = Quaternion.identity;
                o.transform.localScale = Vector3.one;

            }

            Selection.activeGameObject = o;

        }

        //[MenuItem("Window/Atlas/Atlas Store")]
        //public static void AtlasStore() {
        //
        //    Application.OpenURL("<...>");
        //
        //}

        public static string GetProjectViewPath() {

            var path = "Assets";

            foreach (var obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets)) {

                path = AssetDatabase.GetAssetPath(obj);

                if (!string.IsNullOrEmpty(path) && File.Exists(path)) {

                    path = Path.GetDirectoryName(path);

                    break;

                }
            }

            return path;

        }

    }

}
