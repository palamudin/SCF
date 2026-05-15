#if UNITY_EDITOR
using SCF.Gameplay;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SCF.EditorTools
{
    public static class SCFTraversalAnimationSetup
    {
        private const string ControllerFolder = "Assets/SCF/Animation";
        private const string ControllerPath = ControllerFolder + "/SCF_ParkourTraversal.controller";
        private const string ScfClipFolder = "Assets/SCF/Animation";
        private const string StarterClipFolder = "Assets/StarterAssets/ThirdPersonController/Character/Animations";
        private const string StarterVisualName = "SCF_VisibleArmature";
        private const string DeprecatedParkourVisualName = "SCF_ParkourVisual";

        [MenuItem("SCF/Setup/Apply Traversal Camera And Animations")]
        public static void ApplyTraversalCameraAndAnimations()
        {
            ApplyTraversalCameraAndAnimations(true);
        }

        public static void ApplyTraversalCameraAndAnimations(bool saveScene)
        {
            GameObject player = GameObject.Find("SCF_Player");
            RemoveDeprecatedParkourVisual(player);

            player = SCFPrototypeSceneSetup.RepairPlayerVisualModel(false);
            RemoveDeprecatedParkourVisual(player);

            Animator animator = ResolvePlayerAnimator(player);
            AnimatorController controller = BuildTraversalController();

            if (animator != null)
            {
                Undo.RecordObject(animator, "Assign SCF basic traversal animator");
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                EditorUtility.SetDirty(animator);
            }

            RemoveRuntimePanels();
            ApplyStarterCamera(player != null ? player.transform : null);
            RebindAnimationComponents(player, animator);

            if (saveScene)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
            }

            Debug.Log("SCF basic movement rewire applied: Starter locomotion base with SCF jump, roll, and angled wall-run movement.");
        }

        private static Animator ResolvePlayerAnimator(GameObject player)
        {
            if (player == null)
            {
                return null;
            }

            Transform starterVisual = player.transform.Find(StarterVisualName);
            if (starterVisual != null)
            {
                Animator starterAnimator = starterVisual.GetComponentInChildren<Animator>(true);
                if (starterAnimator != null)
                {
                    return starterAnimator;
                }
            }

            Animator[] animators = player.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                Animator animator = animators[i];
                if (animator == null || IsUnderDeprecatedParkourVisual(animator.transform))
                {
                    continue;
                }

                return animator;
            }

            return null;
        }

        private static bool IsUnderDeprecatedParkourVisual(Transform target)
        {
            while (target != null)
            {
                if (target.name == DeprecatedParkourVisualName)
                {
                    return true;
                }

                target = target.parent;
            }

            return false;
        }

        private static void RemoveDeprecatedParkourVisual(GameObject player)
        {
            if (player == null)
            {
                return;
            }

            Transform visual = player.transform.Find(DeprecatedParkourVisualName);
            if (visual != null)
            {
                Undo.DestroyObjectImmediate(visual.gameObject);
            }
        }

        private static AnimatorController BuildTraversalController()
        {
            EnsureFolder(ControllerFolder);

            if (File.Exists(ControllerPath))
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.name = Path.GetFileNameWithoutExtension(ControllerPath);
            AddParameters(controller);
            EnableBaseLayerIk(controller);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState locomotion = stateMachine.AddState("Locomotion", new Vector3(260f, 20f, 0f));
            locomotion.motion = BuildLocomotionBlendTree(controller);
            locomotion.speedParameterActive = true;
            locomotion.speedParameter = "MotionSpeed";
            stateMachine.defaultState = locomotion;

            AnimatorState jump = AddClipState(stateMachine, "Jump", LoadScfClip("Jump") ?? LoadStarterClip("Jump--Jump.anim.fbx"), new Vector3(520f, -90f, 0f));
            AnimatorState roll = AddClipState(stateMachine, "Roll", LoadScfClip("Roll"), new Vector3(520f, 70f, 0f));

            if (jump.motion != null)
            {
                AddAnyTriggerTransition(stateMachine, jump, "JumpTrigger", 0.12f);
                AddBoolReturnTransition(jump, locomotion, "Jumping", false, 0.82f, 0.16f);
            }

            if (roll.motion != null)
            {
                AddAnyTriggerTransition(stateMachine, roll, "CombatRollTrigger", 0.08f);
                AddBoolReturnTransition(roll, locomotion, "CombatRoll", false, 0.72f, 0.12f);
            }

            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void EnableBaseLayerIk(AnimatorController controller)
        {
            if (controller == null || controller.layers == null || controller.layers.Length == 0)
            {
                return;
            }

            AnimatorControllerLayer[] layers = controller.layers;
            layers[0].iKPass = true;
            controller.layers = layers;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static void AddParameters(AnimatorController controller)
        {
            AddParameter(controller, "Speed", AnimatorControllerParameterType.Float);
            AddParameter(controller, "MoveX", AnimatorControllerParameterType.Float);
            AddParameter(controller, "MoveZ", AnimatorControllerParameterType.Float);
            AddParameter(controller, "AimX", AnimatorControllerParameterType.Float);
            AddParameter(controller, "AimZ", AnimatorControllerParameterType.Float);
            AddParameter(controller, "MotionSpeed", AnimatorControllerParameterType.Float);
            AddParameter(controller, "Grounded", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "FreeFall", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "Jump", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "IsMoving", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "Attack", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "CombatRoll", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "CombatRollTrigger", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "Jumping", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "JumpTrigger", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "LandTrigger", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "WallRun", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "WallRunSide", AnimatorControllerParameterType.Float);
            AddParameter(controller, "MobilityState", AnimatorControllerParameterType.Int);
        }

        private static void AddParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            controller.AddParameter(name, type);
        }

        private static BlendTree BuildLocomotionBlendTree(AnimatorController controller)
        {
            BlendTree tree = new BlendTree
            {
                name = "SCF Starter Locomotion",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false
            };

            AssetDatabase.AddObjectToAsset(tree, controller);
            AddBlendChild(tree, LoadStarterClip("Stand--Idle.anim.fbx"), 0f);
            AddBlendChild(tree, LoadStarterClip("Locomotion--Walk_N.anim.fbx"), 2.6f);
            AddBlendChild(tree, LoadStarterClip("Locomotion--Run_N.anim.fbx"), 5.6f);
            return tree;
        }

        private static void AddBlendChild(BlendTree tree, AnimationClip clip, float threshold)
        {
            if (clip != null)
            {
                tree.AddChild(clip, threshold);
            }
        }

        private static AnimatorState AddClipState(AnimatorStateMachine stateMachine, string stateName, AnimationClip clip, Vector3 position)
        {
            AnimatorState state = stateMachine.AddState(stateName, position);
            state.motion = clip;
            return state;
        }

        private static AnimationClip LoadScfClip(string clipName)
        {
            return LoadClipAtPath(ScfClipFolder + "/" + clipName + ".anim");
        }

        private static AnimationClip LoadStarterClip(string fileName)
        {
            return LoadClipAtPath(StarterClipFolder + "/" + fileName);
        }

        private static AnimationClip LoadClipAtPath(string path)
        {
            AnimationClip directClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (directClip != null)
            {
                return directClip;
            }

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip clip && !clip.name.StartsWith("__preview", System.StringComparison.OrdinalIgnoreCase))
                {
                    return clip;
                }
            }

            Debug.LogWarning("SCF traversal setup could not find animation clip at " + path);
            return null;
        }

        private static void AddAnyTriggerTransition(AnimatorStateMachine stateMachine, AnimatorState destination, string triggerName, float duration)
        {
            AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(destination);
            transition.canTransitionToSelf = false;
            transition.hasExitTime = false;
            transition.duration = duration;
            transition.interruptionSource = TransitionInterruptionSource.DestinationThenSource;
            transition.orderedInterruption = true;
            transition.AddCondition(AnimatorConditionMode.If, 0f, triggerName);
        }

        private static void AddBoolReturnTransition(AnimatorState source, AnimatorState destination, string parameterName, bool desired, float exitTime, float duration)
        {
            AnimatorStateTransition transition = source.AddTransition(destination);
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            transition.duration = duration;
            transition.interruptionSource = TransitionInterruptionSource.DestinationThenSource;
            transition.orderedInterruption = true;
            transition.AddCondition(desired ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameterName);
        }

        private static void RemoveRuntimePanels()
        {
            CameraTuningPanel[] cameraPanels = Object.FindObjectsByType<CameraTuningPanel>(FindObjectsInactive.Include);
            for (int i = 0; i < cameraPanels.Length; i++)
            {
                Undo.DestroyObjectImmediate(cameraPanels[i]);
            }

            ParkourAnimationPreviewPanel[] animationPanels = Object.FindObjectsByType<ParkourAnimationPreviewPanel>(FindObjectsInactive.Include);
            for (int i = 0; i < animationPanels.Length; i++)
            {
                Undo.DestroyObjectImmediate(animationPanels[i]);
            }
        }

        private static void ApplyStarterCamera(Transform target)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            Undo.RecordObject(camera, "Apply SCF starter camera");
            camera.fieldOfView = 42f;
            camera.orthographic = false;
            camera.orthographicSize = 7.25f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000f;

            IsometricCameraFollow follow = camera.GetComponent<IsometricCameraFollow>();
            if (follow != null)
            {
                Undo.RecordObject(follow, "Apply SCF starter camera follow");
                follow.ApplyTacticalShooterPreset();
                if (target != null)
                {
                    follow.Configure(target);
                }

                EditorUtility.SetDirty(follow);
            }

            EditorUtility.SetDirty(camera);
        }

        private static void RebindAnimationComponents(GameObject player, Animator animator)
        {
            if (player == null)
            {
                return;
            }

            IsometricPlayerInput playerInput = player.GetComponent<IsometricPlayerInput>();
            IsometricCharacterMotor motor = player.GetComponent<IsometricCharacterMotor>();
            Camera camera = Camera.main;

            if (motor != null)
            {
                Transform facingRoot = animator != null ? animator.transform : player.transform;
                motor.Configure(playerInput, camera, facingRoot);

                SerializedObject serializedMotor = new SerializedObject(motor);
                SetBool(serializedMotor, "scaleMovementWithTransform", false);
                SetEnum(serializedMotor, "traversalProfile", (int)SCFTraversalProfile.Standard);
                SetFloat(serializedMotor, "carriedLoad01", 0f);
                SetBool(serializedMotor, "separateAimFromLocomotion", true);
                SetBool(serializedMotor, "lowerBodyAimsWhenIdle", false);
                SetFloat(serializedMotor, "lowerBodyMoveThreshold", 0.2f);
                SetFloat(serializedMotor, "maxJumpChargePlanarBoost", 1.4f);
                SetBool(serializedMotor, "parkourWallJumpKeepsAirMobility", true);
                SetFloat(serializedMotor, "parkourAirJumpStrength", 0.75f);
                SetFloat(serializedMotor, "parkourAirRollSpeedBonus", 1.2f);
                SetFloat(serializedMotor, "wallRunSpeed", 7.4f);
                SetFloat(serializedMotor, "wallRunGravity", -0.45f);
                SetFloat(serializedMotor, "wallRunAnimationSpeed", 1f);
                SetFloat(serializedMotor, "wallRunVisualLeanVariance", 0f);
                SetInt(serializedMotor, "standardWallRunStepLimit", 3);
                SetFloat(serializedMotor, "standardWallRunStepDuration", 0.27f);
                SetFloat(serializedMotor, "standardWallRunHeightMultiplier", 0.82f);
                SetFloat(serializedMotor, "standardWallJumpUpwardMultiplier", 0.55f);
                SetFloat(serializedMotor, "standardWallJumpAwayMultiplier", 1f);
                SetFloat(serializedMotor, "standardWallRunRegrabCooldown", 0.55f);
                SetBool(serializedMotor, "standardWallRunRequiresReleaseBeforeRegrab", true);
                SetFloat(serializedMotor, "standardWallRunSlideOffDownSpeed", -1.6f);
                SetBool(serializedMotor, "enableParkourWallClimbUp", true);
                SetFloat(serializedMotor, "wallClimbUpApproachDot", 0.72f);
                SetFloat(serializedMotor, "wallClimbUpSpeed", 4.2f);
                SetFloat(serializedMotor, "wallClimbUpMaxDuration", 2.2f);
                SetFloat(serializedMotor, "wallClimbLedgeReachSlack", 0.12f);
                SetFloat(serializedMotor, "wallClimbTopForwardClearance", 0.55f);
                SetFloat(serializedMotor, "wallClimbTopProbeHeight", 0.75f);
                SetBool(serializedMotor, "enableParkourAutoTraversal", true);
                SetFloat(serializedMotor, "parkourAutoTraversalHeadHeightMultiplier", 1f);
                SetFloat(serializedMotor, "parkourAutoSlideMinHeightMultiplier", 0.68f);
                SetFloat(serializedMotor, "parkourSlideVaultDurationMultiplier", 0.9f);
                SetFloat(serializedMotor, "parkourSlideVaultRollSpeedBonus", 1.6f);
                SetFloat(serializedMotor, "parkourManualTraversalDurationMultiplier", 1.35f);
                SetFloat(serializedMotor, "parkourManualVaultMaxHeightMultiplier", 0.78f);
                SetFloat(serializedMotor, "standardVaultDurationMultiplier", 1.65f);
                SetFloat(serializedMotor, "standardClimbDurationMultiplier", 2.1f);
                serializedMotor.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(motor);
            }

            MovementAnimatorBridge bridge = player.GetComponent<MovementAnimatorBridge>();
            if (bridge != null)
            {
                bridge.Configure(animator);
                SerializedObject serializedBridge = new SerializedObject(bridge);
                SetFloat(serializedBridge, "damping", 0.1f);
                serializedBridge.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(bridge);
            }

            SCFAimBodyDifferentiator bodyDifferentiator = player.GetComponent<SCFAimBodyDifferentiator>();
            if (bodyDifferentiator == null)
            {
                bodyDifferentiator = Undo.AddComponent<SCFAimBodyDifferentiator>(player);
            }

            bodyDifferentiator.Configure(motor, animator);
            bodyDifferentiator.SetAimTorsoDuringWallRun(true);
            EditorUtility.SetDirty(bodyDifferentiator);

            SCFClimbHandContactIK climbHandIK = player.GetComponent<SCFClimbHandContactIK>();
            if (climbHandIK == null)
            {
                climbHandIK = Undo.AddComponent<SCFClimbHandContactIK>(player);
            }

            climbHandIK.Configure(motor, animator, true);
            EditorUtility.SetDirty(climbHandIK);

            MotionMatchingSignalHub signalHub = player.GetComponent<MotionMatchingSignalHub>();
            if (signalHub != null)
            {
                signalHub.Configure(animator);
                EditorUtility.SetDirty(signalHub);
            }

            SCFMotionSelector motionSelector = player.GetComponent<SCFMotionSelector>();
            if (motionSelector == null)
            {
                motionSelector = Undo.AddComponent<SCFMotionSelector>(player);
            }

            SCFMotionDatabase motionDatabase = SCFMotionDatabaseBaker.BuildBaseDatabase();
            SCFMotionSystemSetup.DisableLegacyMotionComponents(player);
            motionSelector.Configure(animator, motionDatabase);
            EditorUtility.SetDirty(motionSelector);

        }

        private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void SetInt(SerializedObject serializedObject, string propertyName, int value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
            }
        }

        private static void SetEnum(SerializedObject serializedObject, string propertyName, int value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.enumValueIndex = value;
            }
        }

        private static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }
    }
}
#endif
