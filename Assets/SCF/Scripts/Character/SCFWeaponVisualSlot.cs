using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SCF.Gameplay
{
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
        private const int CurrentRailgunProfileRevision = 12;
        private const string PrototypeRailgunPath = "Assets/SCF/2.8 rail-gun prototype_Texture_Packed.blend";
        private const string RailgunFireClipPath = "Assets/SCF/Audio/kalsstockmedia-a-large-explosive-laser-gun-shot-scifi-410622.mp3";
        private const string ReferenceShotgunPath = "Assets/SCF/MovementAni/NVoperatorsoldier.glb";
        private const string DefaultRailgunRigProfilePath = "SCF/Weapons/SCF_RailgunRigProfile";
        private const string RuntimeWeaponCatalogPath = "SCF/SCFWeaponRuntimeCatalog";
        private const string Pose3RightGripTargetName = "SCF_Pose3RightGripTarget";
        private const string Pose3LeftGripTargetName = "SCF_Pose3LeftGripTarget";
        private const string RailgunMuzzleTargetName = "SCF_RailgunMuzzleTarget";
        private const string RailgunMuzzleMarkerName = "SCF_RailgunMuzzleMarker";
        private const string RightElbowHintTargetName = "SCF_RightElbowHint";
        private const string LeftElbowHintTargetName = "SCF_LeftElbowHint";
        private static readonly Vector3 LegacyWeaponEulerAngles = new Vector3(0f, 90f, 90f);
        private static readonly Vector3 DefaultWeaponEulerAngles = new Vector3(170f, 90f, 180f);
        private static readonly Vector3 LegacyRightGripPosition = new Vector3(0.02f, -0.05f, 0.15f);
        private static readonly Vector3 DefaultRightGripPosition = new Vector3(0.02f, -0.05f, 0.12f);
        private static readonly Vector3 LegacyLeftGripPosition = new Vector3(-0.06f, 0.01f, 0.38f);
        private static readonly Vector3 DefaultLeftGripPosition = new Vector3(-0.04f, -0.02f, 0.28f);
        private static readonly Vector3 Pose3ReferenceRightWristFromShotgunBone = new Vector3(-0.0332f, -0.2147f, -0.8692f);
        private static readonly Vector3 Pose3ReferenceLeftWristFromShotgunBone = new Vector3(0.1325f, -0.2297f, 0.3901f);
        private static readonly Vector3 DefaultRailgunMuzzleLocalPosition = new Vector3(0.92f, 0.11f, 0f);
        private static readonly Vector3 DefaultRailgunMuzzleLocalEulerAngles = new Vector3(0f, 90f, 0f);

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
        [SerializeField] private bool useReferenceShotgunPrototype;
        [SerializeField] private int railgunProfileRevision;
        [SerializeField] private string torsoSocketName = "SCF_ChestWeaponSocket";
        [SerializeField] private string rightGripName = "SCF_RightPistolGrip";
        [SerializeField] private string leftGripName = "SCF_LeftUnderbarrelGrip";
        [SerializeField] private string referenceShotgunMeshName = "SHOTGUN";
        [SerializeField] private string referenceShotgunAnchorName = "SHOTGUNbone";
        [SerializeField] private string referenceRightGripName = "HandWrist.R";
        [SerializeField] private string referenceLeftGripName = "HandWrist.L";
        [SerializeField] private string referencePoseClipName = "SHOTGUNpose3";
        [SerializeField] private string rightElbowHintName = RightElbowHintTargetName;
        [SerializeField] private string leftElbowHintName = LeftElbowHintTargetName;
        [SerializeField, Range(0f, 1f)] private float referencePoseSampleTime01 = 0f;

        [Header("Body Socket")]
        [Tooltip("Chest-bone local weapon socket offset.")]
        [SerializeField] private Vector3 restSocketOffset = new Vector3(0.18f, 0.02f, 0.26f);
        [Tooltip("Right-click raised chest-bone local weapon socket offset.")]
        [SerializeField] private Vector3 raisedSocketOffset = new Vector3(0.06f, 0.08f, 0.3f);
        [SerializeField] private Vector3 restSocketEulerAngles = new Vector3(0f, 0f, 0f);
        [SerializeField] private Vector3 raisedSocketEulerAngles = new Vector3(-5f, 0f, 0f);
        [SerializeField, Min(0.1f)] private float raiseSharpness = 14f;

        [Header("Weapon Local Fit")]
        [SerializeField] private Vector3 weaponLocalPosition = Vector3.zero;
        [SerializeField] private Vector3 weaponLocalEulerAngles = new Vector3(170f, 90f, 180f);
        [SerializeField] private Vector3 weaponLocalScale = Vector3.one;

        [Header("Weapon Anchor")]
        [SerializeField] private bool anchorWeaponByRightGrip = true;
        [SerializeField] private Vector3 restRightGripSocketOffset = Vector3.zero;
        [SerializeField] private Vector3 raisedRightGripSocketOffset = new Vector3(0f, 0.01f, 0.02f);

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
        [SerializeField] private bool captureTuningEveryFrame;
        [SerializeField] private bool copyTuningOnCapture = true;
        [SerializeField] private KeyCode captureTuningHotkey = KeyCode.F8;

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

        [Header("Grip Targets")]
        [SerializeField] private Vector3 rightGripLocalPosition = new Vector3(0.02f, -0.05f, 0.12f);
        [SerializeField] private Vector3 rightGripLocalEulerAngles = new Vector3(0f, 90f, 90f);
        [SerializeField] private Vector3 leftGripLocalPosition = new Vector3(-0.04f, -0.02f, 0.28f);
        [SerializeField] private Vector3 leftGripLocalEulerAngles = new Vector3(0f, 90f, 90f);

        [Header("Hand IK")]
        [SerializeField] private bool enableHandIk = true;
        [SerializeField] private bool preferWeaponPrefabAnchors = true;
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

        [Header("Anatomy Guardrails")]
        [SerializeField] private bool useAnatomyAwareIk = true;
        [SerializeField] private bool useElbowHints = true;
        [SerializeField] private bool useManualElbowHintAssist = true;
        [SerializeField] private Vector3 rightElbowHintLocalPosition = new Vector3(0.28f, -0.1f, -0.08f);
        [SerializeField] private Vector3 leftElbowHintLocalPosition = new Vector3(-0.32f, -0.06f, 0.14f);
        [SerializeField, Range(0f, 1f)] private float rightElbowHintWeight = 0.9f;
        [SerializeField, Range(0f, 1f)] private float leftElbowHintWeight = 0.95f;
        [SerializeField, Range(0f, 1f)] private float manualElbowHintAssistWeight = 0.85f;
        [SerializeField, Range(1, 4)] private int manualArmSolveIterations = 2;
        [SerializeField, Range(0f, 1f)] private float manualArmHandLockWeight = 0.55f;
        [SerializeField, Range(0.5f, 1.05f)] private float maxArmReachMultiplier = 0.94f;
        [SerializeField, Range(0f, 0.5f)] private float armReachSoftZone = 0.16f;
        [SerializeField, Range(0f, 1f)] private float unreachableRotationWeight = 0.35f;

        [Header("Carry State Weights")]
        [SerializeField] private bool useStateBasedCarryWeights = true;
        [SerializeField, Range(0f, 1f)] private float locomotionCarryWeight = 0.95f;
        [SerializeField, Range(0f, 1f)] private float aimedCarryWeight = 1f;
        [SerializeField, Range(0f, 1f)] private float sprintCarryWeight = 0.86f;
        [SerializeField, Range(0f, 1f)] private float airborneCarryWeight = 0.72f;
        [SerializeField, Range(0f, 1f)] private float traversalCarryWeight = 0.5f;
        [SerializeField, Range(0f, 1f)] private float wallRunCarryWeight = 0.58f;
        [SerializeField, Range(0f, 1f)] private float rollCarryWeight = 0.18f;

        [Header("Debug")]
        [SerializeField] private string activeCharacterName;
        [SerializeField] private GameObject activeWeapon;
        [SerializeField] private bool activeWeaponSupportsRailgunFire;
        [SerializeField, Range(0f, 1f)] private float raised01;
        [SerializeField, Range(0f, 1f)] private float currentRightHandWeight;
        [SerializeField, Range(0f, 1f)] private float currentLeftHandWeight;

        private Transform weaponSocket;
        private Transform rightGrip;
        private Transform leftGrip;
        private Transform chestAnchor;
        private Transform referenceWeaponAnchor;
        private Transform pose3RightGripTarget;
        private Transform pose3LeftGripTarget;
        private Transform railgunMuzzleTarget;
        private Transform rightElbowHint;
        private Transform leftElbowHint;
        private ArmRig rightArm;
        private ArmRig leftArm;
        private float nextRailgunFireTime;

        public event Action<SCFRailgunShot> RailgunFired;
        private Material railgunBeamMaterial;
        private Material railgunParticleMaterial;

        public bool HasActiveWeapon => activeWeapon != null;
        public string ActiveWeaponName => activeWeapon != null ? activeWeapon.name.Replace("SCF_Selected_", string.Empty) : "none";
        public GameObject SelectedWeaponPrototype => selectedWeaponPrototype;
        public Transform WeaponSocketTransform => weaponSocket;
        public Transform ActiveWeaponTransform => activeWeapon != null ? activeWeapon.transform : null;
        public Transform RightGripTransform => rightGrip;
        public Transform LeftGripTransform => leftGrip;
        public Transform RightElbowHintTransform => rightElbowHint;
        public Transform LeftElbowHintTransform => leftElbowHint;
        public Transform RailgunMuzzleTransform => railgunMuzzleTarget;
        public float Raised01 => raised01;

        private struct ArmRig
        {
            public Transform Shoulder;
            public Transform UpperArm;
            public Transform Forearm;
            public Transform Hand;
        }

        private void Awake()
        {
            ApplyRailgunProfileDefaultsIfNeeded();
            ApplyRigProfileDefaultsIfPresent();
            ResolveRailgunFireClip();
            ResolveReferences();
            CacheBones();
            EnsureAnimatorIkRelay();
            EnsureWeaponSelectionPanel();
            EnsureWeaponTuningPanel();
        }

        private void OnValidate()
        {
            ApplyRailgunProfileDefaultsIfNeeded();
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

            if (string.IsNullOrWhiteSpace(rightElbowHintName))
            {
                rightElbowHintName = RightElbowHintTargetName;
            }

            if (string.IsNullOrWhiteSpace(leftElbowHintName))
            {
                leftElbowHintName = LeftElbowHintTargetName;
            }

            if (railgunProfileRevision >= CurrentRailgunProfileRevision)
            {
                return;
            }

            if (railgunProfileRevision >= 11)
            {
                ApplyRailgunAnatomyDefaults();
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
            raisedRightGripSocketOffset = new Vector3(0f, 0.01f, 0.02f);
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
            restSocketOffset = new Vector3(0.18f, 0.02f, 0.26f);
            raisedSocketOffset = new Vector3(0.06f, 0.08f, 0.3f);
            restSocketEulerAngles = Vector3.zero;
            raisedSocketEulerAngles = new Vector3(-5f, 0f, 0f);
        }

        private void ApplyRailgunGripTargetDefaults()
        {
            useReferenceShotgunPrototype = false;
            useShotgunPose3GripReplica = true;
            anchorWeaponByRightGrip = true;
            restRightGripSocketOffset = Vector3.zero;
            raisedRightGripSocketOffset = new Vector3(0f, 0.01f, 0.02f);
            shotgunPose3Scale = Mathf.Max(0.01f, shotgunPose3Scale <= 0f ? 0.27f : shotgunPose3Scale);
            shotgunPose3RightGripTuningOffset = new Vector3(0f, -0.08f, -0.14f);
            shotgunPose3LeftGripTuningOffset = new Vector3(-0.105261f, 0.15405f, -0.000011f);
            shotgunPose3RightGripEulerAngles = new Vector3(-106.2f, -23.89999f, -75.70001f);
            shotgunPose3LeftGripEulerAngles = new Vector3(-134.6f, 146.4f, -11.4f);
        }

        private void ApplyRailgunWeaponTransformDefaults()
        {
            weaponLocalPosition = new Vector3(-0.06f, -0.02f, -0.04f) - ResolvePose3WeaponAnchorSocketOffset();
            weaponLocalEulerAngles = new Vector3(-4.95f, -90.25f, 25.36f);
            weaponLocalScale = Vector3.one * 0.8f;
        }

        private void ApplyRailgunAnatomyDefaults()
        {
            preferWeaponPrefabAnchors = true;
            useAnimatorIk = true;
            useAnatomyAwareIk = true;
            useElbowHints = true;
            useManualElbowHintAssist = true;
            applyBoneFallbackForHumanoids = false;
            maxArmReachMultiplier = 0.94f;
            armReachSoftZone = 0.16f;
            unreachableRotationWeight = 0.35f;
            rightElbowHintWeight = 0.9f;
            leftElbowHintWeight = 0.95f;
            manualElbowHintAssistWeight = 0.85f;
            manualArmSolveIterations = 2;
            manualArmHandLockWeight = 0.55f;
            rightElbowHintLocalPosition = new Vector3(0.28f, -0.1f, -0.08f);
            leftElbowHintLocalPosition = new Vector3(-0.32f, -0.06f, 0.14f);
            useStateBasedCarryWeights = true;
            locomotionCarryWeight = 0.95f;
            aimedCarryWeight = 1f;
            sprintCarryWeight = 0.86f;
            airborneCarryWeight = 0.72f;
            traversalCarryWeight = 0.5f;
            wallRunCarryWeight = 0.58f;
            rollCarryWeight = 0.18f;
        }

        private void ApplyRigProfileDefaultsIfPresent()
        {
            SCFWeaponRigProfile profile = ResolveRailgunRigProfile();
            if (profile == null)
            {
                return;
            }

            if (profile.WeaponPrefab != null && preferWeaponPrefabAnchors)
            {
                railgunPrototype = profile.WeaponPrefab;
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

            if (!string.IsNullOrWhiteSpace(anchors.rightElbowHint))
            {
                rightElbowHintName = anchors.rightElbowHint;
            }

            if (!string.IsNullOrWhiteSpace(anchors.leftElbowHint))
            {
                leftElbowHintName = anchors.leftElbowHint;
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
                rightElbowHintWeight = limits.rightElbowHintWeight;
                leftElbowHintWeight = limits.leftElbowHintWeight;
            }
        }

        public void Configure(IsometricCharacterMotor characterMotor, Animator targetAnimator, string characterName)
        {
            ApplyRailgunProfileDefaultsIfNeeded();
            ApplyRigProfileDefaultsIfPresent();
            motor = characterMotor;
            animator = targetAnimator;
            activeCharacterName = characterName ?? string.Empty;
            ResolveReferences();
            CacheBones();
            EnsureAnimatorIkRelay();
            EnsureWeaponSelectionPanel();
            EnsureWeaponTuningPanel();

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

            if (!CanApplyHandIk())
            {
                ClearAnimatorIkGoals(targetAnimator);
                return;
            }

            ApplyHumanoidArmIk(targetAnimator, AvatarIKGoal.RightHand, AvatarIKHint.RightElbow, rightGrip, rightElbowHint, rightArm, currentRightHandWeight, rightArmReachWeight, rightElbowHintWeight);
            ApplyHumanoidArmIk(targetAnimator, AvatarIKGoal.LeftHand, AvatarIKHint.LeftElbow, leftGrip, leftElbowHint, leftArm, currentLeftHandWeight, leftArmReachWeight, leftElbowHintWeight);
        }

        private void LateUpdate()
        {
            ResolveReferences();

            if (activeWeapon == null)
            {
                currentRightHandWeight = 0f;
                currentLeftHandWeight = 0f;
                return;
            }

            if (chestAnchor == null || rightArm.Hand == null || leftArm.Hand == null)
            {
                CacheBones();
            }

            float blend = 1f - Mathf.Exp(-handIkBlendSharpness * Time.deltaTime);
            bool canApplyIk = CanApplyHandIk();
            float carryWeight = ResolveCarryStateWeight();
            currentRightHandWeight = Mathf.Lerp(currentRightHandWeight, canApplyIk ? rightHandIkWeight * carryWeight : 0f, blend);
            currentLeftHandWeight = Mathf.Lerp(currentLeftHandWeight, canApplyIk ? leftHandIkWeight * carryWeight : 0f, blend);

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
                if (!UseShotgunPose3GripReplica())
                {
                    AlignWeaponByRightGrip();
                }
            }

            EnsureElbowHints();
            if (ShouldApplyBoneFallback())
            {
                ApplyGenericArmToGrip(rightArm, rightGrip, currentRightHandWeight, rightArmReachWeight);
                ApplyGenericArmToGrip(leftArm, leftGrip, currentLeftHandWeight, leftArmReachWeight);
            }

            ApplyManualElbowHintAssist();

            TickRailgunLiveTuningCapture();
            TickRailgunFire();
        }

        private void OnDrawGizmosSelected()
        {
            DrawGripGizmo(weaponSocket, Color.yellow, 0.04f);
            DrawGripGizmo(rightGrip, Color.cyan, 0.03f);
            DrawGripGizmo(leftGrip, Color.green, 0.03f);
            DrawGripGizmo(rightElbowHint, new Color(0.2f, 0.55f, 1f, 1f), 0.025f);
            DrawGripGizmo(leftElbowHint, new Color(1f, 0.65f, 0.1f, 1f), 0.025f);
        }

        private void OnDestroy()
        {
            DestroyUnityObject(railgunBeamMaterial);
            DestroyUnityObject(railgunParticleMaterial);
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

        private bool ShouldEquipRailgun()
        {
            return equipRailgunOnSoldier
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

            SCFWeaponRuntimeCatalog catalog = ResolveRuntimeWeaponCatalog();
            if (catalog != null && catalog.RailgunWeaponPrefab != null)
            {
                railgunPrototype = catalog.RailgunWeaponPrefab;
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
            if (!requested || Time.time < nextRailgunFireTime)
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

            railgunMuzzleTarget = FindDescendantByName(activeWeapon.transform, railgunMuzzleTransformName);
            return railgunMuzzleTarget;
        }

        private bool TryFindRailgunHit(Vector3 muzzle, Vector3 direction, out RaycastHit hit)
        {
            hit = default;
            Vector3 origin = muzzle + direction * railgunRaycastStartOffset;
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, railgunFireRange, railgunHitMask, QueryTriggerInteraction.Ignore);
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
            rightArm = default;
            leftArm = default;

            if (animator == null)
            {
                return;
            }

            if (animator.avatar != null && animator.isHuman)
            {
                chestAnchor = FirstNonNull(
                    animator.GetBoneTransform(HumanBodyBones.UpperChest),
                    animator.GetBoneTransform(HumanBodyBones.Chest),
                    animator.GetBoneTransform(HumanBodyBones.Spine),
                    animator.GetBoneTransform(HumanBodyBones.Hips),
                    animator.transform);

                rightArm.Shoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
                rightArm.UpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                rightArm.Forearm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
                rightArm.Hand = animator.GetBoneTransform(HumanBodyBones.RightHand);

                leftArm.Shoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
                leftArm.UpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                leftArm.Forearm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                leftArm.Hand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                return;
            }

            Transform root = animator.transform;
            chestAnchor = FirstNonNull(
                FindFirstBone(root, "upperchest", "upper_chest", "spine2", "spine02", "spine_02", "spine 2", "ccbasespine02"),
                FindFirstBone(root, "chest", "spine1", "spine01", "spine_01", "spine 1", "ccbasespine01"),
                FindFirstBone(root, "waist", "spine", "ccbasespine"),
                root);

            rightArm.Shoulder = FindFirstBone(root, "rightshoulder", "rshoulder", "shoulder_r", "r_shoulder", "rightclavicle", "rclavicle", "clavicle_r", "r_clavicle", "ccbaserclavicle");
            rightArm.UpperArm = FindFirstBone(root, "rightupperarm", "rupperarm", "upperarm_r", "r_upperarm", "ccbaserupperarm", "rightarm");
            rightArm.Forearm = FindFirstBone(root, "rightforearm", "rightlowerarm", "rforearm", "rlowerarm", "forearm_r", "lowerarm_r", "r_forearm", "r_lowerarm", "ccbaserforearm");
            rightArm.Hand = FindFirstBone(root, "righthand", "rhand", "hand_r", "r_hand", "ccbaserhand");

            leftArm.Shoulder = FindFirstBone(root, "leftshoulder", "lshoulder", "shoulder_l", "l_shoulder", "leftclavicle", "lclavicle", "clavicle_l", "l_clavicle", "ccbaselclavicle");
            leftArm.UpperArm = FindFirstBone(root, "leftupperarm", "lupperarm", "upperarm_l", "l_upperarm", "ccbaselupperarm", "leftarm");
            leftArm.Forearm = FindFirstBone(root, "leftforearm", "leftlowerarm", "lforearm", "llowerarm", "forearm_l", "lowerarm_l", "l_forearm", "l_lowerarm", "ccbaselforearm");
            leftArm.Hand = FindFirstBone(root, "lefthand", "lhand", "hand_l", "l_hand", "ccbaselhand");
        }

        private void EnsureWeaponSocket()
        {
            Transform socketParent = ResolveSocketParent();
            if (socketParent == null)
            {
                weaponSocket = null;
                return;
            }

            if (weaponSocket != null)
            {
                if (weaponSocket.parent != socketParent)
                {
                    weaponSocket.SetParent(socketParent, false);
                }

                return;
            }

            Transform existing = FindDirectChild(socketParent, torsoSocketName);
            if (existing == null)
            {
                existing = FindDescendantByName(transform, torsoSocketName);
            }

            weaponSocket = existing != null ? existing : new GameObject(torsoSocketName).transform;
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

            Transform right = FindDescendantByName(activeWeapon.transform, rightGripName);
            Transform left = FindDescendantByName(activeWeapon.transform, leftGripName);

            if (right == null)
            {
                right = FindDescendantByName(activeWeapon.transform, Pose3RightGripTargetName);
            }

            if (left == null)
            {
                left = FindDescendantByName(activeWeapon.transform, Pose3LeftGripTargetName);
            }

            if (right == null || left == null)
            {
                return false;
            }

            EnsureTuningHandle(right.gameObject);
            EnsureTuningHandle(left.gameObject);
            rightGrip = right;
            leftGrip = left;
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
                railgunMuzzleTarget = FindDescendantByName(activeWeapon.transform, railgunMuzzleTransformName);
            }

            if (railgunMuzzleTarget == null)
            {
                if (!createRailgunMuzzleTarget)
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
            EnsureRailgunMuzzleDebugMarker(railgunMuzzleTarget);
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

        private void EnsureElbowHints()
        {
            if (!useElbowHints || weaponSocket == null)
            {
                rightElbowHint = null;
                leftElbowHint = null;
                return;
            }

            if (preferWeaponPrefabAnchors && activeWeapon != null)
            {
                rightElbowHint = FindDescendantByName(activeWeapon.transform, rightElbowHintName);
                leftElbowHint = FindDescendantByName(activeWeapon.transform, leftElbowHintName);
            }

            rightElbowHint = EnsureSocketHint(rightElbowHint, rightElbowHintName, rightElbowHintLocalPosition);
            leftElbowHint = EnsureSocketHint(leftElbowHint, leftElbowHintName, leftElbowHintLocalPosition);
            if (rightElbowHint != null)
            {
                EnsureTuningHandle(rightElbowHint.gameObject);
            }

            if (leftElbowHint != null)
            {
                EnsureTuningHandle(leftElbowHint.gameObject);
            }
        }

        private Transform EnsureSocketHint(Transform current, string targetName, Vector3 localPosition)
        {
            if (current != null)
            {
                return current;
            }

            Transform existing = FindDirectChild(weaponSocket, targetName);
            Transform target = existing != null ? existing : new GameObject(targetName).transform;
            target.SetParent(weaponSocket, false);
            target.localPosition = localPosition;
            target.localRotation = Quaternion.identity;
            target.localScale = Vector3.one;
            EnsureTuningHandle(target.gameObject);
            return target;
        }

        private void ResetWeaponTransform()
        {
            if (activeWeapon == null)
            {
                rightElbowHint = null;
                leftElbowHint = null;
                return;
            }

            ApplyWeaponLocalFit(true);
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
                if (!UseShotgunPose3GripReplica())
                {
                    AlignWeaponByRightGrip();
                }
            }

            EnsureElbowHints();
        }

        private void ApplyWeaponLocalFit(bool force = false)
        {
            if (activeWeapon == null)
            {
                return;
            }

            if (!force && ShouldPreserveTunedRailgunTransform())
            {
                return;
            }

            if (IsReferenceShotgunInstance())
            {
                activeWeapon.transform.localPosition = Vector3.zero;
                activeWeapon.transform.localRotation = Quaternion.identity;
                activeWeapon.transform.localScale = Vector3.one;
                return;
            }

            activeWeapon.transform.localPosition = UseShotgunPose3GripReplica()
                ? weaponLocalPosition + ResolvePose3WeaponAnchorSocketOffset()
                : weaponLocalPosition;
            activeWeapon.transform.localRotation = Quaternion.Euler(weaponLocalEulerAngles);
            activeWeapon.transform.localScale = SanitizedScale(weaponLocalScale);
        }

        private void AlignWeaponByRightGrip()
        {
            if (!anchorWeaponByRightGrip || activeWeapon == null || weaponSocket == null || rightGrip == null)
            {
                return;
            }

            Vector3 socketOffset = Vector3.Lerp(restRightGripSocketOffset, raisedRightGripSocketOffset, raised01);
            Vector3 target = weaponSocket.TransformPoint(socketOffset);
            Vector3 delta = target - rightGrip.position;
            activeWeapon.transform.position += delta;
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
            if (activeWeapon == null || weaponSocket == null)
            {
                return;
            }

            if (referenceWeaponAnchor == null)
            {
                referenceWeaponAnchor = FindDescendantByName(activeWeapon.transform, referenceShotgunAnchorName);
            }

            if (referenceWeaponAnchor == null)
            {
                return;
            }

            Quaternion rotationDelta = weaponSocket.rotation * Quaternion.Inverse(referenceWeaponAnchor.rotation);
            activeWeapon.transform.rotation = rotationDelta * activeWeapon.transform.rotation;
            Vector3 socketOffset = UseShotgunPose3GripReplica()
                ? ResolvePose3WeaponAnchorSocketOffset()
                : Vector3.Lerp(restRightGripSocketOffset, raisedRightGripSocketOffset, raised01);
            Vector3 target = weaponSocket.TransformPoint(socketOffset);
            activeWeapon.transform.position += target - referenceWeaponAnchor.position;
        }

        private bool UseShotgunPose3GripReplica()
        {
            return useShotgunPose3GripReplica && activeWeapon != null;
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
                   && !IsReferenceShotgunInstance();
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

            if (captureTuningEveryFrame)
            {
                CaptureCurrentRailgunTuningInternal(false);
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            if (captureTuningHotkey != KeyCode.None && Input.GetKeyDown(captureTuningHotkey))
            {
                CaptureCurrentRailgunTuningInternal(true);
            }
#endif
        }

        public void CaptureCurrentRailgunTuning()
        {
            CaptureCurrentRailgunTuningInternal(true);
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

            EnsureWeaponSocket();
            bool oldPreserveTunedGripTargets = preserveTunedGripTargets;
            preserveTunedGripTargets = false;
            EnsureShotgunPose3GripTargets();
            preserveTunedGripTargets = oldPreserveTunedGripTargets;
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
            EnsureElbowHints();
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
                   + "railgunMuzzleLocalEulerAngles = " + FormatVector(railgunMuzzleLocalEulerAngles);
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
            float target = input != null && input.AimHeld ? 1f : 0f;
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

            weaponSocket.localPosition = Vector3.Lerp(restSocketOffset, raisedSocketOffset, raised01);
            Quaternion restRotation = Quaternion.Euler(restSocketEulerAngles);
            Quaternion raisedRotation = Quaternion.Euler(raisedSocketEulerAngles);
            weaponSocket.localRotation = Quaternion.Slerp(restRotation, raisedRotation, raised01);
        }

        private Transform ResolveSocketParent()
        {
            return FirstNonNull(chestAnchor, animator != null ? animator.transform : null, transform);
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
                   && genericBoneFallbackWeight > 0.001f
                   && (animator == null || !animator.isHuman || applyBoneFallbackForHumanoids);
        }

        private void ApplyHumanoidArmIk(
            Animator targetAnimator,
            AvatarIKGoal goal,
            AvatarIKHint hint,
            Transform grip,
            Transform elbowHint,
            ArmRig rig,
            float weight,
            float reachWeight,
            float hintWeight)
        {
            if (grip == null || weight <= 0.001f)
            {
                targetAnimator.SetIKPositionWeight(goal, 0f);
                targetAnimator.SetIKRotationWeight(goal, 0f);
                targetAnimator.SetIKHintPositionWeight(hint, 0f);
                return;
            }

            Vector3 targetPosition = ResolveReachLimitedTarget(rig, grip.position, reachWeight, out float reach01);
            float finalPositionWeight = Mathf.Clamp01(weight);
            float finalRotationWeight = Mathf.Clamp01(weight * Mathf.Lerp(unreachableRotationWeight, 1f, reach01));

            targetAnimator.SetIKPositionWeight(goal, finalPositionWeight);
            targetAnimator.SetIKRotationWeight(goal, finalRotationWeight);
            targetAnimator.SetIKPosition(goal, targetPosition);
            targetAnimator.SetIKRotation(goal, grip.rotation);

            if (useElbowHints && elbowHint != null)
            {
                targetAnimator.SetIKHintPositionWeight(hint, Mathf.Clamp01(finalPositionWeight * hintWeight));
                targetAnimator.SetIKHintPosition(hint, elbowHint.position);
            }
            else
            {
                targetAnimator.SetIKHintPositionWeight(hint, 0f);
            }
        }

        private void ApplyGenericArmToGrip(ArmRig rig, Transform grip, float weight, float reachWeight)
        {
            if (rig.Hand == null || grip == null || weight <= 0.001f)
            {
                return;
            }

            float adjustedWeight = Mathf.Clamp01(weight * genericBoneFallbackWeight * reachWeight);
            Vector3 targetPosition = ResolveReachLimitedTarget(rig, grip.position, reachWeight, out float reach01);
            int iterations = Mathf.Clamp(genericSolveIterations, 1, 8);
            for (int i = 0; i < iterations; i++)
            {
                float passWeight = adjustedWeight * (1f - i * 0.12f);
                RotateBoneTowardTarget(rig.Shoulder, rig.Hand, targetPosition, passWeight * 0.28f);
                RotateBoneTowardTarget(rig.UpperArm, rig.Hand, targetPosition, passWeight * 0.68f);
                RotateBoneTowardTarget(rig.Forearm, rig.Hand, targetPosition, passWeight);
            }

            rig.Hand.rotation = Quaternion.Slerp(rig.Hand.rotation, grip.rotation, adjustedWeight * 0.75f);
            float finalPositionWeight = Mathf.Clamp01(adjustedWeight * finalGripPositionWeight);
            float finalRotationWeight = Mathf.Clamp01(adjustedWeight * finalGripRotationWeight);
            rig.Hand.position = Vector3.Lerp(rig.Hand.position, targetPosition, finalPositionWeight);
            rig.Hand.rotation = Quaternion.Slerp(rig.Hand.rotation, grip.rotation, finalRotationWeight);
        }

        private void ApplyManualElbowHintAssist()
        {
            if (!useManualElbowHintAssist || !useElbowHints || !CanApplyHandIk())
            {
                return;
            }

            ApplyManualArmIk(rightArm, rightGrip, rightElbowHint, currentRightHandWeight, rightArmReachWeight, rightElbowHintWeight);
            ApplyManualArmIk(leftArm, leftGrip, leftElbowHint, currentLeftHandWeight, leftArmReachWeight, leftElbowHintWeight);
        }

        private void ApplyManualArmIk(ArmRig rig, Transform grip, Transform elbowHint, float handWeight, float reachWeight, float hintWeight)
        {
            if (rig.UpperArm == null
                || rig.Forearm == null
                || rig.Hand == null
                || grip == null
                || elbowHint == null
                || handWeight <= 0.001f
                || hintWeight <= 0.001f)
            {
                return;
            }

            float weight = Mathf.Clamp01(handWeight * hintWeight * manualElbowHintAssistWeight);
            if (weight <= 0.001f)
            {
                return;
            }

            Vector3 targetPosition = ResolveReachLimitedTarget(rig, grip.position, reachWeight, out _);
            int iterations = Mathf.Clamp(manualArmSolveIterations, 1, 4);
            for (int i = 0; i < iterations; i++)
            {
                ApplyTwoBoneElbowHint(rig, targetPosition, elbowHint.position, weight);
            }

            float handLockWeight = Mathf.Clamp01(weight * manualArmHandLockWeight);
            if (handLockWeight > 0.001f)
            {
                rig.Hand.position = Vector3.Lerp(rig.Hand.position, targetPosition, handLockWeight);
                rig.Hand.rotation = Quaternion.Slerp(rig.Hand.rotation, grip.rotation, handLockWeight);
            }
        }

        private static void ApplyTwoBoneElbowHint(ArmRig rig, Vector3 targetPosition, Vector3 hintPosition, float weight)
        {
            Vector3 root = rig.UpperArm.position;
            float upperLength = Vector3.Distance(root, rig.Forearm.position);
            float lowerLength = Vector3.Distance(rig.Forearm.position, rig.Hand.position);
            if (upperLength <= 0.0001f || lowerLength <= 0.0001f)
            {
                return;
            }

            Vector3 rootToTarget = targetPosition - root;
            float targetDistance = rootToTarget.magnitude;
            if (targetDistance <= 0.0001f)
            {
                return;
            }

            float maxLength = Mathf.Max(0.0001f, upperLength + lowerLength - 0.0001f);
            float minLength = Mathf.Max(0.0001f, Mathf.Abs(upperLength - lowerLength) + 0.0001f);
            float solvedDistance = Mathf.Clamp(targetDistance, minLength, maxLength);
            Vector3 aimDirection = rootToTarget / targetDistance;
            Vector3 currentPole = Vector3.ProjectOnPlane(rig.Forearm.position - root, aimDirection);
            Vector3 desiredPole = Vector3.ProjectOnPlane(hintPosition - root, aimDirection);

            if (desiredPole.sqrMagnitude <= 0.0001f)
            {
                desiredPole = currentPole;
            }

            if (desiredPole.sqrMagnitude <= 0.0001f)
            {
                desiredPole = Vector3.Cross(aimDirection, Vector3.up);
            }

            if (desiredPole.sqrMagnitude <= 0.0001f)
            {
                desiredPole = Vector3.Cross(aimDirection, Vector3.right);
            }

            if (desiredPole.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Vector3 poleDirection = desiredPole.normalized;
            float elbowAlongTarget = ((upperLength * upperLength) + (solvedDistance * solvedDistance) - (lowerLength * lowerLength))
                                      / Mathf.Max(0.0001f, 2f * solvedDistance);
            float elbowOutFromTargetLine = Mathf.Sqrt(Mathf.Max(0f, (upperLength * upperLength) - (elbowAlongTarget * elbowAlongTarget)));
            Vector3 desiredElbowPosition = root + aimDirection * elbowAlongTarget + poleDirection * elbowOutFromTargetLine;

            RotateBoneFromTo(rig.UpperArm, rig.Forearm.position - root, desiredElbowPosition - root, weight);
            RotateBoneFromTo(rig.Forearm, rig.Hand.position - rig.Forearm.position, targetPosition - rig.Forearm.position, weight);
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

            float reachScale = Mathf.Clamp(reachWeight, 0.1f, 2f);
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

            if (length <= 0.0001f && rig.Shoulder != null && rig.Hand != null)
            {
                length = Vector3.Distance(rig.Shoulder.position, rig.Hand.position);
            }

            return length;
        }

        private static void ClearAnimatorIkGoals(Animator targetAnimator)
        {
            targetAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
            targetAnimator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
            targetAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
            targetAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
            targetAnimator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 0f);
            targetAnimator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 0f);
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
            rightElbowHint = null;
            leftElbowHint = null;
            raised01 = 0f;
            currentRightHandWeight = 0f;
            currentLeftHandWeight = 0f;
        }

        private void ClearWeaponInstanceOnly()
        {
            DestroyShotgunPose3GripTargets();
            activeWeaponSupportsRailgunFire = false;

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
            rightElbowHint = null;
            leftElbowHint = null;
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
            if (bone == null || weight <= 0.001f)
            {
                return;
            }

            if (currentDirection.sqrMagnitude <= 0.0001f || targetDirection.sqrMagnitude <= 0.0001f)
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
