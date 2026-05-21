using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SCF.Gameplay
{
    public enum SCFWeaponFitPoseSlot
    {
        Neutral,
        AimWalk,
        AimRun
    }

    public enum SCFRearAimGateMode
    {
        None,
        RightHandRearShot,
        InwardTwoHandTurn
    }

    public readonly struct SCFRailgunShot
    {
        public readonly Vector3 Muzzle;
        public readonly Vector3 Direction;
        public readonly Vector3 Impact;
        public readonly Collider HitCollider;
        public readonly Vector3 HitNormal;

        public SCFRailgunShot(Vector3 muzzle, Vector3 direction, Vector3 impact, Collider hitCollider, Vector3 hitNormal)
        {
            Muzzle = muzzle;
            Direction = direction;
            Impact = impact;
            HitCollider = hitCollider;
            HitNormal = hitNormal;
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(IsometricCharacterMotor))]
    [DefaultExecutionOrder(95)]
    public sealed class SCFWeaponVisualSlot : MonoBehaviour
    {
        private const int CurrentRailgunProfileRevision = 24;
        private const string PrototypeRailgunPath = "Assets/SCF/2.8 rail-gun prototype_Texture_Packed.blend";
        private const string RailgunFireClipPath = "Assets/SCF/Audio/kalsstockmedia-a-large-explosive-laser-gun-shot-scifi-410622.mp3";
        private const string ReferenceShotgunPath = "Assets/SCF/MovementAni/NVoperatorsoldier.glb";
        private const string DefaultRailgunRigProfilePath = "SCF/Weapons/SCF_RailgunRigProfile";
        private const string RuntimeWeaponCatalogPath = "SCF/SCFWeaponRuntimeCatalog";
        private const string CleanRailgunPrefabPath = "Assets/SCF/Prefabs/Weapons/SCF_RailgunNEW.prefab";
        private const string Pose3RightGripTargetName = "SCF_Pose3RightGripTarget";
        private const string Pose3LeftGripTargetName = "SCF_Pose3LeftGripTarget";
        private const string RightHandWeaponSocketName = "SCF_RightHandWeaponSocket";
        private const string RailgunMuzzleTargetName = "SCF_RailgunMuzzleTarget";
        private const string RailgunButtstockAnchorName = "SCF_RailgunButtstock";
        private const string RailgunButtstockTargetName = "SCF_RailgunButtstockTarget";
        private const string RailgunButtstockTypoAliasName = "Butstock";
        private const string RailgunButtstockPlainAliasName = "Buttstock";
        private const string RailgunModelRootName = "SCF_RailgunModel";
        private const string RailgunMuzzleMarkerName = "SCF_RailgunMuzzleMarker";
        private const string AimRayDebugRootName = "SCF_AimRayDebug";
        private const string AimRayFaceLineName = "SCF_AimRay_FaceOrigin";
        private const string AimRayChestLineName = "SCF_AimRay_ChestOrigin";
        private const string AimRayMuzzleToFocusLineName = "SCF_AimRay_MuzzleToMouseColumn";
        private const string AimRayMuzzleForwardLineName = "SCF_AimRay_MuzzleForward";
        private const string AimRayMouseColumnLineName = "SCF_AimRay_MouseVertical";
        private const string AimFocusCursorName = "SCF_AimFocusCursor";
        private const string IdleRightGripCarryPointName = "SCF_RightGrip_Idle";
        private const string HipAimRightGripCarryPointName = "SCF_RightGrip_HipAim";
        private const string ShoulderAimRightGripCarryPointName = "SCF_RightGrip_ShoulderAim";
        private const string RunRightGripCarryPointName = "SCF_RightGrip_Run";
        private const string SprintRightGripCarryPointName = "SCF_RightGrip_Sprint";
        private const string OneHandRightGripCarryPointName = "SCF_RightGrip_OneHand";
        private const string IdleWeaponPlacementPointName = "Idle";
        private const string HipAimWeaponPlacementPointName = "HipGun";
        private const string ShoulderAimWeaponPlacementPointName = "ShoulderGun";
        private const string RunWeaponPlacementPointName = "RunGun";
        private const string SprintWeaponPlacementPointName = "SprintGun";
        private const string OneHandWeaponPlacementPointName = "Gun";
        private static readonly Vector3 LegacyWeaponEulerAngles = new Vector3(0f, 90f, 90f);
        private static readonly Vector3 DefaultWeaponEulerAngles = new Vector3(170f, 90f, 180f);
        private static readonly Vector3 LegacyRightGripPosition = new Vector3(0.02f, -0.05f, 0.15f);
        private static readonly Vector3 DefaultRightGripPosition = new Vector3(0.02f, -0.05f, 0.12f);
        private static readonly Vector3 LegacyLeftGripPosition = new Vector3(-0.06f, 0.01f, 0.38f);
        private static readonly Vector3 DefaultLeftGripPosition = new Vector3(-0.04f, -0.02f, 0.28f);
        private static readonly Vector3 Pose3ReferenceRightWristFromShotgunBone = new Vector3(-0.0332f, -0.2147f, -0.8692f);
        private static readonly Vector3 Pose3ReferenceLeftWristFromShotgunBone = new Vector3(0.1325f, -0.2297f, 0.3901f);
        private static readonly Vector3 DefaultRailgunSocketLocalPosition = new Vector3(0.09301f, 0.063495f, 0.288997f);
        private static readonly Vector3 DefaultRailgunSocketLocalEulerAngles = new Vector3(-3.624756f, 0f, 0f);
        private static readonly Vector3 DefaultRailgunWeaponLocalPosition = new Vector3(0.046814f, 0.008097f, -0.121653f);
        private static readonly Vector3 DefaultRailgunWeaponLocalEulerAngles = new Vector3(-23.89532f, -108.727f, 8.349475f);
        private static readonly Vector3 DefaultRailgunWeaponLocalScale = new Vector3(0.8f, 0.8f, 0.8f);
        private static readonly Vector3 DefaultRailgunModelLocalPosition = new Vector3(0.04f, 0.03f, 0f);
        private static readonly Vector3 DefaultRailgunRightGripLocalPosition = new Vector3(-0.1f, -0.03f, -0.01f);
        private static readonly Vector3 DefaultRailgunRightGripLocalEulerAngles = new Vector3(-30.5f, 95.7f, -77.81f);
        private static readonly Vector3 DefaultRailgunLeftGripLocalPosition = new Vector3(0.86f, -0.07f, -0.2f);
        private static readonly Vector3 DefaultRailgunLeftGripLocalEulerAngles = new Vector3(-17.67001f, 124.98f, -132.4f);
        private static readonly Vector3 DefaultRailgunMuzzleLocalPosition = new Vector3(0.96f, 0.12f, 0f);
        private static readonly Vector3 DefaultRailgunMuzzleLocalEulerAngles = new Vector3(0f, 90f, 0f);
        private static readonly Vector3 DefaultRailgunButtstockLocalPosition = new Vector3(-0.36f, 0.06f, 0f);
        private static readonly Vector3 DefaultRailgunButtstockLocalEulerAngles = Vector3.zero;

        [Header("References")]
        [SerializeField] private IsometricPlayerInput input;
        [SerializeField] private IsometricCharacterMotor motor;
        [SerializeField] private Animator animator;

        [Header("Railgun Profile")]
        [SerializeField] private SCFWeaponRigProfile railgunRigProfile;
        [SerializeField] private SCFWeaponRuntimeCatalog runtimeWeaponCatalog;
        [SerializeField] private GameObject railgunPrototype;
        [SerializeField] private GameObject selectedWeaponPrototype;
        [SerializeField] private GameObject referenceShotgunPrototype;
        [SerializeField] private bool equipRailgunOnSoldier = true;
        [SerializeField] private bool autoEquipRailgunOnConfigure;
        [SerializeField] private bool useReferenceShotgunPrototype;
        [SerializeField] private int railgunProfileRevision;
        [SerializeField] private string torsoSocketName = "SCF_ChestWeaponSocket";
        [SerializeField] private string experimentalRightHandSocketName = RightHandWeaponSocketName;
        [SerializeField] private string rightGripName = "SCF_RightPistolGrip";
        [SerializeField] private string leftGripName = "SCF_LeftUnderbarrelGrip";
        [SerializeField] private string buttstockName = RailgunButtstockAnchorName;
        [SerializeField] private string referenceShotgunMeshName = "SHOTGUN";
        [SerializeField] private string referenceShotgunAnchorName = "SHOTGUNbone";
        [SerializeField] private string referenceRightGripName = "HandWrist.R";
        [SerializeField] private string referenceLeftGripName = "HandWrist.L";
        [SerializeField] private string referencePoseClipName = "SHOTGUNpose3";
        [SerializeField, Range(0f, 1f)] private float referencePoseSampleTime01 = 0f;

        [Header("World Pickup")]
        [SerializeField] private bool spawnDefaultRailgunPickup = true;
        [SerializeField] private KeyCode dropWeaponKey = KeyCode.G;
        [SerializeField, Min(0f)] private float droppedWeaponSelfPickupDelay = 0.75f;

        [Header("Body Socket")]
        [Tooltip("Chest-bone local weapon socket offset.")]
        [SerializeField] private Vector3 restSocketOffset = DefaultRailgunSocketLocalPosition;
        [Tooltip("Right-click raised chest-bone local weapon socket offset.")]
        [SerializeField] private Vector3 raisedSocketOffset = DefaultRailgunSocketLocalPosition;
        [SerializeField] private Vector3 restSocketEulerAngles = DefaultRailgunSocketLocalEulerAngles;
        [SerializeField] private Vector3 raisedSocketEulerAngles = DefaultRailgunSocketLocalEulerAngles;
        [SerializeField, Min(0.1f)] private float raiseSharpness = 14f;

        [Header("Weapon Local Fit")]
        [SerializeField] private Vector3 weaponLocalPosition = Vector3.zero;
        [SerializeField] private Vector3 weaponLocalEulerAngles = DefaultRailgunWeaponLocalEulerAngles;
        [SerializeField] private Vector3 weaponLocalScale = DefaultRailgunWeaponLocalScale;

        [Header("Weapon Anchor")]
        [SerializeField] private bool anchorWeaponByRightGrip = true;
        [SerializeField] private Vector3 restRightGripSocketOffset = Vector3.zero;
        [SerializeField] private Vector3 raisedRightGripSocketOffset = Vector3.zero;

        [Header("Shotgun Pose3 Replica")]
        [SerializeField] private bool useShotgunPose3GripReplica = true;
        [SerializeField, Min(0.01f)] private float shotgunPose3Scale = 0.27f;
        [SerializeField] private Vector3 shotgunPose3RightGripTuningOffset = new Vector3(0f, -0.08f, -0.14f);
        [SerializeField] private Vector3 shotgunPose3LeftGripTuningOffset = new Vector3(-0.105261f, 0.15405f, -0.000011f);
        [SerializeField] private Vector3 shotgunPose3WeaponAnchorTuningOffset = Vector3.zero;
        [SerializeField] private Vector3 shotgunPose3RightGripEulerAngles = new Vector3(-106.2f, -23.89999f, -75.70001f);
        [SerializeField] private Vector3 shotgunPose3LeftGripEulerAngles = new Vector3(-134.6f, 146.4f, -11.4f);

        [Header("Railgun Live Tuning")]
        [SerializeField] private bool enableRailgunLiveTuning = true;
        [SerializeField] private bool preserveTunedRailgunTransform = true;
        [SerializeField] private bool preserveTunedGripTargets = true;
        [SerializeField] private bool returnWeaponToNeutralFitWhenNotAiming = true;
        [SerializeField] private bool captureTuningEveryFrame;
        [SerializeField] private bool copyTuningOnCapture = true;
        [SerializeField] private KeyCode captureTuningHotkey = KeyCode.F8;

        [Header("Weapon Fit Pose Slots")]
        [SerializeField] private bool useWeaponFitPoseSlots = true;
        [SerializeField] private bool weaponFitPoseTuningMode;
        [SerializeField] private SCFWeaponFitPoseSlot tuningWeaponFitPoseSlot = SCFWeaponFitPoseSlot.Neutral;
        [SerializeField] private SCFWeaponFitPose neutralWeaponFitPose;
        [SerializeField] private SCFWeaponFitPose aimWalkWeaponFitPose;
        [SerializeField] private SCFWeaponFitPose aimRunWeaponFitPose;

        [Header("Railgun Fire")]
        [SerializeField] private bool enableRailgunFire = true;
        [SerializeField] private bool fireRailgunWhileHeld;
        [SerializeField, Min(0.01f)] private float railgunFireCooldown = 0.42f;
        [SerializeField, Min(1f)] private float railgunFireRange = 120f;
        [SerializeField] private LayerMask railgunHitMask = ~0;
        [SerializeField] private string railgunMuzzleTransformName = RailgunMuzzleTargetName;
        [SerializeField] private bool createRailgunMuzzleTarget = true;
        [SerializeField] private bool preserveTunedRailgunMuzzleTarget = true;
        [SerializeField] private Vector3 railgunMuzzleLocalPosition = DefaultRailgunMuzzleLocalPosition;
        [SerializeField] private Vector3 railgunMuzzleLocalEulerAngles = DefaultRailgunMuzzleLocalEulerAngles;
        [SerializeField] private bool createRailgunButtstockTarget = true;
        [SerializeField] private bool preserveTunedRailgunButtstockTarget = true;
        [SerializeField] private Vector3 railgunButtstockLocalPosition = DefaultRailgunButtstockLocalPosition;
        [SerializeField] private Vector3 railgunButtstockLocalEulerAngles = DefaultRailgunButtstockLocalEulerAngles;
        [SerializeField] private bool showRailgunMuzzleDebugMarker = true;
        [SerializeField, Min(0.005f)] private float railgunMuzzleDebugMarkerSize = 0.055f;
        [SerializeField, Min(0f)] private float railgunRaycastStartOffset = 0.1f;
        [SerializeField, Min(0f)] private float railgunMuzzleForwardOffset = 0.08f;
        [SerializeField] private AudioClip railgunFireClip;
        [SerializeField, Range(0f, 1f)] private float railgunFireVolume = 0.85f;
        [SerializeField, Range(0f, 1f)] private float railgunFireSpatialBlend = 0.72f;
        [SerializeField, Range(0.1f, 3f)] private float railgunFireMinPitch = 0.96f;
        [SerializeField, Range(0.1f, 3f)] private float railgunFireMaxPitch = 1.04f;
        [SerializeField] private Color railgunBeamColor = new Color(0.35f, 0.95f, 1f, 0.95f);
        [SerializeField] private Color railgunBeamCoreColor = new Color(1f, 1f, 1f, 0.9f);
        [SerializeField, Min(0.001f)] private float railgunBeamWidth = 0.045f;
        [SerializeField, Min(0.01f)] private float railgunBeamLifetime = 0.11f;
        [SerializeField, Min(1f)] private float railgunTracerProjectileSpeed = 260f;
        [SerializeField, Min(0.01f)] private float railgunTracerMaxTravelTime = 0.09f;
        [SerializeField, Min(0.01f)] private float railgunTracerParticleLifetime = 0.42f;
        [SerializeField, Min(0.001f)] private float railgunTracerParticleSize = 0.045f;
        [SerializeField, Min(0.001f)] private float railgunTracerRadius = 0.08f;
        [SerializeField, Min(0f)] private float railgunTracerArcSpeed = 5.5f;
        [SerializeField, Min(0f)] private float railgunTracerEmissionPerDistance = 95f;
        [SerializeField, Min(0f)] private int railgunImpactBurstCount = 28;

        [Header("Muzzle Aim Authority")]
        [SerializeField] private bool muzzleDrivesWeaponAim = true;
        [SerializeField, Min(0.1f)] private float muzzleAimSharpness = 24f;
        [SerializeField, Range(0f, 1f)] private float muzzleAimWeight = 1f;
        [SerializeField] private bool muzzleAimOnlyWhileAimHeld = true;
        [SerializeField] private bool aimMuzzleAroundRightGripPivot = true;
        [SerializeField, Range(0f, 1.5f)] private float muzzleSidePressure = 1f;
        [SerializeField, Range(0f, 1.5f)] private float muzzleVerticalPressure = 0.62f;
        [SerializeField] private bool clampMuzzleYawAroundRightGrip;
        [SerializeField, Range(5f, 135f)] private float muzzleGripYawLimit = 72f;
        [Tooltip("Aim at the face-height point on the mouse vertical column. This matches the eye/intent crossover instead of flattening to the muzzle.")]
        [SerializeField] private bool useFaceMouseColumnAimFocus = true;
        [SerializeField] private bool flattenMuzzleAimToMuzzleHeight;
        [SerializeField] private float muzzleAimTargetHeightOffset;
        [SerializeField] private bool useEyeCarryDirectionForBehindMuzzleTargets = true;
        [Tooltip("When the mouse target is inside/behind the muzzle safety zone, keep the current weapon pose instead of switching to the eye-carry fallback.")]
        [SerializeField] private bool holdWeaponPoseForSelfAimProtection;
        [SerializeField, Min(0f)] private float behindMuzzleTargetSlack = 0.02f;
        [SerializeField] private bool drawMuzzleAimDebug;

        [Header("Action Rear Aim Gate")]
        [SerializeField] private bool useActionRearAimGate;
        [SerializeField, Range(90f, 180f)] private float actionRearAimYawThreshold = 115f;
        [SerializeField] private bool rightSideRearAimUsesOneHand = true;
        [SerializeField, Range(0f, 1f)] private float rightSideRearAimLeftHandWeight = 0f;
        [SerializeField, Range(0f, 1f)] private float inwardRearAimLeftHandWeight = 0.92f;
        [SerializeField] private bool useExperimentalDominantSideAimGate;
        [SerializeField, Range(0f, 90f)] private float dominantSideAimYawThreshold = 15f;
        [SerializeField] private bool dominantGripSideIsRight = true;
        [SerializeField] private bool dominantSideAimUsesOneHand = true;
        [SerializeField] private SCFRearAimGateMode activeRearAimGateMode;
        [SerializeField] private float activeRearAimYaw;

        [Header("Weapon Local X Clamp")]
        [SerializeField] private bool clampSelectedWeaponLocalX;
        [SerializeField] private float selectedWeaponNeutralLocalX;
        [SerializeField, Range(0f, 150f)] private float selectedWeaponMovingLocalXLimit = 3f;
        [SerializeField, Min(0f)] private float selectedWeaponXMovingThreshold = 0.2f;

        [Header("Aim Ray Debug")]
        [SerializeField] private bool showAimRayDebug;
        [SerializeField] private KeyCode aimRayDebugToggleKey = KeyCode.R;
        [SerializeField, Min(0.001f)] private float aimRayDebugWidth = 0.025f;
        [SerializeField, Min(0.1f)] private float aimRayMouseColumnHeight = 7.5f;
        [SerializeField, Min(0f)] private float aimRayMouseColumnDepth = 1.5f;
        [SerializeField] private Color aimRayFaceOriginColor = new Color(0.75f, 0.35f, 1f, 0.95f);
        [SerializeField] private Color aimRayChestOriginColor = new Color(1f, 0.85f, 0.12f, 0.95f);
        [SerializeField] private Color aimRayMuzzleToFocusColor = new Color(0.1f, 0.9f, 1f, 0.95f);
        [SerializeField] private Color aimRayMuzzleForwardColor = new Color(1f, 0.25f, 0.08f, 0.95f);
        [SerializeField] private Color aimRayMouseColumnColor = new Color(0.25f, 1f, 0.35f, 0.9f);

        [Header("Aim Cursor")]
        [SerializeField] private bool showAimFocusCursor = true;
        [SerializeField] private bool aimFocusCursorOnlyWhileAiming;
        [SerializeField, Min(0.01f)] private float aimFocusCursorRadius = 0.085f;
        [SerializeField, Min(0f)] private float aimFocusCursorSurfaceLift = 0.015f;
        [SerializeField] private Color aimFocusCursorColor = new Color(1f, 0.02f, 0.02f, 0.95f);

        [Header("Grip Targets")]
        [SerializeField] private Vector3 rightGripLocalPosition = new Vector3(0.02f, -0.05f, 0.12f);
        [SerializeField] private Vector3 rightGripLocalEulerAngles = new Vector3(0f, 90f, 90f);
        [SerializeField] private Vector3 leftGripLocalPosition = new Vector3(-0.04f, -0.02f, 0.28f);
        [SerializeField] private Vector3 leftGripLocalEulerAngles = new Vector3(0f, 90f, 90f);

        [Header("Hand IK")]
        [SerializeField] private bool enableHandIk = true;
        [SerializeField] private bool preferWeaponPrefabAnchors = true;
        [SerializeField] private bool lockWeaponPrefabGripAnchors = true;
        [SerializeField] private bool useAnimatorIk;
        [SerializeField, Range(0f, 1f)] private float rightHandIkWeight = 0.95f;
        [SerializeField, Range(0f, 1f)] private float leftHandIkWeight = 1f;
        [SerializeField, Range(0f, 1f)] private float genericBoneFallbackWeight = 1f;
        [SerializeField] private bool applyBoneFallbackForHumanoids = true;
        [SerializeField, Range(0f, 2f)] private float rightArmReachWeight = 0.85f;
        [SerializeField, Range(0f, 2f)] private float leftArmReachWeight = 1.35f;
        [SerializeField, Range(1, 8)] private int genericSolveIterations = 6;
        [SerializeField, Range(0f, 1f)] private float finalGripPositionWeight = 0.72f;
        [SerializeField, Range(0f, 1f)] private float finalGripRotationWeight = 0.9f;
        [SerializeField, Min(0.1f)] private float handIkBlendSharpness = 16f;
        [SerializeField] private bool useExperimentalIndexFingerGripContacts = true;
        [SerializeField] private bool rotateExperimentalIndexFingerGripContacts;
        [SerializeField, Range(0f, 1f)] private float experimentalIndexFingerGripPositionWeight = 0.85f;
        [SerializeField, Range(0f, 1f)] private float experimentalIndexFingerGripRotationWeight = 0.15f;
        [SerializeField] private string experimentalRightIndexGripContactName = "RightHandIndex1";
        [SerializeField] private string experimentalLeftIndexGripContactName = "LeftHandIndex1";
        [SerializeField] private string experimentalRightThumbGripContactName = "RightHandThumb1";

        [Header("Experimental Fire Solution")]
        [SerializeField] private bool useExperimentalFireSolutionAuthority = true;
        [Tooltip("For soldierExp, the weapon is translated so its pistol grip stays on the right index/root contact instead of letting the gun float ahead of the hand.")]
        [SerializeField] private bool experimentalRightGripOwnsWeapon = true;
        [SerializeField, Range(0f, 1f)] private float experimentalRightGripOwnershipWeight = 1f;
        [SerializeField] private bool experimentalParentWeaponSocketToRightHand = true;
        [SerializeField] private Vector3 experimentalRightHandSocketLocalPosition = Vector3.zero;
        [SerializeField] private Vector3 experimentalRightHandSocketLocalEulerAngles = Vector3.zero;
        [SerializeField] private bool experimentalRightPalmOwnsWeaponRotation = true;
        [SerializeField, Range(0f, 1f)] private float experimentalRightPalmRotationOwnershipWeight = 1f;
        [SerializeField] private bool experimentalRightPalmPointsAtTarget = true;
        [SerializeField, Range(0f, 1f)] private float experimentalRightPalmAimWeight = 1f;
        [SerializeField] private Vector3 experimentalRightPalmToPistolGripEulerOffset;
        [SerializeField, Range(0f, 1f)] private float experimentalWristOnlyGripRotationWeight = 1f;
        [SerializeField] private bool alignRightHandSocketToFingerPocket = true;
        [SerializeField, Range(0f, 1f)] private float rightHandGripPocketFingerWeight = 0.68f;
        [SerializeField, Range(0f, 1f)] private float rightHandGripPocketBlend = 1f;
        [SerializeField] private Vector3 rightHandGripPocketLocalOffset = Vector3.zero;
        [SerializeField] private bool experimentalWrapRightFingersAroundGrip = true;
        [SerializeField, Range(0f, 1f)] private float experimentalRightFingerWrapWeight = 0.45f;
        [SerializeField] private Vector3 experimentalRightIndexWrapLocalOffset = Vector3.zero;
        [SerializeField] private Vector3 experimentalRightThumbWrapLocalOffset = new Vector3(0.035f, 0f, 0f);
        [SerializeField] private bool experimentalHardLockRightGripContact = true;
        [SerializeField] private bool experimentalHardLockLeftGripContact = true;
        [SerializeField, Range(0f, 1f)] private float experimentalRightGripContactLockWeight = 1f;
        [SerializeField, Range(0f, 1f)] private float experimentalLeftGripContactLockWeight = 0.96f;
        [SerializeField, Range(0f, 1f)] private float experimentalOneHandLeftGripContactLockWeight = 0f;
        [SerializeField] private bool experimentalPreserveGripOwnershipDuringMuzzleAim = true;

        [Header("Experimental Body Carry Points")]
        [SerializeField] private bool useDynamicCharacterBodyRig = true;
        [SerializeField] private bool useGeneratedCharacterCarryPoints = true;
        [SerializeField] private bool useCharacterWeaponPlacementPoints;
        [SerializeField] private bool characterWeaponPlacementTargetsRightGrip = true;
        [SerializeField, Range(0f, 1f)] private float generatedBodyContactPlacementWeight = 0.65f;
        [SerializeField, Range(0f, 1f)] private float generatedBodyContactPostAimWeight = 0.35f;
        [SerializeField] private bool suppressExperimentalWeaponPoseAnimations = true;
        [SerializeField] private bool allowWeaponUpperBodyPoseLayerDuringArmedJump = true;
        [SerializeField] private bool suppressExperimentalWeaponFitPoseSlots = true;
        [SerializeField] private string idleWeaponPlacementPointName = IdleWeaponPlacementPointName;
        [SerializeField] private string hipAimWeaponPlacementPointName = HipAimWeaponPlacementPointName;
        [SerializeField] private string shoulderAimWeaponPlacementPointName = ShoulderAimWeaponPlacementPointName;
        [SerializeField] private string runWeaponPlacementPointName = RunWeaponPlacementPointName;
        [SerializeField] private string sprintWeaponPlacementPointName = SprintWeaponPlacementPointName;
        [SerializeField] private string oneHandWeaponPlacementPointName = OneHandWeaponPlacementPointName;
        [SerializeField, Min(0.1f)] private float characterWeaponPlacementSharpness = 26f;
        [SerializeField] private string activeCharacterWeaponPlacementPointName;
        [SerializeField] private bool useCharacterRightGripCarryPoints;
        [SerializeField] private string idleRightGripCarryPointName = IdleRightGripCarryPointName;
        [SerializeField] private string hipAimRightGripCarryPointName = HipAimRightGripCarryPointName;
        [SerializeField] private string shoulderAimRightGripCarryPointName = ShoulderAimRightGripCarryPointName;
        [SerializeField] private string runRightGripCarryPointName = RunRightGripCarryPointName;
        [SerializeField] private string sprintRightGripCarryPointName = SprintRightGripCarryPointName;
        [SerializeField] private string oneHandRightGripCarryPointName = OneHandRightGripCarryPointName;
        [SerializeField, Min(0.1f)] private float characterRightGripCarrySharpness = 26f;
        [SerializeField] private string activeCharacterRightGripCarryPointName;

        [Header("Anatomy Guardrails")]
        [SerializeField] private bool useAnatomyAwareIk = true;
        [SerializeField] private bool useDynamicBodyKeepoutZones = true;
        [SerializeField, Range(0.5f, 1.05f)] private float maxArmReachMultiplier = 0.94f;
        [SerializeField, Range(0f, 0.5f)] private float armReachSoftZone = 0.16f;
        [SerializeField, Range(0f, 1f)] private float unreachableRotationWeight = 0.35f;
        [SerializeField] private bool constrainGripTargetsAgainstBody = true;
        [SerializeField] private bool constrainForearmJointAgainstBody = true;
        [SerializeField, Range(0f, 0.75f)] private float gripTargetBodyCrossAllowance = 0.32f;
        [SerializeField, Range(0f, 0.75f)] private float gripTargetBehindBodyAllowance = 0.12f;
        [SerializeField, Range(0f, 1f)] private float unreachableGripWeightFloor = 0.35f;
        [SerializeField, Range(0f, 1f)] private float forearmJointGuardrailWeight = 0.85f;
        [SerializeField] private bool solveGripContactLocksThroughArmChain = true;
        [SerializeField, Range(0f, 2f)] private float gripContactArmSolveWeight = 1.1f;
        [SerializeField] private bool limitWristContactTranslation;
        [SerializeField, Range(0.01f, 0.5f)] private float maxWristContactTranslation = 0.12f;
        [SerializeField] private bool constrainWristRotation;
        [SerializeField, Range(0f, 90f)] private float maxWristRotationFromAnimatedPose = 32f;
        [SerializeField, Range(0f, 1f)] private float wristRotationLimitWeight = 1f;

        [Header("Carry State Weights")]
        [SerializeField] private bool useStateBasedCarryWeights = true;
        [SerializeField, Range(0f, 1f)] private float locomotionCarryWeight = 0.95f;
        [SerializeField, Range(0f, 1f)] private float aimedCarryWeight = 1f;
        [SerializeField, Range(0f, 1f)] private float sprintCarryWeight = 0.86f;
        [SerializeField, Range(0f, 1f)] private float airborneCarryWeight = 0.72f;
        [SerializeField, Range(0f, 1f)] private float traversalCarryWeight = 0.5f;
        [SerializeField, Range(0f, 1f)] private float wallRunCarryWeight = 0.58f;
        [SerializeField, Range(0f, 1f)] private float rollCarryWeight = 0.18f;

        [Header("Traversal Weapon Handling")]
        [SerializeField] private bool detachLeftHandDuringVault = true;
        [SerializeField] private bool detachLeftHandDuringWallRun = true;
        [SerializeField] private bool detachBothHandsDuringClimb = true;
        [SerializeField] private bool suspendMuzzleAimDuringClimb = true;
        [SerializeField] private bool lowerWeaponDuringClimb = true;
        [SerializeField] private bool blockRailgunFireDuringClimb = true;
        [SerializeField] private bool snapWeaponFitAfterMobilityAction = true;
        [SerializeField] private bool blockLiveTuningDuringMobilityAction = true;
        [SerializeField, Range(0f, 1f)] private float vaultRightHandCarryWeight = 1f;
        [SerializeField, Range(0f, 1f)] private float vaultLeftHandCarryWeight = 0f;
        [SerializeField, Range(0f, 1f)] private float wallRunRightHandCarryWeight = 1f;
        [SerializeField, Range(0f, 1f)] private float wallRunLeftHandCarryWeight = 0f;
        [SerializeField, Range(0f, 1f)] private float climbRightHandCarryWeight = 1f;
        [SerializeField, Range(0f, 1f)] private float climbHandCarryWeight = 0f;

        [Header("Experimental Pose Authoring")]
        [SerializeField] private bool experimentalPoseAuthoringMode;
        [SerializeField] private KeyCode experimentalPoseAuthoringToggleKey = KeyCode.F6;
        [SerializeField] private bool poseAuthoringDisablesAnimator = true;
        [SerializeField] private bool poseAuthoringDisablesPoseScripts = true;
        [SerializeField] private bool poseAuthoringRecordsFullCharacterHierarchy = true;

        [Header("Debug")]
        [SerializeField] private string activeCharacterName;
        [SerializeField] private GameObject activeWeapon;
        [SerializeField] private bool activeWeaponSupportsRailgunFire;
        [SerializeField, Range(0f, 1f)] private float raised01;
        [SerializeField, Range(0f, 1f)] private float currentRightHandWeight;
        [SerializeField, Range(0f, 1f)] private float currentLeftHandWeight;

        [Header("Weapon IK Live Logger")]
        [SerializeField] private bool enableWeaponIkLiveLogger = true;
        [SerializeField] private bool weaponIkLiveLoggerRecording;
        [SerializeField] private KeyCode weaponIkSnapshotKey = KeyCode.F10;
        [SerializeField] private KeyCode weaponIkRecordToggleKey = KeyCode.F11;
        [SerializeField, Min(0.02f)] private float weaponIkLiveLoggerInterval = 0.1f;
        [SerializeField] private string weaponIkLiveLoggerFilePrefix = "SCF_WeaponIkLive";

        private Transform weaponSocket;
        private Transform rightGrip;
        private Transform leftGrip;
        private Transform buttstockTarget;
        private Transform chestAnchor;
        private Transform faceAnchor;
        private Transform referenceWeaponAnchor;
        private Transform pose3RightGripTarget;
        private Transform pose3LeftGripTarget;
        private Transform railgunMuzzleTarget;
        private Transform idleWeaponPlacementPoint;
        private Transform hipAimWeaponPlacementPoint;
        private Transform shoulderAimWeaponPlacementPoint;
        private Transform runWeaponPlacementPoint;
        private Transform sprintWeaponPlacementPoint;
        private Transform oneHandWeaponPlacementPoint;
        private Transform idleRightGripCarryPoint;
        private Transform hipAimRightGripCarryPoint;
        private Transform shoulderAimRightGripCarryPoint;
        private Transform runRightGripCarryPoint;
        private Transform sprintRightGripCarryPoint;
        private Transform oneHandRightGripCarryPoint;
        private SCFBodyKeepoutSolver bodyKeepoutSolver;
        private ArmRig rightArm;
        private ArmRig leftArm;
        private float nextRailgunFireTime;
        private bool wasInWeaponFitResetAction;
        private bool hasRuntimeWeaponFitPoseSlot;
        private SCFWeaponFitPoseSlot lastRuntimeWeaponFitPoseSlot;

        public event Action<SCFRailgunShot> RailgunFired;
        private Material railgunBeamMaterial;
        private Material railgunParticleMaterial;
        private Material aimRayDebugMaterial;
        private GameObject aimRayDebugRoot;
        private LineRenderer aimRayFaceLine;
        private LineRenderer aimRayChestLine;
        private LineRenderer aimRayMuzzleToFocusLine;
        private LineRenderer aimRayMuzzleForwardLine;
        private LineRenderer aimRayMouseColumnLine;
        private GameObject aimFocusCursor;
        private Renderer aimFocusCursorRenderer;
        private Material aimFocusCursorMaterial;
        private float nextWeaponIkLiveLogTime;
        private int lastWeaponTargetSolveFrame = -1;
        private int lastHandWeightUpdateFrame = -1;
        private Behaviour[] poseAuthoringDisabledBehaviours = new Behaviour[0];
        private bool poseAuthoringAnimatorStateCaptured;
        private bool poseAuthoringAnimatorWasEnabled;
        private float poseAuthoringAnimatorSpeed = 1f;

        public bool HasActiveWeapon => activeWeapon != null;
        public string ActiveWeaponName => activeWeapon != null ? activeWeapon.name.Replace("SCF_Selected_", string.Empty) : "none";
        public GameObject SelectedWeaponPrototype => selectedWeaponPrototype;
        public Transform WeaponSocketTransform => weaponSocket;
        public Transform ActiveWeaponTransform => activeWeapon != null ? activeWeapon.transform : null;
        public Transform RightGripTransform => rightGrip;
        public Transform LeftGripTransform => leftGrip;
        public Transform RailgunMuzzleTransform => railgunMuzzleTarget;
        public Transform RailgunButtstockTransform => buttstockTarget;
        public float Raised01 => raised01;
        public bool SuppressWeaponUpperBodyPoseLayer => suppressExperimentalWeaponPoseAnimations
                                                       && UsesExperimentalFireSolutionAuthority()
                                                       && !ShouldAllowWeaponUpperBodyPoseLayerDuringArmedJump();
        public SCFWeaponFitPoseSlot TuningWeaponFitPoseSlot => tuningWeaponFitPoseSlot;
        public bool WeaponFitPoseTuningMode => weaponFitPoseTuningMode;
        public bool ExperimentalPoseAuthoringMode => experimentalPoseAuthoringMode;

        [Serializable]
        private struct SCFLocalTransformPose
        {
            public bool captured;
            public Vector3 localPosition;
            public Vector3 localEulerAngles;
            public Vector3 localScale;
        }

        [Serializable]
        private struct SCFWeaponFitPose
        {
            public bool captured;
            public SCFLocalTransformPose socket;
            public SCFLocalTransformPose weapon;
            public SCFLocalTransformPose rightGrip;
            public SCFLocalTransformPose leftGrip;
            public SCFLocalTransformPose muzzle;
            public SCFLocalTransformPose buttstock;
        }

        private struct ArmRig
        {
            public Transform Shoulder;
            public Transform UpperArm;
            public Transform Forearm;
            public Transform Hand;
            public Transform GripContact;
            public Transform IndexContact;
            public Transform ThumbContact;

            public Transform EndEffector => GripContact != null ? GripContact : Hand;
        }

        private void Awake()
        {
            ApplyRailgunProfileDefaultsIfNeeded();
            ApplyRigProfileDefaultsIfPresent();
            DisableLegacyRailgunRotationGuards();
            ResolveRailgunFireClip();
            ResolveReferences();
            EnsureDynamicBodyRig();
            CacheBones();
            EnsureAnimatorIkRelay();
            EnsureWeaponSelectionPanel();
            EnsureWeaponTuningPanel();
        }

        private void OnValidate()
        {
            ApplyRailgunProfileDefaultsIfNeeded();
            DisableLegacyRailgunRotationGuards();
            ResolveRailgunFireClip();

            if (NearlyEqual(weaponLocalEulerAngles, LegacyWeaponEulerAngles))
            {
                weaponLocalEulerAngles = DefaultWeaponEulerAngles;
            }

            if (NearlyEqual(rightGripLocalPosition, LegacyRightGripPosition))
            {
                rightGripLocalPosition = DefaultRightGripPosition;
            }

            if (NearlyEqual(leftGripLocalPosition, LegacyLeftGripPosition))
            {
                leftGripLocalPosition = DefaultLeftGripPosition;
            }

            if (Mathf.Abs(leftHandIkWeight - 0.88f) <= 0.001f)
            {
                leftHandIkWeight = 1f;
            }

            if (Mathf.Abs(genericBoneFallbackWeight - 0.65f) <= 0.001f)
            {
                genericBoneFallbackWeight = 1f;
            }

            shotgunPose3Scale = Mathf.Max(0.01f, shotgunPose3Scale);
            railgunFireMaxPitch = Mathf.Max(railgunFireMinPitch, railgunFireMaxPitch);
            maxArmReachMultiplier = Mathf.Clamp(maxArmReachMultiplier, 0.5f, 1.05f);
            armReachSoftZone = Mathf.Max(0f, armReachSoftZone);
            unreachableRotationWeight = Mathf.Clamp01(unreachableRotationWeight);
            gripTargetBodyCrossAllowance = Mathf.Clamp(gripTargetBodyCrossAllowance, 0f, 0.75f);
            gripTargetBehindBodyAllowance = Mathf.Clamp(gripTargetBehindBodyAllowance, 0f, 0.75f);
            unreachableGripWeightFloor = Mathf.Clamp01(unreachableGripWeightFloor);
            gripContactArmSolveWeight = Mathf.Max(0f, gripContactArmSolveWeight);
            maxWristContactTranslation = Mathf.Clamp(maxWristContactTranslation, 0.01f, 0.5f);
            maxWristRotationFromAnimatedPose = Mathf.Clamp(maxWristRotationFromAnimatedPose, 0f, 90f);
            wristRotationLimitWeight = Mathf.Clamp01(wristRotationLimitWeight);
            experimentalWristOnlyGripRotationWeight = Mathf.Clamp01(experimentalWristOnlyGripRotationWeight);
        }

        private void ApplyRailgunProfileDefaultsIfNeeded()
        {
            if (string.IsNullOrWhiteSpace(referencePoseClipName))
            {
                referencePoseClipName = "SHOTGUNpose3";
            }

            if (string.IsNullOrWhiteSpace(railgunMuzzleTransformName)
                || string.Equals(railgunMuzzleTransformName, "Point.003", StringComparison.Ordinal))
            {
                railgunMuzzleTransformName = RailgunMuzzleTargetName;
            }

            if (railgunMuzzleLocalPosition.sqrMagnitude <= 0.0001f)
            {
                railgunMuzzleLocalPosition = DefaultRailgunMuzzleLocalPosition;
            }

            if (railgunMuzzleLocalEulerAngles.sqrMagnitude <= 0.0001f)
            {
                railgunMuzzleLocalEulerAngles = DefaultRailgunMuzzleLocalEulerAngles;
            }

            if (string.IsNullOrWhiteSpace(referenceShotgunMeshName))
            {
                referenceShotgunMeshName = "SHOTGUN";
            }

            if (string.IsNullOrWhiteSpace(referenceShotgunAnchorName))
            {
                referenceShotgunAnchorName = "SHOTGUNbone";
            }

            if (string.IsNullOrWhiteSpace(referenceRightGripName))
            {
                referenceRightGripName = "HandWrist.R";
            }

            if (string.IsNullOrWhiteSpace(referenceLeftGripName))
            {
                referenceLeftGripName = "HandWrist.L";
            }

            if (railgunProfileRevision >= CurrentRailgunProfileRevision)
            {
                EnsureDefaultWeaponFitPosesIfMissing();
                return;
            }

            if (railgunProfileRevision >= 11)
            {
                ApplyRailgunSocketDefaults();
                ApplyRailgunGripTargetDefaults();
                ApplyRailgunWeaponTransformDefaults();
                ApplyRailgunAnatomyDefaults();
                ApplyDefaultWeaponFitPoses();
                railgunProfileRevision = CurrentRailgunProfileRevision;
                return;
            }

            if (railgunProfileRevision >= 9)
            {
                ApplyRailgunSocketDefaults();
                ApplyRailgunGripTargetDefaults();
                ApplyRailgunWeaponTransformDefaults();
                ApplyRailgunAnatomyDefaults();
                railgunProfileRevision = CurrentRailgunProfileRevision;
                return;
            }

            if (railgunProfileRevision >= 8)
            {
                ApplyRailgunSocketDefaults();
                ApplyRailgunGripTargetDefaults();
                ApplyRailgunWeaponTransformDefaults();
                ApplyRailgunAnatomyDefaults();
                railgunProfileRevision = CurrentRailgunProfileRevision;
                return;
            }

            if (railgunProfileRevision >= 7)
            {
                ApplyRailgunSocketDefaults();
                ApplyRailgunGripTargetDefaults();
                ApplyRailgunWeaponTransformDefaults();
                ApplyRailgunAnatomyDefaults();
                railgunProfileRevision = CurrentRailgunProfileRevision;
                return;
            }

            if (railgunProfileRevision >= 6)
            {
                ApplyRailgunSocketDefaults();
                ApplyRailgunGripTargetDefaults();
                ApplyRailgunWeaponTransformDefaults();
                ApplyRailgunAnatomyDefaults();
                railgunProfileRevision = CurrentRailgunProfileRevision;
                return;
            }

            if (railgunProfileRevision >= 5)
            {
                ApplyRailgunSocketDefaults();
                ApplyRailgunGripTargetDefaults();
                ApplyRailgunWeaponTransformDefaults();
                ApplyRailgunAnatomyDefaults();
                railgunProfileRevision = CurrentRailgunProfileRevision;
                return;
            }

            if (railgunProfileRevision >= 4)
            {
                useReferenceShotgunPrototype = false;
                useShotgunPose3GripReplica = true;
                ApplyRailgunSocketDefaults();
                ApplyRailgunGripTargetDefaults();
                ApplyRailgunWeaponTransformDefaults();
                ApplyRailgunAnatomyDefaults();
                railgunProfileRevision = CurrentRailgunProfileRevision;
                return;
            }

            if (railgunProfileRevision >= 3)
            {
                useReferenceShotgunPrototype = false;
                useShotgunPose3GripReplica = true;
                ApplyRailgunSocketDefaults();
                ApplyRailgunGripTargetDefaults();
                ApplyRailgunWeaponTransformDefaults();
                ApplyRailgunAnatomyDefaults();
                if (shotgunPose3RightGripEulerAngles == Vector3.zero)
                {
                    shotgunPose3RightGripEulerAngles = new Vector3(-106.2f, -23.89999f, -75.70001f);
                }

                if (shotgunPose3LeftGripEulerAngles == Vector3.zero)
                {
                    shotgunPose3LeftGripEulerAngles = new Vector3(-134.6f, 146.4f, -11.4f);
                }

                if (shotgunPose3LeftGripTuningOffset == Vector3.zero)
                {
                    shotgunPose3LeftGripTuningOffset = new Vector3(-0.105261f, 0.15405f, -0.000011f);
                }

                railgunProfileRevision = CurrentRailgunProfileRevision;
                return;
            }

            ApplyRailgunSocketDefaults();

            useReferenceShotgunPrototype = false;
            anchorWeaponByRightGrip = true;
            restRightGripSocketOffset = Vector3.zero;
            raisedRightGripSocketOffset = Vector3.zero;
            useShotgunPose3GripReplica = true;
            shotgunPose3Scale = 0.27f;
            shotgunPose3RightGripTuningOffset = new Vector3(0f, -0.08f, -0.14f);
            shotgunPose3LeftGripTuningOffset = new Vector3(-0.105261f, 0.15405f, -0.000011f);
            shotgunPose3WeaponAnchorTuningOffset = Vector3.zero;
            shotgunPose3RightGripEulerAngles = new Vector3(-106.2f, -23.89999f, -75.70001f);
            shotgunPose3LeftGripEulerAngles = new Vector3(-134.6f, 146.4f, -11.4f);
            ApplyRailgunWeaponTransformDefaults();

            rightGripLocalPosition = new Vector3(0.02f, -0.07f, 0.09f);
            rightGripLocalEulerAngles = new Vector3(0f, 90f, 90f);
            leftGripLocalPosition = new Vector3(-0.07f, -0.06f, 0.34f);
            leftGripLocalEulerAngles = new Vector3(0f, 90f, 90f);

            useAnimatorIk = true;
            rightHandIkWeight = 1f;
            leftHandIkWeight = 1f;
            genericBoneFallbackWeight = 1f;
            applyBoneFallbackForHumanoids = true;
            rightArmReachWeight = 1.1f;
            leftArmReachWeight = 1.5f;
            genericSolveIterations = 6;
            finalGripPositionWeight = 0.58f;
            finalGripRotationWeight = 0.9f;
            handIkBlendSharpness = 18f;
            ApplyRailgunAnatomyDefaults();

            railgunProfileRevision = CurrentRailgunProfileRevision;
        }

        private void ApplyRailgunSocketDefaults()
        {
            restSocketOffset = DefaultRailgunSocketLocalPosition;
            raisedSocketOffset = DefaultRailgunSocketLocalPosition;
            restSocketEulerAngles = DefaultRailgunSocketLocalEulerAngles;
            raisedSocketEulerAngles = DefaultRailgunSocketLocalEulerAngles;
        }

        private void ApplyRailgunGripTargetDefaults()
        {
            useReferenceShotgunPrototype = false;
            useShotgunPose3GripReplica = false;
            anchorWeaponByRightGrip = true;
            restRightGripSocketOffset = Vector3.zero;
            raisedRightGripSocketOffset = Vector3.zero;
            shotgunPose3Scale = Mathf.Max(0.01f, shotgunPose3Scale <= 0f ? 0.27f : shotgunPose3Scale);
            shotgunPose3RightGripTuningOffset = new Vector3(0f, -0.08f, -0.14f);
            shotgunPose3LeftGripTuningOffset = new Vector3(-0.105261f, 0.15405f, -0.000011f);
            shotgunPose3RightGripEulerAngles = new Vector3(-106.2f, -23.89999f, -75.70001f);
            shotgunPose3LeftGripEulerAngles = new Vector3(-134.6f, 146.4f, -11.4f);
        }

        private void ApplyRailgunWeaponTransformDefaults()
        {
            weaponLocalPosition = DefaultRailgunWeaponLocalPosition - ResolvePose3WeaponAnchorSocketOffset();
            weaponLocalEulerAngles = DefaultRailgunWeaponLocalEulerAngles;
            weaponLocalScale = DefaultRailgunWeaponLocalScale;
            railgunMuzzleLocalPosition = DefaultRailgunMuzzleLocalPosition;
            railgunMuzzleLocalEulerAngles = DefaultRailgunMuzzleLocalEulerAngles;
            railgunButtstockLocalPosition = DefaultRailgunButtstockLocalPosition;
            railgunButtstockLocalEulerAngles = DefaultRailgunButtstockLocalEulerAngles;
            selectedWeaponNeutralLocalX = 0f;
        }

        private void ApplyRailgunAnatomyDefaults()
        {
            preferWeaponPrefabAnchors = true;
            useAnimatorIk = true;
            useAnatomyAwareIk = true;
            applyBoneFallbackForHumanoids = false;
            maxArmReachMultiplier = 0.94f;
            armReachSoftZone = 0.16f;
            unreachableRotationWeight = 0.35f;
            solveGripContactLocksThroughArmChain = true;
            gripContactArmSolveWeight = 1.1f;
            limitWristContactTranslation = false;
            maxWristContactTranslation = 0.12f;
            constrainWristRotation = false;
            maxWristRotationFromAnimatedPose = 32f;
            wristRotationLimitWeight = 1f;
            rightArmReachWeight = 0.95f;
            leftArmReachWeight = 0.95f;
            useStateBasedCarryWeights = true;
            locomotionCarryWeight = 0.95f;
            aimedCarryWeight = 1f;
            sprintCarryWeight = 0.86f;
            airborneCarryWeight = 0.72f;
            traversalCarryWeight = 0.5f;
            wallRunCarryWeight = 0.58f;
            rollCarryWeight = 0.18f;
            ApplyDefaultWeaponFitPoses();
        }

        private void DisableLegacyRailgunRotationGuards()
        {
            useReferenceShotgunPrototype = false;
            useShotgunPose3GripReplica = false;
            useWeaponFitPoseSlots = false;
            anchorWeaponByRightGrip = false;
            enableHandIk = false;
            useAnimatorIk = false;
            genericBoneFallbackWeight = 0f;
            applyBoneFallbackForHumanoids = false;
            useExperimentalIndexFingerGripContacts = false;
            useExperimentalFireSolutionAuthority = false;
            rotateExperimentalIndexFingerGripContacts = false;
            muzzleDrivesWeaponAim = false;
            clampMuzzleYawAroundRightGrip = false;
            flattenMuzzleAimToMuzzleHeight = false;
            holdWeaponPoseForSelfAimProtection = false;
            useActionRearAimGate = false;
            useExperimentalDominantSideAimGate = false;
            clampSelectedWeaponLocalX = false;
            experimentalRightGripOwnsWeapon = true;
            experimentalParentWeaponSocketToRightHand = true;
            experimentalRightHandSocketLocalPosition = Vector3.zero;
            experimentalRightHandSocketLocalEulerAngles = Vector3.zero;
            alignRightHandSocketToFingerPocket = true;
            useCharacterWeaponPlacementPoints = false;
            useCharacterRightGripCarryPoints = false;
            experimentalRightPalmOwnsWeaponRotation = false;
            experimentalRightPalmPointsAtTarget = false;
            experimentalRightPalmAimWeight = 0f;
            experimentalRightPalmRotationOwnershipWeight = 0f;
            limitWristContactTranslation = false;
            constrainWristRotation = false;
        }

        private void EnsureDefaultWeaponFitPosesIfMissing()
        {
            if (neutralWeaponFitPose.captured && aimWalkWeaponFitPose.captured && aimRunWeaponFitPose.captured)
            {
                return;
            }

            ApplyDefaultWeaponFitPoses();
        }

        private void ApplyDefaultWeaponFitPoses()
        {
            useWeaponFitPoseSlots = true;
            neutralWeaponFitPose = BuildWeaponFitPose(
                new Vector3(0.021819f, -0.028958f, -0.128823f),
                new Vector3(0f, -126.9507f, -17.69623f),
                DefaultRailgunLeftGripLocalPosition,
                DefaultRailgunLeftGripLocalEulerAngles,
                DefaultRailgunMuzzleLocalPosition);

            aimWalkWeaponFitPose = BuildWeaponFitPose(
                new Vector3(0.004294f, -0.011411f, -0.156161f),
                new Vector3(0.000002f, -150.3324f, -6.163818f),
                new Vector3(0.59f, -0.12f, 0.07f),
                new Vector3(-54.20001f, 149.32f, 172.71f),
                new Vector3(0.96f, 0.14f, 0f));

            aimRunWeaponFitPose = BuildWeaponFitPose(
                new Vector3(0.09f, -0.26f, -0.07f),
                new Vector3(0f, -152.81f, -0.378998f),
                new Vector3(0.61f, -0.18f, 0.04f),
                new Vector3(-90f, -28.00943f, 0f),
                new Vector3(0.96f, 0.14f, 0f));
        }

        private static SCFWeaponFitPose BuildWeaponFitPose(
            Vector3 weaponPosition,
            Vector3 weaponEulerAngles,
            Vector3 leftGripPosition,
            Vector3 leftGripEulerAngles,
            Vector3 muzzlePosition)
        {
            return new SCFWeaponFitPose
            {
                captured = true,
                socket = LocalPose(DefaultRailgunSocketLocalPosition, DefaultRailgunSocketLocalEulerAngles, Vector3.one),
                weapon = LocalPose(weaponPosition, weaponEulerAngles, DefaultRailgunWeaponLocalScale),
                rightGrip = LocalPose(DefaultRailgunRightGripLocalPosition, DefaultRailgunRightGripLocalEulerAngles, Vector3.one),
                leftGrip = LocalPose(leftGripPosition, leftGripEulerAngles, Vector3.one),
                muzzle = LocalPose(muzzlePosition, DefaultRailgunMuzzleLocalEulerAngles, Vector3.one),
                buttstock = LocalPose(DefaultRailgunButtstockLocalPosition, DefaultRailgunButtstockLocalEulerAngles, Vector3.one)
            };
        }

        private static SCFLocalTransformPose LocalPose(Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
        {
            return new SCFLocalTransformPose
            {
                captured = true,
                localPosition = localPosition,
                localEulerAngles = localEulerAngles,
                localScale = localScale
            };
        }

        private void ApplyRigProfileDefaultsIfPresent()
        {
            SCFWeaponRigProfile profile = ResolveRailgunRigProfile();
            if (profile == null)
            {
                return;
            }

            if (profile.FireClip != null)
            {
                railgunFireClip = profile.FireClip;
            }

            SCFWeaponAnchorNames anchors = profile.AnchorNames;
            if (!string.IsNullOrWhiteSpace(anchors.rightGrip))
            {
                rightGripName = anchors.rightGrip;
            }

            if (!string.IsNullOrWhiteSpace(anchors.leftGrip))
            {
                leftGripName = anchors.leftGrip;
            }

            if (!string.IsNullOrWhiteSpace(anchors.muzzle))
            {
                railgunMuzzleTransformName = anchors.muzzle;
            }

            if (!string.IsNullOrWhiteSpace(anchors.buttstock))
            {
                buttstockName = anchors.buttstock;
            }

            SCFWeaponCarryWeights weights = profile.CarryWeights;
            if (weights.aim > 0f || weights.locomotion > 0f)
            {
                locomotionCarryWeight = weights.locomotion;
                aimedCarryWeight = weights.aim;
                sprintCarryWeight = weights.sprint;
                airborneCarryWeight = weights.airborne;
                traversalCarryWeight = weights.traversal;
                wallRunCarryWeight = weights.wallRun;
                rollCarryWeight = weights.roll;
            }

            SCFWeaponAnatomyLimits limits = profile.AnatomyLimits;
            if (limits.maxReachMultiplier > 0f)
            {
                maxArmReachMultiplier = limits.maxReachMultiplier;
                armReachSoftZone = limits.reachSoftZone;
                unreachableRotationWeight = limits.unreachableRotationWeight;
            }
        }

        public void Configure(IsometricCharacterMotor characterMotor, Animator targetAnimator, string characterName)
        {
            ApplyRailgunProfileDefaultsIfNeeded();
            ApplyRigProfileDefaultsIfPresent();
            DisableLegacyRailgunRotationGuards();
            motor = characterMotor;
            animator = targetAnimator;
            activeCharacterName = characterName ?? string.Empty;
            ResolveReferences();
            EnsureDynamicBodyRig();
            CacheBones();
            EnsureAnimatorIkRelay();
            EnsureWeaponSelectionPanel();
            EnsureWeaponTuningPanel();
            EnsureWorldWeaponPickupSpawner();

            if (ShouldEquipRailgun())
            {
                EquipRailgun();
            }
            else
            {
                ClearWeapon();
            }
        }

        public void EquipWeaponPrefab(GameObject weaponPrefab)
        {
            selectedWeaponPrototype = weaponPrefab;
            useReferenceShotgunPrototype = false;

            if (weaponPrefab == null)
            {
                ClearWeapon();
                return;
            }

            ResolveReferences();
            EquipRailgun();
        }

        public void EquipDefaultRailgun()
        {
            selectedWeaponPrototype = null;
            useReferenceShotgunPrototype = false;
            ResolveReferences();
            EquipRailgun();
        }

        public void UnequipWeapon()
        {
            selectedWeaponPrototype = null;
            useReferenceShotgunPrototype = false;
            ClearWeapon();
        }

        public void DropActiveWeapon()
        {
            if (activeWeapon == null)
            {
                return;
            }

            GameObject prototype = ResolveWeaponPrototype();
            Vector3 dropPosition = ResolveWeaponDropPosition();
            Quaternion dropRotation = Quaternion.identity;
            string dropName = prototype != null ? "SCF_Dropped_" + SanitizeObjectName(prototype.name) : "SCF_Dropped_Weapon";

            if (prototype != null)
            {
                SCFWeaponPickup pickup = SCFWeaponPickup.SpawnPickup(prototype, dropPosition, dropRotation, dropName, true, false);
                if (pickup != null)
                {
                    pickup.IgnorePicker(this, droppedWeaponSelfPickupDelay);
                }
            }

            UnequipWeapon();
        }

        public void ApplyAnimatorIK(int layerIndex, Animator sourceAnimator)
        {
            if (!useAnimatorIk)
            {
                return;
            }

            Animator targetAnimator = sourceAnimator != null ? sourceAnimator : animator;
            if (targetAnimator == null || targetAnimator != animator || !targetAnimator.isHuman)
            {
                return;
            }

            if (experimentalPoseAuthoringMode)
            {
                ClearAnimatorIkGoals(targetAnimator);
                return;
            }

            ResolveReferences();
            SolveWeaponTargetsForCurrentFrame();
            UpdateHandIkWeightsForCurrentFrame();

            if (!CanApplyHandIk())
            {
                ClearAnimatorIkGoals(targetAnimator);
                return;
            }

            if (ShouldUseExperimentalIndexGripContacts())
            {
                ClearAnimatorIkGoals(targetAnimator);
                return;
            }

            ApplyHumanoidArmIk(targetAnimator, AvatarIKGoal.RightHand, rightGrip, rightArm, currentRightHandWeight, rightArmReachWeight);
            ApplyHumanoidArmIk(targetAnimator, AvatarIKGoal.LeftHand, leftGrip, leftArm, currentLeftHandWeight, leftArmReachWeight);
        }

        private void LateUpdate()
        {
            ResolveReferences();
            TickAimRayDebugToggle();
            TickWeaponIkLiveLoggerInput();
            TickWeaponDropInput();
            TickExperimentalPoseAuthoringInput();

            if (activeWeapon == null)
            {
                currentRightHandWeight = 0f;
                currentLeftHandWeight = 0f;
                wasInWeaponFitResetAction = false;
                TickAimRayDebug(false);
                TickAimFocusCursor(false);
                TickWeaponIkLiveLogger();
                return;
            }

            if (chestAnchor == null || rightArm.Hand == null || leftArm.Hand == null)
            {
                CacheBones();
            }

            if (experimentalPoseAuthoringMode)
            {
                currentRightHandWeight = 0f;
                currentLeftHandWeight = 0f;
                lastWeaponTargetSolveFrame = -1;
                lastHandWeightUpdateFrame = -1;
                TickRailgunLiveTuningCapture();
                TickRailgunFire();
                TickAimRayDebug(true);
                TickAimFocusCursor(true);
                TickWeaponIkLiveLogger();
                return;
            }

            bodyKeepoutSolver?.RefreshFromBones();
            SolveWeaponTargetsForCurrentFrame();
            UpdateHandIkWeightsForCurrentFrame();

            if (ShouldApplyBoneFallback())
            {
                ApplyGenericArmToGrip(rightArm, rightGrip, currentRightHandWeight, rightArmReachWeight, true);
                ApplyGenericArmToGrip(leftArm, leftGrip, currentLeftHandWeight, leftArmReachWeight, false);
            }

            ApplyExperimentalWeaponGripOwnership();
            ApplyExperimentalGripContactLocks();
            ApplyExperimentalWeaponGripOwnership();

            TickRailgunLiveTuningCapture();
            TickRailgunFire();
            TickAimRayDebug(true);
            TickAimFocusCursor(true);
            TickWeaponIkLiveLogger();
        }

        private void TickWeaponDropInput()
        {
            if (!Application.isPlaying || activeWeapon == null || !WasDropWeaponPressed())
            {
                return;
            }

            DropActiveWeapon();
        }

        private void TickExperimentalPoseAuthoringInput()
        {
            if (!Application.isPlaying || !WasExperimentalPoseAuthoringTogglePressed())
            {
                return;
            }

            SetExperimentalPoseAuthoringMode(!experimentalPoseAuthoringMode);
        }

        private bool WasExperimentalPoseAuthoringTogglePressed()
        {
            if (experimentalPoseAuthoringToggleKey == KeyCode.None)
            {
                return false;
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.Input.GetKeyDown(experimentalPoseAuthoringToggleKey))
            {
                return true;
            }
#endif

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null
                   && experimentalPoseAuthoringToggleKey == KeyCode.F6
                   && keyboard.f6Key.wasPressedThisFrame;
#else
            return false;
#endif
        }

        public void SetExperimentalPoseAuthoringMode(bool enabled)
        {
            if (experimentalPoseAuthoringMode == enabled)
            {
                return;
            }

            experimentalPoseAuthoringMode = enabled;
            if (enabled)
            {
                EnterExperimentalPoseAuthoringMode();
            }
            else
            {
                ExitExperimentalPoseAuthoringMode();
            }

            Debug.Log("SCF experimental pose clay " + (experimentalPoseAuthoringMode ? "enabled" : "disabled") + " (" + experimentalPoseAuthoringToggleKey + ")");
        }

        private void EnterExperimentalPoseAuthoringMode()
        {
            ResolveReferences();
            CacheBones();
            RefreshWeaponTuningTargets();
            currentRightHandWeight = 0f;
            currentLeftHandWeight = 0f;
            lastWeaponTargetSolveFrame = -1;
            lastHandWeightUpdateFrame = -1;

            if (poseAuthoringDisablesAnimator && animator != null && !poseAuthoringAnimatorStateCaptured)
            {
                poseAuthoringAnimatorWasEnabled = animator.enabled;
                poseAuthoringAnimatorSpeed = animator.speed;
                poseAuthoringAnimatorStateCaptured = true;
                animator.speed = 0f;
                animator.enabled = false;
            }

            if (poseAuthoringDisablesPoseScripts)
            {
                poseAuthoringDisabledBehaviours = CollectPoseAuthoringBehavioursToDisable();
                for (int i = 0; i < poseAuthoringDisabledBehaviours.Length; i++)
                {
                    Behaviour behaviour = poseAuthoringDisabledBehaviours[i];
                    if (behaviour != null)
                    {
                        behaviour.enabled = false;
                    }
                }
            }
        }

        private void ExitExperimentalPoseAuthoringMode()
        {
            for (int i = 0; i < poseAuthoringDisabledBehaviours.Length; i++)
            {
                Behaviour behaviour = poseAuthoringDisabledBehaviours[i];
                if (behaviour != null)
                {
                    behaviour.enabled = true;
                }
            }

            poseAuthoringDisabledBehaviours = new Behaviour[0];

            if (poseAuthoringAnimatorStateCaptured && animator != null)
            {
                animator.enabled = poseAuthoringAnimatorWasEnabled;
                animator.speed = poseAuthoringAnimatorSpeed;
            }

            poseAuthoringAnimatorStateCaptured = false;
            lastWeaponTargetSolveFrame = -1;
            lastHandWeightUpdateFrame = -1;
        }

        private Behaviour[] CollectPoseAuthoringBehavioursToDisable()
        {
            Behaviour[] behaviours = GetComponentsInChildren<Behaviour>(true);
            ArrayList result = new ArrayList();
            for (int i = 0; i < behaviours.Length; i++)
            {
                Behaviour behaviour = behaviours[i];
                if (behaviour == null
                    || behaviour == this
                    || !behaviour.enabled
                    || !ShouldDisableForPoseAuthoring(behaviour))
                {
                    continue;
                }

                result.Add(behaviour);
            }

            return (Behaviour[])result.ToArray(typeof(Behaviour));
        }

        private static bool ShouldDisableForPoseAuthoring(Behaviour behaviour)
        {
            string typeName = behaviour.GetType().Name;
            return string.Equals(typeName, "MovementAnimatorBridge", StringComparison.Ordinal)
                   || string.Equals(typeName, "SCFAimBodyDifferentiator", StringComparison.Ordinal)
                   || string.Equals(typeName, "SCFBodyKeepoutSolver", StringComparison.Ordinal)
                   || string.Equals(typeName, "SCFClimbHandContactIK", StringComparison.Ordinal)
                   || string.Equals(typeName, "SCFMxMCombatDriver", StringComparison.Ordinal);
        }

        private bool WasDropWeaponPressed()
        {
            if (dropWeaponKey == KeyCode.None)
            {
                return false;
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.Input.GetKeyDown(dropWeaponKey))
            {
                return true;
            }
#endif

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return dropWeaponKey == KeyCode.G && keyboard != null && keyboard.gKey.wasPressedThisFrame;
#else
            return false;
#endif
        }

        private Vector3 ResolveWeaponDropPosition()
        {
            Vector3 source = activeWeapon != null ? activeWeapon.transform.position : transform.position;
            Vector3 planarForward = motor != null && motor.BodyFacingDirection.sqrMagnitude > 0.0001f
                ? motor.BodyFacingDirection
                : transform.forward;
            planarForward.y = 0f;
            if (planarForward.sqrMagnitude <= 0.0001f)
            {
                planarForward = Vector3.forward;
            }

            Vector3 position = source + planarForward.normalized * 1.35f;
            float groundY = SampleGroundY(position);
            return new Vector3(position.x, groundY, position.z);
        }

        private void SolveWeaponTargetsForCurrentFrame()
        {
            if (lastWeaponTargetSolveFrame == Time.frameCount || activeWeapon == null)
            {
                return;
            }

            lastWeaponTargetSolveFrame = Time.frameCount;
            if (chestAnchor == null || rightArm.Hand == null || leftArm.Hand == null)
            {
                CacheBones();
            }

            TickWeaponFitMobilityActionReset();
            UpdateRaisedBlend();
            UpdateWeaponSocket();
            ApplyWeaponLocalFit();
            if (IsReferenceShotgunInstance())
            {
                AlignReferenceShotgunToSocket();
                CacheReferenceShotgunGrips();
            }
            else
            {
                EnsureGrips();
                EnsureRailgunMuzzleTarget();
                EnsureRailgunButtstockTarget();
                if (!UseShotgunPose3GripReplica())
                {
                    AlignWeaponByRightGrip();
                }
            }

            AlignRightHandSocketToFingerPocket();
            UpdateActionRearAimGateState();
            ApplyRuntimeWeaponFitPoseToTargets();
            ApplyExperimentalWeaponGripOwnership();
            ApplyMuzzleAimAuthority();
            if (experimentalPreserveGripOwnershipDuringMuzzleAim)
            {
                ApplyExperimentalWeaponGripOwnership();
            }
        }

        private void UpdateHandIkWeightsForCurrentFrame()
        {
            if (lastHandWeightUpdateFrame == Time.frameCount)
            {
                return;
            }

            lastHandWeightUpdateFrame = Time.frameCount;
            float blend = 1f - Mathf.Exp(-handIkBlendSharpness * Time.deltaTime);
            bool canApplyIk = CanApplyHandIk();
            float rightCarryWeight = ResolveHandCarryStateWeight(true);
            float leftCarryWeight = ResolveHandCarryStateWeight(false);
            float rightTarget = canApplyIk ? rightHandIkWeight * rightCarryWeight : 0f;
            float leftTarget = canApplyIk ? leftHandIkWeight * leftCarryWeight : 0f;

            if (canApplyIk && UsesExperimentalFireSolutionAuthority())
            {
                rightTarget = Mathf.Max(rightTarget, ResolveExperimentalGripContactLockWeight(true));
                leftTarget = Mathf.Max(leftTarget, ResolveExperimentalGripContactLockWeight(false));
            }

            currentRightHandWeight = Mathf.Lerp(currentRightHandWeight, rightTarget, blend);
            currentLeftHandWeight = Mathf.Lerp(currentLeftHandWeight, leftTarget, blend);

            if (canApplyIk && UsesExperimentalFireSolutionAuthority())
            {
                currentRightHandWeight = Mathf.Max(currentRightHandWeight, rightTarget);
                currentLeftHandWeight = Mathf.Max(currentLeftHandWeight, leftTarget);
            }
        }

        private bool ShouldExperimentalRightPalmOwnWeaponRotation()
        {
            return ShouldUseRightHandWeaponOwnership()
                   && experimentalRightPalmOwnsWeaponRotation
                   && experimentalRightPalmRotationOwnershipWeight > 0.001f;
        }

        [ContextMenu("SCF/Record Weapon IK Live Snapshot")]
        private void RecordWeaponIkLiveSnapshotFromContext()
        {
            RecordWeaponIkLiveSnapshot("context", true);
        }

        [ContextMenu("SCF/Toggle Weapon IK Live Recording")]
        private void ToggleWeaponIkLiveRecordingFromContext()
        {
            weaponIkLiveLoggerRecording = !weaponIkLiveLoggerRecording;
            RecordWeaponIkLiveSnapshot(weaponIkLiveLoggerRecording ? "recording-started-context" : "recording-stopped-context", true);
        }

        private void TickWeaponIkLiveLoggerInput()
        {
            if (!enableWeaponIkLiveLogger || !Application.isPlaying)
            {
                return;
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            if (weaponIkSnapshotKey != KeyCode.None && Input.GetKeyDown(weaponIkSnapshotKey))
            {
                RecordWeaponIkLiveSnapshot("manual-key-" + weaponIkSnapshotKey, true);
            }

            if (weaponIkRecordToggleKey != KeyCode.None && Input.GetKeyDown(weaponIkRecordToggleKey))
            {
                weaponIkLiveLoggerRecording = !weaponIkLiveLoggerRecording;
                nextWeaponIkLiveLogTime = 0f;
                RecordWeaponIkLiveSnapshot(weaponIkLiveLoggerRecording ? "recording-started" : "recording-stopped", true);
                Debug.Log("SCF weapon IK live logger " + (weaponIkLiveLoggerRecording ? "recording" : "stopped") + " (" + weaponIkRecordToggleKey + ")");
            }
#endif
        }

        private void TickWeaponIkLiveLogger()
        {
            if (!enableWeaponIkLiveLogger
                || !Application.isPlaying
                || !weaponIkLiveLoggerRecording
                || Time.unscaledTime < nextWeaponIkLiveLogTime)
            {
                return;
            }

            nextWeaponIkLiveLogTime = Time.unscaledTime + weaponIkLiveLoggerInterval;
            RecordWeaponIkLiveSnapshot("live", false);
        }

        private string RecordWeaponIkLiveSnapshot(string reason, bool timestamped)
        {
            string directory = ResolveWeaponIkLiveSnapshotDirectory();
            string prefix = string.IsNullOrWhiteSpace(weaponIkLiveLoggerFilePrefix) ? "SCF_WeaponIkLive" : weaponIkLiveLoggerFilePrefix.Trim();
            string contents = BuildWeaponIkLiveSnapshot(reason);

            try
            {
                Directory.CreateDirectory(directory);
                string latestPath = Path.Combine(directory, prefix + "_LATEST.txt");
                File.WriteAllText(latestPath, contents, Encoding.UTF8);

                if (timestamped)
                {
                    string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
                    File.WriteAllText(Path.Combine(directory, prefix + "_" + stamp + ".txt"), contents, Encoding.UTF8);
                }

                return latestPath;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("SCF weapon IK live snapshot failed: " + exception.Message);
                return string.Empty;
            }
        }

        private static string ResolveWeaponIkLiveSnapshotDirectory()
        {
            if (Application.isEditor && !string.IsNullOrWhiteSpace(Application.dataPath))
            {
                return Path.Combine(Application.dataPath, "Resources", "SCF", "Weapons", "RuntimeSnapshots");
            }

            return Path.Combine(Application.persistentDataPath, "SCF", "Weapons", "RuntimeSnapshots");
        }

        private string BuildWeaponIkLiveSnapshot(string reason)
        {
            StringBuilder builder = new StringBuilder(8192);
            builder.AppendLine("SCF Weapon IK Live Snapshot");
            builder.AppendLine("Reason: " + reason);
            builder.AppendLine("WallTime: " + DateTime.Now.ToString("O", CultureInfo.InvariantCulture));
            builder.AppendLine("Frame: " + Time.frameCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("Time: " + FormatLiveFloat(Time.time));
            builder.AppendLine("UnscaledTime: " + FormatLiveFloat(Time.unscaledTime));
            builder.AppendLine("Character: " + activeCharacterName);
            builder.AppendLine("Weapon: " + ActiveWeaponName);
            builder.AppendLine("PoseTuningMode: " + weaponFitPoseTuningMode);
            builder.AppendLine("TuningPoseSlot: " + tuningWeaponFitPoseSlot);
            builder.AppendLine("RuntimePoseSlotValid: " + hasRuntimeWeaponFitPoseSlot);
            builder.AppendLine("RuntimePoseSlot: " + lastRuntimeWeaponFitPoseSlot);
            builder.AppendLine("SelfAimPoseHold: " + ShouldHoldWeaponPoseForSelfAimProtection());
            builder.AppendLine("RearAimGateMode: " + activeRearAimGateMode);
            builder.AppendLine("RearAimYaw: " + FormatLiveFloat(activeRearAimYaw));
            builder.AppendLine("Raised01: " + FormatLiveFloat(raised01));
            builder.AppendLine("RightHandWeight: " + FormatLiveFloat(currentRightHandWeight));
            builder.AppendLine("LeftHandWeight: " + FormatLiveFloat(currentLeftHandWeight));
            builder.AppendLine("ExpFireSolutionAuthority: " + UsesExperimentalFireSolutionAuthority());
            builder.AppendLine("ExpRightGripOwnershipWeight: " + FormatLiveFloat(ResolveExperimentalGripOwnershipWeight()));
            builder.AppendLine("ExpRightGripContactLock: " + FormatLiveFloat(ResolveExperimentalGripContactLockWeight(true)));
            builder.AppendLine("ExpLeftGripContactLock: " + FormatLiveFloat(ResolveExperimentalGripContactLockWeight(false)));
            builder.AppendLine("CharacterWeaponPlacementPoint: " + activeCharacterWeaponPlacementPointName);
            builder.AppendLine("CharacterRightGripCarryPoint: " + activeCharacterRightGripCarryPointName);
            builder.AppendLine("LeftReachWeight: " + FormatLiveFloat(leftArmReachWeight));
            builder.AppendLine("ConstrainWristRotation: " + constrainWristRotation);
            builder.AppendLine("MaxWristRotationFromAnimatedPose: " + FormatLiveFloat(maxWristRotationFromAnimatedPose));
            builder.AppendLine("LimitWristContactTranslation: " + limitWristContactTranslation);
            builder.AppendLine("MaxWristContactTranslation: " + FormatLiveFloat(maxWristContactTranslation));
            builder.AppendLine("GripContactArmSolveWeight: " + FormatLiveFloat(gripContactArmSolveWeight));
            AppendMotorLiveState(builder);
            AppendMotionSelectorLiveState(builder);
            AppendMuzzleLiveState(builder);
            AppendTransformLiveState(builder, "WEAPON_SOCKET", weaponSocket);
            AppendTransformLiveState(builder, "ACTIVE_WEAPON", activeWeapon != null ? activeWeapon.transform : null);
            AppendTransformLiveState(builder, "RIGHT_GRIP", rightGrip);
            AppendTransformLiveState(builder, "LEFT_GRIP", leftGrip);
            AppendTransformLiveState(builder, "MUZZLE", railgunMuzzleTarget);
            AppendTransformLiveState(builder, "BUTTSTOCK", buttstockTarget);
            AppendTransformLiveState(builder, "CHEST_ANCHOR", chestAnchor);
            AppendTransformLiveState(builder, "FACE_ANCHOR", faceAnchor);
            AppendArmLiveState(builder, "RIGHT_ARM", rightArm, rightGrip, currentRightHandWeight, rightArmReachWeight);
            AppendArmLiveState(builder, "LEFT_ARM", leftArm, leftGrip, currentLeftHandWeight, leftArmReachWeight);
            return builder.ToString();
        }

        private void AppendMotorLiveState(StringBuilder builder)
        {
            builder.AppendLine();
            builder.AppendLine("[MOTOR]");
            if (motor == null)
            {
                builder.AppendLine("null");
                return;
            }

            builder.AppendLine("MobilityState: " + motor.MobilityState);
            builder.AppendLine("AimHeld: " + motor.AimHeld);
            builder.AppendLine("RunHeld: " + motor.RunHeld);
            builder.AppendLine("SprintHeld: " + motor.SprintHeld);
            builder.AppendLine("WalkHeld: " + motor.WalkHeld);
            builder.AppendLine("WalkToggled: " + motor.WalkToggled);
            builder.AppendLine("MoveInput: " + FormatLiveVector2(motor.MoveInput));
            builder.AppendLine("PlanarVelocity: " + FormatLiveVector(motor.PlanarVelocity) + " speed=" + FormatLiveFloat(motor.PlanarVelocity.magnitude));
            builder.AppendLine("DesiredVelocity: " + FormatLiveVector(motor.DesiredVelocity));
            builder.AppendLine("AimDirection: " + FormatLiveVector(motor.AimDirection));
            builder.AppendLine("BodyFacingDirection: " + FormatLiveVector(motor.BodyFacingDirection));
            builder.AppendLine("HasAimWorldPoint: " + motor.HasAimWorldPoint);
            builder.AppendLine("AimWorldPoint: " + FormatLiveVector(motor.AimWorldPoint));
        }

        private void AppendMotionSelectorLiveState(StringBuilder builder)
        {
            builder.AppendLine();
            builder.AppendLine("[MOTION]");
            SCFMotionSelector selector = GetComponentInParent<SCFMotionSelector>();
            if (selector == null && animator != null)
            {
                selector = animator.GetComponentInParent<SCFMotionSelector>();
            }

            if (selector == null)
            {
                builder.AppendLine("null");
                return;
            }

            builder.AppendLine("SelectedMotionIndex: " + selector.SelectedMotionIndex.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("SelectedMotionName: " + selector.SelectedMotionName);
        }

        private void AppendMuzzleLiveState(StringBuilder builder)
        {
            builder.AppendLine();
            builder.AppendLine("[MUZZLE_AIM]");
            Transform muzzle = ResolveRailgunMuzzleTransform();
            if (muzzle == null)
            {
                builder.AppendLine("null");
                return;
            }

            bool hasTarget = TryResolveMuzzleAimTargetPoint(muzzle, out Vector3 targetPoint);
            builder.AppendLine("HasTarget: " + hasTarget);
            builder.AppendLine("TargetPoint: " + FormatLiveVector(targetPoint));
            builder.AppendLine("MuzzleForward: " + FormatLiveVector(muzzle.forward));
            if (hasTarget)
            {
                Vector3 muzzleToTarget = targetPoint - muzzle.position;
                float dot = muzzle.forward.sqrMagnitude > 0.0001f && muzzleToTarget.sqrMagnitude > 0.0001f
                    ? Vector3.Dot(muzzle.forward.normalized, muzzleToTarget.normalized)
                    : 0f;
                builder.AppendLine("MuzzleToTarget: " + FormatLiveVector(muzzleToTarget) + " distance=" + FormatLiveFloat(muzzleToTarget.magnitude));
                builder.AppendLine("MuzzleForwardDotTarget: " + FormatLiveFloat(dot));
                builder.AppendLine("UsesEyeCarryProtection: " + ShouldUseEyeCarryForMuzzleTarget(muzzle, targetPoint));
            }
        }

        private void AppendArmLiveState(
            StringBuilder builder,
            string label,
            ArmRig rig,
            Transform grip,
            float handWeight,
            float reachWeight)
        {
            builder.AppendLine();
            builder.AppendLine("[" + label + "]");
            builder.AppendLine("HandWeight: " + FormatLiveFloat(handWeight));
            builder.AppendLine("ReachWeight: " + FormatLiveFloat(reachWeight));
            AppendTransformLiveState(builder, label + "_SHOULDER", rig.Shoulder);
            AppendTransformLiveState(builder, label + "_UPPER_ARM", rig.UpperArm);
            AppendTransformLiveState(builder, label + "_FOREARM", rig.Forearm);
            AppendTransformLiveState(builder, label + "_HAND", rig.Hand);
            AppendTransformLiveState(builder, label + "_GRIP_CONTACT", rig.GripContact);
            AppendTransformLiveState(builder, label + "_INDEX_CONTACT", rig.IndexContact);
            AppendTransformLiveState(builder, label + "_THUMB_CONTACT", rig.ThumbContact);

            Transform effector = rig.EndEffector;
            if (effector == null || grip == null)
            {
                builder.AppendLine("ReachDiagnostics: unavailable");
                return;
            }

            Transform root = rig.UpperArm != null ? rig.UpperArm : rig.Shoulder;
            if (root == null)
            {
                builder.AppendLine("ReachDiagnostics: missing root");
                return;
            }

            Vector3 limitedTarget = ResolveReachLimitedTarget(rig, grip.position, reachWeight, out float reach01);
            float armLength = ResolveArmLength(rig);
            float reachScale = Mathf.Clamp(reachWeight, 0.1f, 1f);
            float hardReach = armLength * maxArmReachMultiplier * reachScale;
            float softReach = hardReach + armLength * armReachSoftZone;
            Vector3 rawToGrip = grip.position - root.position;
            Vector3 rootToTarget = limitedTarget - root.position;
            builder.AppendLine("ArmLength: " + FormatLiveFloat(armLength));
            builder.AppendLine("HardReach: " + FormatLiveFloat(hardReach));
            builder.AppendLine("SoftReach: " + FormatLiveFloat(softReach));
            builder.AppendLine("EndEffector: " + effector.name);
            builder.AppendLine("EffectorToGripDistance: " + FormatLiveFloat(Vector3.Distance(effector.position, grip.position)));
            builder.AppendLine("RawRootToGripDistance: " + FormatLiveFloat(rawToGrip.magnitude));
            builder.AppendLine("LimitedRootToTargetDistance: " + FormatLiveFloat(rootToTarget.magnitude));
            builder.AppendLine("Reach01: " + FormatLiveFloat(reach01));
            builder.AppendLine("GripLocalToWeapon: " + FormatLocalTo(activeWeapon != null ? activeWeapon.transform : null, grip));
        }

        private static void AppendTransformLiveState(StringBuilder builder, string label, Transform target)
        {
            builder.AppendLine();
            builder.AppendLine("[" + label + "]");
            if (target == null)
            {
                builder.AppendLine("null");
                return;
            }

            builder.AppendLine("Name: " + target.name);
            builder.AppendLine("Parent: " + (target.parent != null ? target.parent.name : "none"));
            builder.AppendLine("LocalPosition: " + FormatLiveVector(target.localPosition));
            builder.AppendLine("LocalEuler: " + FormatLiveVector(NormalizeEuler(target.localEulerAngles)));
            builder.AppendLine("LocalScale: " + FormatLiveVector(target.localScale));
            builder.AppendLine("WorldPosition: " + FormatLiveVector(target.position));
            builder.AppendLine("WorldEuler: " + FormatLiveVector(NormalizeEuler(target.eulerAngles)));
            builder.AppendLine("Forward: " + FormatLiveVector(target.forward));
            builder.AppendLine("Up: " + FormatLiveVector(target.up));
        }

        private static string FormatLocalTo(Transform root, Transform target)
        {
            if (root == null || target == null)
            {
                return "unavailable";
            }

            return "pos=" + FormatLiveVector(root.InverseTransformPoint(target.position))
                   + " rot=" + FormatLiveVector(NormalizeEuler((Quaternion.Inverse(root.rotation) * target.rotation).eulerAngles));
        }

        private static string FormatLiveVector(Vector3 value)
        {
            return string.Format(CultureInfo.InvariantCulture, "({0:0.#####}, {1:0.#####}, {2:0.#####})", value.x, value.y, value.z);
        }

        private static string FormatLiveVector2(Vector2 value)
        {
            return string.Format(CultureInfo.InvariantCulture, "({0:0.#####}, {1:0.#####})", value.x, value.y);
        }

        private static string FormatLiveFloat(float value)
        {
            return value.ToString("0.#####", CultureInfo.InvariantCulture);
        }

        private void OnDrawGizmosSelected()
        {
            DrawGripGizmo(weaponSocket, Color.yellow, 0.04f);
            DrawGripGizmo(rightGrip, Color.cyan, 0.03f);
            DrawGripGizmo(leftGrip, Color.green, 0.03f);
            DrawGripGizmo(buttstockTarget, Color.magenta, 0.035f);
        }

        private void OnDisable()
        {
            if (!experimentalPoseAuthoringMode)
            {
                return;
            }

            experimentalPoseAuthoringMode = false;
            ExitExperimentalPoseAuthoringMode();
        }

        private void OnDestroy()
        {
            DestroyUnityObject(railgunBeamMaterial);
            DestroyUnityObject(railgunParticleMaterial);
            DestroyUnityObject(aimRayDebugMaterial);
            DestroyUnityObject(aimFocusCursorMaterial);
            DestroyUnityObject(aimRayDebugRoot);
            DestroyUnityObject(aimFocusCursor);
        }

        private void TickAimRayDebugToggle()
        {
            if (!Application.isPlaying || !WasAimRayDebugTogglePressed())
            {
                return;
            }

            showAimRayDebug = !showAimRayDebug;
            Debug.Log("SCF aim ray debug " + (showAimRayDebug ? "enabled" : "disabled") + " (R)");
            if (!showAimRayDebug)
            {
                SetAimRayDebugVisible(false);
            }
        }

        private bool WasAimRayDebugTogglePressed()
        {
            if (aimRayDebugToggleKey == KeyCode.None)
            {
                return false;
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.Input.GetKeyDown(aimRayDebugToggleKey))
            {
                return true;
            }
#endif

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return aimRayDebugToggleKey == KeyCode.R && keyboard != null && keyboard.rKey.wasPressedThisFrame;
#else
            return false;
#endif
        }

        private void TickAimRayDebug(bool canDraw)
        {
            if (!Application.isPlaying || !showAimRayDebug || !canDraw || activeWeapon == null)
            {
                SetAimRayDebugVisible(false);
                return;
            }

            Transform muzzle = ResolveRailgunMuzzleTransform();
            if (muzzle == null)
            {
                SetAimRayDebugVisible(false);
                return;
            }

            EnsureAimRayDebugLines();
            if (aimRayDebugRoot == null)
            {
                return;
            }

            SetAimRayDebugVisible(true);

            Vector3 origin = muzzle.position;
            Vector3 rawMousePoint = motor != null && motor.HasAimWorldPoint
                ? motor.AimWorldPoint
                : origin + ResolveRailgunFireDirection() * railgunFireRange;
            Vector3 focusPoint = ResolveMuzzleAimFocusPoint(muzzle, rawMousePoint, ResolveFaceOrigin());
            Vector3 columnTop;
            Vector3 columnBottom;
            ResolveMouseVerticalDebugRay(rawMousePoint, out columnTop, out columnBottom);
            SetAimRayDebugLine(aimRayMouseColumnLine, columnTop, columnBottom, aimRayMouseColumnColor);
            Debug.DrawLine(columnTop, columnBottom, aimRayMouseColumnColor);

            Transform face = ResolveFaceOrigin();
            if (face != null)
            {
                Vector3 faceEnd = ResolveAimDebugRayEnd(face.position, focusPoint);
                SetAimRayDebugLine(aimRayFaceLine, face.position, faceEnd, aimRayFaceOriginColor);
                Debug.DrawLine(face.position, faceEnd, aimRayFaceOriginColor);
            }
            else
            {
                SetLineEnabled(aimRayFaceLine, false);
            }

            Transform chest = ResolveChestOrigin();
            if (chest != null)
            {
                Vector3 chestEnd = ResolveAimDebugRayEnd(chest.position, focusPoint);
                SetAimRayDebugLine(aimRayChestLine, chest.position, chestEnd, aimRayChestOriginColor);
                Debug.DrawLine(chest.position, chestEnd, aimRayChestOriginColor);
            }
            else
            {
                SetLineEnabled(aimRayChestLine, false);
            }

            Vector3 muzzleFocusEnd = ResolveAimDebugRayEnd(origin, focusPoint);
            SetAimRayDebugLine(aimRayMuzzleToFocusLine, origin, muzzleFocusEnd, aimRayMuzzleToFocusColor);
            Debug.DrawLine(origin, muzzleFocusEnd, aimRayMuzzleToFocusColor);

            Vector3 muzzleDirection = muzzle.forward.sqrMagnitude > 0.0001f ? muzzle.forward.normalized : transform.forward;
            Vector3 muzzleEnd = TryFindRailgunHit(origin, muzzleDirection, out RaycastHit muzzleHit)
                ? muzzleHit.point
                : origin + muzzleDirection * railgunFireRange;

            SetAimRayDebugLine(aimRayMuzzleForwardLine, origin, muzzleEnd, aimRayMuzzleForwardColor);
            Debug.DrawLine(origin, muzzleEnd, aimRayMuzzleForwardColor);
        }

        private Vector3 ResolveMuzzleAimFocusPoint(Transform muzzle, Vector3 rawTargetPoint, Transform faceOrigin)
        {
            Vector3 targetPoint = rawTargetPoint;

            if (useFaceMouseColumnAimFocus && faceOrigin != null)
            {
                targetPoint.y = faceOrigin.position.y + muzzleAimTargetHeightOffset;
            }
            else
            {
                targetPoint.y += muzzleAimTargetHeightOffset;
            }

            return targetPoint;
        }

        private void TickAimFocusCursor(bool canDraw)
        {
            if (!Application.isPlaying
                || !showAimFocusCursor
                || !canDraw
                || activeWeapon == null
                || motor == null
                || !motor.HasAimWorldPoint
                || (aimFocusCursorOnlyWhileAiming && !motor.AimHeld))
            {
                SetAimFocusCursorVisible(false);
                return;
            }

            EnsureAimFocusCursor();
            if (aimFocusCursor == null)
            {
                return;
            }

            Transform muzzle = ResolveRailgunMuzzleTransform();
            Vector3 focusPoint = ResolveMuzzleAimFocusPoint(muzzle, motor.AimWorldPoint, ResolveFaceOrigin());
            aimFocusCursor.transform.position = focusPoint + Vector3.up * aimFocusCursorSurfaceLift;
            aimFocusCursor.transform.rotation = Quaternion.identity;
            aimFocusCursor.transform.localScale = Vector3.one * (Mathf.Max(0.01f, aimFocusCursorRadius) * 2f);

            if (aimFocusCursorRenderer != null)
            {
                aimFocusCursorRenderer.enabled = true;
            }

            aimFocusCursor.SetActive(true);
        }

        private void EnsureAimFocusCursor()
        {
            if (aimFocusCursor != null)
            {
                return;
            }

            aimFocusCursor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            aimFocusCursor.name = AimFocusCursorName;
            aimFocusCursor.hideFlags = HideFlags.DontSave;
            aimFocusCursor.layer = 2;

            Collider markerCollider = aimFocusCursor.GetComponent<Collider>();
            DestroyUnityObject(markerCollider);

            aimFocusCursorRenderer = aimFocusCursor.GetComponent<Renderer>();
            if (aimFocusCursorRenderer != null)
            {
                aimFocusCursorMaterial = CreateRailgunMaterial(aimFocusCursorColor);
                if (aimFocusCursorMaterial != null)
                {
                    aimFocusCursorRenderer.sharedMaterial = aimFocusCursorMaterial;
                }
            }

            aimFocusCursor.SetActive(false);
        }

        private void SetAimFocusCursorVisible(bool visible)
        {
            if (aimFocusCursor != null)
            {
                aimFocusCursor.SetActive(visible);
            }

            if (aimFocusCursorRenderer != null)
            {
                aimFocusCursorRenderer.enabled = visible;
            }
        }

        private Vector3 ResolveAimDebugRayEnd(Vector3 start, Vector3 focusPoint)
        {
            Vector3 direction = focusPoint - start;
            float distance = direction.magnitude;
            if (distance <= 0.001f)
            {
                return focusPoint;
            }

            RaycastHit[] hits = Physics.RaycastAll(start, direction / distance, distance, railgunHitMask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
            {
                return focusPoint;
            }

            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                if (IsOwnRailgunHit(hits[i].transform))
                {
                    continue;
                }

                return hits[i].point;
            }

            return focusPoint;
        }

        private void ResolveMouseVerticalDebugRay(Vector3 rawMousePoint, out Vector3 columnTop, out Vector3 columnBottom)
        {
            columnTop = rawMousePoint + Vector3.up * aimRayMouseColumnHeight;
            Vector3 fallbackBottom = rawMousePoint + Vector3.down * aimRayMouseColumnDepth;
            columnBottom = fallbackBottom;

            float distance = aimRayMouseColumnHeight + aimRayMouseColumnDepth;
            RaycastHit[] hits = Physics.RaycastAll(columnTop, Vector3.down, distance, railgunHitMask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
            {
                return;
            }

            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                if (IsOwnRailgunHit(hits[i].transform))
                {
                    continue;
                }

                columnBottom = hits[i].point;
                return;
            }
        }

        private Transform ResolveFaceOrigin()
        {
            if (faceAnchor == null && animator != null)
            {
                CacheBones();
            }

            return FirstNonNull(faceAnchor, chestAnchor, animator != null ? animator.transform : null, transform);
        }

        private Transform ResolveChestOrigin()
        {
            if (chestAnchor == null && animator != null)
            {
                CacheBones();
            }

            return FirstNonNull(chestAnchor, animator != null ? animator.transform : null, transform);
        }

        private void EnsureAimRayDebugLines()
        {
            if (aimRayDebugRoot == null)
            {
                aimRayDebugRoot = new GameObject(AimRayDebugRootName);
                aimRayDebugRoot.hideFlags = HideFlags.DontSave;
            }

            aimRayFaceLine = EnsureAimRayDebugLine(aimRayFaceLine, AimRayFaceLineName, aimRayFaceOriginColor);
            aimRayChestLine = EnsureAimRayDebugLine(aimRayChestLine, AimRayChestLineName, aimRayChestOriginColor);
            aimRayMuzzleToFocusLine = EnsureAimRayDebugLine(aimRayMuzzleToFocusLine, AimRayMuzzleToFocusLineName, aimRayMuzzleToFocusColor);
            aimRayMuzzleForwardLine = EnsureAimRayDebugLine(aimRayMuzzleForwardLine, AimRayMuzzleForwardLineName, aimRayMuzzleForwardColor);
            aimRayMouseColumnLine = EnsureAimRayDebugLine(aimRayMouseColumnLine, AimRayMouseColumnLineName, aimRayMouseColumnColor);
        }

        private LineRenderer EnsureAimRayDebugLine(LineRenderer line, string lineName, Color color)
        {
            if (line != null)
            {
                return line;
            }

            GameObject lineObject = new GameObject(lineName);
            lineObject.hideFlags = HideFlags.DontSave;
            lineObject.transform.SetParent(aimRayDebugRoot.transform, false);
            line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.material = ResolveAimRayDebugMaterial();
            line.startWidth = aimRayDebugWidth;
            line.endWidth = aimRayDebugWidth;
            line.startColor = color;
            line.endColor = color;
            line.numCapVertices = 4;
            line.numCornerVertices = 1;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            return line;
        }

        private void SetAimRayDebugLine(LineRenderer line, Vector3 start, Vector3 end, Color color)
        {
            if (line == null)
            {
                return;
            }

            line.enabled = true;
            line.startWidth = aimRayDebugWidth;
            line.endWidth = aimRayDebugWidth;
            line.startColor = color;
            line.endColor = color;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
        }

        private void SetAimRayDebugVisible(bool visible)
        {
            if (aimRayDebugRoot != null)
            {
                aimRayDebugRoot.SetActive(visible);
            }

            SetLineEnabled(aimRayFaceLine, visible);
            SetLineEnabled(aimRayChestLine, visible);
            SetLineEnabled(aimRayMuzzleToFocusLine, visible);
            SetLineEnabled(aimRayMuzzleForwardLine, visible);
            SetLineEnabled(aimRayMouseColumnLine, visible);
        }

        private static void SetLineEnabled(LineRenderer line, bool enabled)
        {
            if (line != null)
            {
                line.enabled = enabled;
            }
        }

        private void ResolveReferences()
        {
            if (input == null)
            {
                input = GetComponent<IsometricPlayerInput>();
            }

            if (motor == null)
            {
                motor = GetComponent<IsometricCharacterMotor>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }
        }

        private void EnsureDynamicBodyRig()
        {
            if (!useDynamicCharacterBodyRig)
            {
                bodyKeepoutSolver = null;
                return;
            }

            if (bodyKeepoutSolver == null)
            {
                bodyKeepoutSolver = GetComponent<SCFBodyKeepoutSolver>();
            }

            if (bodyKeepoutSolver == null)
            {
                bodyKeepoutSolver = gameObject.AddComponent<SCFBodyKeepoutSolver>();
            }

            bodyKeepoutSolver.Configure(animator);
        }

        private void EnsureAnimatorIkRelay()
        {
            if (animator == null)
            {
                return;
            }

            SCFWeaponIkRelay relay = animator.GetComponent<SCFWeaponIkRelay>();
            if (relay == null)
            {
                relay = animator.gameObject.AddComponent<SCFWeaponIkRelay>();
            }

            relay.enabled = true;
            relay.Configure(this);
        }

        private void EnsureWeaponSelectionPanel()
        {
            SCFWeaponSelectionPanel panel = GetComponent<SCFWeaponSelectionPanel>();
            if (panel == null)
            {
                panel = gameObject.AddComponent<SCFWeaponSelectionPanel>();
            }

            panel.Configure(this);
        }

        private void EnsureWeaponTuningPanel()
        {
            SCFWeaponTuningPanel panel = GetComponent<SCFWeaponTuningPanel>();
            if (panel == null)
            {
                panel = gameObject.AddComponent<SCFWeaponTuningPanel>();
            }

            panel.Configure(this);
        }

        private void EnsureWorldWeaponPickupSpawner()
        {
            if (!spawnDefaultRailgunPickup)
            {
                return;
            }

            SCFWorldWeaponPickupSpawner spawner = GetComponent<SCFWorldWeaponPickupSpawner>();
            if (spawner == null)
            {
                spawner = gameObject.AddComponent<SCFWorldWeaponPickupSpawner>();
            }

            spawner.Configure(ResolveRailgunPrototype());
        }

        private bool ShouldEquipRailgun()
        {
            return autoEquipRailgunOnConfigure
                   && equipRailgunOnSoldier
                   && animator != null
                   && HasSoldierVisualIdentity()
                   && ResolveWeaponPrototype() != null;
        }

        private void EquipRailgun()
        {
            GameObject prototype = ResolveWeaponPrototype();
            if (prototype == null || animator == null)
            {
                ClearWeapon();
                return;
            }

            CacheBones();
            EnsureWeaponSocket();
            if (weaponSocket == null)
            {
                ClearWeapon();
                return;
            }

            string weaponInstanceName = IsUsingReferenceShotgun()
                ? "SCF_Selected_ReferenceShotgun"
                : "SCF_Selected_" + SanitizeObjectName(prototype.name);
            if (activeWeapon != null && string.Equals(activeWeapon.name, weaponInstanceName, StringComparison.Ordinal))
            {
                activeWeaponSupportsRailgunFire = IsRailgunWeapon(prototype);
                ResetWeaponTransform();
                return;
            }

            ClearWeaponInstanceOnly();
            activeWeapon = Instantiate(prototype, weaponSocket);
            activeWeapon.name = weaponInstanceName;
            activeWeaponSupportsRailgunFire = IsRailgunWeapon(prototype);
            EnsureTuningHandle(activeWeapon);
            ResetWeaponTransform();
            if (IsUsingReferenceShotgun())
            {
                PrepareReferenceShotgunInstance();
            }

            StripColliders(activeWeapon);
        }

        private GameObject ResolveWeaponPrototype()
        {
            GameObject reference = IsUsingReferenceShotgun() ? ResolveReferenceShotgunPrototype() : null;
            return reference != null ? reference : ResolveRailgunPrototype();
        }

        private bool IsUsingReferenceShotgun()
        {
            return useReferenceShotgunPrototype;
        }

        private GameObject ResolveRailgunPrototype()
        {
            if (selectedWeaponPrototype != null)
            {
                return selectedWeaponPrototype;
            }

            SCFWeaponRuntimeCatalog catalog = ResolveRuntimeWeaponCatalog();
            GameObject cleanPrefab = catalog != null ? catalog.CleanRailgunWeaponPrefab : null;
            if (cleanPrefab != null)
            {
                railgunPrototype = cleanPrefab;
                return cleanPrefab;
            }

#if UNITY_EDITOR
            cleanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CleanRailgunPrefabPath);
            if (cleanPrefab != null)
            {
                railgunPrototype = cleanPrefab;
                return cleanPrefab;
            }
#endif

            if (catalog != null && catalog.RailgunWeaponPrefab != null)
            {
                railgunPrototype = catalog.RailgunWeaponPrefab;
                return railgunPrototype;
            }

            SCFWeaponRigProfile rigProfile = ResolveRailgunRigProfile();
            if (preferWeaponPrefabAnchors && rigProfile != null && rigProfile.WeaponPrefab != null)
            {
                railgunPrototype = rigProfile.WeaponPrefab;
                return railgunPrototype;
            }

            if (railgunPrototype != null)
            {
                return railgunPrototype;
            }

#if UNITY_EDITOR
            railgunPrototype = AssetDatabase.LoadAssetAtPath<GameObject>(PrototypeRailgunPath);
#endif
            return railgunPrototype;
        }

        private GameObject ResolveReferenceShotgunPrototype()
        {
            if (referenceShotgunPrototype != null)
            {
                return referenceShotgunPrototype;
            }

#if UNITY_EDITOR
            referenceShotgunPrototype = AssetDatabase.LoadAssetAtPath<GameObject>(ReferenceShotgunPath);
#endif
            return referenceShotgunPrototype;
        }

        private AudioClip ResolveRailgunFireClip()
        {
            if (railgunFireClip != null)
            {
                return railgunFireClip;
            }

            SCFWeaponRigProfile rigProfile = ResolveRailgunRigProfile();
            if (rigProfile != null && rigProfile.FireClip != null)
            {
                railgunFireClip = rigProfile.FireClip;
                return railgunFireClip;
            }

            SCFWeaponRuntimeCatalog catalog = ResolveRuntimeWeaponCatalog();
            if (catalog != null && catalog.RailgunFireClip != null)
            {
                railgunFireClip = catalog.RailgunFireClip;
                return railgunFireClip;
            }

#if UNITY_EDITOR
            railgunFireClip = AssetDatabase.LoadAssetAtPath<AudioClip>(RailgunFireClipPath);
#endif
            return railgunFireClip;
        }

        private SCFWeaponRigProfile ResolveRailgunRigProfile()
        {
            if (railgunRigProfile != null)
            {
                return railgunRigProfile;
            }

            SCFWeaponRuntimeCatalog catalog = ResolveRuntimeWeaponCatalog();
            if (catalog != null && catalog.RailgunRigProfile != null)
            {
                railgunRigProfile = catalog.RailgunRigProfile;
                return railgunRigProfile;
            }

            railgunRigProfile = Resources.Load<SCFWeaponRigProfile>(DefaultRailgunRigProfilePath);
            return railgunRigProfile;
        }

        private SCFWeaponRuntimeCatalog ResolveRuntimeWeaponCatalog()
        {
            if (runtimeWeaponCatalog != null)
            {
                return runtimeWeaponCatalog;
            }

            runtimeWeaponCatalog = Resources.Load<SCFWeaponRuntimeCatalog>(RuntimeWeaponCatalogPath);
            return runtimeWeaponCatalog;
        }

        private void TickRailgunFire()
        {
            if (!Application.isPlaying
                || !enableRailgunFire
                || input == null
                || activeWeapon == null
                || !activeWeaponSupportsRailgunFire)
            {
                return;
            }

            bool requested = input.AttackPressedThisFrame || (fireRailgunWhileHeld && input.AttackHeld);
            if (!requested || Time.time < nextRailgunFireTime || ShouldBlockRailgunFireForTraversal())
            {
                return;
            }

            FireRailgun();
            nextRailgunFireTime = Time.time + railgunFireCooldown;
        }

        private void FireRailgun()
        {
            Vector3 direction = ResolveRailgunFireDirection();
            Vector3 muzzle = ResolveRailgunMuzzlePosition(direction);
            Vector3 impact = TryFindRailgunHit(muzzle, direction, out RaycastHit hit)
                ? hit.point
                : muzzle + direction * railgunFireRange;

            SpawnRailgunBeam(muzzle, impact);
            SpawnRailgunCorkscrewTracer(muzzle, impact);
            if (hit.collider != null)
            {
                SpawnRailgunImpact(hit.point, hit.normal);
            }

            PlayRailgunFireSound(muzzle);
            RailgunFired?.Invoke(new SCFRailgunShot(muzzle, direction, impact, hit.collider, hit.normal));
        }

        private Vector3 ResolveRailgunFireDirection()
        {
            Vector3 direction = Vector3.zero;
            Transform muzzleTransform = ResolveRailgunMuzzleTransform();
            if (muzzleTransform != null)
            {
                direction = muzzleTransform.forward;
            }

            if (direction.sqrMagnitude <= 0.0001f && activeWeapon != null)
            {
                direction = activeWeapon.transform.forward;
            }

            if (direction.sqrMagnitude <= 0.0001f && motor != null && motor.HasAimDirection)
            {
                direction = motor.AimDirection;
            }

            if (direction.sqrMagnitude <= 0.0001f && weaponSocket != null)
            {
                direction = weaponSocket.forward;
            }

            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = transform.forward;
            }

            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.forward;
            }

            return direction.normalized;
        }

        private Vector3 ResolveRailgunMuzzlePosition(Vector3 direction)
        {
            Transform muzzleTransform = ResolveRailgunMuzzleTransform();
            if (muzzleTransform != null)
            {
                return muzzleTransform.position + direction * railgunMuzzleForwardOffset;
            }

            if (activeWeapon == null)
            {
                return transform.position + Vector3.up * 1.1f + direction * 0.35f;
            }

            return ResolveFallbackRailgunMuzzlePosition(direction);
        }

        private Vector3 ResolveFallbackRailgunMuzzlePosition(Vector3 direction)
        {
            Renderer[] renderers = activeWeapon.GetComponentsInChildren<Renderer>(true);
            Bounds combinedBounds = default;
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combinedBounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasBounds)
            {
                return activeWeapon.transform.position + direction * (0.35f + railgunMuzzleForwardOffset);
            }

            Vector3 extents = combinedBounds.extents;
            float forwardReach = Mathf.Abs(direction.x) * extents.x + Mathf.Abs(direction.z) * extents.z;
            Vector3 muzzlePoint = combinedBounds.center + direction * forwardReach;
            return muzzlePoint + direction * railgunMuzzleForwardOffset;
        }

        private Transform ResolveRailgunMuzzleTransform()
        {
            if (activeWeapon == null || string.IsNullOrWhiteSpace(railgunMuzzleTransformName))
            {
                return null;
            }

            if (railgunMuzzleTarget != null && railgunMuzzleTarget.IsChildOf(activeWeapon.transform))
            {
                return railgunMuzzleTarget;
            }

            railgunMuzzleTarget = FindWeaponPrefabAnchor(railgunMuzzleTransformName);
            return railgunMuzzleTarget;
        }

        private Transform ResolveRailgunButtstockTransform()
        {
            if (activeWeapon == null)
            {
                return null;
            }

            if (buttstockTarget != null && buttstockTarget.IsChildOf(activeWeapon.transform))
            {
                return buttstockTarget;
            }

            buttstockTarget = FindRailgunButtstockAnchor(false);
            return buttstockTarget;
        }

        private bool TryFindRailgunHit(Vector3 muzzle, Vector3 direction, out RaycastHit hit)
        {
            hit = default;
            Vector3 origin = muzzle + direction * railgunRaycastStartOffset;
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, railgunFireRange, railgunHitMask, QueryTriggerInteraction.Collide);
            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                if (IsOwnRailgunHit(hits[i].transform))
                {
                    continue;
                }

                hit = hits[i];
                return true;
            }

            return false;
        }

        private bool IsOwnRailgunHit(Transform hitTransform)
        {
            return hitTransform != null
                   && (hitTransform == transform
                       || hitTransform.IsChildOf(transform)
                       || (activeWeapon != null && hitTransform.IsChildOf(activeWeapon.transform)));
        }

        private void SpawnRailgunBeam(Vector3 start, Vector3 end)
        {
            GameObject beamObject = new GameObject("SCF_RailgunBeam");
            LineRenderer line = beamObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.material = ResolveRailgunBeamMaterial();
            line.startWidth = railgunBeamWidth;
            line.endWidth = railgunBeamWidth * 0.42f;
            line.startColor = railgunBeamCoreColor;
            line.endColor = railgunBeamColor;
            line.numCapVertices = 6;
            line.numCornerVertices = 2;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            StartCoroutine(FadeRailgunBeam(line, beamObject));
        }

        private IEnumerator FadeRailgunBeam(LineRenderer line, GameObject beamObject)
        {
            float age = 0f;
            float startWidth = line != null ? line.startWidth : railgunBeamWidth;
            float endWidth = line != null ? line.endWidth : railgunBeamWidth * 0.42f;
            while (line != null && age < railgunBeamLifetime)
            {
                float t = Mathf.Clamp01(age / Mathf.Max(0.001f, railgunBeamLifetime));
                float alpha = 1f - t;
                line.startWidth = Mathf.Lerp(startWidth, 0.001f, t);
                line.endWidth = Mathf.Lerp(endWidth, 0.001f, t);
                line.startColor = WithAlpha(railgunBeamCoreColor, railgunBeamCoreColor.a * alpha);
                line.endColor = WithAlpha(railgunBeamColor, railgunBeamColor.a * alpha);
                age += Time.deltaTime;
                yield return null;
            }

            DestroyUnityObject(beamObject);
        }

        private void SpawnRailgunCorkscrewTracer(Vector3 start, Vector3 end)
        {
            Vector3 direction = end - start;
            float distance = direction.magnitude;
            if (distance <= 0.001f)
            {
                return;
            }

            GameObject tracerObject = new GameObject("SCF_RailgunCorkscrewTracer");
            tracerObject.transform.SetPositionAndRotation(start, Quaternion.LookRotation(direction.normalized, Vector3.up));
            ParticleSystem particles = tracerObject.AddComponent<ParticleSystem>();
            ConfigureRailgunTracerParticles(particles);
            particles.Play(true);

            float travelTime = Mathf.Clamp(distance / Mathf.Max(1f, railgunTracerProjectileSpeed), 0.015f, railgunTracerMaxTravelTime);
            StartCoroutine(MoveRailgunTracer(tracerObject.transform, particles, start, end, travelTime));
        }

        private void ConfigureRailgunTracerParticles(ParticleSystem particles)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particles.main;
            main.duration = Mathf.Max(0.05f, railgunTracerMaxTravelTime);
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(railgunTracerParticleLifetime * 0.65f, railgunTracerParticleLifetime);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(railgunTracerParticleSize * 0.65f, railgunTracerParticleSize);
            main.startColor = railgunBeamColor;
            main.maxParticles = 1800;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = railgunTracerEmissionPerDistance;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = railgunTracerRadius;
            shape.radiusThickness = 0f;
            shape.arc = 360f;
            shape.arcMode = ParticleSystemShapeMultiModeValue.Loop;
            shape.arcSpeed = railgunTracerArcSpeed;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(railgunBeamCoreColor, 0f),
                    new GradientColorKey(railgunBeamColor, 0.35f),
                    new GradientColorKey(railgunBeamColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(railgunBeamColor.a, 0f),
                    new GradientAlphaKey(railgunBeamColor.a * 0.72f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = ResolveRailgunParticleMaterial();
            renderer.sortingFudge = 2f;
        }

        private IEnumerator MoveRailgunTracer(Transform tracer, ParticleSystem particles, Vector3 start, Vector3 end, float travelTime)
        {
            float age = 0f;
            while (tracer != null && age < travelTime)
            {
                float t = Mathf.Clamp01(age / Mathf.Max(0.001f, travelTime));
                tracer.position = Vector3.Lerp(start, end, t);
                age += Time.deltaTime;
                yield return null;
            }

            if (tracer != null)
            {
                tracer.position = end;
            }

            if (particles != null)
            {
                particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            yield return new WaitForSeconds(railgunTracerParticleLifetime + 0.08f);
            if (tracer != null)
            {
                DestroyUnityObject(tracer.gameObject);
            }
        }

        private void SpawnRailgunImpact(Vector3 position, Vector3 normal)
        {
            GameObject impactObject = new GameObject("SCF_RailgunImpact");
            Vector3 forward = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;
            impactObject.transform.SetPositionAndRotation(position + forward * 0.02f, Quaternion.LookRotation(forward, Vector3.up));
            ParticleSystem particles = impactObject.AddComponent<ParticleSystem>();
            ConfigureRailgunImpactParticles(particles);
            particles.Play(true);
            Destroy(impactObject, railgunTracerParticleLifetime + 0.35f);
        }

        private void ConfigureRailgunImpactParticles(ParticleSystem particles)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.08f;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(railgunTracerParticleSize, railgunTracerParticleSize * 3.2f);
            main.startColor = railgunBeamCoreColor;
            main.maxParticles = 96;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            short burstCount = (short)Mathf.Clamp(railgunImpactBurstCount, 0, short.MaxValue);
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burstCount) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.06f;

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = ResolveRailgunParticleMaterial();
            renderer.sortingFudge = 3f;
        }

        private void PlayRailgunFireSound(Vector3 position)
        {
            AudioClip clip = ResolveRailgunFireClip();
            if (clip == null)
            {
                return;
            }

            GameObject audioObject = new GameObject("SCF_RailgunFireAudio");
            audioObject.transform.position = position;
            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = railgunFireVolume;
            source.spatialBlend = railgunFireSpatialBlend;
            source.pitch = UnityEngine.Random.Range(railgunFireMinPitch, railgunFireMaxPitch);
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 2f;
            source.maxDistance = 45f;
            source.Play();
            Destroy(audioObject, Mathf.Max(0.1f, clip.length / Mathf.Max(0.1f, Mathf.Abs(source.pitch))) + 0.1f);
        }

        private Material ResolveRailgunBeamMaterial()
        {
            if (railgunBeamMaterial == null)
            {
                railgunBeamMaterial = CreateRailgunMaterial(railgunBeamCoreColor);
            }

            return railgunBeamMaterial;
        }

        private Material ResolveRailgunParticleMaterial()
        {
            if (railgunParticleMaterial == null)
            {
                railgunParticleMaterial = CreateRailgunMaterial(railgunBeamColor);
            }

            return railgunParticleMaterial;
        }

        private Material ResolveAimRayDebugMaterial()
        {
            if (aimRayDebugMaterial == null)
            {
                aimRayDebugMaterial = CreateRailgunMaterial(Color.white);
            }

            return aimRayDebugMaterial;
        }

        private static Material CreateRailgunMaterial(Color color)
        {
            Shader shader = Shader.Find("Particles/Standard Unlit")
                            ?? Shader.Find("Sprites/Default")
                            ?? Shader.Find("HDRP/Unlit")
                            ?? Shader.Find("Universal Render Pipeline/Unlit")
                            ?? Shader.Find("Unlit/Color")
                            ?? Shader.Find("Standard");
            if (shader == null)
            {
                return null;
            }

            Material material = new Material(shader);
            material.hideFlags = HideFlags.DontSave;
            material.renderQueue = 3000;
            SetMaterialColor(material, color);
            return material;
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_EmissiveColor"))
            {
                material.SetColor("_EmissiveColor", color * 2f);
            }
        }

        private void CacheBones()
        {
            chestAnchor = null;
            faceAnchor = null;
            rightArm = default;
            leftArm = default;

            if (animator == null)
            {
                return;
            }

            EnsureDynamicBodyRig();

            if (animator.avatar != null && animator.isHuman)
            {
                chestAnchor = FirstNonNull(
                    animator.GetBoneTransform(HumanBodyBones.UpperChest),
                    animator.GetBoneTransform(HumanBodyBones.Chest),
                    animator.GetBoneTransform(HumanBodyBones.Spine),
                    animator.GetBoneTransform(HumanBodyBones.Hips),
                    animator.transform);

                faceAnchor = FirstNonNull(
                    animator.GetBoneTransform(HumanBodyBones.Head),
                    animator.GetBoneTransform(HumanBodyBones.Neck),
                    chestAnchor,
                    animator.transform);

                rightArm.Shoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
                rightArm.UpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                rightArm.Forearm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
                rightArm.Hand = animator.GetBoneTransform(HumanBodyBones.RightHand);

                leftArm.Shoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
                leftArm.UpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                leftArm.Forearm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                leftArm.Hand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                AssignGripContactBones(animator.transform);
                CacheCharacterWeaponPlacementPoints();
                CacheCharacterRightGripCarryPoints();
                return;
            }

            Transform root = animator.transform;
            chestAnchor = FirstNonNull(
                FindFirstBone(root, "upperchest", "upper_chest", "spine2", "spine02", "spine_02", "spine 2", "ccbasespine02"),
                FindFirstBone(root, "chest", "spine1", "spine01", "spine_01", "spine 1", "ccbasespine01"),
                FindFirstBone(root, "waist", "spine", "ccbasespine"),
                root);

            faceAnchor = FirstNonNull(
                FindFirstBone(root, "head", "ccbasehead"),
                FindFirstBone(root, "neck", "necktwist", "ccbaseneck"),
                chestAnchor,
                root);

            rightArm.Shoulder = FindFirstBone(root, "rightshoulder", "rshoulder", "shoulder_r", "r_shoulder", "rightclavicle", "rclavicle", "clavicle_r", "r_clavicle", "ccbaserclavicle");
            rightArm.UpperArm = FindFirstBone(root, "rightupperarm", "rupperarm", "upperarm_r", "r_upperarm", "ccbaserupperarm", "rightarm");
            rightArm.Forearm = FindFirstBone(root, "rightforearm", "rightlowerarm", "rforearm", "rlowerarm", "forearm_r", "lowerarm_r", "r_forearm", "r_lowerarm", "ccbaserforearm");
            rightArm.Hand = FindFirstBone(root, "righthand", "rhand", "hand_r", "r_hand", "ccbaserhand");

            leftArm.Shoulder = FindFirstBone(root, "leftshoulder", "lshoulder", "shoulder_l", "l_shoulder", "leftclavicle", "lclavicle", "clavicle_l", "l_clavicle", "ccbaselclavicle");
            leftArm.UpperArm = FindFirstBone(root, "leftupperarm", "lupperarm", "upperarm_l", "l_upperarm", "ccbaselupperarm", "leftarm");
            leftArm.Forearm = FindFirstBone(root, "leftforearm", "leftlowerarm", "lforearm", "llowerarm", "forearm_l", "lowerarm_l", "l_forearm", "l_lowerarm", "ccbaselforearm");
            leftArm.Hand = FindFirstBone(root, "lefthand", "lhand", "hand_l", "l_hand", "ccbaselhand");
            AssignGripContactBones(root);
            CacheCharacterWeaponPlacementPoints();
            CacheCharacterRightGripCarryPoints();
        }

        private void CacheCharacterWeaponPlacementPoints()
        {
            idleWeaponPlacementPoint = null;
            hipAimWeaponPlacementPoint = null;
            shoulderAimWeaponPlacementPoint = null;
            runWeaponPlacementPoint = null;
            sprintWeaponPlacementPoint = null;
            oneHandWeaponPlacementPoint = null;

            if (!useCharacterWeaponPlacementPoints || animator == null)
            {
                return;
            }

            if (useGeneratedCharacterCarryPoints && bodyKeepoutSolver != null)
            {
                idleWeaponPlacementPoint = bodyKeepoutSolver.IdleWeaponPoint;
                hipAimWeaponPlacementPoint = bodyKeepoutSolver.HipWeaponPoint;
                shoulderAimWeaponPlacementPoint = bodyKeepoutSolver.ChestWeaponPoint;
                runWeaponPlacementPoint = bodyKeepoutSolver.RunWeaponPoint;
                sprintWeaponPlacementPoint = bodyKeepoutSolver.SprintWeaponPoint;
                oneHandWeaponPlacementPoint = bodyKeepoutSolver.OneHandWeaponPoint;
                if (HasAnyCharacterWeaponPlacementPoint())
                {
                    return;
                }
            }

            Transform root = animator.transform;
            idleWeaponPlacementPoint = FindCarryPoint(root, idleWeaponPlacementPointName);
            hipAimWeaponPlacementPoint = FindCarryPoint(root, hipAimWeaponPlacementPointName);
            shoulderAimWeaponPlacementPoint = FindCarryPoint(root, shoulderAimWeaponPlacementPointName);
            runWeaponPlacementPoint = FindCarryPoint(root, runWeaponPlacementPointName);
            sprintWeaponPlacementPoint = FindCarryPoint(root, sprintWeaponPlacementPointName);
            oneHandWeaponPlacementPoint = FindCarryPoint(root, oneHandWeaponPlacementPointName);
        }

        private void CacheCharacterRightGripCarryPoints()
        {
            idleRightGripCarryPoint = null;
            hipAimRightGripCarryPoint = null;
            shoulderAimRightGripCarryPoint = null;
            runRightGripCarryPoint = null;
            sprintRightGripCarryPoint = null;
            oneHandRightGripCarryPoint = null;

            if (!useCharacterRightGripCarryPoints || animator == null)
            {
                return;
            }

            if (useGeneratedCharacterCarryPoints && bodyKeepoutSolver != null)
            {
                idleRightGripCarryPoint = bodyKeepoutSolver.IdleRightGripPoint;
                hipAimRightGripCarryPoint = bodyKeepoutSolver.HipRightGripPoint;
                shoulderAimRightGripCarryPoint = bodyKeepoutSolver.ChestRightGripPoint;
                runRightGripCarryPoint = bodyKeepoutSolver.RunRightGripPoint;
                sprintRightGripCarryPoint = bodyKeepoutSolver.SprintRightGripPoint;
                oneHandRightGripCarryPoint = bodyKeepoutSolver.OneHandRightGripPoint;
                if (HasAnyCharacterRightGripCarryPoint())
                {
                    return;
                }
            }

            Transform root = animator.transform;
            idleRightGripCarryPoint = FindCarryPoint(root, idleRightGripCarryPointName);
            hipAimRightGripCarryPoint = FindCarryPoint(root, hipAimRightGripCarryPointName);
            shoulderAimRightGripCarryPoint = FindCarryPoint(root, shoulderAimRightGripCarryPointName);
            runRightGripCarryPoint = FindCarryPoint(root, runRightGripCarryPointName);
            sprintRightGripCarryPoint = FindCarryPoint(root, sprintRightGripCarryPointName);
            oneHandRightGripCarryPoint = FindCarryPoint(root, oneHandRightGripCarryPointName);
        }

        private Transform FindCarryPoint(Transform root, string targetName)
        {
            return FirstNonNull(
                FindDescendantByName(root, targetName),
                FindDescendantByName(transform, targetName),
                FindDescendantByCompactName(root, targetName),
                FindDescendantByCompactName(transform, targetName));
        }

        private void AssignGripContactBones(Transform root)
        {
            rightArm.GripContact = rightArm.Hand;
            leftArm.GripContact = leftArm.Hand;
            rightArm.IndexContact = null;
            rightArm.ThumbContact = null;
            leftArm.IndexContact = null;
            leftArm.ThumbContact = null;

            if (root == null)
            {
                return;
            }

            Transform rightIndex = FirstNonNull(
                FindExactBone(root, experimentalRightIndexGripContactName),
                FindExactBone(root, "RightHandIndex1"),
                FindFirstBone(
                    root,
                    "Right_IndexProximal",
                    "RightIndexProximal",
                    "RightIndex1",
                    "Index1_R",
                    "index_01_r"));
            Transform rightThumb = FirstNonNull(
                FindExactBone(root, experimentalRightThumbGripContactName),
                FindExactBone(root, "RightHandThumb1"),
                FindFirstBone(
                    root,
                    "Right_ThumbProximal",
                    "RightThumbProximal",
                    "RightThumb1",
                    "Thumb1_R",
                    "thumb_01_r"));
            Transform leftIndex = FirstNonNull(
                FindExactBone(root, experimentalLeftIndexGripContactName),
                FindExactBone(root, "LeftHandIndex1"),
                FindFirstBone(
                    root,
                    "Left_IndexProximal",
                    "LeftIndexProximal",
                    "LeftIndex1",
                    "Index1_L",
                    "index_01_l"));

            rightArm.IndexContact = rightIndex;
            rightArm.ThumbContact = rightThumb;
            leftArm.IndexContact = leftIndex;

            if (ShouldUseExperimentalIndexGripContacts())
            {
                rightArm.GripContact = rightIndex != null ? rightIndex : rightArm.Hand;
                leftArm.GripContact = leftIndex != null ? leftIndex : leftArm.Hand;
            }
        }

        private void EnsureWeaponSocket()
        {
            Transform socketParent = ResolveSocketParent();
            if (socketParent == null)
            {
                weaponSocket = null;
                return;
            }

            string socketName = ResolveWeaponSocketName();
            if (weaponSocket != null)
            {
                if (weaponSocket.parent != socketParent)
                {
                    weaponSocket.SetParent(socketParent, false);
                }

                if (!string.Equals(weaponSocket.name, socketName, StringComparison.Ordinal))
                {
                    weaponSocket.name = socketName;
                }

                UpdateWeaponSocket();
                return;
            }

            Transform existing = FindDirectChild(socketParent, socketName);
            if (existing == null && !ShouldParentWeaponSocketToRightHand())
            {
                existing = FindDescendantByName(transform, socketName);
            }

            weaponSocket = existing != null ? existing : new GameObject(socketName).transform;
            weaponSocket.SetParent(socketParent, false);
            weaponSocket.localScale = Vector3.one;
            UpdateWeaponSocket();
        }

        private void EnsureGrips()
        {
            if (activeWeapon == null)
            {
                rightGrip = null;
                leftGrip = null;
                return;
            }

            if (TryUseWeaponPrefabGripAnchors())
            {
                return;
            }

            if (UsesAuthoredWeaponPrefabAnchorsOnly())
            {
                rightGrip = null;
                leftGrip = null;
                return;
            }

            if (UseShotgunPose3GripReplica())
            {
                if (IsReferenceShotgunInstance())
                {
                    referenceWeaponAnchor = FindDescendantByName(activeWeapon.transform, referenceShotgunAnchorName);
                }

                EnsureShotgunPose3GripTargets();
                return;
            }

            if (IsReferenceShotgunInstance())
            {
                CacheReferenceShotgunGrips();
                return;
            }

            rightGrip = EnsureGrip(rightGripName, rightGripLocalPosition, rightGripLocalEulerAngles);
            leftGrip = EnsureGrip(leftGripName, leftGripLocalPosition, leftGripLocalEulerAngles);
        }

        private bool TryUseWeaponPrefabGripAnchors()
        {
            if (!preferWeaponPrefabAnchors || activeWeapon == null)
            {
                return false;
            }

            Transform right = FindWeaponPrefabAnchor(rightGripName);
            Transform left = FindWeaponPrefabAnchor(leftGripName);

            if (right == null)
            {
                right = FindWeaponPrefabAnchor(Pose3RightGripTargetName);
            }

            if (left == null)
            {
                left = FindWeaponPrefabAnchor(Pose3LeftGripTargetName);
            }

            if (right == null || left == null)
            {
                return false;
            }

            EnsureTuningHandle(right.gameObject);
            EnsureTuningHandle(left.gameObject);
            rightGrip = right;
            leftGrip = left;
            PruneDuplicateWeaponAnchorInstances(rightGripName, rightGrip);
            PruneDuplicateWeaponAnchorInstances(leftGripName, leftGrip);
            return true;
        }

        private Transform EnsureGrip(string gripName, Vector3 localPosition, Vector3 localEulerAngles)
        {
            Transform existing = activeWeapon.transform.Find(gripName);
            Transform grip = existing != null ? existing : new GameObject(gripName).transform;
            grip.SetParent(activeWeapon.transform, false);
            grip.localPosition = localPosition;
            grip.localRotation = Quaternion.Euler(localEulerAngles);
            grip.localScale = Vector3.one;
            return grip;
        }

        private void EnsureRailgunMuzzleTarget()
        {
            if (activeWeapon == null || string.IsNullOrWhiteSpace(railgunMuzzleTransformName))
            {
                railgunMuzzleTarget = null;
                return;
            }

            bool wasMissing = railgunMuzzleTarget == null || !railgunMuzzleTarget.IsChildOf(activeWeapon.transform);
            if (wasMissing)
            {
                railgunMuzzleTarget = FindWeaponPrefabAnchor(railgunMuzzleTransformName);
            }

            if (railgunMuzzleTarget == null)
            {
                if (!createRailgunMuzzleTarget || UsesAuthoredWeaponPrefabAnchorsOnly())
                {
                    return;
                }

                railgunMuzzleTarget = new GameObject(railgunMuzzleTransformName).transform;
                railgunMuzzleTarget.SetParent(activeWeapon.transform, false);
                railgunMuzzleTarget.localPosition = railgunMuzzleLocalPosition;
                railgunMuzzleTarget.localRotation = Quaternion.Euler(railgunMuzzleLocalEulerAngles);
            }
            else if (!preserveTunedRailgunMuzzleTarget && (wasMissing || Application.isPlaying))
            {
                railgunMuzzleTarget.localPosition = railgunMuzzleLocalPosition;
                railgunMuzzleTarget.localRotation = Quaternion.Euler(railgunMuzzleLocalEulerAngles);
            }

            railgunMuzzleTarget.localScale = Vector3.one;
            EnsureTuningHandle(railgunMuzzleTarget.gameObject);
            PruneDuplicateWeaponAnchorInstances(railgunMuzzleTransformName, railgunMuzzleTarget);
            if (!UsesAuthoredWeaponPrefabAnchorsOnly())
            {
                EnsureRailgunMuzzleDebugMarker(railgunMuzzleTarget);
            }
        }

        private void EnsureRailgunMuzzleDebugMarker(Transform target)
        {
            if (target == null)
            {
                return;
            }

            Transform existing = FindDirectChild(target, RailgunMuzzleMarkerName);
            if (!showRailgunMuzzleDebugMarker)
            {
                if (existing != null)
                {
                    DestroyUnityObject(existing.gameObject);
                }

                return;
            }

            Transform marker = existing;
            if (marker == null)
            {
                GameObject markerObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                markerObject.name = RailgunMuzzleMarkerName;
                marker = markerObject.transform;
                marker.SetParent(target, false);
                Collider markerCollider = markerObject.GetComponent<Collider>();
                if (markerCollider != null)
                {
                    DestroyUnityObject(markerCollider);
                }
            }

            marker.localPosition = Vector3.zero;
            marker.localRotation = Quaternion.identity;
            marker.localScale = Vector3.one * railgunMuzzleDebugMarkerSize;
            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = ResolveRailgunParticleMaterial();
            }
        }

        private void EnsureRailgunButtstockTarget()
        {
            if (activeWeapon == null)
            {
                buttstockTarget = null;
                return;
            }

            bool wasMissing = buttstockTarget == null || !buttstockTarget.IsChildOf(activeWeapon.transform);
            if (wasMissing)
            {
                buttstockTarget = FindRailgunButtstockAnchor(true);
            }

            if (buttstockTarget == null)
            {
                if (!createRailgunButtstockTarget || UsesAuthoredWeaponPrefabAnchorsOnly())
                {
                    return;
                }

                buttstockTarget = new GameObject(ResolveButtstockCreationName()).transform;
                buttstockTarget.SetParent(activeWeapon.transform, false);
                buttstockTarget.localPosition = railgunButtstockLocalPosition;
                buttstockTarget.localRotation = Quaternion.Euler(railgunButtstockLocalEulerAngles);
            }
            else if (!preserveTunedRailgunButtstockTarget && (wasMissing || Application.isPlaying))
            {
                buttstockTarget.localPosition = railgunButtstockLocalPosition;
                buttstockTarget.localRotation = Quaternion.Euler(railgunButtstockLocalEulerAngles);
            }

            buttstockTarget.localScale = Vector3.one;
            EnsureTuningHandle(buttstockTarget.gameObject);
            PruneDuplicateWeaponAnchorInstances(ResolveButtstockCreationName(), buttstockTarget);
            PruneDuplicateWeaponAnchorInstances(RailgunButtstockAnchorName, buttstockTarget);
            PruneDuplicateWeaponAnchorInstances(RailgunButtstockTargetName, buttstockTarget);
            PruneDuplicateWeaponAnchorInstances(RailgunButtstockTypoAliasName, buttstockTarget);
            PruneDuplicateWeaponAnchorInstances(RailgunButtstockPlainAliasName, buttstockTarget);
        }

        private Transform FindRailgunButtstockAnchor(bool cleanupDuplicateTarget)
        {
            if (activeWeapon == null)
            {
                return null;
            }

            Transform preferred = FindWeaponPrefabAnchor(RailgunButtstockAnchorName);
            Transform configured = !string.IsNullOrWhiteSpace(buttstockName)
                                   && !string.Equals(buttstockName, RailgunButtstockAnchorName, StringComparison.Ordinal)
                ? FindWeaponPrefabAnchor(buttstockName)
                : null;
            Transform legacyTarget = !string.Equals(buttstockName, RailgunButtstockTargetName, StringComparison.Ordinal)
                ? FindWeaponPrefabAnchor(RailgunButtstockTargetName)
                : configured;
            Transform typoAlias = FindWeaponPrefabAnchor(RailgunButtstockTypoAliasName);
            Transform plainAlias = FindWeaponPrefabAnchor(RailgunButtstockPlainAliasName);

            if (preferred != null)
            {
                if (cleanupDuplicateTarget && legacyTarget != null && legacyTarget != preferred)
                {
                    DestroyUnityObject(legacyTarget.gameObject);
                }

                return preferred;
            }

            return configured != null ? configured : (typoAlias != null ? typoAlias : (plainAlias != null ? plainAlias : legacyTarget));
        }

        private string ResolveButtstockCreationName()
        {
            if (string.IsNullOrWhiteSpace(buttstockName) || string.Equals(buttstockName, RailgunButtstockTargetName, StringComparison.Ordinal))
            {
                return RailgunButtstockAnchorName;
            }

            return buttstockName;
        }

        private void ApplyRailgunPrefabAnchorDefaults(bool force)
        {
            if (!force || activeWeapon == null || IsReferenceShotgunInstance() || UsesAuthoredWeaponPrefabAnchorsOnly())
            {
                return;
            }

            ApplyDefaultRailgunAnchorTransform("SCF_RailgunModel", DefaultRailgunModelLocalPosition, Vector3.zero);
            ApplyDefaultRailgunAnchorTransform(rightGripName, DefaultRailgunRightGripLocalPosition, DefaultRailgunRightGripLocalEulerAngles);
            ApplyDefaultRailgunAnchorTransform(leftGripName, DefaultRailgunLeftGripLocalPosition, DefaultRailgunLeftGripLocalEulerAngles);
            if (!preserveTunedRailgunMuzzleTarget)
            {
                ApplyDefaultRailgunAnchorTransform(railgunMuzzleTransformName, DefaultRailgunMuzzleLocalPosition, DefaultRailgunMuzzleLocalEulerAngles);
            }

            if (!preserveTunedRailgunButtstockTarget)
            {
                ApplyDefaultRailgunAnchorTransform(buttstockName, DefaultRailgunButtstockLocalPosition, DefaultRailgunButtstockLocalEulerAngles);
            }
        }

        private void ApplyDefaultRailgunAnchorTransform(string anchorName, Vector3 localPosition, Vector3 localEulerAngles)
        {
            if (activeWeapon == null || string.IsNullOrWhiteSpace(anchorName))
            {
                return;
            }

            Transform anchor = FindDescendantByName(activeWeapon.transform, anchorName);
            if (anchor == null)
            {
                return;
            }

            if (lockWeaponPrefabGripAnchors && IsGripAnchorName(anchorName))
            {
                return;
            }

            anchor.localPosition = localPosition;
            anchor.localRotation = Quaternion.Euler(localEulerAngles);
            anchor.localScale = Vector3.one;
        }

        private void ResetWeaponTransform()
        {
            if (activeWeapon == null)
            {
                return;
            }

            ApplyWeaponLocalFit(true);
            ApplyRailgunPrefabAnchorDefaults(true);
            UpdateWeaponSocket();
            if (IsReferenceShotgunInstance())
            {
                PrepareReferenceShotgunInstance();
                AlignReferenceShotgunToSocket();
                CacheReferenceShotgunGrips();
            }
            else
            {
                EnsureGrips();
                EnsureRailgunMuzzleTarget();
                EnsureRailgunButtstockTarget();
                if (!UseShotgunPose3GripReplica())
                {
                    AlignWeaponByRightGrip();
                }
            }

            AlignRightHandSocketToFingerPocket();
        }

        private void ApplyWeaponLocalFit(bool force = false)
        {
        }

        private void AlignWeaponByRightGrip()
        {
        }

        private void AlignRightHandSocketToFingerPocket()
        {
            if (!alignRightHandSocketToFingerPocket
                || !ShouldParentWeaponSocketToRightHand()
                || weaponSocket == null
                || activeWeapon == null
                || rightGrip == null
                || rightArm.Hand == null)
            {
                return;
            }

            if (!TryResolveRightHandGripPocket(out Vector3 pocketWorld))
            {
                return;
            }

            Vector3 delta = pocketWorld - rightGrip.position;
            if (delta.sqrMagnitude <= 0.0000001f)
            {
                return;
            }

            weaponSocket.position += delta * Mathf.Clamp01(rightHandGripPocketBlend);
        }

        private bool TryResolveRightHandGripPocket(out Vector3 pocketWorld)
        {
            pocketWorld = Vector3.zero;
            if (rightArm.Hand == null)
            {
                return false;
            }

            Vector3 handWorld = rightArm.Hand.position;
            bool hasIndex = rightArm.IndexContact != null;
            bool hasThumb = rightArm.ThumbContact != null;
            Vector3 fingerPocket = handWorld;

            if (hasIndex && hasThumb)
            {
                fingerPocket = Vector3.Lerp(rightArm.IndexContact.position, rightArm.ThumbContact.position, 0.5f);
            }
            else if (hasIndex)
            {
                fingerPocket = rightArm.IndexContact.position;
            }
            else if (hasThumb)
            {
                fingerPocket = rightArm.ThumbContact.position;
            }

            pocketWorld = Vector3.Lerp(handWorld, fingerPocket, Mathf.Clamp01(rightHandGripPocketFingerWeight));
            pocketWorld += rightArm.Hand.TransformVector(rightHandGripPocketLocalOffset);
            return true;
        }

        private void PrepareReferenceShotgunInstance()
        {
            if (!IsReferenceShotgunInstance())
            {
                return;
            }

            SampleReferenceShotgunPose();
            SetReferenceShotgunRenderers();
            CacheReferenceShotgunGrips();
        }

        private void SampleReferenceShotgunPose()
        {
#if UNITY_EDITOR
            AnimationClip poseClip = LoadReferencePoseClip();
            if (poseClip == null)
            {
                return;
            }

            float sampleTime = Mathf.Clamp01(referencePoseSampleTime01) * Mathf.Max(0.01f, poseClip.length);
            poseClip.SampleAnimation(activeWeapon, sampleTime);
#endif
        }

        private void SetReferenceShotgunRenderers()
        {
            Transform shotgun = FindDescendantByName(activeWeapon.transform, referenceShotgunMeshName);
            Renderer[] renderers = activeWeapon.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                renderer.enabled = shotgun != null && (renderer.transform == shotgun || renderer.transform.IsChildOf(shotgun));
            }
        }

        private void CacheReferenceShotgunGrips()
        {
            if (activeWeapon == null)
            {
                rightGrip = null;
                leftGrip = null;
                referenceWeaponAnchor = null;
                return;
            }

            referenceWeaponAnchor = FindDescendantByName(activeWeapon.transform, referenceShotgunAnchorName);
            if (UseShotgunPose3GripReplica())
            {
                EnsureShotgunPose3GripTargets();
                return;
            }

            rightGrip = FindDescendantByName(activeWeapon.transform, referenceRightGripName);
            leftGrip = FindDescendantByName(activeWeapon.transform, referenceLeftGripName);
        }

        private void EnsureShotgunPose3GripTargets()
        {
            if (TryUseWeaponPrefabGripAnchors())
            {
                return;
            }

            if (weaponSocket == null)
            {
                rightGrip = null;
                leftGrip = null;
                return;
            }

            bool rightWasMissing = pose3RightGripTarget == null || pose3RightGripTarget.parent != weaponSocket;
            bool leftWasMissing = pose3LeftGripTarget == null || pose3LeftGripTarget.parent != weaponSocket;
            pose3RightGripTarget = EnsureSocketTarget(pose3RightGripTarget, Pose3RightGripTargetName);
            pose3LeftGripTarget = EnsureSocketTarget(pose3LeftGripTarget, Pose3LeftGripTargetName);

            if (rightWasMissing || !ShouldPreserveTunedGripTargets())
            {
                pose3RightGripTarget.localPosition = ResolvePose3RightGripSocketOffset();
                pose3RightGripTarget.localRotation = Quaternion.Euler(shotgunPose3RightGripEulerAngles);
            }

            pose3RightGripTarget.localScale = Vector3.one;

            if (leftWasMissing || !ShouldPreserveTunedGripTargets())
            {
                pose3LeftGripTarget.localPosition = ResolvePose3LeftGripSocketOffset();
                pose3LeftGripTarget.localRotation = Quaternion.Euler(shotgunPose3LeftGripEulerAngles);
            }

            pose3LeftGripTarget.localScale = Vector3.one;

            rightGrip = pose3RightGripTarget;
            leftGrip = pose3LeftGripTarget;
        }

        private Transform EnsureSocketTarget(Transform current, string targetName)
        {
            if (current != null && current.parent == weaponSocket)
            {
                return current;
            }

            Transform existing = FindDirectChild(weaponSocket, targetName);
            Transform target = existing != null ? existing : new GameObject(targetName).transform;
            target.SetParent(weaponSocket, false);
            EnsureTuningHandle(target.gameObject);
            return target;
        }

        private void AlignReferenceShotgunToSocket()
        {
        }

        private bool UseShotgunPose3GripReplica()
        {
            return useShotgunPose3GripReplica && activeWeapon != null && UsesLegacyTpsWeaponFit();
        }

        private Vector3 ResolvePose3RightGripSocketOffset()
        {
            return Vector3.Lerp(restRightGripSocketOffset, raisedRightGripSocketOffset, raised01)
                   + shotgunPose3RightGripTuningOffset;
        }

        private Vector3 ResolvePose3LeftGripSocketOffset()
        {
            Vector3 rightFromAnchor = MapPose3ReferenceOffset(Pose3ReferenceRightWristFromShotgunBone) * shotgunPose3Scale;
            Vector3 leftFromAnchor = MapPose3ReferenceOffset(Pose3ReferenceLeftWristFromShotgunBone) * shotgunPose3Scale;
            return ResolvePose3RightGripSocketOffset()
                   + leftFromAnchor
                   - rightFromAnchor
                   + shotgunPose3LeftGripTuningOffset;
        }

        private Vector3 ResolvePose3WeaponAnchorSocketOffset()
        {
            Vector3 rightFromAnchor = MapPose3ReferenceOffset(Pose3ReferenceRightWristFromShotgunBone) * shotgunPose3Scale;
            return ResolvePose3RightGripSocketOffset()
                   - rightFromAnchor
                   + shotgunPose3WeaponAnchorTuningOffset;
        }

        private static Vector3 MapPose3ReferenceOffset(Vector3 referenceOffset)
        {
            return new Vector3(-referenceOffset.x, referenceOffset.y, referenceOffset.z);
        }

        private bool ShouldPreserveTunedRailgunTransform()
        {
            return enableRailgunLiveTuning
                   && preserveTunedRailgunTransform
                   && activeWeapon != null
                   && UsesLegacyTpsWeaponFit()
                   && !IsReferenceShotgunInstance()
                   && !ShouldReturnSelectedWeaponToNeutralFit();
        }

        private bool ShouldReturnSelectedWeaponToNeutralFit()
        {
            return returnWeaponToNeutralFitWhenNotAiming
                   && Application.isPlaying
                   && activeWeapon != null
                   && !IsReferenceShotgunInstance()
                   && !weaponFitPoseTuningMode
                   && !IsWeaponFitResetActionActive()
                   && (motor == null || !motor.AimHeld);
        }

        private bool ShouldPreserveTunedGripTargets()
        {
            return enableRailgunLiveTuning && preserveTunedGripTargets;
        }

        private void TickRailgunLiveTuningCapture()
        {
            if (!enableRailgunLiveTuning || activeWeapon == null || IsReferenceShotgunInstance())
            {
                return;
            }

            if (blockLiveTuningDuringMobilityAction && IsWeaponFitResetActionActive())
            {
                return;
            }

            if (captureTuningEveryFrame)
            {
                CaptureCurrentRailgunTuningInternal(false);
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            if (captureTuningHotkey != KeyCode.None && Input.GetKeyDown(captureTuningHotkey))
            {
                CaptureCurrentRailgunTuning();
            }
#endif
        }

        public void CaptureCurrentRailgunTuning()
        {
            CaptureCurrentRailgunTuningInternal(true);
            RecordWeaponFitSnapshot("SCF_RailgunCapture");
        }

        public void CopyCurrentRailgunTuning()
        {
            CopyCurrentRailgunTuningToClipboard();
        }

        public void ApplySavedRailgunGripTargets()
        {
            if (activeWeapon == null)
            {
                return;
            }

            if (!UsesLegacyTpsWeaponFit())
            {
                EnsureWeaponSocket();
                ApplyWeaponLocalFit(true);
                EnsureGrips();
                EnsureRailgunMuzzleTarget();
                EnsureRailgunButtstockTarget();
                return;
            }

            if (TryApplyWeaponFitPose(ResolveRuntimeWeaponFitPoseSlot(), true))
            {
                return;
            }

            EnsureWeaponSocket();
            ApplyRailgunPrefabAnchorDefaults(true);
            ApplyWeaponLocalFit(true);
            EnsureRailgunMuzzleTarget();
            EnsureRailgunButtstockTarget();
            bool oldPreserveTunedGripTargets = preserveTunedGripTargets;
            preserveTunedGripTargets = false;
            EnsureShotgunPose3GripTargets();
            preserveTunedGripTargets = oldPreserveTunedGripTargets;
        }

        public void SetWeaponFitPoseTuningMode(SCFWeaponFitPoseSlot slot, bool enabled)
        {
            tuningWeaponFitPoseSlot = slot;
            weaponFitPoseTuningMode = enabled;
            if (enabled)
            {
                ApplyWeaponFitPose(slot);
            }
        }

        public void ApplyWeaponFitPose(SCFWeaponFitPoseSlot slot)
        {
            TryApplyWeaponFitPose(slot, true);
        }

        public void CaptureCurrentWeaponFitPose(SCFWeaponFitPoseSlot slot)
        {
            RefreshWeaponTuningTargets();
            SCFWeaponFitPose pose = CaptureCurrentWeaponFitPoseData();
            SetWeaponFitPose(slot, pose);
            RecordWeaponFitPoseSnapshot(slot);
        }

        public string RecordWeaponFitPoseSnapshot(SCFWeaponFitPoseSlot slot)
        {
            RefreshWeaponTuningTargets();
            string snapshotName = "SCF_WeaponFitPose_" + slot;
            return SCFTransformSnapshotRecorder.CaptureHierarchy(snapshotName, transform, BuildWeaponFitSnapshotTargets());
        }

        public void RefreshWeaponTuningTargets()
        {
            EnsureWeaponSocket();
            if (activeWeapon == null)
            {
                return;
            }

            EnsureGrips();
            EnsureRailgunMuzzleTarget();
            EnsureRailgunButtstockTarget();
        }

        public string RecordWeaponFitSnapshot(string snapshotName = "SCF_WeaponFit")
        {
            RefreshWeaponTuningTargets();
            return SCFTransformSnapshotRecorder.CaptureHierarchy(snapshotName, ActiveWeaponTransform, BuildWeaponFitSnapshotTargets());
        }

        public string RecordExperimentalPoseClaySnapshot()
        {
            RefreshWeaponTuningTargets();
            CacheBones();
            Transform root = poseAuthoringRecordsFullCharacterHierarchy ? transform : ActiveWeaponTransform;
            return SCFTransformSnapshotRecorder.CaptureHierarchy("SCF_ExperimentalPoseClay", root, BuildExperimentalPoseClaySnapshotTargets());
        }

        public SCFTransformSnapshotTarget[] BuildWeaponFitSnapshotTargets()
        {
            return new[]
            {
                new SCFTransformSnapshotTarget("SOCKET / SCF_ChestWeaponSocket", weaponSocket),
                new SCFTransformSnapshotTarget("WEAPON ROOT / SCF_Selected_Railgun", ActiveWeaponTransform),
                new SCFTransformSnapshotTarget("RIGHT HAND GRIP / SCF_RightPistolGrip", rightGrip),
                new SCFTransformSnapshotTarget("LEFT HAND GRIP / SCF_LeftUnderbarrelGrip", leftGrip),
                new SCFTransformSnapshotTarget("MUZZLE / SCF_RailgunMuzzleTarget", railgunMuzzleTarget),
                new SCFTransformSnapshotTarget("BUTTSTOCK / SCF_RailgunButtstock", buttstockTarget)
            };
        }

        private SCFTransformSnapshotTarget[] BuildExperimentalPoseClaySnapshotTargets()
        {
            return new[]
            {
                new SCFTransformSnapshotTarget("CHARACTER ROOT", transform),
                new SCFTransformSnapshotTarget("ANIMATOR ROOT", animator != null ? animator.transform : null),
                new SCFTransformSnapshotTarget("WEAPON SOCKET / SCF_ChestWeaponSocket", weaponSocket),
                new SCFTransformSnapshotTarget("WEAPON ROOT / SCF_Selected_Railgun", ActiveWeaponTransform),
                new SCFTransformSnapshotTarget("RIGHT HAND GRIP / SCF_RightPistolGrip", rightGrip),
                new SCFTransformSnapshotTarget("LEFT HAND GRIP / SCF_LeftUnderbarrelGrip", leftGrip),
                new SCFTransformSnapshotTarget("MUZZLE / SCF_RailgunMuzzleTarget", railgunMuzzleTarget),
                new SCFTransformSnapshotTarget("BUTTSTOCK / SCF_RailgunButtstock", buttstockTarget),
                new SCFTransformSnapshotTarget("CHEST ANCHOR", chestAnchor),
                new SCFTransformSnapshotTarget("FACE ANCHOR", faceAnchor),
                new SCFTransformSnapshotTarget("RIGHT SHOULDER", rightArm.Shoulder),
                new SCFTransformSnapshotTarget("RIGHT UPPER ARM", rightArm.UpperArm),
                new SCFTransformSnapshotTarget("RIGHT FOREARM", rightArm.Forearm),
                new SCFTransformSnapshotTarget("RIGHT HAND", rightArm.Hand),
                new SCFTransformSnapshotTarget("RIGHT GRIP CONTACT", rightArm.GripContact),
                new SCFTransformSnapshotTarget("RIGHT INDEX CONTACT", rightArm.IndexContact),
                new SCFTransformSnapshotTarget("RIGHT THUMB CONTACT", rightArm.ThumbContact),
                new SCFTransformSnapshotTarget("LEFT SHOULDER", leftArm.Shoulder),
                new SCFTransformSnapshotTarget("LEFT UPPER ARM", leftArm.UpperArm),
                new SCFTransformSnapshotTarget("LEFT FOREARM", leftArm.Forearm),
                new SCFTransformSnapshotTarget("LEFT HAND", leftArm.Hand),
                new SCFTransformSnapshotTarget("LEFT GRIP CONTACT", leftArm.GripContact)
            };
        }

        [ContextMenu("SCF/Record Experimental Pose Clay Snapshot")]
        private void RecordExperimentalPoseClaySnapshotFromContext()
        {
            RecordExperimentalPoseClaySnapshot();
        }

        [ContextMenu("SCF/Capture Current Railgun Tuning")]
        private void CaptureCurrentRailgunTuningFromContext()
        {
            CaptureCurrentRailgunTuning();
        }

        [ContextMenu("SCF/Copy Current Railgun Tuning")]
        private void CopyCurrentRailgunTuningFromContext()
        {
            CopyCurrentRailgunTuning();
        }

        [ContextMenu("SCF/Apply Saved Railgun Grip Targets")]
        private void ApplySavedRailgunGripTargetsFromContext()
        {
            ApplySavedRailgunGripTargets();
        }

        private void ApplyRuntimeWeaponFitPoseToTargets()
        {
            if (weaponFitPoseTuningMode || !useWeaponFitPoseSlots || activeWeapon == null || ShouldSuppressExperimentalWeaponFitPoseSlots())
            {
                return;
            }

            if (hasRuntimeWeaponFitPoseSlot && ShouldHoldWeaponPoseForSelfAimProtection())
            {
                return;
            }

            SCFWeaponFitPoseSlot slot = ResolveRuntimeWeaponFitPoseSlot();
            bool includeRootPose = !hasRuntimeWeaponFitPoseSlot
                                   || lastRuntimeWeaponFitPoseSlot != slot
                                   || ShouldRefreshRuntimeWeaponRootPoseEveryFrame();
            if (TryApplyWeaponFitPose(slot, true, false, includeRootPose))
            {
                lastRuntimeWeaponFitPoseSlot = slot;
                hasRuntimeWeaponFitPoseSlot = true;
            }
        }

        private bool ShouldSuppressExperimentalWeaponFitPoseSlots()
        {
            return !UsesLegacyTpsWeaponFit()
                   || (suppressExperimentalWeaponFitPoseSlots && UsesExperimentalFireSolutionAuthority());
        }

        private bool ShouldRefreshRuntimeWeaponRootPoseEveryFrame()
        {
            if (UsesExperimentalFireSolutionAuthority())
            {
                return false;
            }

            return motor == null || !motor.AimHeld;
        }

        private SCFWeaponFitPoseSlot ResolveRuntimeWeaponFitPoseSlot()
        {
            if (weaponFitPoseTuningMode)
            {
                return tuningWeaponFitPoseSlot;
            }

            if (motor != null && (motor.IsAirborne || motor.IsJumpCharging))
            {
                return motor.AimHeld ? SCFWeaponFitPoseSlot.AimWalk : SCFWeaponFitPoseSlot.Neutral;
            }

            if (motor != null && motor.AimHeld)
            {
                return motor.RunHeld ? SCFWeaponFitPoseSlot.AimRun : SCFWeaponFitPoseSlot.AimWalk;
            }

            return SCFWeaponFitPoseSlot.Neutral;
        }

        private bool TryApplyWeaponFitPose(SCFWeaponFitPoseSlot slot, bool allowNeutralFallback)
        {
            return TryApplyWeaponFitPose(slot, allowNeutralFallback, true);
        }

        private bool TryApplyWeaponFitPose(SCFWeaponFitPoseSlot slot, bool allowNeutralFallback, bool ensureTargets)
        {
            return TryApplyWeaponFitPose(slot, allowNeutralFallback, ensureTargets, true);
        }

        private bool TryApplyWeaponFitPose(SCFWeaponFitPoseSlot slot, bool allowNeutralFallback, bool ensureTargets, bool includeRootPose)
        {
            if (!UsesLegacyTpsWeaponFit())
            {
                return false;
            }

            if (!TryGetWeaponFitPose(slot, out SCFWeaponFitPose pose)
                && (!allowNeutralFallback || slot == SCFWeaponFitPoseSlot.Neutral || !TryGetWeaponFitPose(SCFWeaponFitPoseSlot.Neutral, out pose)))
            {
                return false;
            }

            ApplyWeaponFitPoseData(pose, ensureTargets, includeRootPose);
            return true;
        }

        private void ApplyWeaponFitPoseData(SCFWeaponFitPose pose, bool ensureTargets)
        {
            ApplyWeaponFitPoseData(pose, ensureTargets, true);
        }

        private void ApplyWeaponFitPoseData(SCFWeaponFitPose pose, bool ensureTargets, bool includeRootPose)
        {
            if (!pose.captured || activeWeapon == null)
            {
                return;
            }

            if (ensureTargets)
            {
                EnsureWeaponSocket();
            }

            if (includeRootPose)
            {
                ApplyLocalPose(weaponSocket, pose.socket);
            }

            if (ensureTargets)
            {
                EnsureGrips();
                EnsureRailgunMuzzleTarget();
                EnsureRailgunButtstockTarget();
            }

            if (!ShouldLockWeaponPrefabGripAnchor(rightGrip))
            {
                ApplyLocalPose(rightGrip, pose.rightGrip);
            }

            if (!ShouldLockWeaponPrefabGripAnchor(leftGrip))
            {
                ApplyLocalPose(leftGrip, pose.leftGrip);
            }

            ApplyLocalPose(railgunMuzzleTarget, pose.muzzle);
            ApplyLocalPose(buttstockTarget, pose.buttstock);

            if (pose.muzzle.captured)
            {
                railgunMuzzleLocalPosition = pose.muzzle.localPosition;
                railgunMuzzleLocalEulerAngles = pose.muzzle.localEulerAngles;
            }

            if (pose.buttstock.captured)
            {
                railgunButtstockLocalPosition = pose.buttstock.localPosition;
                railgunButtstockLocalEulerAngles = pose.buttstock.localEulerAngles;
            }
        }

        private SCFWeaponFitPose CaptureCurrentWeaponFitPoseData()
        {
            return new SCFWeaponFitPose
            {
                captured = activeWeapon != null,
                socket = CaptureLocalPose(weaponSocket),
                weapon = CaptureLocalPose(activeWeapon != null ? activeWeapon.transform : null),
                rightGrip = CaptureLocalPoseUnlessLockedGrip(rightGrip),
                leftGrip = CaptureLocalPoseUnlessLockedGrip(leftGrip),
                muzzle = CaptureLocalPose(railgunMuzzleTarget),
                buttstock = CaptureLocalPose(buttstockTarget)
            };
        }

        private SCFLocalTransformPose CaptureLocalPoseUnlessLockedGrip(Transform target)
        {
            return ShouldLockWeaponPrefabGripAnchor(target)
                ? default
                : CaptureLocalPose(target);
        }

        private bool ShouldLockWeaponPrefabGripAnchor(Transform target)
        {
            return lockWeaponPrefabGripAnchors
                   && IsWeaponPrefabGripAnchor(target);
        }

        private bool IsWeaponPrefabGripAnchor(Transform target)
        {
            if (target == null || activeWeapon == null || !target.IsChildOf(activeWeapon.transform))
            {
                return false;
            }

            return IsGripAnchorName(target.name);
        }

        private bool IsGripAnchorName(string targetName)
        {
            return string.Equals(targetName, rightGripName, StringComparison.Ordinal)
                   || string.Equals(targetName, leftGripName, StringComparison.Ordinal);
        }

        private bool TryGetWeaponFitPose(SCFWeaponFitPoseSlot slot, out SCFWeaponFitPose pose)
        {
            switch (slot)
            {
                case SCFWeaponFitPoseSlot.AimWalk:
                    pose = aimWalkWeaponFitPose;
                    break;
                case SCFWeaponFitPoseSlot.AimRun:
                    pose = aimRunWeaponFitPose;
                    break;
                default:
                    pose = neutralWeaponFitPose;
                    break;
            }

            return pose.captured;
        }

        private void SetWeaponFitPose(SCFWeaponFitPoseSlot slot, SCFWeaponFitPose pose)
        {
            switch (slot)
            {
                case SCFWeaponFitPoseSlot.AimWalk:
                    aimWalkWeaponFitPose = pose;
                    break;
                case SCFWeaponFitPoseSlot.AimRun:
                    aimRunWeaponFitPose = pose;
                    break;
                default:
                    neutralWeaponFitPose = pose;
                    break;
            }

            hasRuntimeWeaponFitPoseSlot = false;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
            }
#endif
        }

        private static SCFLocalTransformPose CaptureLocalPose(Transform target)
        {
            if (target == null)
            {
                return default;
            }

            return new SCFLocalTransformPose
            {
                captured = true,
                localPosition = target.localPosition,
                localEulerAngles = NormalizeEuler(target.localEulerAngles),
                localScale = target.localScale
            };
        }

        private static void ApplyLocalPose(Transform target, SCFLocalTransformPose pose)
        {
            if (target == null || !pose.captured)
            {
                return;
            }

            target.localPosition = pose.localPosition;
            target.localRotation = Quaternion.Euler(pose.localEulerAngles);
            target.localScale = pose.localScale;
        }

        private void CaptureCurrentRailgunTuningInternal(bool announce)
        {
            if (activeWeapon == null)
            {
                return;
            }

            Vector3 currentWeaponLocalPosition = activeWeapon.transform.localPosition;
            Vector3 currentWeaponLocalEulerAngles = NormalizeEuler(activeWeapon.transform.localEulerAngles);
            Vector3 currentWeaponLocalScale = SanitizedScale(activeWeapon.transform.localScale);

            if (pose3RightGripTarget != null)
            {
                shotgunPose3RightGripTuningOffset = pose3RightGripTarget.localPosition
                                                    - Vector3.Lerp(restRightGripSocketOffset, raisedRightGripSocketOffset, raised01);
                shotgunPose3RightGripEulerAngles = NormalizeEuler(pose3RightGripTarget.localEulerAngles);
            }

            if (pose3LeftGripTarget != null)
            {
                Vector3 rightFromAnchor = MapPose3ReferenceOffset(Pose3ReferenceRightWristFromShotgunBone) * shotgunPose3Scale;
                Vector3 leftFromAnchor = MapPose3ReferenceOffset(Pose3ReferenceLeftWristFromShotgunBone) * shotgunPose3Scale;
                Vector3 baseLeftGrip = ResolvePose3RightGripSocketOffset() + leftFromAnchor - rightFromAnchor;
                shotgunPose3LeftGripTuningOffset = pose3LeftGripTarget.localPosition - baseLeftGrip;
                shotgunPose3LeftGripEulerAngles = NormalizeEuler(pose3LeftGripTarget.localEulerAngles);
            }

            if (railgunMuzzleTarget != null)
            {
                railgunMuzzleLocalPosition = railgunMuzzleTarget.localPosition;
                railgunMuzzleLocalEulerAngles = NormalizeEuler(railgunMuzzleTarget.localEulerAngles);
            }

            if (buttstockTarget != null)
            {
                railgunButtstockLocalPosition = buttstockTarget.localPosition;
                railgunButtstockLocalEulerAngles = NormalizeEuler(buttstockTarget.localEulerAngles);
            }

            weaponLocalPosition = UseShotgunPose3GripReplica()
                ? currentWeaponLocalPosition - ResolvePose3WeaponAnchorSocketOffset()
                : currentWeaponLocalPosition;
            weaponLocalEulerAngles = currentWeaponLocalEulerAngles;
            weaponLocalScale = currentWeaponLocalScale;

            if (announce && copyTuningOnCapture)
            {
                CopyCurrentRailgunTuningToClipboard();
            }
            else if (announce)
            {
                Debug.Log(BuildRailgunTuningProfileText());
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
            }
#endif
        }

        private void CopyCurrentRailgunTuningToClipboard()
        {
            string profile = BuildRailgunTuningProfileText();
            GUIUtility.systemCopyBuffer = profile;
            Debug.Log(profile);
        }

        private void EnsureTuningHandle(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            SCFWeaponTuningHandle handle = target.GetComponent<SCFWeaponTuningHandle>();
            if (handle == null)
            {
                handle = target.AddComponent<SCFWeaponTuningHandle>();
            }

            handle.Configure(this);
        }

        private string BuildRailgunTuningProfileText()
        {
            return "SCF railgun tuning captured\n"
                   + "weaponLocalPosition = " + FormatVector(weaponLocalPosition) + "\n"
                   + "weaponLocalEulerAngles = " + FormatVector(weaponLocalEulerAngles) + "\n"
                   + "weaponLocalScale = " + FormatVector(weaponLocalScale) + "\n"
                   + "shotgunPose3Scale = " + FormatFloat(shotgunPose3Scale) + "\n"
                   + "shotgunPose3RightGripTuningOffset = " + FormatVector(shotgunPose3RightGripTuningOffset) + "\n"
                   + "shotgunPose3RightGripEulerAngles = " + FormatVector(shotgunPose3RightGripEulerAngles) + "\n"
                   + "shotgunPose3LeftGripTuningOffset = " + FormatVector(shotgunPose3LeftGripTuningOffset) + "\n"
                   + "shotgunPose3LeftGripEulerAngles = " + FormatVector(shotgunPose3LeftGripEulerAngles) + "\n"
                   + "shotgunPose3WeaponAnchorTuningOffset = " + FormatVector(shotgunPose3WeaponAnchorTuningOffset) + "\n"
                   + "railgunMuzzleLocalPosition = " + FormatVector(railgunMuzzleLocalPosition) + "\n"
                   + "railgunMuzzleLocalEulerAngles = " + FormatVector(railgunMuzzleLocalEulerAngles) + "\n"
                   + "railgunButtstockLocalPosition = " + FormatVector(railgunButtstockLocalPosition) + "\n"
                   + "railgunButtstockLocalEulerAngles = " + FormatVector(railgunButtstockLocalEulerAngles);
        }

        private static Vector3 NormalizeEuler(Vector3 eulerAngles)
        {
            return new Vector3(NormalizeEulerAxis(eulerAngles.x), NormalizeEulerAxis(eulerAngles.y), NormalizeEulerAxis(eulerAngles.z));
        }

        private static float NormalizeEulerAxis(float value)
        {
            value %= 360f;
            if (value > 180f)
            {
                value -= 360f;
            }
            else if (value < -180f)
            {
                value += 360f;
            }

            return value;
        }

        private static string FormatVector(Vector3 value)
        {
            return "new Vector3(" + FormatFloat(value.x) + "f, " + FormatFloat(value.y) + "f, " + FormatFloat(value.z) + "f)";
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }

        private static string SanitizeObjectName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Weapon";
            }

            char[] chars = value.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
                {
                    chars[i] = '_';
                }
            }

            return new string(chars);
        }

        private static bool IsRailgunWeapon(GameObject weapon)
        {
            return weapon != null
                   && weapon.name.IndexOf("railgun", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool IsReferenceShotgunInstance()
        {
            return activeWeapon != null && activeWeapon.name.StartsWith("SCF_Selected_ReferenceShotgun", StringComparison.Ordinal);
        }

#if UNITY_EDITOR
        private AnimationClip LoadReferencePoseClip()
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(ReferenceShotgunPath);
            AnimationClip fallback = null;
            for (int i = 0; i < assets.Length; i++)
            {
                if (!(assets[i] is AnimationClip clip) || clip.name.StartsWith("__preview", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (fallback == null)
                {
                    fallback = clip;
                }

                if (string.Equals(clip.name, referencePoseClipName, StringComparison.OrdinalIgnoreCase))
                {
                    return clip;
                }
            }

            return fallback;
        }
#endif

        private void UpdateRaisedBlend()
        {
            float target = input != null && input.AimHeld && !ShouldLowerWeaponForTraversal() ? 1f : 0f;
            float blend = 1f - Mathf.Exp(-raiseSharpness * Time.deltaTime);
            raised01 = Mathf.Lerp(raised01, target, blend);
        }

        private float ResolveCarryStateWeight()
        {
            if (!useStateBasedCarryWeights || motor == null)
            {
                return 1f;
            }

            if (motor.IsCombatRolling)
            {
                return rollCarryWeight;
            }

            if (motor.IsObstacleTraversing)
            {
                return traversalCarryWeight;
            }

            if (motor.IsWallRunning)
            {
                return wallRunCarryWeight;
            }

            if (motor.IsAirborne || motor.IsJumpCharging)
            {
                return airborneCarryWeight;
            }

            if (motor.AimHeld)
            {
                return aimedCarryWeight;
            }

            if (motor.SprintHeld && motor.PlanarVelocity.sqrMagnitude > 0.2f)
            {
                return sprintCarryWeight;
            }

            return locomotionCarryWeight;
        }

        private float ResolveHandCarryStateWeight(bool rightHand)
        {
            if (!useStateBasedCarryWeights || motor == null)
            {
                return 1f;
            }

            if (activeRearAimGateMode == SCFRearAimGateMode.RightHandRearShot)
            {
                return rightHand ? 1f : rightSideRearAimLeftHandWeight;
            }

            if (activeRearAimGateMode == SCFRearAimGateMode.InwardTwoHandTurn)
            {
                return rightHand ? 1f : inwardRearAimLeftHandWeight;
            }

            if (detachLeftHandDuringWallRun && motor.IsWallRunning)
            {
                return rightHand ? wallRunRightHandCarryWeight : wallRunLeftHandCarryWeight;
            }

            if (detachBothHandsDuringClimb && motor.IsClimbing)
            {
                return rightHand ? climbRightHandCarryWeight : climbHandCarryWeight;
            }

            if (detachLeftHandDuringVault && motor.IsVaulting)
            {
                return rightHand ? vaultRightHandCarryWeight : vaultLeftHandCarryWeight;
            }

            return ResolveCarryStateWeight();
        }

        private bool ShouldSuspendMuzzleAimForTraversal()
        {
            return suspendMuzzleAimDuringClimb
                   && detachBothHandsDuringClimb
                   && motor != null
                   && motor.IsClimbing
                   && climbRightHandCarryWeight <= 0.001f;
        }

        private bool ShouldLowerWeaponForTraversal()
        {
            return lowerWeaponDuringClimb
                   && detachBothHandsDuringClimb
                   && motor != null
                   && motor.IsClimbing
                   && climbRightHandCarryWeight <= 0.001f;
        }

        private bool ShouldBlockRailgunFireForTraversal()
        {
            return blockRailgunFireDuringClimb
                   && detachBothHandsDuringClimb
                   && motor != null
                   && motor.IsClimbing;
        }

        private void TickWeaponFitMobilityActionReset()
        {
            bool actionActive = IsWeaponFitResetActionActive();
            if (snapWeaponFitAfterMobilityAction && wasInWeaponFitResetAction && !actionActive)
            {
                ApplySavedRailgunGripTargets();
            }

            wasInWeaponFitResetAction = actionActive;
        }

        private bool IsWeaponFitResetActionActive()
        {
            return motor != null && motor.MobilityState != CharacterMobilityState.Locomotion;
        }

        private void UpdateWeaponSocket()
        {
            if (weaponSocket == null)
            {
                return;
            }

            Transform socketParent = ResolveSocketParent();
            if (socketParent != null && weaponSocket.parent != socketParent)
            {
                weaponSocket.SetParent(socketParent, false);
            }

            if (ShouldParentWeaponSocketToRightHand())
            {
                weaponSocket.localPosition = experimentalRightHandSocketLocalPosition;
                weaponSocket.localScale = Vector3.one;
                return;
            }

            weaponSocket.localPosition = Vector3.Lerp(restSocketOffset, raisedSocketOffset, raised01);
        }

        private void ApplyCharacterRightGripCarryPointAuthority()
        {
            activeCharacterRightGripCarryPointName = string.Empty;
        }

        private bool ShouldUseCharacterRightGripCarryPointAuthority()
        {
            return useCharacterRightGripCarryPoints
                   && UsesExperimentalFireSolutionAuthority()
                   && activeWeapon != null
                   && rightGrip != null
                   && !(useCharacterWeaponPlacementPoints && HasAnyCharacterWeaponPlacementPoint());
        }

        private bool ApplyCharacterWeaponPlacementAuthority()
        {
            activeCharacterWeaponPlacementPointName = string.Empty;
            return false;
        }

        private void AlignWeaponGripToWorldPose(Transform grip, Transform target, float blend)
        {
        }

        private bool IsGeneratedCharacterCarryPoint(Transform point)
        {
            return useGeneratedCharacterCarryPoints
                   && bodyKeepoutSolver != null
                   && bodyKeepoutSolver.OwnsGeneratedCarryPoint(point);
        }

        private void AlignWeaponBodyContactToGeneratedCarryPoint(Transform carryPoint, float blend)
        {
        }

        private void PinGeneratedBodyContactAfterAim()
        {
        }

        private Quaternion ResolveGeneratedCarryWeaponRotation(Transform carryPoint)
        {
            Transform muzzle = ResolveRailgunMuzzleTransform();
            Vector3 desiredForward = ResolveGeneratedCarryForward(carryPoint);
            if (muzzle == null || desiredForward.sqrMagnitude <= 0.0001f || muzzle.forward.sqrMagnitude <= 0.0001f)
            {
                return activeWeapon.transform.rotation;
            }

            Vector3 planarForward = Vector3.ProjectOnPlane(desiredForward, Vector3.up);
            Vector3 stableForward = planarForward.sqrMagnitude > 0.0001f ? planarForward.normalized : desiredForward.normalized;
            Quaternion rootToMuzzle = Quaternion.Inverse(activeWeapon.transform.rotation) * muzzle.rotation;
            Quaternion targetMuzzleRotation = Quaternion.LookRotation(stableForward, Vector3.up);
            return targetMuzzleRotation * Quaternion.Inverse(rootToMuzzle);
        }

        private Vector3 ResolveGeneratedCarryForward(Transform carryPoint)
        {
            if (motor != null && motor.BodyFacingDirection.sqrMagnitude > 0.0001f)
            {
                return motor.BodyFacingDirection.normalized;
            }

            if (carryPoint != null && carryPoint.forward.sqrMagnitude > 0.0001f)
            {
                return Vector3.ProjectOnPlane(carryPoint.forward, Vector3.up).sqrMagnitude > 0.0001f
                    ? Vector3.ProjectOnPlane(carryPoint.forward, Vector3.up).normalized
                    : carryPoint.forward.normalized;
            }

            Vector3 fallback = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : transform.forward;
        }

        private bool ShouldUseCharacterWeaponPlacementAuthority()
        {
            return useCharacterWeaponPlacementPoints
                   && UsesExperimentalFireSolutionAuthority()
                   && activeWeapon != null;
        }

        private bool ShouldUseAirborneAimCarry()
        {
            return motor != null && (motor.IsAirborne || motor.IsJumpCharging) && motor.AimHeld;
        }

        private bool ShouldUseAirborneRegularCarry()
        {
            return motor != null && (motor.IsAirborne || motor.IsJumpCharging) && !motor.AimHeld;
        }

        private bool ShouldAllowWeaponUpperBodyPoseLayerDuringArmedJump()
        {
            return allowWeaponUpperBodyPoseLayerDuringArmedJump
                   && activeWeapon != null
                   && motor != null
                   && (motor.IsAirborne || motor.IsJumpCharging);
        }

        private bool TryResolveCharacterWeaponPlacementPoint(out Transform placementPoint)
        {
            if (!HasAnyCharacterWeaponPlacementPoint())
            {
                CacheCharacterWeaponPlacementPoints();
            }

            placementPoint = null;
            if (!ShouldUseCharacterWeaponPlacementAuthority())
            {
                return false;
            }

            if (activeRearAimGateMode == SCFRearAimGateMode.RightHandRearShot)
            {
                placementPoint = FirstNonNull(oneHandWeaponPlacementPoint, hipAimWeaponPlacementPoint, shoulderAimWeaponPlacementPoint, runWeaponPlacementPoint, idleWeaponPlacementPoint);
            }
            else if (ShouldUseAirborneAimCarry())
            {
                placementPoint = FirstNonNull(shoulderAimWeaponPlacementPoint, hipAimWeaponPlacementPoint, runWeaponPlacementPoint, idleWeaponPlacementPoint, oneHandWeaponPlacementPoint);
            }
            else if (ShouldUseAirborneRegularCarry())
            {
                placementPoint = FirstNonNull(runWeaponPlacementPoint, idleWeaponPlacementPoint, shoulderAimWeaponPlacementPoint, hipAimWeaponPlacementPoint, oneHandWeaponPlacementPoint);
            }
            else if (motor != null && motor.AimHeld)
            {
                placementPoint = motor.RunHeld
                    ? FirstNonNull(runWeaponPlacementPoint, hipAimWeaponPlacementPoint, shoulderAimWeaponPlacementPoint, idleWeaponPlacementPoint, oneHandWeaponPlacementPoint)
                    : FirstNonNull(shoulderAimWeaponPlacementPoint, hipAimWeaponPlacementPoint, idleWeaponPlacementPoint, oneHandWeaponPlacementPoint);
            }
            else if (motor != null && motor.SprintHeld)
            {
                placementPoint = FirstNonNull(sprintWeaponPlacementPoint, runWeaponPlacementPoint, idleWeaponPlacementPoint, hipAimWeaponPlacementPoint, shoulderAimWeaponPlacementPoint, oneHandWeaponPlacementPoint);
            }
            else if (motor != null && motor.RunHeld)
            {
                placementPoint = FirstNonNull(hipAimWeaponPlacementPoint, idleWeaponPlacementPoint, runWeaponPlacementPoint, shoulderAimWeaponPlacementPoint, oneHandWeaponPlacementPoint);
            }
            else
            {
                placementPoint = FirstNonNull(idleWeaponPlacementPoint, runWeaponPlacementPoint, hipAimWeaponPlacementPoint, shoulderAimWeaponPlacementPoint, oneHandWeaponPlacementPoint);
            }

            return placementPoint != null;
        }

        private bool HasAnyCharacterWeaponPlacementPoint()
        {
            return idleWeaponPlacementPoint != null
                   || hipAimWeaponPlacementPoint != null
                   || shoulderAimWeaponPlacementPoint != null
                   || runWeaponPlacementPoint != null
                   || sprintWeaponPlacementPoint != null
                   || oneHandWeaponPlacementPoint != null;
        }

        private bool TryResolveCharacterRightGripCarryPoint(out Transform carryPoint)
        {
            if (!HasAnyCharacterRightGripCarryPoint())
            {
                CacheCharacterRightGripCarryPoints();
            }

            carryPoint = null;
            if (!ShouldUseCharacterRightGripCarryPointAuthority())
            {
                return false;
            }

            if (activeRearAimGateMode == SCFRearAimGateMode.RightHandRearShot)
            {
                carryPoint = FirstNonNull(oneHandRightGripCarryPoint, hipAimRightGripCarryPoint, shoulderAimRightGripCarryPoint, runRightGripCarryPoint, idleRightGripCarryPoint);
            }
            else if (ShouldUseAirborneAimCarry())
            {
                carryPoint = FirstNonNull(shoulderAimRightGripCarryPoint, hipAimRightGripCarryPoint, runRightGripCarryPoint, idleRightGripCarryPoint);
            }
            else if (ShouldUseAirborneRegularCarry())
            {
                carryPoint = FirstNonNull(runRightGripCarryPoint, idleRightGripCarryPoint, shoulderAimRightGripCarryPoint, hipAimRightGripCarryPoint);
            }
            else if (motor != null && motor.AimHeld)
            {
                carryPoint = motor.RunHeld
                    ? FirstNonNull(runRightGripCarryPoint, hipAimRightGripCarryPoint, shoulderAimRightGripCarryPoint, idleRightGripCarryPoint)
                    : FirstNonNull(shoulderAimRightGripCarryPoint, hipAimRightGripCarryPoint, idleRightGripCarryPoint);
            }
            else if (motor != null && motor.SprintHeld)
            {
                carryPoint = FirstNonNull(sprintRightGripCarryPoint, runRightGripCarryPoint, idleRightGripCarryPoint, hipAimRightGripCarryPoint, shoulderAimRightGripCarryPoint);
            }
            else if (motor != null && motor.RunHeld)
            {
                carryPoint = FirstNonNull(runRightGripCarryPoint, idleRightGripCarryPoint, hipAimRightGripCarryPoint, shoulderAimRightGripCarryPoint);
            }
            else
            {
                carryPoint = FirstNonNull(idleRightGripCarryPoint, runRightGripCarryPoint, hipAimRightGripCarryPoint, shoulderAimRightGripCarryPoint);
            }

            return carryPoint != null;
        }

        private bool HasAnyCharacterRightGripCarryPoint()
        {
            return idleRightGripCarryPoint != null
                   || hipAimRightGripCarryPoint != null
                   || shoulderAimRightGripCarryPoint != null
                   || runRightGripCarryPoint != null
                   || sprintRightGripCarryPoint != null
                   || oneHandRightGripCarryPoint != null;
        }

        private void ApplyMuzzleAimAuthority()
        {
        }

        private bool TryResolveMuzzleAimPivot(out Vector3 pivot)
        {
            pivot = Vector3.zero;
            if (rightGrip == null)
            {
                return false;
            }

            pivot = rightGrip.position;
            return true;
        }

        private void ApplyGripPivotMuzzleAim(Transform muzzle, Vector3 currentDirection, Vector3 desiredDirection, float blend, Vector3 pivot)
        {
        }

        private bool TryResolveMuzzleAimDirection(Transform muzzle, out Vector3 direction)
        {
            direction = Vector3.zero;
            if (muzzle == null)
            {
                return false;
            }

            if (!TryResolveMuzzleAimTargetPoint(muzzle, out Vector3 targetPoint))
            {
                return false;
            }

            if (ShouldUseEyeCarryForMuzzleTarget(muzzle, targetPoint))
            {
                if (holdWeaponPoseForSelfAimProtection)
                {
                    return false;
                }

                direction = ResolveEyeCarryDirection(muzzle);
            }
            else
            {
                direction = targetPoint - muzzle.position;
            }

            return direction.sqrMagnitude > 0.0001f;
        }

        private bool ShouldHoldWeaponPoseForSelfAimProtection()
        {
            if (!holdWeaponPoseForSelfAimProtection
                || activeWeapon == null
                || motor == null
                || !motor.AimHeld
                || activeRearAimGateMode != SCFRearAimGateMode.None)
            {
                return false;
            }

            Transform muzzle = ResolveRailgunMuzzleTransform();
            return muzzle != null
                   && TryResolveMuzzleAimTargetPoint(muzzle, out Vector3 targetPoint)
                   && ShouldUseEyeCarryForMuzzleTarget(muzzle, targetPoint);
        }

        private bool TryResolveMuzzleAimTargetPoint(Transform muzzle, out Vector3 targetPoint)
        {
            targetPoint = Vector3.zero;
            if (muzzle == null)
            {
                return false;
            }

            if (motor != null && motor.HasAimWorldPoint)
            {
                targetPoint = motor.AimWorldPoint;
            }
            else if (motor != null && motor.HasAimDirection)
            {
                targetPoint = muzzle.position + motor.AimDirection.normalized * railgunFireRange;
            }
            else
            {
                return false;
            }

            targetPoint = ResolveMuzzleAimFocusPoint(muzzle, targetPoint, ResolveFaceOrigin());
            return true;
        }

        private bool ShouldUseEyeCarryForMuzzleTarget(Transform muzzle, Vector3 targetPoint)
        {
            return false;
        }

        private Vector3 ResolveEyeCarryDirection(Transform muzzle)
        {
            Transform face = ResolveFaceOrigin();
            if (face != null && face.forward.sqrMagnitude > 0.0001f)
            {
                return face.forward.normalized;
            }

            if (motor != null && motor.HasAimDirection && motor.AimDirection.sqrMagnitude > 0.0001f)
            {
                return motor.AimDirection.normalized;
            }

            if (muzzle != null && muzzle.forward.sqrMagnitude > 0.0001f)
            {
                return muzzle.forward.normalized;
            }

            return transform.forward.sqrMagnitude > 0.0001f ? transform.forward.normalized : Vector3.forward;
        }

        private void UpdateActionRearAimGateState()
        {
            activeRearAimGateMode = SCFRearAimGateMode.None;
            activeRearAimYaw = 0f;

            if (activeWeapon == null
                || motor == null
                || !motor.AimHeld
                || !motor.HasAimDirection)
            {
                return;
            }

            if (!TryResolveAimYaw(out activeRearAimYaw))
            {
                return;
            }

            if (ShouldUseDominantSideAimGate(activeRearAimYaw))
            {
                activeRearAimGateMode = dominantSideAimUsesOneHand
                    ? SCFRearAimGateMode.RightHandRearShot
                    : SCFRearAimGateMode.InwardTwoHandTurn;
                return;
            }

            if (!useActionRearAimGate || !IsActionRearAimGateState())
            {
                return;
            }

            if (Mathf.Abs(activeRearAimYaw) < actionRearAimYawThreshold)
            {
                return;
            }

            bool aimIsOnRightSide = activeRearAimYaw > 0f;
            activeRearAimGateMode = aimIsOnRightSide && rightSideRearAimUsesOneHand
                ? SCFRearAimGateMode.RightHandRearShot
                : SCFRearAimGateMode.InwardTwoHandTurn;
        }

        private bool TryResolveAimYaw(out float yaw)
        {
            yaw = 0f;
            if (motor == null || !motor.HasAimDirection)
            {
                return false;
            }

            Vector3 bodyForward = motor.BodyFacingDirection.sqrMagnitude > 0.0001f
                ? motor.BodyFacingDirection
                : transform.forward;
            bodyForward = Vector3.ProjectOnPlane(bodyForward, Vector3.up);

            Vector3 aimForward = Vector3.ProjectOnPlane(motor.AimDirection, Vector3.up);
            if (bodyForward.sqrMagnitude <= 0.0001f || aimForward.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            yaw = SignedAngleAroundAxis(bodyForward.normalized, aimForward.normalized, Vector3.up);
            return true;
        }

        private bool ShouldUseDominantSideAimGate(float yaw)
        {
            if (!useExperimentalDominantSideAimGate
                || !UsesExperimentalFireSolutionAuthority()
                || motor == null
                || !motor.AimHeld)
            {
                return false;
            }

            if (Mathf.Abs(yaw) < dominantSideAimYawThreshold)
            {
                return false;
            }

            return dominantGripSideIsRight ? yaw > 0f : yaw < 0f;
        }

        private bool IsActionRearAimGateState()
        {
            return motor != null
                   && (motor.IsCombatRolling
                       || motor.IsVaulting
                       || motor.IsAirborne
                       || motor.IsWallRunning);
        }

        private void ClampSelectedWeaponLocalXAxis(bool forceNeutral = false)
        {
        }

        private bool ShouldHoldSelectedWeaponXNeutral()
        {
            if (motor == null)
            {
                return true;
            }

            if (motor.AimHeld)
            {
                return !motor.RunHeld;
            }

            if (!motor.RunHeld)
            {
                return true;
            }

            float threshold = selectedWeaponXMovingThreshold;
            return motor.PlanarVelocity.sqrMagnitude <= threshold * threshold;
        }

        private Transform ResolveSocketParent()
        {
            if (ShouldParentWeaponSocketToRightHand())
            {
                if (rightArm.Hand == null && animator != null)
                {
                    CacheBones();
                }

                return FirstNonNull(rightArm.Hand, rightArm.GripContact, chestAnchor, animator != null ? animator.transform : null, transform);
            }

            return FirstNonNull(chestAnchor, animator != null ? animator.transform : null, transform);
        }

        private string ResolveWeaponSocketName()
        {
            if (ShouldParentWeaponSocketToRightHand())
            {
                return string.IsNullOrWhiteSpace(experimentalRightHandSocketName)
                    ? RightHandWeaponSocketName
                    : experimentalRightHandSocketName;
            }

            return string.IsNullOrWhiteSpace(torsoSocketName) ? "SCF_ChestWeaponSocket" : torsoSocketName;
        }

        private bool ShouldParentWeaponSocketToRightHand()
        {
            return ShouldUseRightHandWeaponOwnership()
                   && experimentalParentWeaponSocketToRightHand;
        }

        private bool ShouldUseRightHandWeaponOwnership()
        {
            return experimentalRightGripOwnsWeapon
                   && HasSoldierVisualIdentity()
                   && !IsReferenceShotgunInstance();
        }

        private bool CanApplyHandIk()
        {
            return enableHandIk
                   && activeWeapon != null
                   && rightGrip != null
                   && leftGrip != null
                   && (rightArm.Hand != null || leftArm.Hand != null);
        }

        private bool ShouldApplyBoneFallback()
        {
            return CanApplyHandIk()
                   && !ShouldUseExperimentalWristOnlyGripLocks()
                   && genericBoneFallbackWeight > 0.001f
                   && (ShouldUseExperimentalIndexGripContacts() || animator == null || !animator.isHuman || applyBoneFallbackForHumanoids);
        }

        private bool ShouldUseExperimentalWristOnlyGripLocks()
        {
            return UsesExperimentalFireSolutionAuthority() && !UsesLegacyTpsWeaponFit();
        }

        private bool ShouldUseExperimentalIndexGripContacts()
        {
            return useExperimentalIndexFingerGripContacts
                   && HasExperimentalSoldierVisualIdentity();
        }

        private bool UsesExperimentalFireSolutionAuthority()
        {
            return useExperimentalFireSolutionAuthority
                   && ShouldUseExperimentalIndexGripContacts();
        }

        private float ResolveExperimentalGripContactLockWeight(bool rightHand)
        {
            if (!UsesExperimentalFireSolutionAuthority() || activeWeapon == null)
            {
                return 0f;
            }

            if (rightHand)
            {
                return experimentalHardLockRightGripContact ? Mathf.Clamp01(experimentalRightGripContactLockWeight) : 0f;
            }

            if (!experimentalHardLockLeftGripContact)
            {
                return 0f;
            }

            return activeRearAimGateMode == SCFRearAimGateMode.RightHandRearShot
                ? Mathf.Clamp01(experimentalOneHandLeftGripContactLockWeight)
                : Mathf.Clamp01(experimentalLeftGripContactLockWeight);
        }

        private void ApplyExperimentalWeaponGripOwnership()
        {
        }

        private void ApplyWeaponRotationAroundRightGrip(Quaternion targetGripRotation, float weight)
        {
        }

        private bool TryResolveExperimentalRightGripTargetRotation(out Quaternion targetGripRotation)
        {
            targetGripRotation = Quaternion.identity;
            if (!TryResolveExperimentalDesiredRightPalmFrame(out Quaternion palmFrame))
            {
                return false;
            }

            targetGripRotation = palmFrame * Quaternion.Euler(experimentalRightPalmToPistolGripEulerOffset);
            return true;
        }

        private bool TryResolveExperimentalDesiredRightPalmFrame(out Quaternion palmFrame)
        {
            palmFrame = Quaternion.identity;
            if (rightArm.Hand == null)
            {
                return false;
            }

            if (!TryResolveExperimentalRightPalmAimForward(out Vector3 forward))
            {
                return TryResolveCurrentRightPalmFrame(out palmFrame);
            }

            Vector3 up = Vector3.ProjectOnPlane(Vector3.up, forward);
            if (up.sqrMagnitude <= 0.0001f && TryResolveCurrentRightPalmFrame(out Quaternion currentPalmFrame))
            {
                up = Vector3.ProjectOnPlane(currentPalmFrame * Vector3.up, forward);
            }

            if (up.sqrMagnitude <= 0.0001f)
            {
                up = rightArm.Hand.up;
            }

            return TryBuildLookRotation(forward, up, out palmFrame);
        }

        private bool TryResolveExperimentalRightPalmAimForward(out Vector3 forward)
        {
            forward = Vector3.zero;
            if (rightArm.Hand == null)
            {
                return false;
            }

            Vector3 origin = rightArm.Hand.position;
            if (experimentalRightPalmPointsAtTarget && motor != null && motor.AimHeld)
            {
                if (motor.HasAimWorldPoint)
                {
                    Transform muzzle = ResolveRailgunMuzzleTransform();
                    Vector3 targetPoint = ResolveMuzzleAimFocusPoint(muzzle, motor.AimWorldPoint, ResolveFaceOrigin());
                    forward = targetPoint - origin;
                }
                else if (motor.HasAimDirection)
                {
                    forward = motor.AimDirection;
                }
            }

            if (forward.sqrMagnitude <= 0.0001f && motor != null && motor.BodyFacingDirection.sqrMagnitude > 0.0001f)
            {
                forward = motor.BodyFacingDirection;
            }

            if (forward.sqrMagnitude <= 0.0001f && rightArm.Hand.forward.sqrMagnitude > 0.0001f)
            {
                forward = rightArm.Hand.forward;
            }

            return forward.sqrMagnitude > 0.0001f;
        }

        private bool TryResolveCurrentRightPalmFrame(out Quaternion palmFrame)
        {
            palmFrame = Quaternion.identity;
            if (rightArm.Hand == null)
            {
                return false;
            }

            Transform index = FirstNonNull(rightArm.IndexContact, rightArm.GripContact);
            Vector3 forward = ResolveFingerDirection(index, rightArm.Hand);
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = rightArm.Hand.forward;
            }

            Vector3 thumbDirection = ResolveFingerDirection(rightArm.ThumbContact, rightArm.Hand);
            Vector3 up = Vector3.ProjectOnPlane(thumbDirection, forward);
            if (up.sqrMagnitude <= 0.0001f)
            {
                up = Vector3.ProjectOnPlane(rightArm.Hand.up, forward);
            }

            return TryBuildLookRotation(forward, up, out palmFrame);
        }

        private static Vector3 ResolveFingerDirection(Transform fingerRoot, Transform hand)
        {
            if (fingerRoot != null)
            {
                Transform child = FindFirstChildTransform(fingerRoot);
                if (child != null)
                {
                    Vector3 childDirection = child.position - fingerRoot.position;
                    if (childDirection.sqrMagnitude > 0.0001f)
                    {
                        return childDirection;
                    }
                }

                if (hand != null)
                {
                    Vector3 rootDirection = fingerRoot.position - hand.position;
                    if (rootDirection.sqrMagnitude > 0.0001f)
                    {
                        return rootDirection;
                    }
                }
            }

            return hand != null ? hand.forward : Vector3.zero;
        }

        private void ApplyExperimentalGripContactLocks()
        {
            if (!UsesExperimentalFireSolutionAuthority() || activeWeapon == null)
            {
                return;
            }

            ApplyExperimentalGripContactLock(rightArm, rightGrip, true);
            ApplyExperimentalGripContactLock(leftArm, leftGrip, false);
        }

        private void ApplyExperimentalGripContactLock(ArmRig rig, Transform grip, bool rightHand)
        {
            Transform effector = rig.EndEffector;
            float lockWeight = ResolveExperimentalGripContactLockWeight(rightHand);
            if (rig.Hand == null || effector == null || grip == null || lockWeight <= 0.001f)
            {
                return;
            }

            if (ShouldUseExperimentalWristOnlyGripLocks())
            {
                ApplyExperimentalWristOnlyGripLock(rig, grip, effector, lockWeight, rightHand);
                return;
            }

            Vector3 targetPosition = ResolveAnatomyLimitedTarget(rig, grip.position, rightHand ? rightArmReachWeight : leftArmReachWeight, rightHand, out float reach01);
            float constrainedLockWeight = Mathf.Clamp01(lockWeight * ResolveGripReachWeight(reach01));
            if (solveGripContactLocksThroughArmChain)
            {
                RotateArmChainTowardTarget(rig, effector, targetPosition, Mathf.Clamp01(constrainedLockWeight * gripContactArmSolveWeight));
                ApplyForearmJointGuardrail(rig, rightHand, constrainedLockWeight);
                targetPosition = ResolveAnatomyLimitedTarget(rig, grip.position, rightHand ? rightArmReachWeight : leftArmReachWeight, rightHand, out reach01);
                constrainedLockWeight = Mathf.Clamp01(lockWeight * ResolveGripReachWeight(reach01));
            }

            rig.Hand.position += ResolveWristLimitedContactDelta(targetPosition - effector.position) * constrainedLockWeight;

            if (rightHand)
            {
                ApplyExperimentalRightPalmAimToHand(constrainedLockWeight);
                ApplyExperimentalRightFingerWrap(constrainedLockWeight);
            }

            if (!rotateExperimentalIndexFingerGripContacts)
            {
                return;
            }

            float rotationWeight = Mathf.Clamp01(constrainedLockWeight * experimentalIndexFingerGripRotationWeight);
            if (rotationWeight <= 0.001f)
            {
                return;
            }

            Quaternion contactToGrip = grip.rotation * Quaternion.Inverse(effector.rotation);
            Quaternion targetRotation = ResolveWristLimitedRotation(rig, contactToGrip * rig.Hand.rotation, rig.Hand.rotation);
            rig.Hand.rotation = Quaternion.Slerp(rig.Hand.rotation, targetRotation, rotationWeight);
        }

        private void ApplyExperimentalWristOnlyGripLock(ArmRig rig, Transform grip, Transform effector, float lockWeight, bool rightHand)
        {
            if (rig.Hand == null || grip == null || effector == null || lockWeight <= 0.001f)
            {
                return;
            }

            float rotationWeight = Mathf.Clamp01(lockWeight * experimentalWristOnlyGripRotationWeight);
            if (rotationWeight > 0.001f)
            {
                Quaternion targetRotation;
                if (effector == rig.Hand)
                {
                    targetRotation = grip.rotation;
                }
                else
                {
                    Quaternion contactToGrip = grip.rotation * Quaternion.Inverse(effector.rotation);
                    targetRotation = contactToGrip * rig.Hand.rotation;
                }

                targetRotation = ResolveWristLimitedRotation(rig, targetRotation, rig.Hand.rotation);
                rig.Hand.rotation = Quaternion.Slerp(rig.Hand.rotation, targetRotation, rotationWeight);
            }

            if (rightHand)
            {
                ApplyExperimentalRightPalmAimToHand(lockWeight);
                ApplyExperimentalRightFingerWrap(lockWeight);
            }
        }

        private void ApplyExperimentalRightPalmAimToHand(float weight)
        {
            if (!experimentalRightPalmPointsAtTarget
                || rightArm.Hand == null
                || weight <= 0.001f
                || !TryResolveCurrentRightPalmFrame(out Quaternion currentPalmFrame)
                || !TryResolveExperimentalDesiredRightPalmFrame(out Quaternion desiredPalmFrame))
            {
                return;
            }

            Quaternion delta = desiredPalmFrame * Quaternion.Inverse(currentPalmFrame);
            Quaternion targetHandRotation = delta * rightArm.Hand.rotation;
            float rotationWeight = Mathf.Clamp01(weight * experimentalRightPalmAimWeight);
            rightArm.Hand.rotation = Quaternion.Slerp(rightArm.Hand.rotation, targetHandRotation, rotationWeight);
        }

        private void ApplyExperimentalRightFingerWrap(float weight)
        {
            if (!experimentalWrapRightFingersAroundGrip || rightGrip == null || weight <= 0.001f)
            {
                return;
            }

            float wrapWeight = Mathf.Clamp01(weight * experimentalRightFingerWrapWeight);
            RotateFingerTowardGrip(rightArm.IndexContact, rightGrip.TransformPoint(experimentalRightIndexWrapLocalOffset), wrapWeight);
            RotateFingerTowardGrip(rightArm.ThumbContact, rightGrip.TransformPoint(experimentalRightThumbWrapLocalOffset), wrapWeight);
        }

        private static void RotateFingerTowardGrip(Transform fingerRoot, Vector3 targetPosition, float weight)
        {
            if (fingerRoot == null || weight <= 0.001f)
            {
                return;
            }

            Transform fingerTip = FindFirstChildTransform(fingerRoot);
            if (fingerTip == null)
            {
                return;
            }

            RotateBoneTowardTarget(fingerRoot, fingerTip, targetPosition, weight);
        }

        private float ResolveExperimentalGripOwnershipWeight()
        {
            if (!ShouldUseRightHandWeaponOwnership()
                || activeWeapon == null
                || !experimentalRightGripOwnsWeapon)
            {
                return 0f;
            }

            return Mathf.Clamp01(experimentalRightGripOwnershipWeight);
        }

        private void ApplyHumanoidArmIk(
            Animator targetAnimator,
            AvatarIKGoal goal,
            Transform grip,
            ArmRig rig,
            float weight,
            float reachWeight)
        {
            if (grip == null || weight <= 0.001f)
            {
                targetAnimator.SetIKPositionWeight(goal, 0f);
                targetAnimator.SetIKRotationWeight(goal, 0f);
                return;
            }

            bool rightHand = goal == AvatarIKGoal.RightHand;
            Vector3 targetPosition = ResolveAnatomyLimitedTarget(rig, grip.position, reachWeight, rightHand, out float reach01);
            float reachWeight01 = ResolveGripReachWeight(reach01);
            float finalPositionWeight = Mathf.Clamp01(weight * reachWeight01);
            float finalRotationWeight = Mathf.Clamp01(weight * Mathf.Lerp(unreachableRotationWeight, 1f, reach01) * reachWeight01);

            targetAnimator.SetIKPositionWeight(goal, finalPositionWeight);
            targetAnimator.SetIKRotationWeight(goal, finalRotationWeight);
            targetAnimator.SetIKPosition(goal, targetPosition);
            targetAnimator.SetIKRotation(goal, ResolveWristLimitedRotation(rig, grip.rotation, rig.Hand != null ? rig.Hand.rotation : grip.rotation));
        }

        private void ApplyGenericArmToGrip(ArmRig rig, Transform grip, float weight, float reachWeight, bool rightHand)
        {
            Transform effector = rig.EndEffector;
            float hardLockWeight = ResolveExperimentalGripContactLockWeight(rightHand);
            if (rig.Hand == null || effector == null || grip == null || (weight <= 0.001f && hardLockWeight <= 0.001f))
            {
                return;
            }

            float effectiveWeight = Mathf.Max(weight, hardLockWeight);
            float effectiveReachWeight = hardLockWeight > 0.001f ? Mathf.Max(1f, reachWeight) : reachWeight;
            Vector3 targetPosition = ResolveAnatomyLimitedTarget(rig, grip.position, effectiveReachWeight, rightHand, out float reach01);
            float reachWeight01 = ResolveGripReachWeight(reach01);
            float adjustedWeight = Mathf.Clamp01(effectiveWeight * genericBoneFallbackWeight * effectiveReachWeight * reachWeight01);
            float constrainedHardLockWeight = Mathf.Clamp01(hardLockWeight * reachWeight01);
            int iterations = Mathf.Clamp(genericSolveIterations, 1, 8);
            RotateArmChainTowardTarget(rig, effector, targetPosition, adjustedWeight, iterations);
            ApplyForearmJointGuardrail(rig, rightHand, adjustedWeight);

            float finalPositionWeight = Mathf.Clamp01(adjustedWeight * finalGripPositionWeight);
            float finalRotationWeight = Mathf.Clamp01(adjustedWeight * finalGripRotationWeight * Mathf.Lerp(unreachableRotationWeight, 1f, reach01));
            finalPositionWeight = Mathf.Max(finalPositionWeight, constrainedHardLockWeight);
            Quaternion wristBaseline = rig.Hand.rotation;
            Quaternion wristLimitedGripRotation = ResolveWristLimitedRotation(rig, grip.rotation, wristBaseline);
            if (effector == rig.Hand)
            {
                rig.Hand.rotation = Quaternion.Slerp(rig.Hand.rotation, wristLimitedGripRotation, adjustedWeight * 0.75f);
                rig.Hand.position = Vector3.Lerp(rig.Hand.position, targetPosition, finalPositionWeight);
                rig.Hand.rotation = Quaternion.Slerp(rig.Hand.rotation, wristLimitedGripRotation, finalRotationWeight);
                return;
            }

            float contactPositionWeight = Mathf.Clamp01(finalPositionWeight * experimentalIndexFingerGripPositionWeight);
            contactPositionWeight = Mathf.Max(contactPositionWeight, constrainedHardLockWeight);
            Vector3 contactDelta = ResolveWristLimitedContactDelta(targetPosition - effector.position);
            rig.Hand.position = Vector3.Lerp(rig.Hand.position, rig.Hand.position + contactDelta, contactPositionWeight);

            if (rotateExperimentalIndexFingerGripContacts)
            {
                float contactRotationWeight = Mathf.Clamp01(finalRotationWeight * experimentalIndexFingerGripRotationWeight);
                Quaternion contactToGrip = grip.rotation * Quaternion.Inverse(effector.rotation);
                Quaternion targetRotation = ResolveWristLimitedRotation(rig, contactToGrip * rig.Hand.rotation, rig.Hand.rotation);
                rig.Hand.rotation = Quaternion.Slerp(rig.Hand.rotation, targetRotation, contactRotationWeight);
            }
        }

        private void RotateArmChainTowardTarget(ArmRig rig, Transform endEffector, Vector3 targetPosition, float weight)
        {
            RotateArmChainTowardTarget(rig, endEffector, targetPosition, weight, Mathf.Clamp(genericSolveIterations, 1, 8));
        }

        private static void RotateArmChainTowardTarget(ArmRig rig, Transform endEffector, Vector3 targetPosition, float weight, int iterations)
        {
            if (endEffector == null || weight <= 0.001f)
            {
                return;
            }

            int safeIterations = Mathf.Clamp(iterations, 1, 8);
            for (int i = 0; i < safeIterations; i++)
            {
                float passWeight = weight * (1f - i * 0.12f);
                RotateBoneTowardTarget(rig.Shoulder, endEffector, targetPosition, passWeight * 0.28f);
                RotateBoneTowardTarget(rig.UpperArm, endEffector, targetPosition, passWeight * 0.68f);
                RotateBoneTowardTarget(rig.Forearm, endEffector, targetPosition, passWeight);
            }
        }

        private void ApplyForearmJointGuardrail(ArmRig rig, bool rightHand, float weight)
        {
            if (!useAnatomyAwareIk
                || !constrainForearmJointAgainstBody
                || bodyKeepoutSolver == null
                || rig.UpperArm == null
                || rig.Forearm == null
                || weight <= 0.001f)
            {
                return;
            }

            Vector3 desiredJointPosition = bodyKeepoutSolver.ResolveJointPosition(rig.Forearm.position, rightHand, out bool adjusted);
            if (!adjusted)
            {
                return;
            }

            Vector3 currentDirection = rig.Forearm.position - rig.UpperArm.position;
            Vector3 targetDirection = desiredJointPosition - rig.UpperArm.position;
            RotateBoneFromTo(rig.UpperArm, currentDirection, targetDirection, Mathf.Clamp01(weight * forearmJointGuardrailWeight));
        }

        private Quaternion ResolveWristLimitedRotation(ArmRig rig, Quaternion desiredRotation, Quaternion animatedRotation)
        {
            if (!constrainWristRotation
                || rig.Hand == null
                || maxWristRotationFromAnimatedPose >= 89.9f
                || wristRotationLimitWeight <= 0.001f)
            {
                return desiredRotation;
            }

            Quaternion limitedRotation = ClampRotationDelta(animatedRotation, desiredRotation, maxWristRotationFromAnimatedPose);
            return Quaternion.Slerp(desiredRotation, limitedRotation, wristRotationLimitWeight);
        }

        private Vector3 ResolveWristLimitedContactDelta(Vector3 delta)
        {
            if (!limitWristContactTranslation || maxWristContactTranslation <= 0f)
            {
                return delta;
            }

            float magnitude = delta.magnitude;
            if (magnitude <= maxWristContactTranslation || magnitude <= 0.0001f)
            {
                return delta;
            }

            return delta / magnitude * maxWristContactTranslation;
        }

        private static Quaternion ClampRotationDelta(Quaternion baseRotation, Quaternion desiredRotation, float maxDegrees)
        {
            float safeMax = Mathf.Max(0f, maxDegrees);
            Quaternion delta = desiredRotation * Quaternion.Inverse(baseRotation);
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if (axis.sqrMagnitude <= 0.0001f
                || float.IsNaN(axis.x)
                || float.IsNaN(axis.y)
                || float.IsNaN(axis.z)
                || float.IsNaN(angle))
            {
                return desiredRotation;
            }

            if (angle > 180f)
            {
                angle -= 360f;
            }

            float clampedAngle = Mathf.Clamp(angle, -safeMax, safeMax);
            return Quaternion.AngleAxis(clampedAngle, axis.normalized) * baseRotation;
        }

        private Vector3 ResolveAnatomyLimitedTarget(ArmRig rig, Vector3 targetPosition, float reachWeight, bool rightHand, out float reach01)
        {
            Vector3 limitedTarget = ResolveReachLimitedTarget(rig, targetPosition, reachWeight, out reach01);
            if (useDynamicBodyKeepoutZones && bodyKeepoutSolver != null)
            {
                Transform armRoot = FirstNonNull(rig.Shoulder, rig.UpperArm, rig.Hand, chestAnchor, animator != null ? animator.transform : null, transform);
                limitedTarget = bodyKeepoutSolver.ResolveArmTarget(armRoot.position, limitedTarget, rightHand, out _);
            }

            return ResolveBodySideLimitedTarget(rig, limitedTarget, rightHand);
        }

        private Vector3 ResolveBodySideLimitedTarget(ArmRig rig, Vector3 targetPosition, bool rightHand)
        {
            if (!useAnatomyAwareIk || !constrainGripTargetsAgainstBody)
            {
                return targetPosition;
            }

            Transform body = chestAnchor != null ? chestAnchor : (animator != null ? animator.transform : transform);
            if (body == null)
            {
                return targetPosition;
            }

            Vector3 bodyRight = ResolvePlanarAxis(body.right, Vector3.right);
            Vector3 bodyForward = ResolvePlanarAxis(body.forward, Vector3.forward);
            Vector3 bodyOrigin = body.position;
            float bodyScale = Mathf.Max(0.25f, ResolveArmLength(rig) * 0.5f);
            float crossAllowance = bodyScale * gripTargetBodyCrossAllowance;
            float behindAllowance = bodyScale * gripTargetBehindBodyAllowance;
            Vector3 offset = targetPosition - bodyOrigin;
            float side = Vector3.Dot(offset, bodyRight);
            float forward = Vector3.Dot(offset, bodyForward);

            if (rightHand && side < -crossAllowance)
            {
                targetPosition += bodyRight * (-crossAllowance - side);
            }
            else if (!rightHand && side > crossAllowance)
            {
                targetPosition -= bodyRight * (side - crossAllowance);
            }

            if (forward < -behindAllowance)
            {
                targetPosition += bodyForward * (-behindAllowance - forward);
            }

            return targetPosition;
        }

        private static Vector3 ResolvePlanarAxis(Vector3 axis, Vector3 fallback)
        {
            Vector3 planar = Vector3.ProjectOnPlane(axis, Vector3.up);
            if (planar.sqrMagnitude > 0.0001f)
            {
                return planar.normalized;
            }

            if (axis.sqrMagnitude > 0.0001f)
            {
                return axis.normalized;
            }

            return fallback.normalized;
        }

        private float ResolveGripReachWeight(float reach01)
        {
            if (!useAnatomyAwareIk)
            {
                return 1f;
            }

            return Mathf.Lerp(unreachableGripWeightFloor, 1f, Mathf.Clamp01(reach01));
        }

        private Vector3 ResolveReachLimitedTarget(ArmRig rig, Vector3 targetPosition, float reachWeight, out float reach01)
        {
            reach01 = 1f;
            if (!useAnatomyAwareIk || rig.Hand == null)
            {
                return targetPosition;
            }

            Transform root = rig.UpperArm != null ? rig.UpperArm : rig.Shoulder;
            if (root == null)
            {
                return targetPosition;
            }

            float armLength = ResolveArmLength(rig);
            if (armLength <= 0.0001f)
            {
                return targetPosition;
            }

            float reachScale = Mathf.Clamp(reachWeight, 0.1f, 1f);
            float hardReach = armLength * maxArmReachMultiplier * reachScale;
            float softReach = hardReach + armLength * armReachSoftZone;
            Vector3 toTarget = targetPosition - root.position;
            float distance = toTarget.magnitude;
            if (distance <= hardReach || distance <= 0.0001f)
            {
                return targetPosition;
            }

            reach01 = Mathf.Clamp01(1f - Mathf.InverseLerp(hardReach, softReach, distance));
            return root.position + toTarget / distance * hardReach;
        }

        private static float ResolveArmLength(ArmRig rig)
        {
            float length = 0f;
            if (rig.UpperArm != null && rig.Forearm != null)
            {
                length += Vector3.Distance(rig.UpperArm.position, rig.Forearm.position);
            }

            if (rig.Forearm != null && rig.Hand != null)
            {
                length += Vector3.Distance(rig.Forearm.position, rig.Hand.position);
            }

            if (rig.Hand != null && rig.GripContact != null && rig.GripContact != rig.Hand)
            {
                length += Vector3.Distance(rig.Hand.position, rig.GripContact.position);
            }

            if (length <= 0.0001f && rig.Shoulder != null && rig.Hand != null)
            {
                length = Vector3.Distance(rig.Shoulder.position, rig.EndEffector.position);
            }

            return length;
        }

        private static void ClearAnimatorIkGoals(Animator targetAnimator)
        {
            targetAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
            targetAnimator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
            targetAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
            targetAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
        }

        private void ClearWeapon()
        {
            ClearWeaponInstanceOnly();
            if (weaponSocket != null)
            {
                DestroyUnityObject(weaponSocket.gameObject);
                weaponSocket = null;
            }

            rightGrip = null;
            leftGrip = null;
            buttstockTarget = null;
            raised01 = 0f;
            currentRightHandWeight = 0f;
            currentLeftHandWeight = 0f;
        }

        private void ClearWeaponInstanceOnly()
        {
            DestroyShotgunPose3GripTargets();
            activeWeaponSupportsRailgunFire = false;
            hasRuntimeWeaponFitPoseSlot = false;

            if (activeWeapon == null)
            {
                return;
            }

            DestroyUnityObject(activeWeapon);
            activeWeapon = null;
            rightGrip = null;
            leftGrip = null;
            referenceWeaponAnchor = null;
            railgunMuzzleTarget = null;
            buttstockTarget = null;
        }

        private void DestroyShotgunPose3GripTargets()
        {
            if (pose3RightGripTarget != null)
            {
                DestroyUnityObject(pose3RightGripTarget.gameObject);
                pose3RightGripTarget = null;
            }

            if (pose3LeftGripTarget != null)
            {
                DestroyUnityObject(pose3LeftGripTarget.gameObject);
                pose3LeftGripTarget = null;
            }

            if (rightGrip != null && string.Equals(rightGrip.name, Pose3RightGripTargetName, StringComparison.Ordinal))
            {
                rightGrip = null;
            }

            if (leftGrip != null && string.Equals(leftGrip.name, Pose3LeftGripTargetName, StringComparison.Ordinal))
            {
                leftGrip = null;
            }
        }

        private static bool IsSoldierCharacter(string characterName)
        {
            return !string.IsNullOrWhiteSpace(characterName)
                   && characterName.IndexOf("soldier", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsExperimentalSoldierCharacter(string characterName)
        {
            return !string.IsNullOrWhiteSpace(characterName)
                   && characterName.IndexOf("soldierExp", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool UsesLegacyTpsWeaponFit()
        {
            return false;
        }

        private bool UsesAuthoredWeaponPrefabAnchorsOnly()
        {
            return preferWeaponPrefabAnchors
                   && activeWeapon != null
                   && !IsReferenceShotgunInstance();
        }

        private Transform FindWeaponPrefabAnchor(string anchorName)
        {
            if (activeWeapon == null || string.IsNullOrWhiteSpace(anchorName))
            {
                return null;
            }

            if (UsesAuthoredWeaponPrefabAnchorsOnly())
            {
                Transform modelRoot = FindRailgunModelRoot();
                Transform modelAnchor = FindDescendantByName(modelRoot, anchorName);
                if (modelAnchor != null)
                {
                    return modelAnchor;
                }
            }

            Transform direct = FindDirectChild(activeWeapon.transform, anchorName);
            return direct != null ? direct : FindDescendantByName(activeWeapon.transform, anchorName);
        }

        private Transform FindRailgunModelRoot()
        {
            return activeWeapon != null ? FindDirectChild(activeWeapon.transform, RailgunModelRootName) : null;
        }

        private void PruneDuplicateWeaponAnchorInstances(string anchorName, Transform keep)
        {
            if (!UsesAuthoredWeaponPrefabAnchorsOnly() || activeWeapon == null || keep == null || string.IsNullOrWhiteSpace(anchorName))
            {
                return;
            }

            Transform[] transforms = activeWeapon.GetComponentsInChildren<Transform>(true);
            for (int i = transforms.Length - 1; i >= 0; i--)
            {
                Transform candidate = transforms[i];
                if (candidate == null || candidate == keep || !string.Equals(candidate.name, anchorName, StringComparison.Ordinal))
                {
                    continue;
                }

                DestroyUnityObject(candidate.gameObject);
            }
        }

        private bool HasExperimentalSoldierVisualIdentity()
        {
            if (IsExperimentalSoldierCharacter(activeCharacterName))
            {
                return true;
            }

            Transform current = animator != null ? animator.transform : null;
            while (current != null && current != transform)
            {
                if (IsExperimentalSoldierCharacter(current.name)
                    || string.Equals(current.name, "SCF_Selected_SCF_Player", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private bool HasSoldierVisualIdentity()
        {
            if (IsSoldierCharacter(activeCharacterName))
            {
                return true;
            }

            Transform current = animator != null ? animator.transform : null;
            while (current != null && current != transform)
            {
                if (IsSoldierCharacter(current.name))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static bool NearlyEqual(Vector3 left, Vector3 right)
        {
            return (left - right).sqrMagnitude <= 0.0001f;
        }

        private static Vector3 SanitizedScale(Vector3 value)
        {
            return value == Vector3.zero ? Vector3.one : value;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static float SampleGroundY(Vector3 position)
        {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                return terrain.SampleHeight(position) + terrain.transform.position.y;
            }

            Vector3 rayOrigin = position + Vector3.up * 8f;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 30f, ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.point.y;
            }

            return position.y;
        }

        private static void StripColliders(GameObject root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                DestroyUnityObject(colliders[i]);
            }
        }

        private static void RotateBoneTowardTarget(Transform bone, Transform endEffector, Vector3 target, float weight)
        {
            if (bone == null || endEffector == null || weight <= 0.001f)
            {
                return;
            }

            Vector3 currentDirection = endEffector.position - bone.position;
            Vector3 targetDirection = target - bone.position;
            if (currentDirection.sqrMagnitude <= 0.0001f || targetDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion delta = Quaternion.FromToRotation(currentDirection.normalized, targetDirection.normalized);
            bone.rotation = Quaternion.Slerp(bone.rotation, delta * bone.rotation, Mathf.Clamp01(weight));
        }

        private static void RotateBoneFromTo(Transform bone, Vector3 currentDirection, Vector3 targetDirection, float weight)
        {
            if (bone == null || weight <= 0.001f || currentDirection.sqrMagnitude <= 0.0001f || targetDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion delta = Quaternion.FromToRotation(currentDirection.normalized, targetDirection.normalized);
            bone.rotation = Quaternion.Slerp(bone.rotation, delta * bone.rotation, Mathf.Clamp01(weight));
        }

        private static void DrawGripGizmo(Transform target, Color color, float radius)
        {
            if (target == null)
            {
                return;
            }

            Gizmos.color = color;
            Gizmos.DrawWireSphere(target.position, radius);
            Gizmos.DrawRay(target.position, target.forward * radius * 2f);
        }

        private static float SignedAngleAroundAxis(Vector3 from, Vector3 to, Vector3 axis)
        {
            from -= Vector3.Project(from, axis);
            to -= Vector3.Project(to, axis);
            if (from.sqrMagnitude <= 0.0001f || to.sqrMagnitude <= 0.0001f)
            {
                return 0f;
            }

            float angle = Vector3.Angle(from, to);
            return angle * (Vector3.Dot(axis, Vector3.Cross(from, to)) < 0f ? -1f : 1f);
        }

        private static bool TryBuildLookRotation(Vector3 forward, Vector3 up, out Quaternion rotation)
        {
            rotation = Quaternion.identity;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            Vector3 safeForward = forward.normalized;
            Vector3 safeUp = up - Vector3.Project(up, safeForward);
            if (safeUp.sqrMagnitude <= 0.0001f)
            {
                safeUp = Vector3.up - Vector3.Project(Vector3.up, safeForward);
            }

            if (safeUp.sqrMagnitude <= 0.0001f)
            {
                safeUp = Vector3.Cross(safeForward, Vector3.right);
            }

            if (safeUp.sqrMagnitude <= 0.0001f)
            {
                safeUp = Vector3.Cross(safeForward, Vector3.forward);
            }

            if (safeUp.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            rotation = Quaternion.LookRotation(safeForward, safeUp.normalized);
            return true;
        }

        private static Transform FindFirstChildTransform(Transform root)
        {
            if (root == null || root.childCount <= 0)
            {
                return null;
            }

            return root.GetChild(0);
        }

        private static Transform FirstNonNull(params Transform[] transforms)
        {
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null)
                {
                    return transforms[i];
                }
            }

            return null;
        }

        private static Transform FindDirectChild(Transform parent, string childName)
        {
            if (parent == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(child.name, childName, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static Transform FindDescendantByName(Transform root, string childName)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate != null && string.Equals(candidate.name, childName, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static Transform FindDescendantByNameExcluding(Transform root, string childName, Transform excludedRoot)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate == null || (excludedRoot != null && candidate.IsChildOf(excludedRoot)))
                {
                    continue;
                }

                if (string.Equals(candidate.name, childName, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static Transform FindDescendantByCompactName(Transform root, string childName)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            string compactChildName = Compact(childName);
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate != null && Compact(candidate.name) == compactChildName)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void DestroyUnityObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private static Transform FindFirstBone(Transform root, params string[] patterns)
        {
            if (root == null || patterns == null || patterns.Length == 0)
            {
                return null;
            }

            Transform best = null;
            int bestScore = int.MaxValue;
            FindFirstBoneRecursive(root, patterns, 0, ref best, ref bestScore);
            return best;
        }

        private static Transform FindExactBone(Transform root, string boneName)
        {
            if (root == null || string.IsNullOrWhiteSpace(boneName))
            {
                return null;
            }

            string compactBoneName = Compact(boneName);
            return FindExactBoneRecursive(root, compactBoneName);
        }

        private static Transform FindExactBoneRecursive(Transform node, string compactBoneName)
        {
            if (Compact(node.name) == compactBoneName)
            {
                return node;
            }

            for (int i = 0; i < node.childCount; i++)
            {
                Transform match = FindExactBoneRecursive(node.GetChild(i), compactBoneName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static void FindFirstBoneRecursive(Transform node, string[] patterns, int depth, ref Transform best, ref int bestScore)
        {
            string compactName = Compact(node.name);
            for (int i = 0; i < patterns.Length; i++)
            {
                string pattern = Compact(patterns[i]);
                if (!string.IsNullOrEmpty(pattern) && compactName.Contains(pattern))
                {
                    int score = depth * 10 + i;
                    if (score < bestScore)
                    {
                        best = node;
                        bestScore = score;
                    }
                }
            }

            for (int i = 0; i < node.childCount; i++)
            {
                FindFirstBoneRecursive(node.GetChild(i), patterns, depth + 1, ref best, ref bestScore);
            }
        }

        private static string Compact(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty)
                .Replace(":", string.Empty)
                .ToLowerInvariant();
        }
    }
}
