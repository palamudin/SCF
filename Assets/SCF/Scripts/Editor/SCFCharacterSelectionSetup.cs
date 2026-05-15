#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using SCF.Gameplay;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SCF.EditorTools
{
    public static class SCFCharacterSelectionSetup
    {
        private const string ControllerFolder = "Assets/SCF/Animation";
        private const string ControllerPath = ControllerFolder + "/SCF_MxMBaseTraversal.controller";
        private const string ParkourPlayerAnimationFolder = "Assets/RedNotRed/3D Adaptive Parkour System/Animations/Player";

        [MenuItem("SCF/Setup/Add Character Selection Panel")]
        public static void ApplyCharacterSelectionSetup()
        {
            ApplyCharacterSelectionSetup(true);
        }

        public static void ApplyCharacterSelectionSetup(bool saveScene)
        {
            GameObject player = GameObject.Find("SCF_Player");
            if (player == null)
            {
                player = new GameObject("SCF_Player");
                Undo.RegisterCreatedObjectUndo(player, "Create SCF player");
                player.transform.position = Vector3.up * 0.05f;
            }

            RuntimeAnimatorController controller = BuildBaseTraversalController();
            SCFMotionDatabase motionDatabase = SCFMotionDatabaseBaker.BuildBaseDatabase();
            SCFMotionDatabase humanoidMotionDatabase = SCFMotionDatabaseBaker.BuildHumanoidDatabase();
            SCFCharacterCandidate[] candidates = FindCharacterCandidates();

            ConfigurePlayerStack(player, controller, motionDatabase, humanoidMotionDatabase, candidates);

            if (saveScene)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
            }

            Selection.activeGameObject = player;
            Debug.Log("SCF character selection panel is ready. Enter Play Mode and press F8 to toggle it.");
        }

        private static void ConfigurePlayerStack(
            GameObject player,
            RuntimeAnimatorController controller,
            SCFMotionDatabase motionDatabase,
            SCFMotionDatabase humanoidMotionDatabase,
            SCFCharacterCandidate[] candidates)
        {
            CharacterController characterController = EnsureComponent<CharacterController>(player);
            ConfigureCharacterController(characterController);

            IsometricPlayerInput input = EnsureComponent<IsometricPlayerInput>(player);
            EnsureComponent<CursorLockGuard>(player);
            IsometricCharacterMotor motor = EnsureComponent<IsometricCharacterMotor>(player);
            ConfigureMotorDefaults(motor);
            MovementAnimatorBridge bridge = EnsureComponent<MovementAnimatorBridge>(player);
            SCFAimBodyDifferentiator bodyDifferentiator = EnsureComponent<SCFAimBodyDifferentiator>(player);
            SCFClimbHandContactIK climbHandIK = EnsureComponent<SCFClimbHandContactIK>(player);
            SCFWeaponVisualSlot weaponSlot = EnsureComponent<SCFWeaponVisualSlot>(player);
            MotionMatchingSignalHub signalHub = EnsureComponent<MotionMatchingSignalHub>(player);
            SCFMotionSelector motionSelector = EnsureComponent<SCFMotionSelector>(player);
            SCFCharacterVisualSlot visualSlot = EnsureComponent<SCFCharacterVisualSlot>(player);
            SCFCharacterSelectionPanel panel = EnsureComponent<SCFCharacterSelectionPanel>(player);
            SCFMotionSystemSetup.DisableLegacyMotionComponents(player);

            ConfigureVisualSlotDefaults(visualSlot);
            visualSlot.Configure(controller, motionDatabase, humanoidMotionDatabase);
            visualSlot.ClearCharacter();
            panel.Configure(visualSlot, candidates);

            Camera camera = EnsureCamera(player.transform);
            Animator animator = visualSlot.ActiveAnimator != null ? visualSlot.ActiveAnimator : player.GetComponentInChildren<Animator>(true);
            Transform facingRoot = animator != null ? animator.transform : player.transform;

            motor.Configure(input, camera, facingRoot);
            bridge.Configure(animator);
            bodyDifferentiator.Configure(motor, animator);
            bodyDifferentiator.SetAimTorsoDuringWallRun(true);
            climbHandIK.Configure(motor, animator, true);
            weaponSlot.Configure(motor, animator, visualSlot.ActiveCharacterName);
            signalHub.Configure(animator);
            motionSelector.Configure(animator, motionDatabase);

            MarkDirty(player, characterController, input, motor, bridge, bodyDifferentiator, climbHandIK, weaponSlot, signalHub, motionSelector, visualSlot, panel);
        }

        private static void ConfigureVisualSlotDefaults(SCFCharacterVisualSlot visualSlot)
        {
            if (visualSlot == null)
            {
                return;
            }

            SerializedObject serializedSlot = new SerializedObject(visualSlot);
            SetBool(serializedSlot, "fitVisualToController", false);
            serializedSlot.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(visualSlot);
        }

        private static void ConfigureMotorDefaults(IsometricCharacterMotor motor)
        {
            if (motor == null)
            {
                return;
            }

            SerializedObject serializedMotor = new SerializedObject(motor);
            SetBool(serializedMotor, "enableVault", true);
            SetBool(serializedMotor, "enableProne", false);
            SetEnum(serializedMotor, "traversalProfile", (int)SCFTraversalProfile.Standard);
            SetFloat(serializedMotor, "carriedLoad01", 0f);
            SetFloat(serializedMotor, "parkourHopDuration", 0.13f);
            SetFloat(serializedMotor, "parkourHopSpeed", 6.2f);
            SetFloat(serializedMotor, "minJumpHeight", 0.75f);
            SetFloat(serializedMotor, "maxJumpHeight", 2.2f);
            SetFloat(serializedMotor, "maxJumpChargePlanarBoost", 1.4f);
            SetBool(serializedMotor, "parkourWallJumpKeepsAirMobility", true);
            SetFloat(serializedMotor, "parkourAirJumpStrength", 0.75f);
            SetFloat(serializedMotor, "parkourAirRollSpeedBonus", 1.2f);
            SetBool(serializedMotor, "separateAimFromLocomotion", true);
            SetBool(serializedMotor, "lowerBodyAimsWhenIdle", false);
            SetFloat(serializedMotor, "lowerBodyMoveThreshold", 0.2f);
            SetInt(serializedMotor, "standardWallRunStepLimit", 3);
            SetFloat(serializedMotor, "standardWallRunStepDuration", 0.27f);
            SetFloat(serializedMotor, "standardWallRunHeightMultiplier", 0.82f);
            SetFloat(serializedMotor, "heavyWallRunHeightMultiplier", 0.68f);
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
            SetFloat(serializedMotor, "standardVaultDurationMultiplier", 1.65f);
            SetFloat(serializedMotor, "standardClimbDurationMultiplier", 2.1f);
            SetFloat(serializedMotor, "standardClimbMaxHeight", 1.55f);
            SetFloat(serializedMotor, "heavyClimbMaxHeight", 1.18f);
            SetFloat(serializedMotor, "loadTraversalDurationMultiplier", 1.45f);
            SetFloat(serializedMotor, "climbMinHeight", 1.35f);
            SetFloat(serializedMotor, "climbMaxHeight", 2.05f);
            SetFloat(serializedMotor, "climbForwardClearance", 0.45f);
            SetFloat(serializedMotor, "climbDuration", 0.78f);
            SetFloat(serializedMotor, "climbArcHeight", 0.2f);
            SetBool(serializedMotor, "enableParkourAutoTraversal", true);
            SetFloat(serializedMotor, "parkourAutoTraversalHeadHeightMultiplier", 1f);
            SetFloat(serializedMotor, "parkourAutoSlideMinHeightMultiplier", 0.68f);
            SetFloat(serializedMotor, "parkourSlideVaultDurationMultiplier", 0.9f);
            SetFloat(serializedMotor, "parkourSlideVaultRollSpeedBonus", 1.6f);
            SetFloat(serializedMotor, "parkourManualTraversalDurationMultiplier", 1.35f);
            SetFloat(serializedMotor, "parkourManualVaultMaxHeightMultiplier", 0.78f);
            serializedMotor.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(motor);
        }

        private static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
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

        private static RuntimeAnimatorController BuildBaseTraversalController()
        {
            EnsureFolder(ControllerFolder);
            if (File.Exists(ControllerPath))
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AddParameters(controller);
            EnableBaseLayerIk(controller);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState locomotion = stateMachine.AddState("MxM Locomotion", new Vector3(250f, 0f, 0f));
            locomotion.motion = BuildLocomotionBlendTree(controller);
            stateMachine.defaultState = locomotion;

            AnimatorState jump = AddClipState(stateMachine, "Jump", FindFirstClip(
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/EquipedAnimations/Jump/jump start.FBX",
                ParkourPlayerAnimationFolder + "/Jump.anim",
                ParkourPlayerAnimationFolder + "/HopUp.anim",
                "Assets/SCF/Animation/Jump.anim",
                "Assets/StarterAssets/ThirdPersonController/Character/Animations/Jump--Jump.anim.fbx",
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/FreehandsAnimations/Jump/jump.fbx"), new Vector3(520f, -80f, 0f));

            AnimatorState roll = AddClipState(stateMachine, "Combat Roll", FindFirstClip(
                ParkourPlayerAnimationFolder + "/Roll.anim",
                "Assets/SCF/Animation/Roll.anim",
                ParkourPlayerAnimationFolder + "/Vault.anim",
                "Assets/SCF/Animation/Vault.anim"), new Vector3(520f, 90f, 0f));

            if (jump.motion != null)
            {
                AddAnyTriggerTransition(stateMachine, jump, "JumpTrigger", 0.1f);
                AddBoolReturnTransition(jump, locomotion, "Jumping", false, 0.82f, 0.14f);
            }

            if (roll.motion != null)
            {
                AddAnyTriggerTransition(stateMachine, roll, "CombatRollTrigger", 0.06f);
                AddBoolReturnTransition(roll, locomotion, "CombatRoll", false, 0.72f, 0.1f);
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

        private static BlendTree BuildLocomotionBlendTree(AnimatorController controller)
        {
            BlendTree tree = new BlendTree
            {
                name = "SCF MxM Base Locomotion",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false
            };

            AssetDatabase.AddObjectToAsset(tree, controller);
            AddBlendChild(tree, FindFirstClip(
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/EquipedAnimations/Idle/idle aiming.fbx",
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/EquipedAnimations/Idle/idle.fbx",
                ParkourPlayerAnimationFolder + "/Idle.anim",
                "Assets/StarterAssets/ThirdPersonController/Character/Animations/Stand--Idle.anim.fbx",
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/FreehandsAnimations/Idle/Idle.fbx"), 0f);
            AddBlendChild(tree, FindFirstClip(
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/EquipedAnimations/Walk/walk.fbx",
                ParkourPlayerAnimationFolder + "/Run.anim",
                "Assets/StarterAssets/ThirdPersonController/Character/Animations/Locomotion--Walk_N.anim.fbx",
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/FreehandsAnimations/Walk/walk.fbx"), 2.6f);
            AddBlendChild(tree, FindFirstClip(
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/EquipedAnimations/Run/run.fbx",
                ParkourPlayerAnimationFolder + "/Run.anim",
                "Assets/StarterAssets/ThirdPersonController/Character/Animations/Locomotion--Run_N.anim.fbx",
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/FreehandsAnimations/Run/run.fbx"), 5.8f);
            return tree;
        }

        private static SCFCharacterCandidate[] FindCharacterCandidates()
        {
            List<SCFCharacterCandidate> candidates = new List<SCFCharacterCandidate>();
            AddPrefabCandidates(candidates, "Assets/Fighters_(Pack)/Prefabs");
            AddCandidate(candidates, "TPS Soldier", "Assets/TPS Shooter (Military style)/Models/Soldiers/Player/Soldier.fbx");
            AddCandidate(candidates, "Parkour Frank", "Assets/RedNotRed/3D Adaptive Parkour System/Player/Frank.prefab", Vector3.zero, Vector3.zero, Vector3.one * 15f);
            return candidates.ToArray();
        }

        private static void AddPrefabCandidates(List<SCFCharacterCandidate> candidates, string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
            System.Array.Sort(guids, (left, right) => string.CompareOrdinal(AssetDatabase.GUIDToAssetPath(left), AssetDatabase.GUIDToAssetPath(right)));
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                AddCandidate(candidates, Path.GetFileNameWithoutExtension(path), path);
            }
        }

        private static void AddCandidate(List<SCFCharacterCandidate> candidates, string displayName, string path)
        {
            AddCandidate(candidates, displayName, path, Vector3.zero, Vector3.zero, Vector3.one);
        }

        private static void AddCandidate(List<SCFCharacterCandidate> candidates, string displayName, string path, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
        {
            if (IsExcludedCharacterCandidate(displayName, path))
            {
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || !HasCharacterVisual(prefab))
            {
                return;
            }

            candidates.Add(new SCFCharacterCandidate(displayName, prefab, localPosition, localEulerAngles, localScale));
        }

        private static bool IsExcludedCharacterCandidate(string displayName, string path)
        {
            return IsNamedFighter(displayName, 8)
                   || IsNamedFighter(displayName, 9)
                   || IsNamedFighter(displayName, 10)
                   || IsNamedFighter(Path.GetFileNameWithoutExtension(path), 8)
                   || IsNamedFighter(Path.GetFileNameWithoutExtension(path), 9)
                   || IsNamedFighter(Path.GetFileNameWithoutExtension(path), 10);
        }

        private static bool IsNamedFighter(string value, int number)
        {
            return string.Equals(value, "Fighter_" + number.ToString("00"), System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasCharacterVisual(GameObject prefab)
        {
            return prefab.GetComponentInChildren<SkinnedMeshRenderer>(true) != null
                   || prefab.GetComponentInChildren<Animator>(true) != null;
        }

        private static AnimationClip FindFirstClip(params string[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                AnimationClip clip = LoadClip(paths[i]);
                if (clip != null)
                {
                    return clip;
                }
            }

            Debug.LogWarning("SCF base traversal controller could not find a clip from: " + string.Join(", ", paths));
            return null;
        }

        private static AnimationClip LoadClip(string path)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null)
            {
                return clip;
            }

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip candidate && !candidate.name.StartsWith("__preview", System.StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            return FindClipByName(path);
        }

        private static AnimationClip FindClipByName(string path)
        {
            string clipName = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrEmpty(clipName))
            {
                return null;
            }

            string[] guids = AssetDatabase.FindAssets(clipName + " t:AnimationClip", new[] { ParkourPlayerAnimationFolder, ControllerFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!string.Equals(Path.GetFileNameWithoutExtension(assetPath), clipName, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
                if (clip != null)
                {
                    return clip;
                }
            }

            return null;
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
            AddParameter(controller, "JumpCharging", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "Jumping", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "JumpTrigger", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "LandTrigger", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "Prone", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "WallRun", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "WallRunTrigger", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "WallRunSide", AnimatorControllerParameterType.Float);
            AddParameter(controller, "Vault", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "VaultTrigger", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "Climb", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "ClimbTrigger", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "JumpCharge01", AnimatorControllerParameterType.Float);
            AddParameter(controller, "MobilityState", AnimatorControllerParameterType.Int);
            AddParameter(controller, "MxMRequiredTags", AnimatorControllerParameterType.Int);
        }

        private static void AddParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            controller.AddParameter(name, type);
        }

        private static AnimatorState AddClipState(AnimatorStateMachine stateMachine, string stateName, AnimationClip clip, Vector3 position)
        {
            AnimatorState state = stateMachine.AddState(stateName, position);
            state.motion = clip;
            return state;
        }

        private static void AddBlendChild(BlendTree tree, AnimationClip clip, float threshold)
        {
            if (clip != null)
            {
                tree.AddChild(clip, threshold);
            }
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

        private static Camera EnsureCamera(Transform target)
        {
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

            follow.Configure(target);
            return camera;
        }

        private static void ConfigureCharacterController(CharacterController controller)
        {
            Undo.RecordObject(controller, "Configure SCF character controller");
            controller.height = 1.85f;
            controller.radius = 0.34f;
            controller.center = new Vector3(0f, 0.92f, 0f);
            controller.slopeLimit = 55f;
            controller.stepOffset = 0.35f;
            controller.skinWidth = 0.04f;
            EditorUtility.SetDirty(controller);
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            return Undo.AddComponent<T>(gameObject);
        }

        private static void MarkDirty(params Object[] objects)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null)
                {
                    EditorUtility.SetDirty(objects[i]);
                }
            }
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
    }
}
#endif
