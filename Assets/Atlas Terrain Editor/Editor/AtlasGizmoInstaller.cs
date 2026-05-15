using UnityEngine;
using UnityEditor;

namespace Atlas.Unity {

    [InitializeOnLoad]
    public class AtlasGizmoInstaller {

        static AtlasGizmoInstaller() {

            var installed = SessionState.GetBool("AtlasGizmosInstalled", false);

            if (!installed) {

                SessionState.SetBool("AtlasGizmosInstalled", true);

                var path = "Assets/Gizmos";

                if (System.IO.Directory.Exists(path) == false) {

                    System.IO.Directory.CreateDirectory(path);

                }


                var iconPath = GetIconPath("l:Atlas_icon");

                var targetPath = path + "/AtlasIcon.tif";

                if (string.IsNullOrEmpty(iconPath) == false && System.IO.File.Exists(targetPath) == false) {

                    AssetDatabase.CopyAsset(iconPath, targetPath);

                }


                iconPath = GetIconPath("l:Atlas_spline_icon");

                targetPath = path + "/AtlasSplineIcon.tif";

                if (string.IsNullOrEmpty(iconPath) == false && System.IO.File.Exists(targetPath) == false) {

                    AssetDatabase.CopyAsset(iconPath, targetPath);

                }


                AssetDatabase.Refresh();

            }

        }

        public static string GetIconPath(string pattern) {

            var guids = AssetDatabase.FindAssets(pattern);

            if (guids.Length > 0) {

                return AssetDatabase.GUIDToAssetPath(guids[0]);

            }

            return null;

        }

    }

}