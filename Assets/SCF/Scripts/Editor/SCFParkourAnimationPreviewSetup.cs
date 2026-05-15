#if UNITY_EDITOR
using SCF.Gameplay;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SCF.EditorTools
{
    public static class SCFParkourAnimationPreviewSetup
    {
        private const string PreviewRigName = "SCF_ParkourPreviewRig";
        private const string ScfVisualName = "SCF_VisibleArmature";
        private const string ScfClipsFolder = "Assets/SCF/Animation";

        [MenuItem("SCF/Setup/Add Parkour Animation Browser")]
        public static void AddParkourAnimationBrowser()
        {
            AddParkourAnimationBrowser(true, true);
        }

        public static GameObject AddParkourAnimationBrowser(bool selectObject, bool saveScene)
        {
            GameObject player = SCFPrototypeSceneSetup.RepairPlayerVisualModel(false);
            RemoveOldPreviewRig();

            Animator previewAnimator = ResolvePlayerAnimator(player);
            ParkourAnimationPreviewPanel panel = player.GetComponent<ParkourAnimationPreviewPanel>();
            if (panel == null)
            {
                panel = Undo.AddComponent<ParkourAnimationPreviewPanel>(player);
            }

            panel.enabled = true;
            panel.Configure(previewAnimator, LoadScfClips());
            RebindPlayerAnimationStack(player, previewAnimator != null ? previewAnimator.transform : player.transform, previewAnimator);
            EditorUtility.SetDirty(panel);
            EditorUtility.SetDirty(player);

            if (selectObject)
            {
                Selection.activeGameObject = player;
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            if (saveScene)
            {
                EditorSceneManager.SaveOpenScenes();
            }

            Debug.Log("SCF animation browser is ready. Enter Play Mode and click a clip-name button to preview Assets/SCF/Animation clips.");
            return player;
        }

        private static Animator ResolvePlayerAnimator(GameObject player)
        {
            if (player == null)
            {
                return null;
            }

            Transform visual = player.transform.Find(ScfVisualName);
            if (visual != null)
            {
                Animator visualAnimator = visual.GetComponentInChildren<Animator>(true);
                if (visualAnimator != null)
                {
                    return visualAnimator;
                }
            }

            return player.GetComponentInChildren<Animator>(true);
        }

        private static void RebindPlayerAnimationStack(GameObject player, Transform visualRoot, Animator animator)
        {
            IsometricPlayerInput playerInput = player.GetComponent<IsometricPlayerInput>();
            IsometricCharacterMotor motor = player.GetComponent<IsometricCharacterMotor>();
            Camera camera = Camera.main;

            if (motor != null)
            {
                motor.Configure(playerInput, camera, visualRoot);
                EditorUtility.SetDirty(motor);
            }

            MovementAnimatorBridge animatorBridge = player.GetComponent<MovementAnimatorBridge>();
            if (animatorBridge != null)
            {
                animatorBridge.Configure(animator);
                EditorUtility.SetDirty(animatorBridge);
            }

            MotionMatchingSignalHub signalHub = player.GetComponent<MotionMatchingSignalHub>();
            if (signalHub != null)
            {
                signalHub.Configure(animator);
                EditorUtility.SetDirty(signalHub);
            }

            SCFMotionSelector motionSelector = player.GetComponent<SCFMotionSelector>();
            if (motionSelector != null)
            {
                motionSelector.Configure(animator, SCFMotionDatabaseBaker.BuildBaseDatabase());
                EditorUtility.SetDirty(motionSelector);
            }

            SCFMotionSystemSetup.DisableLegacyMotionComponents(player);
        }

        private static void RemoveOldPreviewRig()
        {
            GameObject oldPreviewRig = GameObject.Find(PreviewRigName);
            if (oldPreviewRig != null)
            {
                Undo.DestroyObjectImmediate(oldPreviewRig);
            }
        }

        private static List<AnimationClip> LoadScfClips()
        {
            List<AnimationClip> clips = new List<AnimationClip>();
            string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { ScfClipsFolder });

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null && !clips.Contains(clip))
                {
                    clips.Add(clip);
                }
            }

            clips.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            return clips;
        }
    }
}
#endif
