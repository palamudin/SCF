using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SCF.Gameplay
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(20)]
    [RequireComponent(typeof(IsometricCharacterMotor))]
    public sealed class SCFMotionSelector : MonoBehaviour
    {
        private const string TpsEquippedWalkPath = "Assets/TPS Shooter (Military style)/Animations/Humanoid/EquipedAnimations/Walk/walk.fbx";
        private const string TpsEquippedWalkForwardClipName = "walk_fwd";

        [Header("References")]
        [SerializeField] private IsometricCharacterMotor motor;
        [SerializeField] private Animator animator;
        [SerializeField] private SCFMotionDatabase database;
        [SerializeField] private SCFWeaponVisualSlot weaponVisualSlot;

        [Header("Selection")]
        [SerializeField, Min(0.01f)] private float searchInterval = 0.05f;
        [SerializeField, Min(0f)] private float idleSpeedThreshold = 0.15f;
        [SerializeField, Min(0f)] private float currentClipBias = 0.08f;
        [SerializeField] private bool preferDirectionalLocomotionWhileAiming = true;
        [SerializeField] private bool useDedicatedAimLocomotion = true;
        [SerializeField] private bool useAimFrameForAimLocomotion = true;

        [Header("Blending")]
        [SerializeField, Min(0.01f)] private float locomotionBlendTime = 0.18f;
        [SerializeField, Min(0.01f)] private float actionBlendTime = 0.08f;
        [SerializeField] private bool applyFootIK = true;
        [SerializeField] private bool syncActionsToMotorTime = true;
        [SerializeField] private bool drawDebugTrajectory = true;

        [Header("Weapon Upper Body")]
        [Tooltip("Only use with SCF-authored clips. Imported reference packs stay as reference data, not runtime cycles.")]
        [SerializeField] private bool enableWeaponUpperBodyLayer = true;
        [SerializeField] private bool autoLoadTpsArmedForwardUpperBody = true;
        [SerializeField] private AnimationClip weaponCarryPoseClip;
        [SerializeField] private AnimationClip weaponAimPoseClip;
        [SerializeField] private AnimationClip weaponMoveClip;
        [SerializeField] private AnimationClip weaponUpperBodyClip;
        [SerializeField] private bool useWalkUpperBodyForAimRun = true;
        [SerializeField] private AnimationClip aimRunUpperBodyClip;
        [SerializeField, Range(0f, 1f)] private float aimRunUpperBodyWeight = 1f;
        [SerializeField, Min(0.01f)] private float aimRunUpperBodyPlaybackSpeed = 1f;
        [SerializeField, Range(0f, 1f)] private float weaponCarryUpperBodyWeight = 0.62f;
        [SerializeField, Range(0f, 1f)] private float weaponAimUpperBodyWeight = 0.92f;
        [SerializeField, Min(0.01f)] private float weaponUpperBodyPlaybackSpeed = 1f;
        [SerializeField, Min(0.1f)] private float weaponUpperBodyBlendSharpness = 12f;
        [SerializeField] private bool freezeWeaponUpperBodyWhenIdle = true;
        [SerializeField, Range(0f, 1f)] private float weaponUpperBodyIdlePoseTime = 0.08f;
        [SerializeField] private bool suppressWeaponLayerDuringTraversal = true;

        [Header("Transient Windows")]
        [SerializeField, Range(0f, 1f)] private float jumpClipStart = 0f;
        [SerializeField, Range(0f, 1f)] private float jumpClipEnd = 1f;
        [SerializeField, Range(0f, 1f)] private float combatRollClipStart = 0f;
        [SerializeField, Range(0f, 1f)] private float combatRollClipEnd = 1f;
        [SerializeField, Range(0f, 1f)] private float vaultClipStart = 0f;
        [SerializeField, Range(0f, 1f)] private float vaultClipEnd = 1f;
        [SerializeField, Range(0f, 1f)] private float climbClipStart = 0f;
        [SerializeField, Range(0f, 1f)] private float climbClipEnd = 1f;
        [SerializeField, Range(0f, 1f)] private float wallRunLoopStart = 0f;
        [SerializeField, Range(0f, 1f)] private float wallRunLoopEnd = 1f;

        [Header("Status")]
        [SerializeField] private int selectedMotionIndex = -1;
        [SerializeField] private string selectedMotionName;
        [SerializeField] private float lastSelectionCost;
        [SerializeField, Range(0f, 1f)] private float currentWeaponUpperBodyWeight;

        private readonly Playable[] slotPlayables = new Playable[2];
        private readonly int[] slotMotionIndices = { -1, -1 };
        private PlayableGraph graph;
        private AnimationMixerPlayable mixer;
        private AnimationLayerMixerPlayable layerMixer;
        private AnimationClipPlayable weaponUpperBodyPlayable;
        private AnimationClip activeWeaponUpperBodyClip;
        private AvatarMask weaponUpperBodyMask;
        private int activeSlot = -1;
        private int fadingSlot = -1;
        private float fadeTime;
        private float fadeDuration;
        private float searchTimer;
        private int lastCombatRollSequence = -1;
        private int lastJumpSequence = -1;
        private int lastWallRunSequence = -1;
        private int lastVaultSequence = -1;
        private int lastClimbSequence = -1;

        public SCFMotionDatabase Database => database;
        public string SelectedMotionName => selectedMotionName;
        public int SelectedMotionIndex => selectedMotionIndex;
        public float LastSelectionCost => lastSelectionCost;

        private void Reset()
        {
            motor = GetComponent<IsometricCharacterMotor>();
            animator = GetComponentInChildren<Animator>(true);
        }

        private void Awake()
        {
            ResolveReferences();
            CaptureActionSequences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            CaptureActionSequences();
        }

        private void OnDisable()
        {
            DestroyGraph();
        }

        private void Update()
        {
            if (!EnsureGraph())
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            bool forceRestart = ConsumeActionSequenceChange();
            bool wantsImmediateSearch = selectedMotionIndex < 0 || forceRestart || IsActionStateActive();
            searchTimer -= deltaTime;

            if (wantsImmediateSearch || searchTimer <= 0f)
            {
                int desiredMotion = ChooseDesiredMotion(out float cost);
                lastSelectionCost = cost;
                if (desiredMotion >= 0 && (desiredMotion != selectedMotionIndex || forceRestart))
                {
                    SwitchToMotion(desiredMotion, ResolveBlendTime(desiredMotion));
                }

                searchTimer = searchInterval;
            }

            SyncActiveMotionSpeed();
            TickClipPlayback();
            TickWeaponUpperBodyLayer(deltaTime);
            TickFade(deltaTime);
        }

        public void Configure(Animator targetAnimator)
        {
            animator = targetAnimator != null ? targetAnimator : animator;
            ResolveReferences();
            if (Application.isPlaying)
            {
                RebuildGraph();
            }
        }

        public void Configure(Animator targetAnimator, SCFMotionDatabase targetDatabase)
        {
            database = targetDatabase;
            Configure(targetAnimator);
        }

        public void ConfigureDatabase(SCFMotionDatabase targetDatabase)
        {
            database = targetDatabase;
            if (Application.isPlaying)
            {
                RebuildGraph();
            }
        }

        private void ResolveReferences()
        {
            if (motor == null)
            {
                motor = GetComponent<IsometricCharacterMotor>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (weaponVisualSlot == null)
            {
                weaponVisualSlot = GetComponent<SCFWeaponVisualSlot>();
            }

#if UNITY_EDITOR
            ResolveDefaultWeaponUpperBodyClips();
#endif
        }

#if UNITY_EDITOR
        private void ResolveDefaultWeaponUpperBodyClips()
        {
            if (!autoLoadTpsArmedForwardUpperBody || HasAllDefaultWeaponUpperBodyClips())
            {
                return;
            }

            AnimationClip forwardArmedWalk = FindClipInAsset(TpsEquippedWalkPath, TpsEquippedWalkForwardClipName);
            if (forwardArmedWalk == null)
            {
                return;
            }

            if (weaponCarryPoseClip == null)
            {
                weaponCarryPoseClip = forwardArmedWalk;
            }

            if (weaponAimPoseClip == null)
            {
                weaponAimPoseClip = forwardArmedWalk;
            }

            if (weaponMoveClip == null)
            {
                weaponMoveClip = forwardArmedWalk;
            }

            if (weaponUpperBodyClip == null)
            {
                weaponUpperBodyClip = forwardArmedWalk;
            }

            if (aimRunUpperBodyClip == null)
            {
                aimRunUpperBodyClip = forwardArmedWalk;
            }

            enableWeaponUpperBodyLayer = true;
        }

        private static AnimationClip FindClipInAsset(string assetPath, string clipName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip clip
                    && !clip.name.StartsWith("__preview", System.StringComparison.OrdinalIgnoreCase)
                    && ClipNameMatches(clip.name, clipName))
                {
                    return clip;
                }
            }

            return null;
        }

        private static bool ClipNameMatches(string actualName, string requestedName)
        {
            if (string.Equals(actualName, requestedName, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            int pipeIndex = actualName.LastIndexOf('|');
            if (pipeIndex >= 0 && pipeIndex < actualName.Length - 1)
            {
                return string.Equals(actualName.Substring(pipeIndex + 1), requestedName, System.StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
#endif

        private bool HasAllDefaultWeaponUpperBodyClips()
        {
            return weaponCarryPoseClip != null
                   && weaponAimPoseClip != null
                   && weaponMoveClip != null
                   && weaponUpperBodyClip != null
                   && aimRunUpperBodyClip != null;
        }

        private bool EnsureGraph()
        {
            ResolveReferences();
            if (database == null || animator == null)
            {
                return false;
            }

            if (graph.IsValid())
            {
                return true;
            }

            graph = PlayableGraph.Create("SCF Motion Selector");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            mixer = AnimationMixerPlayable.Create(graph, 2);
            layerMixer = AnimationLayerMixerPlayable.Create(graph, 2);
            graph.Connect(mixer, 0, layerMixer, 0);
            layerMixer.SetInputWeight(0, 1f);
            layerMixer.SetInputWeight(1, 0f);
            EnsureWeaponUpperBodyLayer();

            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "SCF Motion Output", animator);
            output.SetSourcePlayable(layerMixer);
            graph.Play();

            activeSlot = -1;
            fadingSlot = -1;
            selectedMotionIndex = -1;
            selectedMotionName = string.Empty;
            slotMotionIndices[0] = -1;
            slotMotionIndices[1] = -1;

            return true;
        }

        private void RebuildGraph()
        {
            DestroyGraph();
            CaptureActionSequences();
            searchTimer = 0f;
            EnsureGraph();
        }

        private void DestroyGraph()
        {
            if (graph.IsValid())
            {
                graph.Destroy();
            }

            slotPlayables[0] = Playable.Null;
            slotPlayables[1] = Playable.Null;
            layerMixer = default;
            weaponUpperBodyPlayable = default;
            activeWeaponUpperBodyClip = null;
            slotMotionIndices[0] = -1;
            slotMotionIndices[1] = -1;
            activeSlot = -1;
            fadingSlot = -1;
            selectedMotionIndex = -1;
            selectedMotionName = string.Empty;
            currentWeaponUpperBodyWeight = 0f;
        }

        private void EnsureWeaponUpperBodyLayer(AnimationClip desiredClip = null)
        {
            if (!graph.IsValid() || !layerMixer.IsValid())
            {
                return;
            }

            desiredClip = desiredClip != null ? desiredClip : ResolveDesiredWeaponUpperBodyClip();
            if (!enableWeaponUpperBodyLayer || desiredClip == null)
            {
                layerMixer.SetInputWeight(1, 0f);
                return;
            }

            if (weaponUpperBodyPlayable.IsValid() && activeWeaponUpperBodyClip == desiredClip)
            {
                return;
            }

            ClearWeaponUpperBodyPlayable();

            activeWeaponUpperBodyClip = desiredClip;
            weaponUpperBodyPlayable = AnimationClipPlayable.Create(graph, activeWeaponUpperBodyClip);
            weaponUpperBodyPlayable.SetApplyFootIK(false);
            weaponUpperBodyPlayable.SetApplyPlayableIK(false);
            weaponUpperBodyPlayable.SetTime(0d);
            weaponUpperBodyPlayable.SetSpeed(0d);

            graph.Connect(weaponUpperBodyPlayable, 0, layerMixer, 1);
            layerMixer.SetLayerMaskFromAvatarMask(1u, ResolveWeaponUpperBodyMask());
            layerMixer.SetLayerAdditive(1u, false);
            layerMixer.SetInputWeight(1, 0f);
        }

        private void TickWeaponUpperBodyLayer(float deltaTime)
        {
            if (!layerMixer.IsValid())
            {
                return;
            }

            float targetWeight = ResolveWeaponUpperBodyTargetWeight();
            AnimationClip desiredClip = targetWeight > 0.001f ? ResolveDesiredWeaponUpperBodyClip() : activeWeaponUpperBodyClip;
            if (targetWeight > 0.001f && desiredClip != null)
            {
                EnsureWeaponUpperBodyLayer(desiredClip);
            }

            float blend = 1f - Mathf.Exp(-weaponUpperBodyBlendSharpness * deltaTime);
            currentWeaponUpperBodyWeight = Mathf.Lerp(currentWeaponUpperBodyWeight, targetWeight, blend);

            if (!weaponUpperBodyPlayable.IsValid() || activeWeaponUpperBodyClip == null)
            {
                currentWeaponUpperBodyWeight = 0f;
                layerMixer.SetInputWeight(1, 0f);
                return;
            }

            if (ShouldPlayActiveWeaponUpperBodyClip()
                && currentWeaponUpperBodyWeight > 0.001f
                && ShouldPlayWeaponUpperBodyMotion())
            {
                weaponUpperBodyPlayable.SetSpeed(ResolveActiveWeaponUpperBodyPlaybackSpeed());
            }
            else
            {
                weaponUpperBodyPlayable.SetSpeed(0d);
                weaponUpperBodyPlayable.SetTime(Mathf.Clamp01(weaponUpperBodyIdlePoseTime) * Mathf.Max(0.01f, activeWeaponUpperBodyClip.length));
            }

            WrapWeaponUpperBodyClip();
            layerMixer.SetInputWeight(0, 1f);
            layerMixer.SetInputWeight(1, currentWeaponUpperBodyWeight);
        }

        private float ResolveWeaponUpperBodyTargetWeight()
        {
            if (!enableWeaponUpperBodyLayer
                || !HasAnyWeaponUpperBodyClip()
                || weaponVisualSlot == null
                || !weaponVisualSlot.HasActiveWeapon
                || animator == null
                || animator.avatar == null
                || !animator.avatar.isHuman
                || !CanApplyWeaponUpperBodyLayer())
            {
                return 0f;
            }

            if (IsAimRunUpperBodySplitActive())
            {
                return aimRunUpperBodyWeight;
            }

            return Mathf.Lerp(weaponCarryUpperBodyWeight, weaponAimUpperBodyWeight, weaponVisualSlot.Raised01);
        }

        private AnimationClip ResolveDesiredWeaponUpperBodyClip()
        {
            if (IsAimRunUpperBodySplitActive())
            {
                return FirstAvailableClip(aimRunUpperBodyClip, weaponMoveClip, weaponAimPoseClip, weaponUpperBodyClip, weaponCarryPoseClip);
            }

            bool moving = ShouldPlayWeaponUpperBodyMotion();
            if (moving && weaponMoveClip != null)
            {
                return weaponMoveClip;
            }

            if (weaponVisualSlot != null && weaponVisualSlot.Raised01 > 0.35f && weaponAimPoseClip != null)
            {
                return weaponAimPoseClip;
            }

            return FirstAvailableClip(weaponCarryPoseClip, weaponAimPoseClip, weaponMoveClip, weaponUpperBodyClip);
        }

        private bool IsAimRunUpperBodySplitActive()
        {
            return useWalkUpperBodyForAimRun
                   && motor != null
                   && motor.AimHeld
                   && motor.RunHeld
                   && CanApplyWeaponUpperBodyLayer();
        }

        private bool ShouldPlayActiveWeaponUpperBodyClip()
        {
            return activeWeaponUpperBodyClip != null
                   && (activeWeaponUpperBodyClip == weaponMoveClip
                       || (IsAimRunUpperBodySplitActive() && activeWeaponUpperBodyClip == aimRunUpperBodyClip));
        }

        private float ResolveActiveWeaponUpperBodyPlaybackSpeed()
        {
            return IsAimRunUpperBodySplitActive() && activeWeaponUpperBodyClip == aimRunUpperBodyClip
                ? aimRunUpperBodyPlaybackSpeed
                : weaponUpperBodyPlaybackSpeed;
        }

        private bool HasAnyWeaponUpperBodyClip()
        {
            return weaponCarryPoseClip != null
                   || weaponAimPoseClip != null
                   || weaponMoveClip != null
                   || weaponUpperBodyClip != null
                   || aimRunUpperBodyClip != null;
        }

        private bool CanApplyWeaponUpperBodyLayer()
        {
            if (!suppressWeaponLayerDuringTraversal || motor == null)
            {
                return true;
            }

            return !motor.IsCombatRolling
                   && !motor.IsVaulting
                   && !motor.IsClimbing
                   && !motor.IsWallRunning;
        }

        private bool ShouldPlayWeaponUpperBodyMotion()
        {
            if (!freezeWeaponUpperBodyWhenIdle || motor == null)
            {
                return true;
            }

            return motor.PlanarVelocity.magnitude > idleSpeedThreshold;
        }

        private void WrapWeaponUpperBodyClip()
        {
            double duration = Mathf.Max(0.01f, activeWeaponUpperBodyClip != null ? activeWeaponUpperBodyClip.length : 0.01f);
            double time = weaponUpperBodyPlayable.GetTime();
            if (time >= duration || time < 0d)
            {
                weaponUpperBodyPlayable.SetTime(time - System.Math.Floor(time / duration) * duration);
            }
        }

        private void ClearWeaponUpperBodyPlayable()
        {
            if (layerMixer.IsValid())
            {
                Playable input = layerMixer.GetInput(1);
                if (input.IsValid())
                {
                    layerMixer.DisconnectInput(1);
                }

                layerMixer.SetInputWeight(1, 0f);
            }

            if (weaponUpperBodyPlayable.IsValid())
            {
                weaponUpperBodyPlayable.Destroy();
            }

            weaponUpperBodyPlayable = default;
            activeWeaponUpperBodyClip = null;
        }

        private AvatarMask ResolveWeaponUpperBodyMask()
        {
            if (weaponUpperBodyMask != null)
            {
                return weaponUpperBodyMask;
            }

            weaponUpperBodyMask = new AvatarMask();
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            {
                weaponUpperBodyMask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
            }

            weaponUpperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            weaponUpperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
            weaponUpperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            weaponUpperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            weaponUpperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            weaponUpperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            weaponUpperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK, true);
            weaponUpperBodyMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK, true);
            return weaponUpperBodyMask;
        }

        private static AnimationClip FirstAvailableClip(params AnimationClip[] clips)
        {
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                {
                    return clips[i];
                }
            }

            return null;
        }

        private int ChooseDesiredMotion(out float cost)
        {
            cost = 0f;
            if (database == null || database.Count == 0)
            {
                return -1;
            }

            if (motor != null)
            {
                if (motor.IsCombatRolling)
                {
                    return ChooseActionMotion(SCFMotionType.CombatRoll, ResolveLocalMotionDirection(), out cost);
                }

                if (motor.IsWallRunning)
                {
                    return ChooseWallRunLocomotion(out cost);
                }

                if (motor.IsVaulting)
                {
                    return ChooseVaultMotion(out cost);
                }

                if (motor.IsClimbing)
                {
                    return ChooseActionMotion(SCFMotionType.Climb, ResolveLocalMotionDirection(), out cost);
                }

                if (motor.IsJumping || motor.IsFalling)
                {
                    return ChooseAirborneMotion(out cost);
                }

                float speed = motor.PlanarVelocity.magnitude;
                if (speed <= idleSpeedThreshold)
                {
                    int idle = database.FindFirst(SCFMotionType.Idle);
                    if (idle >= 0)
                    {
                        return idle;
                    }
                }

                Vector2 localMotionDirection = ResolveLocalMotionDirection(motor.AimHeld && useAimFrameForAimLocomotion);
                if (useDedicatedAimLocomotion && motor.AimHeld)
                {
                    int aimMotion = ChooseAimLocomotion(localMotionDirection, speed, out cost);
                    if (aimMotion >= 0)
                    {
                        return aimMotion;
                    }
                }

                if (preferDirectionalLocomotionWhileAiming && motor.AimHeld)
                {
                    int directionalMotion = database.FindBestByType(SCFMotionType.Locomotion, localMotionDirection, selectedMotionIndex, currentClipBias, out cost);
                    if (directionalMotion >= 0)
                    {
                        return directionalMotion;
                    }
                }

                return database.FindBestLocomotion(localMotionDirection, speed, selectedMotionIndex, currentClipBias, out cost);
            }

            return database.FindFirst(SCFMotionType.Idle);
        }

        private int ChooseAimLocomotion(Vector2 localMotionDirection, float speed, out float cost)
        {
            cost = float.PositiveInfinity;
            if (database == null || motor == null)
            {
                return -1;
            }

            SCFMotionTags gaitTag = motor.RunHeld ? SCFMotionTags.AimRun : SCFMotionTags.AimWalk;
            int gaitMotion = database.FindBestLocomotionWithTags(
                localMotionDirection,
                speed,
                SCFMotionTags.Aim | gaitTag,
                selectedMotionIndex,
                currentClipBias,
                out cost);
            if (gaitMotion >= 0)
            {
                return gaitMotion;
            }

            return database.FindBestLocomotionWithTags(
                localMotionDirection,
                speed,
                SCFMotionTags.Aim,
                selectedMotionIndex,
                currentClipBias,
                out cost);
        }

        private int ChooseWallRunLocomotion(out float cost)
        {
            Vector2 wallRunDirection = Vector2.up;
            float speed = motor != null ? motor.PlanarVelocity.magnitude : 0f;
            int locomotion = database.FindBestLocomotion(wallRunDirection, speed, selectedMotionIndex, currentClipBias, out cost);
            if (locomotion >= 0)
            {
                return locomotion;
            }

            return database.FindFastestLocomotion(out cost);
        }

        private int ChooseActionMotion(SCFMotionType motionType, Vector2 desiredDirection, out float cost)
        {
            int index = database.FindBestByType(motionType, desiredDirection, selectedMotionIndex, currentClipBias, out cost);
            if (index >= 0)
            {
                return index;
            }

            return database.FindBestLocomotion(desiredDirection, motor != null ? motor.PlanarVelocity.magnitude : 0f, selectedMotionIndex, currentClipBias, out cost);
        }

        private int ChooseVaultMotion(out float cost)
        {
            Vector2 direction = ResolveLocalMotionDirection();
            if (motor != null && motor.IsVaultSliding)
            {
                int slide = database.FindBestByType(SCFMotionType.VaultSlide, direction, selectedMotionIndex, currentClipBias, out cost);
                if (slide >= 0)
                {
                    return slide;
                }

                int rollFallback = database.FindBestByType(SCFMotionType.CombatRoll, Vector2.up, selectedMotionIndex, currentClipBias, out cost);
                if (rollFallback >= 0)
                {
                    return rollFallback;
                }
            }

            return ChooseActionMotion(SCFMotionType.Vault, direction, out cost);
        }

        private int ChooseAirborneMotion(out float cost)
        {
            int jump = database.FindBestByType(SCFMotionType.Jump, ResolveLocalMotionDirection(), selectedMotionIndex, currentClipBias, out cost);
            if (jump >= 0)
            {
                return jump;
            }

            int idle = database.FindFirst(SCFMotionType.Idle);
            if (idle >= 0)
            {
                cost = 0f;
                return idle;
            }

            return -1;
        }

        private float ResolveBlendTime(int motionIndex)
        {
            if (database != null && database.TryGetClip(motionIndex, out SCFMotionClipData clipData))
            {
                return clipData.MotionType == SCFMotionType.Idle || clipData.MotionType == SCFMotionType.Locomotion
                    ? locomotionBlendTime
                    : actionBlendTime;
            }

            return locomotionBlendTime;
        }

        private void SwitchToMotion(int motionIndex, float blendTime)
        {
            if (!graph.IsValid() || !mixer.IsValid() || database == null || !database.TryGetClip(motionIndex, out SCFMotionClipData clipData))
            {
                return;
            }

            int nextSlot = activeSlot == 0 ? 1 : 0;
            ClearSlot(nextSlot);

            AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(graph, clipData.Clip);
            clipPlayable.SetApplyFootIK(applyFootIK);
            clipPlayable.SetTime(0d);
            clipPlayable.SetDuration(Mathf.Max(0.01f, clipData.Duration));

            mixer.ConnectInput(nextSlot, clipPlayable, 0);
            mixer.SetInputWeight(nextSlot, activeSlot < 0 ? 1f : 0f);

            slotPlayables[nextSlot] = clipPlayable;
            slotMotionIndices[nextSlot] = motionIndex;
            fadingSlot = activeSlot;
            activeSlot = nextSlot;
            selectedMotionIndex = motionIndex;
            selectedMotionName = clipData.DisplayName;
            fadeTime = 0f;
            fadeDuration = Mathf.Max(0.001f, blendTime);

            if (fadingSlot < 0)
            {
                fadeDuration = 0f;
            }

            SyncActiveMotionSpeed();
        }

        private void ClearSlot(int slot)
        {
            if (slot < 0 || slot >= slotPlayables.Length)
            {
                return;
            }

            if (mixer.IsValid())
            {
                Playable input = mixer.GetInput(slot);
                if (input.IsValid())
                {
                    mixer.DisconnectInput(slot);
                }

                mixer.SetInputWeight(slot, 0f);
            }

            if (slotPlayables[slot].IsValid())
            {
                slotPlayables[slot].Destroy();
            }

            slotPlayables[slot] = Playable.Null;
            slotMotionIndices[slot] = -1;
        }

        private void TickFade(float deltaTime)
        {
            if (!mixer.IsValid() || activeSlot < 0)
            {
                return;
            }

            if (fadingSlot < 0 || fadeDuration <= 0f)
            {
                mixer.SetInputWeight(activeSlot, 1f);
                return;
            }

            fadeTime += deltaTime;
            float blend = Mathf.Clamp01(fadeTime / fadeDuration);
            mixer.SetInputWeight(activeSlot, blend);
            mixer.SetInputWeight(fadingSlot, 1f - blend);

            if (blend >= 1f)
            {
                ClearSlot(fadingSlot);
                fadingSlot = -1;
            }
        }

        private void SyncActiveMotionSpeed()
        {
            if (database == null || activeSlot < 0 || !slotPlayables[activeSlot].IsValid())
            {
                return;
            }

            if (!database.TryGetClip(slotMotionIndices[activeSlot], out SCFMotionClipData clipData))
            {
                return;
            }

            float speed = 1f;
            if (motor != null)
            {
                if (clipData.MotionType == SCFMotionType.WallRun)
                {
                    speed = motor.WallRunAnimationSpeed;
                }
                else if (clipData.MotionType == SCFMotionType.Locomotion && clipData.AveragePlanarSpeed > 0.1f)
                {
                    speed = Mathf.Clamp(motor.PlanarVelocity.magnitude / clipData.AveragePlanarSpeed, 0.75f, 1.35f);
                }
            }

            slotPlayables[activeSlot].SetSpeed(speed);
        }

        private void TickClipPlayback()
        {
            for (int i = 0; i < slotPlayables.Length; i++)
            {
                Playable playable = slotPlayables[i];
                if (!playable.IsValid() || database == null || !database.TryGetClip(slotMotionIndices[i], out SCFMotionClipData clipData))
                {
                    continue;
                }

                double duration = Mathf.Max(0.01f, clipData.Duration);
                if (clipData.Looping)
                {
                    double time = playable.GetTime();
                    if (clipData.MotionType == SCFMotionType.WallRun)
                    {
                        playable.SetTime(WrapLoopTimeIntoWindow(time, duration, wallRunLoopStart, wallRunLoopEnd));
                        continue;
                    }

                    if (time >= duration || time < 0d)
                    {
                        playable.SetTime(time - System.Math.Floor(time / duration) * duration);
                    }

                    continue;
                }

                if (syncActionsToMotorTime && ShouldSyncActionToMotor(clipData.MotionType))
                {
                    playable.SetSpeed(0d);
                    playable.SetTime(ResolveSyncedActionTime(clipData.MotionType, duration));
                }
            }
        }

        private double ResolveSyncedActionTime(SCFMotionType motionType, double duration)
        {
            float motorTime = Mathf.Clamp01(motor != null ? motor.MobilityStateNormalizedTime : 0f);
            ResolveClipWindow(motionType, out float start, out float end);
            float clipTime = Mathf.Lerp(start, Mathf.Max(start, end), motorTime);
            return clipTime * duration;
        }

        private void ResolveClipWindow(SCFMotionType motionType, out float start, out float end)
        {
            switch (motionType)
            {
                case SCFMotionType.Jump:
                    start = jumpClipStart;
                    end = jumpClipEnd;
                    break;
                case SCFMotionType.CombatRoll:
                    start = combatRollClipStart;
                    end = combatRollClipEnd;
                    break;
                case SCFMotionType.Vault:
                case SCFMotionType.VaultSlide:
                    start = vaultClipStart;
                    end = vaultClipEnd;
                    break;
                case SCFMotionType.Climb:
                    start = climbClipStart;
                    end = climbClipEnd;
                    break;
                default:
                    start = 0f;
                    end = 1f;
                    break;
            }

            if (end < start)
            {
                end = start;
            }
        }

        private static double WrapLoopTimeIntoWindow(double time, double duration, float startNormalized, float endNormalized)
        {
            float start = Mathf.Clamp01(startNormalized);
            float end = Mathf.Clamp01(Mathf.Max(startNormalized, endNormalized));
            double startTime = start * duration;
            double endTime = Mathf.Max(start + 0.001f, end) * duration;
            double windowDuration = System.Math.Max(0.01d, endTime - startTime);
            if (time < startTime || time >= endTime)
            {
                time = startTime + (time - startTime - System.Math.Floor((time - startTime) / windowDuration) * windowDuration);
            }

            return time;
        }

        private bool ShouldSyncActionToMotor(SCFMotionType motionType)
        {
            if (motor == null)
            {
                return false;
            }

            return (motionType == SCFMotionType.CombatRoll && motor.IsCombatRolling)
                || (motionType == SCFMotionType.Jump && motor.IsAirborne)
                || (motionType == SCFMotionType.Vault && motor.IsVaulting)
                || (motionType == SCFMotionType.VaultSlide && motor.IsVaulting && motor.IsVaultSliding)
                || (motionType == SCFMotionType.Climb && motor.IsClimbing);
        }

        private Vector2 ResolveLocalMotionDirection()
        {
            return ResolveLocalMotionDirection(false);
        }

        private Vector2 ResolveLocalMotionDirection(bool useAimReference)
        {
            if (motor == null)
            {
                return Vector2.zero;
            }

            Vector3 velocity = motor.PlanarVelocity.sqrMagnitude > 0.01f ? motor.PlanarVelocity : motor.DesiredVelocity;
            velocity.y = 0f;
            if (velocity.sqrMagnitude <= 0.0001f)
            {
                return Vector2.zero;
            }

            Vector3 localVelocity;
            Vector3 planarAim = useAimReference && motor.HasAimDirection
                ? Vector3.ProjectOnPlane(motor.AimDirection, Vector3.up)
                : Vector3.zero;
            if (planarAim.sqrMagnitude > 0.0001f)
            {
                localVelocity = Quaternion.Inverse(Quaternion.LookRotation(planarAim.normalized, Vector3.up)) * velocity.normalized;
            }
            else
            {
                Transform reference = animator != null ? animator.transform : transform;
                localVelocity = reference.InverseTransformDirection(velocity.normalized);
            }

            Vector2 localDirection = new Vector2(localVelocity.x, localVelocity.z);
            return localDirection.sqrMagnitude > 0.0001f ? localDirection.normalized : Vector2.zero;
        }

        private bool IsActionStateActive()
        {
            return motor != null && (motor.IsCombatRolling || motor.IsWallRunning || motor.IsVaulting || motor.IsClimbing || motor.IsJumpCharging || motor.IsAirborne);
        }

        private void CaptureActionSequences()
        {
            if (motor == null)
            {
                return;
            }

            lastCombatRollSequence = motor.CombatRollSequence;
            lastJumpSequence = motor.JumpSequence;
            lastWallRunSequence = motor.WallRunSequence;
            lastVaultSequence = motor.VaultSequence;
            lastClimbSequence = motor.ClimbSequence;
        }

        private bool ConsumeActionSequenceChange()
        {
            if (motor == null)
            {
                return false;
            }

            bool changed = false;
            if (lastCombatRollSequence < 0)
            {
                CaptureActionSequences();
                return false;
            }

            if (lastCombatRollSequence != motor.CombatRollSequence)
            {
                changed |= motor.IsCombatRolling;
                lastCombatRollSequence = motor.CombatRollSequence;
            }

            if (lastJumpSequence != motor.JumpSequence)
            {
                changed |= motor.IsAirborne || motor.IsJumpCharging;
                lastJumpSequence = motor.JumpSequence;
            }

            if (lastWallRunSequence != motor.WallRunSequence)
            {
                changed |= motor.IsWallRunning;
                lastWallRunSequence = motor.WallRunSequence;
            }

            if (lastVaultSequence != motor.VaultSequence)
            {
                changed |= motor.IsVaulting;
                lastVaultSequence = motor.VaultSequence;
            }

            if (lastClimbSequence != motor.ClimbSequence)
            {
                changed |= motor.IsClimbing;
                lastClimbSequence = motor.ClimbSequence;
            }

            return changed;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugTrajectory || database == null || selectedMotionIndex < 0)
            {
                return;
            }

            if (!database.TryGetClip(selectedMotionIndex, out SCFMotionClipData clipData))
            {
                return;
            }

            SCFMotionFrame[] frames = clipData.Frames;
            if (frames == null || frames.Length == 0 || frames[0].Trajectory == null)
            {
                return;
            }

            Vector3 origin = transform.position + Vector3.up * 0.05f;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = Vector3.forward;
            }

            Quaternion rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            Gizmos.color = Color.cyan;
            Vector3 previous = origin;
            SCFTrajectoryPoint[] trajectory = frames[0].Trajectory;
            for (int i = 0; i < trajectory.Length; i++)
            {
                Vector3 next = origin + rotation * trajectory[i].LocalPosition;
                Gizmos.DrawLine(previous, next);
                Gizmos.DrawSphere(next, 0.05f);
                previous = next;
            }

            if (motor != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(origin, origin + motor.DesiredVelocity * 0.25f);
            }
        }
    }
}
