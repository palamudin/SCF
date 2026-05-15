using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Atlas.Unity {

    class AtlasSceneProcessor : IProcessSceneWithReport {

        public int callbackOrder { get { return 0; } }

        public void OnProcessScene(UnityEngine.SceneManagement.Scene scene, BuildReport report) {

            if (AtlasProjectSettings.GetOrCreateSettings().stripStampsFromBuild) {

                Debug.Log("Atlas Scene processor: Removing stamps from scene due to project setting 'strip stamps from build' being enabled: " + scene.name);

                var allStamps = GameObject.FindObjectsOfType<StampBase>();

                foreach (var i in allStamps) {

                    GameObject.DestroyImmediate(i.gameObject, false);

                }

                var allPreviewVolumes = GameObject.FindObjectsOfType<AtlasUnityPreviewVolume>();

                foreach (var i in allPreviewVolumes) {

                    GameObject.DestroyImmediate(i.gameObject, false);

                }


            }

        }

    }

}