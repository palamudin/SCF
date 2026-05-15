#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SCF.EditorTools
{
    [InitializeOnLoad]
    public static class SCFOneShotSceneBootstrap
    {
        private const string CreatePlayerRequestPath = "Assets/SCF/EditorBootstrap/CreatePlayer.request";
        private const string RepairVisualRequestPath = "Assets/SCF/EditorBootstrap/RepairPlayerVisual.request";
        private const string AddParkourBrowserRequestPath = "Assets/SCF/EditorBootstrap/AddParkourBrowser.request";
        private const string AddCameraTuningRequestPath = "Assets/SCF/EditorBootstrap/AddCameraTuning.request";
        private const string AddMobilityTestBuildingsRequestPath = "Assets/SCF/EditorBootstrap/AddMobilityTestBuildings.request";
        private const string ApplyTraversalAnimationRequestPath = "Assets/SCF/EditorBootstrap/ApplyTraversalAnimation.request";
        private const string AttachMotionSystemRequestPath = "Assets/SCF/EditorBootstrap/AttachMotionSystem.request";
        private const string AddCharacterSelectionRequestPath = "Assets/SCF/EditorBootstrap/AddCharacterSelection.request";
        private static double nextPollTime;
        private static bool mobilityTestPreviewCreatedInPlayMode;
        private static bool traversalPreviewCreatedInPlayMode;

        static SCFOneShotSceneBootstrap()
        {
            EditorApplication.delayCall += TryRun;
            EditorApplication.update += PollForPendingRequests;
            EditorApplication.playModeStateChanged += _ => EditorApplication.delayCall += TryRun;
        }

        [MenuItem("SCF/Setup/Run Pending One-Shot Requests")]
        public static void RunPendingRequests()
        {
            TryRun();
        }

        private static void PollForPendingRequests()
        {
            if (EditorApplication.timeSinceStartup < nextPollTime)
            {
                return;
            }

            nextPollTime = EditorApplication.timeSinceStartup + 1d;
            if (HasPendingRequest())
            {
                TryRun();
            }
        }

        private static void TryRun()
        {
            bool shouldAddMobilityTestBuildings = File.Exists(AddMobilityTestBuildingsRequestPath);
            bool shouldApplyTraversalAnimation = File.Exists(ApplyTraversalAnimationRequestPath);
            bool hasEditModeOnlyRequest = File.Exists(CreatePlayerRequestPath)
                || File.Exists(RepairVisualRequestPath)
                || File.Exists(AddParkourBrowserRequestPath)
                || File.Exists(AddCameraTuningRequestPath)
                || File.Exists(AttachMotionSystemRequestPath)
                || File.Exists(AddCharacterSelectionRequestPath);
            if (hasEditModeOnlyRequest && EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += TryRun;
                return;
            }

            if (EditorApplication.isPlaying && (shouldAddMobilityTestBuildings || shouldApplyTraversalAnimation))
            {
                if (shouldAddMobilityTestBuildings && !mobilityTestPreviewCreatedInPlayMode)
                {
                    SCFMobilityTestSceneSetup.AddMobilityTestBuildings(false);
                    mobilityTestPreviewCreatedInPlayMode = true;
                }

                if (shouldApplyTraversalAnimation && !traversalPreviewCreatedInPlayMode)
                {
                    SCFTraversalAnimationSetup.ApplyTraversalCameraAndAnimations(false);
                    traversalPreviewCreatedInPlayMode = true;
                }

                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.path.Replace("\\", "/").StartsWith("Temp/", System.StringComparison.OrdinalIgnoreCase))
            {
                EditorApplication.delayCall += TryRun;
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += TryRun;
                return;
            }

            mobilityTestPreviewCreatedInPlayMode = false;
            traversalPreviewCreatedInPlayMode = false;

            bool shouldCreatePlayer = File.Exists(CreatePlayerRequestPath);
            bool shouldRepairVisual = File.Exists(RepairVisualRequestPath);
            bool shouldAddParkourBrowser = File.Exists(AddParkourBrowserRequestPath);
            bool shouldAddCameraTuning = File.Exists(AddCameraTuningRequestPath);
            bool shouldAttachMotionSystem = File.Exists(AttachMotionSystemRequestPath);
            bool shouldAddCharacterSelection = File.Exists(AddCharacterSelectionRequestPath);
            if (!shouldCreatePlayer && !shouldRepairVisual && !shouldAddParkourBrowser && !shouldAddCameraTuning && !shouldAddMobilityTestBuildings && !shouldApplyTraversalAnimation && !shouldAttachMotionSystem && !shouldAddCharacterSelection)
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(CreatePlayerRequestPath));
            if (shouldCreatePlayer)
            {
                SCFPrototypeSceneSetup.CreateIsometricPrototypePlayer(false, true);
                DeleteRequest(CreatePlayerRequestPath);
            }

            if (shouldRepairVisual)
            {
                SCFPrototypeSceneSetup.RepairPlayerVisualModel(true);
                DeleteRequest(RepairVisualRequestPath);
            }

            if (shouldAddParkourBrowser)
            {
                SCFParkourAnimationPreviewSetup.AddParkourAnimationBrowser(false, true);
                DeleteRequest(AddParkourBrowserRequestPath);
            }

            if (shouldAddCameraTuning)
            {
                SCFCameraTuningSetup.AddCameraTuningPanel(false, true, true);
                DeleteRequest(AddCameraTuningRequestPath);
            }

            if (shouldAddMobilityTestBuildings)
            {
                SCFMobilityTestSceneSetup.AddMobilityTestBuildings(true);
                DeleteRequest(AddMobilityTestBuildingsRequestPath);
            }

            if (shouldApplyTraversalAnimation)
            {
                SCFTraversalAnimationSetup.ApplyTraversalCameraAndAnimations(true);
                DeleteRequest(ApplyTraversalAnimationRequestPath);
            }

            if (shouldAttachMotionSystem)
            {
                SCFMotionSystemSetup.AttachMotionSystemToPlayer();
                DeleteRequest(AttachMotionSystemRequestPath);
            }

            if (shouldAddCharacterSelection)
            {
                SCFCharacterSelectionSetup.ApplyCharacterSelectionSetup(true);
                DeleteRequest(AddCharacterSelectionRequestPath);
            }

            AssetDatabase.Refresh();
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("SCF one-shot bootstrap updated SCF_Player and saved the open scene.");
        }

        private static bool HasPendingRequest()
        {
            return File.Exists(CreatePlayerRequestPath)
                || File.Exists(RepairVisualRequestPath)
                || File.Exists(AddParkourBrowserRequestPath)
                || File.Exists(AddCameraTuningRequestPath)
                || File.Exists(AddMobilityTestBuildingsRequestPath)
                || File.Exists(ApplyTraversalAnimationRequestPath)
                || File.Exists(AttachMotionSystemRequestPath)
                || File.Exists(AddCharacterSelectionRequestPath);
        }

        private static void DeleteRequest(string path)
        {
            if (!AssetDatabase.DeleteAsset(path) && File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
#endif
