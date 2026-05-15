#if UNITY_EDITOR
using SCF.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SCF.EditorTools
{
    public static class SCFCameraTuningSetup
    {
        [MenuItem("SCF/Setup/Add Camera Tuning Panel")]
        public static void AddCameraTuningPanel()
        {
            AddCameraTuningPanel(true, true, true);
        }

        public static CameraTuningPanel AddCameraTuningPanel(bool selectObject, bool saveScene, bool applyPreset)
        {
            GameObject player = GameObject.Find("SCF_Player");
            Transform target = player != null ? player.transform : null;

            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                Undo.RegisterCreatedObjectUndo(cameraObject, "Create SCF camera");
                cameraObject.tag = "MainCamera";
                camera = Undo.AddComponent<Camera>(cameraObject);
            }

            IsometricCameraFollow follow = camera.GetComponent<IsometricCameraFollow>();
            if (follow == null)
            {
                follow = Undo.AddComponent<IsometricCameraFollow>(camera.gameObject);
            }

            if (applyPreset)
            {
                Undo.RecordObject(follow, "Apply SCF tactical camera preset");
                follow.ApplyTacticalShooterPreset();
            }

            if (target != null)
            {
                Undo.RecordObject(follow, "Bind SCF camera follow target");
                follow.Configure(target);
            }

            CameraTuningPanel panel = camera.GetComponent<CameraTuningPanel>();
            if (panel == null)
            {
                panel = Undo.AddComponent<CameraTuningPanel>(camera.gameObject);
            }

            panel.Configure(camera, follow);
            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(follow);
            EditorUtility.SetDirty(panel);

            if (selectObject)
            {
                Selection.activeGameObject = camera.gameObject;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            if (saveScene)
            {
                EditorSceneManager.SaveOpenScenes();
            }

            Debug.Log("SCF camera tuning panel is ready. Enter Play Mode and press F9 to show or hide it.");
            return panel;
        }
    }
}
#endif
