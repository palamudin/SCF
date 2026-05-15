using System;
using UnityEngine;

namespace SCF.Gameplay
{
    public enum CharacterFacingMode
    {
        AimThenMovement,
        MovementOnly,
        AimOnly
    }

    public enum CharacterMobilityState
    {
        Locomotion,
        MobilityTapWindow,
        JumpCharge,
        Jumping,
        Falling,
        CombatRoll,
        Prone,
        WallRun,
        Vault,
        Climb
    }

    public enum SCFTraversalProfile
    {
        Standard,
        Parkour
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [DefaultExecutionOrder(-60)]
    public sealed class IsometricCharacterMotor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private IsometricPlayerInput input;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Transform facingRoot;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float walkSpeed = 5.2f;
        [SerializeField, Min(0f)] private float sprintSpeed = 7.4f;
        [SerializeField, Min(0.1f)] private float acceleration = 38f;
        [SerializeField, Min(0.1f)] private float deceleration = 46f;
        [SerializeField] private float gravity = -28f;
        [SerializeField] private float groundedStickForce = -3f;
        [SerializeField] private bool scaleMovementWithTransform;

        [Header("Mobility Action")]
        [SerializeField, Min(0.01f)] private float rollTapThreshold = 0.1f;
        [SerializeField, Min(0.01f)] private float jumpChargeDuration = 0.25f;
        [SerializeField, Range(0f, 1f)] private float heldJumpStrength = 0.55f;
        [SerializeField, Min(0.1f)] private float minJumpHeight = 0.95f;
        [SerializeField, Min(0.1f)] private float maxJumpHeight = 2.75f;
        [SerializeField, Min(0f)] private float jumpPlanarSpeed = 5.5f;
        [SerializeField, Min(0f)] private float maxJumpChargePlanarBoost = 1.4f;
        [SerializeField, Min(0.05f)] private float jumpAnimationDuration = 0.72f;
        [SerializeField, Range(0f, 1f)] private float airborneMoveMultiplier = 0.35f;
        [SerializeField] private bool parkourWallJumpKeepsAirMobility = true;
        [SerializeField, Range(0f, 1f)] private float parkourAirJumpStrength = 0.75f;
        [SerializeField, Min(0f)] private float parkourAirRollSpeedBonus = 1.2f;
        [SerializeField, Min(0.01f)] private float combatRollDuration = 0.58f;
        [SerializeField, Min(0f)] private float combatRollSpeed = 8.4f;
        [Tooltip("RedNotRed parkour uses 0.13s hop windows for back/side traversal pops.")]
        [SerializeField, Min(0.01f)] private float parkourHopDuration = 0.13f;
        [SerializeField, Min(0f)] private float parkourHopSpeed = 6.2f;
        [SerializeField, Range(-1f, 1f)] private float parkourHopForwardDot = 0.35f;
        [SerializeField, Min(0f)] private float proneCrawlSpeed = 1.45f;
        [SerializeField, Min(0.1f)] private float proneControllerHeight = 0.7f;
        [SerializeField] private Vector3 proneControllerCenter = new Vector3(0f, 0.36f, 0f);
        [SerializeField, Min(0.1f)] private float controllerPoseBlendSpeed = 12f;

        [Header("Traversal Profile")]
        [SerializeField] private SCFTraversalProfile traversalProfile;
        [SerializeField, Range(0f, 1f)] private float carriedLoad01;
        [Tooltip("Non-parkour characters are kicked off the wall after this many implied steps.")]
        [SerializeField, Min(1)] private int standardWallRunStepLimit = 3;
        [SerializeField, Min(0.05f)] private float standardWallRunStepDuration = 0.27f;
        [SerializeField, Range(0.45f, 1.25f)] private float standardWallRunHeightMultiplier = 0.82f;
        [SerializeField, Range(0.45f, 1.25f)] private float heavyWallRunHeightMultiplier = 0.68f;
        [SerializeField, Range(0.1f, 1f)] private float standardWallJumpUpwardMultiplier = 0.55f;
        [SerializeField, Range(0.1f, 1.25f)] private float standardWallJumpAwayMultiplier = 1f;
        [SerializeField, Min(0f)] private float standardWallRunRegrabCooldown = 0.55f;
        [SerializeField] private bool standardWallRunRequiresReleaseBeforeRegrab = true;
        [SerializeField] private float standardWallRunSlideOffDownSpeed = -1.6f;
        [SerializeField, Min(1f)] private float standardVaultDurationMultiplier = 1.65f;
        [SerializeField, Min(1f)] private float standardClimbDurationMultiplier = 2.1f;
        [SerializeField, Min(0.05f)] private float standardClimbMaxHeight = 1.55f;
        [SerializeField, Min(0.05f)] private float heavyClimbMaxHeight = 1.18f;
        [SerializeField, Min(1f)] private float loadTraversalDurationMultiplier = 1.45f;

        [Header("Wall Run")]
        [SerializeField] private LayerMask traversalMask = ~0;
        [SerializeField, Min(0.05f)] private float wallProbeDistance = 0.9f;
        [SerializeField, Range(0f, 0.7f)] private float maxWallNormalY = 0.25f;
        [SerializeField, Min(0.1f)] private float wallRunSpeed = 8.2f;
        [Tooltip("Used as the wall-run animation phase length only. Wall-run stays active while jump is held.")]
        [SerializeField, Min(0.1f)] private float wallRunMaxDuration = 1.35f;
        [SerializeField] private float wallRunGravity = -0.45f;
        [SerializeField, Min(0f)] private float wallRunStickForce = 3.2f;
        [SerializeField, Range(0f, 1f)] private float wallRunMinMoveInput = 0.2f;
        [SerializeField, Range(0f, 1f)] private float wallRunMinAlongWallDot = 0.2f;
        [SerializeField, Min(0f)] private float wallRunContactOffset = 0.04f;
        [SerializeField, Min(0.1f)] private float wallRunLatchSharpness = 18f;
        [SerializeField, Range(0.25f, 1.25f)] private float wallRunAnimationSpeed = 1f;
        [SerializeField, Min(0f)] private float wallRunMinimumAirTime = 0.08f;
        [SerializeField, Min(0f)] private float wallRunMountUpwardSpeed = 5.8f;
        [SerializeField, Range(0.5f, 1.6f)] private float wallRunHeadHeightMultiplier = 1.05f;
        [SerializeField, Min(0.1f)] private float wallRunHeightLatchSharpness = 10f;
        [SerializeField, Range(0f, 80f)] private float wallRunVisualLeanDegrees = 34f;
        [SerializeField, Range(0f, 18f)] private float wallRunVisualLeanVariance;
        [SerializeField, Min(0f)] private float wallRunVisualLeanFrequency = 1.35f;
        [SerializeField, Min(0.1f)] private float wallRunVisualRotationSharpness = 12f;
        [SerializeField, Min(0f)] private float wallJumpUpwardSpeed = 7.8f;
        [SerializeField, Min(0f)] private float wallJumpAwaySpeed = 2.2f;
        [SerializeField, Min(0f)] private float wallJumpRollMomentumBonus = 2.8f;
        [SerializeField, Min(0f)] private float wallJumpRollInputWindow = 0.45f;

        [Header("Parkour Wall Climb")]
        [SerializeField] private bool enableParkourWallClimbUp = true;
        [SerializeField, Range(0.3f, 1f)] private float wallClimbUpApproachDot = 0.72f;
        [SerializeField, Min(0f)] private float wallClimbUpSpeed = 4.2f;
        [SerializeField, Min(0.1f)] private float wallClimbUpMaxDuration = 2.2f;
        [SerializeField, Min(0f)] private float wallClimbLedgeReachSlack = 0.12f;
        [SerializeField, Min(0f)] private float wallClimbTopForwardClearance = 0.55f;
        [SerializeField, Min(0f)] private float wallClimbTopProbeHeight = 0.75f;

        [Header("Vault")]
        [SerializeField] private bool enableVault = true;
        [SerializeField] private bool enableProne;
        [SerializeField, Min(0.05f)] private float vaultProbeDistance = 1.25f;
        [SerializeField, Min(0.05f)] private float vaultMinHeight = 0.35f;
        [SerializeField, Min(0.05f)] private float vaultMaxHeight = 1.35f;
        [SerializeField, Min(0.05f)] private float vaultForwardClearance = 1.2f;
        [SerializeField, Min(0.05f)] private float vaultDuration = 0.42f;
        [SerializeField, Min(0f)] private float vaultArcHeight = 0.55f;
        [SerializeField, Min(0.05f)] private float climbMinHeight = 1.35f;
        [SerializeField, Min(0.05f)] private float climbMaxHeight = 2.05f;
        [SerializeField, Min(0.05f)] private float climbForwardClearance = 0.45f;
        [SerializeField, Min(0.05f)] private float climbDuration = 0.78f;
        [SerializeField, Min(0f)] private float climbArcHeight = 0.2f;
        [SerializeField] private bool enableParkourAutoTraversal = true;
        [SerializeField, Range(0.5f, 1.2f)] private float parkourAutoTraversalHeadHeightMultiplier = 1f;
        [SerializeField, Range(0.35f, 1f)] private float parkourAutoSlideMinHeightMultiplier = 0.68f;
        [SerializeField, Range(0.25f, 2f)] private float parkourSlideVaultDurationMultiplier = 0.9f;
        [SerializeField, Min(0f)] private float parkourSlideVaultRollSpeedBonus = 1.6f;
        [SerializeField, Min(1f)] private float parkourManualTraversalDurationMultiplier = 1.35f;
        [SerializeField, Range(0.45f, 1.1f)] private float parkourManualVaultMaxHeightMultiplier = 0.78f;

        [Header("Facing")]
        [SerializeField] private CharacterFacingMode facingMode = CharacterFacingMode.AimThenMovement;
        [SerializeField, Min(0.1f)] private float movementRotationSharpness = 16f;
        [SerializeField, Min(0.1f)] private float aimRotationSharpness = 24f;
        [SerializeField] private bool separateAimFromLocomotion = true;
        [SerializeField] private bool lowerBodyAimsWhenIdle;
        [SerializeField, Min(0f)] private float lowerBodyMoveThreshold = 0.2f;

        [Header("Aiming")]
        [SerializeField] private LayerMask aimSurfaceMask = ~0;
        [SerializeField] private float aimRayDistance = 500f;
        [SerializeField] private float fallbackAimPlaneY = 0f;

        public Vector2 MoveInput => input != null ? input.Move : Vector2.zero;
        public Vector3 DesiredVelocity { get; private set; }
        public Vector3 PlanarVelocity { get; private set; }
        public Vector3 AimDirection { get; private set; } = Vector3.forward;
        public bool HasAimDirection { get; private set; }
        public bool IsGrounded => IsControllerSupported(0.03f);
        public bool AimHeld => input != null && input.AimHeld;
        public bool SprintHeld => input != null && input.SprintHeld && !AimHeld;
        public float CurrentMaxSpeed => ResolveCurrentMaxSpeed();
        public CharacterMobilityState MobilityState { get; private set; } = CharacterMobilityState.Locomotion;
        public float MobilityStateNormalizedTime { get; private set; }
        public float JumpCharge01 { get; private set; }
        public bool IsJumpCharging => MobilityState == CharacterMobilityState.JumpCharge;
        public bool IsJumping => MobilityState == CharacterMobilityState.Jumping;
        public bool IsFalling => MobilityState == CharacterMobilityState.Falling;
        public bool IsAirborne => IsJumping || IsFalling;
        public bool IsCombatRolling => MobilityState == CharacterMobilityState.CombatRoll;
        public bool IsProne => MobilityState == CharacterMobilityState.Prone;
        public bool IsWallRunning => MobilityState == CharacterMobilityState.WallRun;
        public bool IsWallClimbingUp => IsWallRunning && wallRunIsVertical;
        public bool IsVaulting => MobilityState == CharacterMobilityState.Vault;
        public bool IsVaultSliding => IsVaulting && activeTraversalUsesSlide;
        public bool IsClimbing => MobilityState == CharacterMobilityState.Climb;
        public bool IsObstacleTraversing => IsVaulting || IsClimbing;
        public SCFTraversalProfile TraversalProfile => traversalProfile;
        public float CarriedLoad01 => carriedLoad01;
        public bool SeparateAimFromLocomotion => separateAimFromLocomotion;
        public Vector3 BodyFacingDirection { get; private set; } = Vector3.forward;
        public Vector3 WallNormal => wallNormal;
        public Vector3 WallSurfacePoint => wallRunSurfacePoint;
        public LayerMask TraversalMask => traversalMask;
        public float ControllerWorldHeight => GetControllerWorldHeight();
        public float ControllerFootY => GetControllerFootY();
        public float WallRunCycle01 => Mathf.Repeat(mobilityStateTimer / Mathf.Max(0.001f, wallRunMaxDuration), 1f);
        public float WallRunSide { get; private set; } = 1f;
        public float WallRunAnimationSpeed => wallRunAnimationSpeed;
        public int CombatRollSequence { get; private set; }
        public int JumpSequence { get; private set; }
        public int LandSequence { get; private set; }
        public int WallRunSequence { get; private set; }
        public int VaultSequence { get; private set; }
        public int ClimbSequence { get; private set; }

        private struct TraversalCandidate
        {
            public Vector3 LandingPosition;
            public Vector3 ContactPosition;
            public Vector3 ForwardDirection;
            public bool ShouldClimb;
            public bool ShouldSlide;

            public TraversalCandidate(Vector3 landingPosition, Vector3 contactPosition, Vector3 forwardDirection, bool shouldClimb, bool shouldSlide = false)
            {
                LandingPosition = landingPosition;
                ContactPosition = contactPosition;
                ForwardDirection = forwardDirection;
                ShouldClimb = shouldClimb;
                ShouldSlide = shouldSlide;
            }
        }

        private float verticalVelocity;
        private float mobilityStateTimer;
        private bool jumpHasLeftGround;
        private bool mobilityTapStartedAirborne;
        private bool jumpChargeStartedAirborne;
        private bool combatRollStartedAirborne;
        private bool parkourAirMobilityAvailable;
        private float jumpRetainedPlanarSpeed;
        private float activeCombatRollDuration;
        private float activeCombatRollSpeed;
        private float activeCombatRollSpeedBonus;
        private float wallRunRetainedSpeed;
        private float wallJumpRollBoostTimer;
        private float wallRunRegrabLockoutTimer;
        private bool standardWallRunAwaitingRelease;
        private bool wallRunIsVertical;
        private Collider wallRunCollider;
        private Vector3 lockedMobilityDirection = Vector3.forward;
        private Vector3 wallNormal = Vector3.back;
        private Vector3 wallRunDirection = Vector3.forward;
        private Vector3 wallRunSurfacePoint;
        private float wallRunGroundY;
        private float wallRunTargetCenterY;
        private Vector3 vaultStartPosition;
        private Vector3 vaultContactPosition;
        private Vector3 vaultEndPosition;
        private bool hasVaultContactPosition;
        private bool activeTraversalUsesSlide;
        private float activeTraversalDuration;
        private float activeTraversalArcHeight;
        private float standingControllerHeight;
        private Vector3 standingControllerCenter;
        private bool hasControllerPoseDefaults;

        private void Reset()
        {
            characterController = GetComponent<CharacterController>();
            input = GetComponent<IsometricPlayerInput>();
            facingRoot = transform;
        }

        private void Awake()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            if (input == null)
            {
                input = GetComponent<IsometricPlayerInput>();
            }

            if (facingRoot == null)
            {
                facingRoot = transform;
            }

            CacheStandingControllerPose();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Configure(IsometricPlayerInput playerInput, Camera camera, Transform visualFacingRoot)
        {
            input = playerInput;
            worldCamera = camera;
            facingRoot = visualFacingRoot != null ? visualFacingRoot : transform;
            CacheStandingControllerPose();
        }

        public void SetTraversalProfile(SCFTraversalProfile profile)
        {
            traversalProfile = profile;
        }

        public void SetCarriedLoad(float load01)
        {
            carriedLoad01 = Mathf.Clamp01(load01);
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            UpdateAimDirection();
            UpdateMobility(deltaTime);

            Vector3 moveDirection = CameraRelativeDirection(MoveInput);
            float desiredSpeed = ResolveDesiredSpeed();
            DesiredVelocity = ResolveDesiredVelocity(moveDirection, desiredSpeed);

            float rate = DesiredVelocity.sqrMagnitude > PlanarVelocity.sqrMagnitude ? acceleration : deceleration;
            if (MobilityState == CharacterMobilityState.CombatRoll || MobilityState == CharacterMobilityState.Jumping)
            {
                PlanarVelocity = DesiredVelocity;
            }
            else
            {
                PlanarVelocity = Vector3.MoveTowards(PlanarVelocity, DesiredVelocity, rate * deltaTime);
            }

            ApplyMovement(deltaTime);
            UpdateFacing(deltaTime);
            UpdateControllerPose(deltaTime);
        }

        private float ResolveCurrentMaxSpeed()
        {
            if (MobilityState == CharacterMobilityState.CombatRoll)
            {
                return ScaleMetric(ResolveActiveCombatRollSpeed() + activeCombatRollSpeedBonus);
            }

            if (MobilityState == CharacterMobilityState.Prone)
            {
                return ScaleMetric(proneCrawlSpeed);
            }

            if (MobilityState == CharacterMobilityState.JumpCharge)
            {
                return ScaleMetric(SprintHeld ? sprintSpeed : walkSpeed);
            }

            if (MobilityState == CharacterMobilityState.Falling)
            {
                return ScaleMetric(SprintHeld ? sprintSpeed : walkSpeed) * airborneMoveMultiplier;
            }

            if (MobilityState == CharacterMobilityState.WallRun)
            {
                return ResolveActiveWallRunSpeed();
            }

            if (IsObstacleTraversing)
            {
                return 0f;
            }

            return ScaleMetric(SprintHeld ? sprintSpeed : walkSpeed);
        }

        private float ResolveDesiredSpeed()
        {
            if (MobilityState == CharacterMobilityState.CombatRoll)
            {
                return ScaleMetric(ResolveActiveCombatRollSpeed() + activeCombatRollSpeedBonus);
            }

            if (MobilityState == CharacterMobilityState.Jumping)
            {
                return Mathf.Max(ScaleMetric(jumpPlanarSpeed), jumpRetainedPlanarSpeed);
            }

            if (MobilityState == CharacterMobilityState.Falling)
            {
                return CurrentMaxSpeed * Mathf.Clamp01(MoveInput.magnitude);
            }

            if (MobilityState == CharacterMobilityState.JumpCharge && jumpChargeStartedAirborne)
            {
                return Mathf.Max(jumpRetainedPlanarSpeed, CurrentMaxSpeed * Mathf.Clamp01(MoveInput.magnitude));
            }

            if (MobilityState == CharacterMobilityState.MobilityTapWindow && mobilityTapStartedAirborne)
            {
                return Mathf.Max(jumpRetainedPlanarSpeed, CurrentMaxSpeed * Mathf.Clamp01(MoveInput.magnitude));
            }

            if (MobilityState == CharacterMobilityState.WallRun)
            {
                return ResolveActiveWallRunSpeed();
            }

            if (IsObstacleTraversing)
            {
                return 0f;
            }

            return CurrentMaxSpeed * Mathf.Clamp01(MoveInput.magnitude);
        }

        private Vector3 ResolveDesiredVelocity(Vector3 moveDirection, float desiredSpeed)
        {
            if (MobilityState == CharacterMobilityState.CombatRoll || MobilityState == CharacterMobilityState.Jumping)
            {
                return lockedMobilityDirection * desiredSpeed;
            }

            if ((MobilityState == CharacterMobilityState.JumpCharge && jumpChargeStartedAirborne)
                || (MobilityState == CharacterMobilityState.MobilityTapWindow && mobilityTapStartedAirborne))
            {
                return lockedMobilityDirection * desiredSpeed;
            }

            if (MobilityState == CharacterMobilityState.WallRun)
            {
                return wallRunDirection * desiredSpeed;
            }

            if (IsObstacleTraversing)
            {
                return Vector3.zero;
            }

            return moveDirection * desiredSpeed;
        }

        private float ResolveActiveWallRunSpeed()
        {
            float profileSpeed = wallRunIsVertical ? wallClimbUpSpeed : wallRunSpeed;
            return Mathf.Max(ScaleMetric(profileSpeed), wallRunRetainedSpeed);
        }

        private void UpdateMobility(float deltaTime)
        {
            TickWallJumpRollWindow(deltaTime);

            if (ShouldBeginEnvironmentalFall())
            {
                BeginEnvironmentalFall();
            }

            if (input == null)
            {
                if (IsAirborne)
                {
                    mobilityStateTimer += deltaTime;
                }

                MobilityStateNormalizedTime = ResolveMobilityNormalizedTime();
                return;
            }

            switch (MobilityState)
            {
                case CharacterMobilityState.Locomotion:
                    if (TryBeginAutoTraversal())
                    {
                        break;
                    }

                    if (input.MobilityPressedThisFrame)
                    {
                        BeginMobilityTapWindow();
                    }
                    break;

                case CharacterMobilityState.MobilityTapWindow:
                    TickMobilityTapWindow(deltaTime);
                    break;

                case CharacterMobilityState.JumpCharge:
                    TickJumpCharge(deltaTime);
                    break;

                case CharacterMobilityState.Jumping:
                    mobilityStateTimer += deltaTime;
                    if (input.MobilityPressedThisFrame && TryBeginParkourAirMobility())
                    {
                        break;
                    }

                    if (wallJumpRollBoostTimer > 0f && input.MobilityPressedThisFrame)
                    {
                        BeginCombatRoll(wallJumpRollMomentumBonus);
                        break;
                    }

                    if (input.MobilityHeld)
                    {
                        TryBeginWallRun();
                    }
                    break;

                case CharacterMobilityState.Falling:
                    mobilityStateTimer += deltaTime;
                    if (input.MobilityPressedThisFrame && TryBeginParkourAirMobility())
                    {
                        break;
                    }

                    if (input.MobilityHeld)
                    {
                        TryBeginWallRun();
                    }
                    break;

                case CharacterMobilityState.CombatRoll:
                    TickCombatRoll(deltaTime);
                    break;

                case CharacterMobilityState.Prone:
                    ReturnToFeet();
                    break;

                case CharacterMobilityState.WallRun:
                    TickWallRun(deltaTime);
                    break;

                case CharacterMobilityState.Vault:
                case CharacterMobilityState.Climb:
                    break;
            }

            MobilityStateNormalizedTime = ResolveMobilityNormalizedTime();
        }

        private bool TryBeginParkourAirMobility()
        {
            if (!parkourAirMobilityAvailable || !IsParkourTraversal())
            {
                return false;
            }

            parkourAirMobilityAvailable = false;
            BeginMobilityTapWindow(true);
            return true;
        }

        private void BeginMobilityTapWindow(bool startedAirborne = false)
        {
            MobilityState = CharacterMobilityState.MobilityTapWindow;
            mobilityStateTimer = 0f;
            JumpCharge01 = 0f;
            mobilityTapStartedAirborne = startedAirborne;
            jumpChargeStartedAirborne = false;
            combatRollStartedAirborne = false;
            if (startedAirborne)
            {
                lockedMobilityDirection = PlanarVelocity.sqrMagnitude > 0.01f ? PlanarVelocity.normalized : ResolveMobilityDirection();
                jumpRetainedPlanarSpeed = Mathf.Max(PlanarVelocity.magnitude, jumpRetainedPlanarSpeed, ScaleMetric(jumpPlanarSpeed));
                jumpHasLeftGround = true;
            }
        }

        private void TickWallJumpRollWindow(float deltaTime)
        {
            if (wallJumpRollBoostTimer > 0f)
            {
                wallJumpRollBoostTimer = Mathf.Max(0f, wallJumpRollBoostTimer - deltaTime);
            }

            if (wallRunRegrabLockoutTimer > 0f)
            {
                wallRunRegrabLockoutTimer = Mathf.Max(0f, wallRunRegrabLockoutTimer - deltaTime);
            }

            if (standardWallRunAwaitingRelease && (input == null || !input.MobilityHeld))
            {
                standardWallRunAwaitingRelease = false;
            }
        }

        private void TickMobilityTapWindow(float deltaTime)
        {
            mobilityStateTimer += deltaTime;
            if (mobilityTapStartedAirborne)
            {
                TickAirMobilityTapWindow();
                return;
            }

            if (input.MobilityReleasedThisFrame)
            {
                if (mobilityStateTimer < rollTapThreshold)
                {
                    BeginCombatRoll();
                }
                else
                {
                    LaunchHeldTraversal(true);
                }

                return;
            }

            if (mobilityStateTimer >= rollTapThreshold)
            {
                LaunchHeldTraversal(false);
            }
        }

        private void TickAirMobilityTapWindow()
        {
            if (input.MobilityReleasedThisFrame)
            {
                if (mobilityStateTimer < rollTapThreshold)
                {
                    BeginCombatRoll(parkourAirRollSpeedBonus, true);
                }
                else
                {
                    LaunchChargedJump(parkourAirJumpStrength);
                }

                return;
            }

            if (mobilityStateTimer >= rollTapThreshold)
            {
                BeginJumpCharge(true);
            }
        }

        private void LaunchHeldTraversal(bool released)
        {
            if (TryBeginVault(false))
            {
                return;
            }

            if (released)
            {
                LaunchChargedJump(heldJumpStrength);
                return;
            }

            BeginJumpCharge(false);
        }

        private bool TryBeginAutoTraversal()
        {
            if (!enableParkourAutoTraversal
                || !IsParkourTraversal()
                || !SprintHeld
                || MoveInput.magnitude < wallRunMinMoveInput)
            {
                return false;
            }

            return TryBeginVault(true);
        }

        private void BeginJumpCharge(bool startedAirborne)
        {
            MobilityState = CharacterMobilityState.JumpCharge;
            mobilityStateTimer = 0f;
            JumpCharge01 = 0f;
            jumpChargeStartedAirborne = startedAirborne;
            mobilityTapStartedAirborne = false;
            lockedMobilityDirection = startedAirborne && PlanarVelocity.sqrMagnitude > 0.01f ? PlanarVelocity.normalized : ResolveMobilityDirection();
            if (startedAirborne)
            {
                jumpRetainedPlanarSpeed = Mathf.Max(PlanarVelocity.magnitude, jumpRetainedPlanarSpeed, ScaleMetric(jumpPlanarSpeed));
                jumpHasLeftGround = true;
            }
        }

        private void TickJumpCharge(float deltaTime)
        {
            mobilityStateTimer += deltaTime;
            JumpCharge01 = Mathf.Clamp01(mobilityStateTimer / Mathf.Max(0.001f, jumpChargeDuration));

            if (input.MobilityReleasedThisFrame || mobilityStateTimer >= jumpChargeDuration)
            {
                LaunchChargedJump(JumpCharge01);
            }
        }

        private void LaunchChargedJump(float charge01)
        {
            bool launchedFromAir = jumpChargeStartedAirborne
                                   || mobilityTapStartedAirborne
                                   || MobilityState == CharacterMobilityState.Jumping
                                   || MobilityState == CharacterMobilityState.Falling;
            MobilityState = CharacterMobilityState.Jumping;
            mobilityStateTimer = 0f;
            JumpCharge01 = Mathf.Clamp01(charge01);
            jumpHasLeftGround = launchedFromAir;
            mobilityTapStartedAirborne = false;
            jumpChargeStartedAirborne = false;
            combatRollStartedAirborne = false;
            parkourAirMobilityAvailable = false;
            lockedMobilityDirection = PlanarVelocity.sqrMagnitude > 0.01f ? PlanarVelocity.normalized : ResolveMobilityDirection();
            float chargeSpeedBoost = ScaleMetric(maxJumpChargePlanarBoost) * JumpCharge01;
            jumpRetainedPlanarSpeed = Mathf.Max(PlanarVelocity.magnitude + chargeSpeedBoost, jumpRetainedPlanarSpeed, ScaleMetric(jumpPlanarSpeed));
            verticalVelocity = Mathf.Sqrt(2f * Mathf.Abs(ScaleMetric(gravity)) * ScaleMetric(Mathf.Lerp(minJumpHeight, maxJumpHeight, JumpCharge01)));
            if (input != null && input.MobilityHeld && MoveInput.magnitude >= wallRunMinMoveInput && TryFindRunnableWall(lockedMobilityDirection, out _))
            {
                verticalVelocity = Mathf.Max(verticalVelocity, ScaleMetric(wallRunMountUpwardSpeed));
            }

            JumpSequence++;
        }

        private bool ShouldBeginEnvironmentalFall()
        {
            if ((MobilityState == CharacterMobilityState.MobilityTapWindow && mobilityTapStartedAirborne)
                || (MobilityState == CharacterMobilityState.JumpCharge && jumpChargeStartedAirborne)
                || (MobilityState == CharacterMobilityState.CombatRoll && combatRollStartedAirborne))
            {
                return false;
            }

            if (MobilityState != CharacterMobilityState.Locomotion
                && MobilityState != CharacterMobilityState.MobilityTapWindow
                && MobilityState != CharacterMobilityState.Prone)
            {
                return false;
            }

            if (characterController == null || !characterController.enabled || IsControllerSupported(0.08f))
            {
                return false;
            }

            return verticalVelocity <= 0.01f;
        }

        private void BeginEnvironmentalFall()
        {
            MobilityState = CharacterMobilityState.Falling;
            mobilityStateTimer = 0f;
            JumpCharge01 = 0f;
            mobilityTapStartedAirborne = false;
            jumpChargeStartedAirborne = false;
            combatRollStartedAirborne = false;
            jumpHasLeftGround = true;
            lockedMobilityDirection = PlanarVelocity.sqrMagnitude > 0.01f ? PlanarVelocity.normalized : ResolveMobilityDirection();
            jumpRetainedPlanarSpeed = Mathf.Max(0f, PlanarVelocity.magnitude);
            verticalVelocity = Mathf.Min(verticalVelocity, 0f);
            JumpSequence++;
        }

        private void TickCombatRoll(float deltaTime)
        {
            mobilityStateTimer += deltaTime;

            if (mobilityStateTimer >= ResolveActiveCombatRollDuration())
            {
                if (combatRollStartedAirborne && !IsControllerSupported(0.08f))
                {
                    BeginAirborneFallFromCurrentMotion();
                    return;
                }

                ReturnToFeet();
            }
        }

        private void BeginCombatRoll(float speedBonus = 0f, bool startedAirborne = false)
        {
            MobilityState = CharacterMobilityState.CombatRoll;
            mobilityStateTimer = 0f;
            JumpCharge01 = 0f;
            mobilityTapStartedAirborne = false;
            jumpChargeStartedAirborne = false;
            combatRollStartedAirborne = startedAirborne;
            parkourAirMobilityAvailable = false;
            activeCombatRollSpeedBonus = speedBonus;
            lockedMobilityDirection = PlanarVelocity.sqrMagnitude > 0.01f ? PlanarVelocity.normalized : ResolveMobilityDirection();
            bool forwardRoll = IsForwardMobilityDirection(lockedMobilityDirection);
            activeCombatRollDuration = forwardRoll ? combatRollDuration : parkourHopDuration;
            activeCombatRollSpeed = forwardRoll ? combatRollSpeed : parkourHopSpeed;
            if (!startedAirborne)
            {
                verticalVelocity = Mathf.Min(verticalVelocity, ScaleMetric(groundedStickForce));
            }

            CombatRollSequence++;
        }

        private void BeginAirborneFallFromCurrentMotion()
        {
            MobilityState = CharacterMobilityState.Falling;
            mobilityStateTimer = 0f;
            JumpCharge01 = 0f;
            mobilityTapStartedAirborne = false;
            jumpChargeStartedAirborne = false;
            combatRollStartedAirborne = false;
            jumpHasLeftGround = true;
            lockedMobilityDirection = PlanarVelocity.sqrMagnitude > 0.01f ? PlanarVelocity.normalized : ResolveMobilityDirection();
            jumpRetainedPlanarSpeed = Mathf.Max(0f, PlanarVelocity.magnitude);
            verticalVelocity = Mathf.Min(verticalVelocity, 0f);
        }

        private void BeginProne()
        {
            if (!enableProne)
            {
                ReturnToFeet();
                return;
            }

            MobilityState = CharacterMobilityState.Prone;
            mobilityStateTimer = 0f;
            JumpCharge01 = 0f;
            PlanarVelocity = Vector3.zero;
        }

        private void ReturnToFeet()
        {
            MobilityState = CharacterMobilityState.Locomotion;
            mobilityStateTimer = 0f;
            JumpCharge01 = 0f;
            mobilityTapStartedAirborne = false;
            jumpChargeStartedAirborne = false;
            combatRollStartedAirborne = false;
            parkourAirMobilityAvailable = false;
            jumpRetainedPlanarSpeed = 0f;
            activeCombatRollDuration = 0f;
            activeCombatRollSpeed = 0f;
            activeCombatRollSpeedBonus = 0f;
            wallRunRetainedSpeed = 0f;
            wallJumpRollBoostTimer = 0f;
            wallRunIsVertical = false;
            wallRunCollider = null;
            hasVaultContactPosition = false;
            activeTraversalUsesSlide = false;
            activeTraversalDuration = 0f;
            activeTraversalArcHeight = 0f;
        }

        private float ResolveActiveCombatRollDuration()
        {
            return activeCombatRollDuration > 0.001f ? activeCombatRollDuration : combatRollDuration;
        }

        private float ResolveActiveCombatRollSpeed()
        {
            return activeCombatRollSpeed > 0.001f ? activeCombatRollSpeed : combatRollSpeed;
        }

        private bool IsForwardMobilityDirection(Vector3 worldDirection)
        {
            if (worldDirection.sqrMagnitude <= 0.0001f)
            {
                return true;
            }

            Transform root = facingRoot != null ? facingRoot : transform;
            Vector3 forward = Vector3.ProjectOnPlane(root.forward, Vector3.up);
            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = AimDirection.sqrMagnitude > 0.0001f ? AimDirection : Vector3.forward;
            }

            return Vector3.Dot(forward.normalized, worldDirection.normalized) >= parkourHopForwardDot;
        }

        private void CompleteJumpLanding()
        {
            LandSequence++;
            verticalVelocity = ScaleMetric(groundedStickForce);
            ReturnToFeet();
        }

        private bool TryBeginWallRun()
        {
            if (input == null
                || !input.MobilityHeld
                || MoveInput.magnitude < wallRunMinMoveInput
                || (MobilityState == CharacterMobilityState.Jumping && mobilityStateTimer < wallRunMinimumAirTime))
            {
                return false;
            }

            if (!IsParkourTraversal() && wallRunRegrabLockoutTimer > 0f)
            {
                return false;
            }

            if (!IsParkourTraversal() && standardWallRunAwaitingRelease)
            {
                return false;
            }

            Vector3 moveDirection = CameraRelativeDirection(MoveInput);
            if (!TryFindRunnableWall(moveDirection, out RaycastHit wallHit))
            {
                return false;
            }

            if (!CanStartWallRunAtCurrentHeight())
            {
                return false;
            }

            if (ShouldBeginParkourWallClimbUp(moveDirection, wallHit.normal))
            {
                BeginWallClimbUp(wallHit);
                return true;
            }

            BeginWallRun(wallHit, moveDirection);
            return true;
        }

        private void BeginWallRun(RaycastHit wallHit, Vector3 desiredDirection)
        {
            wallNormal = Vector3.ProjectOnPlane(wallHit.normal, Vector3.up).normalized;
            if (wallNormal.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            wallRunIsVertical = false;
            wallRunCollider = wallHit.collider;
            wallRunSurfacePoint = wallHit.point;
            wallRunDirection = ResolveWallRunDirection(wallNormal, desiredDirection);
            UpdateWallRunSide();
            lockedMobilityDirection = wallRunDirection;
            wallRunRetainedSpeed = Mathf.Max(PlanarVelocity.magnitude, ScaleMetric(wallRunSpeed));
            wallRunGroundY = SampleWallRunGroundY(GetControllerFootY());
            wallRunTargetCenterY = ResolveWallRunTargetCenterY();
            MobilityState = CharacterMobilityState.WallRun;
            mobilityStateTimer = 0f;
            JumpCharge01 = 0f;
            parkourAirMobilityAvailable = false;
            verticalVelocity = Mathf.Max(verticalVelocity, ScaleMetric(groundedStickForce) * 0.25f);
            WallRunSequence++;
        }

        private void BeginWallClimbUp(RaycastHit wallHit)
        {
            wallNormal = Vector3.ProjectOnPlane(wallHit.normal, Vector3.up).normalized;
            if (wallNormal.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            wallRunIsVertical = true;
            wallRunCollider = wallHit.collider;
            wallRunSurfacePoint = wallHit.point;
            wallRunDirection = -wallNormal;
            UpdateWallRunSide();
            lockedMobilityDirection = wallRunDirection;
            wallRunRetainedSpeed = Mathf.Max(PlanarVelocity.magnitude, ScaleMetric(wallClimbUpSpeed));
            wallRunGroundY = SampleWallRunGroundY(GetControllerFootY());
            wallRunTargetCenterY = GetCurrentControllerCenterY();
            MobilityState = CharacterMobilityState.WallRun;
            mobilityStateTimer = 0f;
            JumpCharge01 = 0f;
            parkourAirMobilityAvailable = false;
            verticalVelocity = Mathf.Max(verticalVelocity, ScaleMetric(wallClimbUpSpeed));
            WallRunSequence++;
        }

        private void TickWallRun(float deltaTime)
        {
            mobilityStateTimer += deltaTime;

            if (input == null || !input.MobilityHeld)
            {
                BeginWallJumpFromRun();
                return;
            }

            if (wallRunIsVertical)
            {
                TickWallClimbUp(deltaTime);
                return;
            }

            if (!IsParkourTraversal() && mobilityStateTimer >= ResolveStandardWallRunDurationLimit())
            {
                SlideOffStandardWallRun();
                return;
            }

            Vector3 moveDirection = CameraRelativeDirection(MoveInput);
            Vector3 desiredDirection = moveDirection.sqrMagnitude > 0.0001f ? moveDirection : wallRunDirection;
            if (!TryRefreshWallRunContact(desiredDirection, out RaycastHit wallHit))
            {
                DetachFromWallRun(false);
                return;
            }

            wallNormal = Vector3.ProjectOnPlane(wallHit.normal, Vector3.up).normalized;
            wallRunSurfacePoint = wallHit.point;
            wallRunDirection = ResolveWallRunDirection(wallNormal, desiredDirection);
            UpdateWallRunSide();
            lockedMobilityDirection = wallRunDirection;
            wallRunRetainedSpeed = Mathf.Max(wallRunRetainedSpeed, PlanarVelocity.magnitude);
            wallRunGroundY = SampleWallRunGroundY(wallRunGroundY);
            wallRunTargetCenterY = Mathf.Max(wallRunTargetCenterY, ResolveWallRunTargetCenterY());
        }

        private void TickWallClimbUp(float deltaTime)
        {
            if (TryBeginWallClimbTopOut())
            {
                return;
            }

            Vector3 moveDirection = CameraRelativeDirection(MoveInput);
            Vector3 desiredDirection = moveDirection.sqrMagnitude > 0.0001f ? moveDirection : wallRunDirection;
            if (!TryRefreshWallRunContact(desiredDirection, out RaycastHit wallHit))
            {
                if (!TryBeginWallClimbTopOut())
                {
                    DetachFromWallRun(false);
                }

                return;
            }

            wallNormal = Vector3.ProjectOnPlane(wallHit.normal, Vector3.up).normalized;
            if (wallNormal.sqrMagnitude <= 0.0001f)
            {
                DetachFromWallRun(false);
                return;
            }

            wallRunCollider = wallHit.collider;
            wallRunSurfacePoint = wallHit.point;
            wallRunDirection = -wallNormal;
            UpdateWallRunSide();
            lockedMobilityDirection = wallRunDirection;
            wallRunRetainedSpeed = Mathf.Max(wallRunRetainedSpeed, PlanarVelocity.magnitude, ScaleMetric(wallClimbUpSpeed));
            wallRunGroundY = SampleWallRunGroundY(wallRunGroundY);
            wallRunTargetCenterY = GetCurrentControllerCenterY();

            if (TryBeginWallClimbTopOut())
            {
                return;
            }

            if (mobilityStateTimer >= wallClimbUpMaxDuration)
            {
                DetachFromWallRun(false);
            }
        }

        private bool TryRefreshWallRunContact(Vector3 desiredDirection, out RaycastHit wallHit)
        {
            if (wallNormal.sqrMagnitude > 0.0001f && TryFindWall(-wallNormal, out wallHit))
            {
                return true;
            }

            return TryFindRunnableWall(desiredDirection, out wallHit);
        }

        private bool ShouldBeginParkourWallClimbUp(Vector3 desiredDirection, Vector3 hitNormal)
        {
            if (!enableParkourWallClimbUp || !IsParkourTraversal())
            {
                return false;
            }

            Vector3 direction = Vector3.ProjectOnPlane(desiredDirection, Vector3.up);
            Vector3 normal = Vector3.ProjectOnPlane(hitNormal, Vector3.up);
            if (direction.sqrMagnitude <= 0.0001f || normal.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            float approachDot = Vector3.Dot(direction.normalized, -normal.normalized);
            return approachDot >= wallClimbUpApproachDot;
        }

        private bool TryBeginWallClimbTopOut()
        {
            if (!wallRunIsVertical || !TryResolveWallClimbTopOut(out TraversalCandidate traversal))
            {
                return false;
            }

            BeginWallClimbTopOut(traversal);
            return true;
        }

        private bool TryResolveWallClimbTopOut(out TraversalCandidate traversal)
        {
            traversal = default(TraversalCandidate);
            if (!wallRunIsVertical || wallRunCollider == null || wallNormal.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            Bounds obstacleBounds = wallRunCollider.bounds;
            float topY = obstacleBounds.max.y;
            float headY = characterController != null && characterController.enabled
                ? characterController.bounds.max.y
                : transform.position.y + GetControllerWorldHeight();
            if (headY + ScaleMetric(wallClimbLedgeReachSlack) < topY)
            {
                return false;
            }

            Vector3 climbDirection = -wallNormal.normalized;
            float controllerRadius = GetControllerWorldRadius();
            Vector3 edgePoint = wallRunSurfacePoint.sqrMagnitude > 0.0001f
                ? wallRunSurfacePoint
                : transform.position - climbDirection * controllerRadius;

            Vector3 contactPosition = edgePoint + climbDirection * controllerRadius;
            contactPosition.y = topY + 0.04f;

            Vector3 landingPosition = edgePoint + climbDirection * (controllerRadius + ScaleMetric(wallClimbTopForwardClearance));
            landingPosition.y = SampleWallClimbTopY(landingPosition, topY);

            traversal = new TraversalCandidate(landingPosition, contactPosition, climbDirection, true);
            return true;
        }

        private void BeginWallClimbTopOut(TraversalCandidate traversal)
        {
            MobilityState = CharacterMobilityState.Climb;
            mobilityStateTimer = 0f;
            JumpCharge01 = 0f;
            parkourAirMobilityAvailable = false;
            vaultStartPosition = transform.position;
            vaultContactPosition = traversal.ContactPosition;
            vaultEndPosition = traversal.LandingPosition;
            hasVaultContactPosition = true;
            activeTraversalUsesSlide = false;
            activeTraversalDuration = ResolveTraversalDuration(true, true, false);
            activeTraversalArcHeight = climbArcHeight;
            lockedMobilityDirection = traversal.ForwardDirection.sqrMagnitude > 0.0001f ? traversal.ForwardDirection.normalized : -wallNormal.normalized;
            PlanarVelocity = Vector3.zero;
            verticalVelocity = 0f;
            wallRunIsVertical = false;
            wallRunCollider = null;
            ClimbSequence++;
        }

        private void BeginWallJumpFromRun()
        {
            Vector3 awayFromWall = wallNormal.sqrMagnitude > 0.0001f ? wallNormal.normalized : Vector3.zero;
            float retainedSpeed = Mathf.Max(PlanarVelocity.magnitude, wallRunRetainedSpeed, ScaleMetric(wallRunSpeed));
            Vector3 momentum = wallRunDirection * retainedSpeed + awayFromWall * ScaleMetric(wallJumpAwaySpeed * ResolveWallJumpAwayMultiplier());
            lockedMobilityDirection = momentum.sqrMagnitude > 0.0001f ? momentum.normalized : ResolveMobilityDirection();
            jumpRetainedPlanarSpeed = momentum.magnitude;
            PlanarVelocity = lockedMobilityDirection * jumpRetainedPlanarSpeed;
            MobilityState = CharacterMobilityState.Jumping;
            mobilityStateTimer = 0f;
            JumpCharge01 = 1f;
            parkourAirMobilityAvailable = IsParkourTraversal() && parkourWallJumpKeepsAirMobility;
            jumpHasLeftGround = true;
            verticalVelocity = Mathf.Max(verticalVelocity, ScaleMetric(wallJumpUpwardSpeed * ResolveWallJumpUpwardMultiplier()));
            wallJumpRollBoostTimer = wallJumpRollInputWindow;
            wallRunIsVertical = false;
            wallRunCollider = null;
            BeginStandardWallRunLockout();
            JumpSequence++;
        }

        private void DetachFromWallRun(bool keepRollBoost)
        {
            MobilityState = CharacterMobilityState.Jumping;
            mobilityStateTimer = 0f;
            jumpHasLeftGround = true;
            parkourAirMobilityAvailable = IsParkourTraversal() && parkourWallJumpKeepsAirMobility;
            lockedMobilityDirection = wallRunDirection.sqrMagnitude > 0.0001f ? wallRunDirection : ResolveMobilityDirection();
            jumpRetainedPlanarSpeed = Mathf.Max(PlanarVelocity.magnitude, wallRunRetainedSpeed, ScaleMetric(jumpPlanarSpeed));
            verticalVelocity = Mathf.Min(verticalVelocity, 0f);
            if (!keepRollBoost)
            {
                wallJumpRollBoostTimer = 0f;
            }

            wallRunIsVertical = false;
            wallRunCollider = null;
            BeginStandardWallRunLockout();
        }

        private void SlideOffStandardWallRun()
        {
            float retainedSpeed = Mathf.Max(PlanarVelocity.magnitude, wallRunRetainedSpeed, ScaleMetric(wallRunSpeed));
            lockedMobilityDirection = wallRunDirection.sqrMagnitude > 0.0001f ? wallRunDirection : ResolveMobilityDirection();
            jumpRetainedPlanarSpeed = retainedSpeed;
            PlanarVelocity = lockedMobilityDirection * retainedSpeed;
            MobilityState = CharacterMobilityState.Falling;
            mobilityStateTimer = 0f;
            JumpCharge01 = 0f;
            parkourAirMobilityAvailable = false;
            jumpHasLeftGround = true;
            verticalVelocity = Mathf.Min(verticalVelocity, ScaleMetric(standardWallRunSlideOffDownSpeed));
            wallJumpRollBoostTimer = 0f;
            wallRunIsVertical = false;
            wallRunCollider = null;
            BeginStandardWallRunLockout();
        }

        private void BeginStandardWallRunLockout()
        {
            if (!IsParkourTraversal())
            {
                wallRunRegrabLockoutTimer = Mathf.Max(wallRunRegrabLockoutTimer, standardWallRunRegrabCooldown);
                if (standardWallRunRequiresReleaseBeforeRegrab)
                {
                    standardWallRunAwaitingRelease = true;
                }
            }
        }

        private Vector3 ResolveWallRunDirection(Vector3 normal, Vector3 desiredDirection)
        {
            Vector3 projectedDesired = Vector3.ProjectOnPlane(desiredDirection, normal);
            projectedDesired = Vector3.ProjectOnPlane(projectedDesired, Vector3.up);
            if (projectedDesired.sqrMagnitude > 0.0001f)
            {
                return projectedDesired.normalized;
            }

            Vector3 tangent = Vector3.Cross(Vector3.up, normal).normalized;
            if (tangent.sqrMagnitude <= 0.0001f)
            {
                tangent = Vector3.forward;
            }

            if (desiredDirection.sqrMagnitude > 0.0001f && Vector3.Dot(tangent, desiredDirection) < 0f)
            {
                tangent = -tangent;
            }

            return tangent;
        }

        private void UpdateWallRunSide()
        {
            if (wallRunDirection.sqrMagnitude <= 0.0001f || wallNormal.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float side = Vector3.Dot(Vector3.Cross(wallRunDirection, Vector3.up), wallNormal);
            if (Mathf.Abs(side) > 0.01f)
            {
                WallRunSide = side >= 0f ? 1f : -1f;
            }
        }

        private float ResolveWallRunTargetCenterY()
        {
            float targetFromGround = wallRunGroundY + GetControllerWorldHeight() * ResolveWallRunHeightMultiplier();

            if (characterController != null && characterController.enabled)
            {
                return IsParkourTraversal()
                    ? Mathf.Max(characterController.bounds.center.y, targetFromGround)
                    : targetFromGround;
            }

            return IsParkourTraversal()
                ? Mathf.Max(transform.position.y, targetFromGround)
                : targetFromGround;
        }

        private bool CanStartWallRunAtCurrentHeight()
        {
            if (IsParkourTraversal())
            {
                return true;
            }

            float groundY = SampleWallRunGroundY(GetControllerFootY());
            float maxCenterY = groundY + GetControllerWorldHeight() * ResolveWallRunHeightMultiplier() + ScaleMetric(0.12f);
            float currentCenterY = characterController != null && characterController.enabled
                ? characterController.bounds.center.y
                : transform.position.y;
            return currentCenterY <= maxCenterY;
        }

        private float ResolveWallRunHeightMultiplier()
        {
            if (IsParkourTraversal())
            {
                return wallRunHeadHeightMultiplier;
            }

            return Mathf.Lerp(standardWallRunHeightMultiplier, heavyWallRunHeightMultiplier, carriedLoad01);
        }

        private float ResolveStandardWallRunDurationLimit()
        {
            float loadPenalty = Mathf.Lerp(1f, 0.65f, carriedLoad01);
            return Mathf.Max(0.05f, standardWallRunStepLimit * standardWallRunStepDuration * loadPenalty);
        }

        private float ResolveWallJumpUpwardMultiplier()
        {
            if (IsParkourTraversal())
            {
                return 1f;
            }

            return Mathf.Lerp(standardWallJumpUpwardMultiplier, standardWallJumpUpwardMultiplier * 0.65f, carriedLoad01);
        }

        private float ResolveWallJumpAwayMultiplier()
        {
            if (IsParkourTraversal())
            {
                return 1f;
            }

            return Mathf.Lerp(standardWallJumpAwayMultiplier, standardWallJumpAwayMultiplier * 0.8f, carriedLoad01);
        }

        private bool TryBeginVault(bool automaticTraversal)
        {
            if (!enableVault)
            {
                return false;
            }

            if (MoveInput.magnitude < wallRunMinMoveInput)
            {
                return false;
            }

            Vector3 moveDirection = CameraRelativeDirection(MoveInput);
            if (!TryFindVault(moveDirection, automaticTraversal, out TraversalCandidate traversal))
            {
                return false;
            }

            bool shouldClimb = traversal.ShouldClimb;
            bool shouldSlide = traversal.ShouldSlide;
            MobilityState = shouldClimb ? CharacterMobilityState.Climb : CharacterMobilityState.Vault;
            mobilityStateTimer = 0f;
            JumpCharge01 = 0f;
            vaultStartPosition = transform.position;
            vaultContactPosition = traversal.ContactPosition;
            vaultEndPosition = traversal.LandingPosition;
            hasVaultContactPosition = true;
            activeTraversalUsesSlide = shouldSlide;
            activeTraversalDuration = ResolveTraversalDuration(shouldClimb, automaticTraversal, shouldSlide);
            activeTraversalArcHeight = shouldClimb ? climbArcHeight : shouldSlide ? vaultArcHeight * 0.35f : vaultArcHeight;
            lockedMobilityDirection = traversal.ForwardDirection.sqrMagnitude > 0.0001f ? traversal.ForwardDirection.normalized : moveDirection.normalized;
            verticalVelocity = 0f;
            if (shouldClimb)
            {
                ClimbSequence++;
            }
            else
            {
                VaultSequence++;
            }
            return true;
        }

        private Vector3 ResolveMobilityDirection()
        {
            Vector3 moveDirection = CameraRelativeDirection(MoveInput);
            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                return moveDirection.normalized;
            }

            if (HasAimDirection && AimDirection.sqrMagnitude > 0.0001f)
            {
                return AimDirection.normalized;
            }

            Transform root = facingRoot != null ? facingRoot : transform;
            Vector3 forward = Vector3.ProjectOnPlane(root.forward, Vector3.up);
            return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        }

        private float ResolveMobilityNormalizedTime()
        {
            if (MobilityState == CharacterMobilityState.CombatRoll)
            {
                return Mathf.Clamp01(mobilityStateTimer / Mathf.Max(0.001f, ResolveActiveCombatRollDuration()));
            }

            if (MobilityState == CharacterMobilityState.JumpCharge)
            {
                return JumpCharge01;
            }

            if (MobilityState == CharacterMobilityState.Jumping || MobilityState == CharacterMobilityState.Falling)
            {
                return Mathf.Clamp01(mobilityStateTimer / Mathf.Max(0.001f, jumpAnimationDuration));
            }

            if (MobilityState == CharacterMobilityState.MobilityTapWindow)
            {
                return Mathf.Clamp01(mobilityStateTimer / Mathf.Max(0.001f, rollTapThreshold));
            }

            if (MobilityState == CharacterMobilityState.WallRun)
            {
                return Mathf.Repeat(mobilityStateTimer / Mathf.Max(0.001f, wallRunMaxDuration), 1f);
            }

            if (IsObstacleTraversing)
            {
                float duration = activeTraversalDuration > 0.001f ? activeTraversalDuration : (IsClimbing ? climbDuration : vaultDuration);
                return Mathf.Clamp01(mobilityStateTimer / Mathf.Max(0.001f, duration));
            }

            return 0f;
        }

        private float ResolveTraversalDuration(bool shouldClimb, bool automaticTraversal, bool shouldSlide)
        {
            float duration = shouldClimb ? climbDuration : vaultDuration;
            if (IsParkourTraversal())
            {
                if (shouldSlide)
                {
                    duration *= parkourSlideVaultDurationMultiplier;
                }

                if (!automaticTraversal)
                {
                    duration *= parkourManualTraversalDurationMultiplier;
                }

                return duration;
            }

            float profileMultiplier = shouldClimb ? standardClimbDurationMultiplier : standardVaultDurationMultiplier;
            float loadMultiplier = Mathf.Lerp(1f, loadTraversalDurationMultiplier, carriedLoad01);
            return duration * profileMultiplier * loadMultiplier;
        }

        private bool TryFindRunnableWall(Vector3 desiredDirection, out RaycastHit wallHit)
        {
            wallHit = default;
            Vector3 direction = desiredDirection.sqrMagnitude > 0.0001f ? desiredDirection.normalized : ResolveMobilityDirection();
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            Vector3 sideProbe = Vector3.Cross(Vector3.up, direction).normalized;
            if (sideProbe.sqrMagnitude > 0.0001f)
            {
                if (TryFindRunnableWallProbe(sideProbe, direction, true, out wallHit)
                    || TryFindRunnableWallProbe(-sideProbe, direction, true, out wallHit))
                {
                    return true;
                }
            }

            return TryFindRunnableWallProbe(direction, direction, false, out wallHit);
        }

        private bool TryFindRunnableWallProbe(Vector3 probeDirection, Vector3 desiredDirection, bool requireAlongWall, out RaycastHit wallHit)
        {
            if (!TryFindWall(probeDirection, out wallHit))
            {
                return false;
            }

            return !requireAlongWall || IsRunnableWallForDirection(desiredDirection, wallHit.normal);
        }

        private bool IsRunnableWallForDirection(Vector3 desiredDirection, Vector3 hitNormal)
        {
            Vector3 normal = Vector3.ProjectOnPlane(hitNormal, Vector3.up);
            Vector3 direction = Vector3.ProjectOnPlane(desiredDirection, Vector3.up);
            if (normal.sqrMagnitude <= 0.0001f || direction.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            Vector3 alongWall = Vector3.ProjectOnPlane(direction.normalized, normal.normalized);
            if (alongWall.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            return Vector3.Dot(alongWall.normalized, direction.normalized) >= wallRunMinAlongWallDot;
        }

        private bool TryFindWall(Vector3 desiredDirection, out RaycastHit wallHit)
        {
            wallHit = default;
            Vector3 direction = desiredDirection.sqrMagnitude > 0.0001f ? desiredDirection.normalized : ResolveMobilityDirection();
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            Vector3 origin = GetTraversalProbeOrigin();
            float radius = Mathf.Max(0.05f, GetControllerWorldRadius() * 0.55f);
            float distance = GetControllerWorldRadius() + ScaleMetric(wallProbeDistance);
            RaycastHit[] hits = Physics.SphereCastAll(origin, radius, direction, distance, traversalMask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (IsSelfHit(hit.transform) || Mathf.Abs(hit.normal.y) > maxWallNormalY)
                {
                    continue;
                }

                wallHit = hit;
                return true;
            }

            return false;
        }

        private bool TryFindVault(Vector3 desiredDirection, bool automaticTraversal, out TraversalCandidate traversal)
        {
            traversal = default(TraversalCandidate);
            if (desiredDirection.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            Vector3 direction = desiredDirection.normalized;
            float radius = Mathf.Max(0.05f, GetControllerWorldRadius() * 0.45f);
            float distance = GetControllerWorldRadius() + ScaleMetric(vaultProbeDistance);
            float footY = GetControllerFootY();
            float worldHeight = GetControllerWorldHeight();
            float effectiveVaultMinHeight = ScaleMetric(vaultMinHeight);
            float effectiveVaultMaxHeight = ResolveEffectiveVaultMaxHeight();
            float effectiveClimbMinHeight = ScaleMetric(climbMinHeight);
            float effectiveClimbMaxHeight = ResolveEffectiveClimbMaxHeight();
            if (IsParkourTraversal() && !automaticTraversal)
            {
                effectiveVaultMaxHeight = Mathf.Min(effectiveVaultMaxHeight, worldHeight * parkourManualVaultMaxHeightMultiplier);
            }

            float autoTraversalMaxHeight = worldHeight * parkourAutoTraversalHeadHeightMultiplier;
            float autoSlideMinHeight = worldHeight * parkourAutoSlideMinHeightMultiplier;
            float maxTraversalHeight = Mathf.Max(effectiveVaultMaxHeight, effectiveClimbMaxHeight);
            if (IsParkourTraversal() && automaticTraversal)
            {
                maxTraversalHeight = Mathf.Max(maxTraversalHeight, autoTraversalMaxHeight);
            }

            float[] probeHeights =
            {
                Mathf.Min(maxTraversalHeight, effectiveVaultMinHeight + radius),
                Mathf.Min(maxTraversalHeight, worldHeight * 0.52f),
                Mathf.Min(maxTraversalHeight, worldHeight * 0.82f)
            };

            for (int p = 0; p < probeHeights.Length; p++)
            {
                Vector3 origin = transform.position + Vector3.up * Mathf.Max(radius, probeHeights[p]);
                RaycastHit[] hits = Physics.SphereCastAll(origin, radius, direction, distance, traversalMask, QueryTriggerInteraction.Ignore);
                if (hits == null || hits.Length == 0)
                {
                    continue;
                }

                Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
                for (int i = 0; i < hits.Length; i++)
                {
                    RaycastHit hit = hits[i];
                    if (IsSelfHit(hit.transform) || hit.collider == null || Mathf.Abs(hit.normal.y) > maxWallNormalY)
                    {
                        continue;
                    }

                    Bounds bounds = hit.collider.bounds;
                    float obstacleHeight = bounds.max.y - footY;
                    if (obstacleHeight < effectiveVaultMinHeight)
                    {
                        continue;
                    }

                    bool shouldSlide = false;
                    bool canVault = obstacleHeight <= effectiveVaultMaxHeight;
                    bool canClimb = obstacleHeight >= effectiveClimbMinHeight && obstacleHeight <= effectiveClimbMaxHeight;
                    if (IsParkourTraversal() && automaticTraversal)
                    {
                        if (obstacleHeight > autoTraversalMaxHeight)
                        {
                            continue;
                        }

                        shouldSlide = obstacleHeight >= autoSlideMinHeight;
                        canVault = true;
                        canClimb = false;
                    }

                    if (!canVault && !canClimb)
                    {
                        continue;
                    }

                    bool shouldClimb = !canVault || obstacleHeight >= effectiveClimbMinHeight;
                    if (shouldSlide)
                    {
                        shouldClimb = false;
                    }

                    Vector3 contactPosition = ResolveTraversalContact(bounds, hit.point, direction, shouldClimb);
                    Vector3 landingPosition = ResolveTraversalLanding(bounds, hit.point, direction, shouldClimb);
                    traversal = new TraversalCandidate(landingPosition, contactPosition, direction, shouldClimb, shouldSlide);
                    return true;
                }
            }

            return false;
        }

        private float ResolveEffectiveVaultMaxHeight()
        {
            if (IsParkourTraversal())
            {
                return ScaleMetric(vaultMaxHeight);
            }

            float loadedVaultMax = Mathf.Max(vaultMinHeight, vaultMaxHeight * 0.82f);
            return ScaleMetric(Mathf.Lerp(vaultMaxHeight, loadedVaultMax, carriedLoad01));
        }

        private float ResolveEffectiveClimbMaxHeight()
        {
            if (IsParkourTraversal())
            {
                return ScaleMetric(climbMaxHeight);
            }

            float unloadedMax = Mathf.Min(climbMaxHeight, standardClimbMaxHeight);
            float loadedMax = Mathf.Min(unloadedMax, heavyClimbMaxHeight);
            return ScaleMetric(Mathf.Lerp(unloadedMax, loadedMax, carriedLoad01));
        }

        private bool IsParkourTraversal()
        {
            return traversalProfile == SCFTraversalProfile.Parkour;
        }

        private Vector3 ResolveTraversalContact(Bounds obstacleBounds, Vector3 hitPoint, Vector3 direction, bool shouldClimb)
        {
            float controllerRadius = GetControllerWorldRadius();
            Vector3 contact = shouldClimb ? hitPoint + direction * controllerRadius : obstacleBounds.center;
            if (!shouldClimb)
            {
                float topExtent = ProjectBoundsExtent(obstacleBounds, direction);
                contact += direction * Mathf.Max(0f, topExtent - controllerRadius * 0.35f);
            }

            contact.y = obstacleBounds.max.y + 0.04f;
            return contact;
        }

        private Vector3 ResolveTraversalLanding(Bounds obstacleBounds, Vector3 hitPoint, Vector3 direction, bool shouldClimb)
        {
            float controllerRadius = GetControllerWorldRadius();
            if (shouldClimb)
            {
                Vector3 climbTarget = hitPoint + direction * (controllerRadius + ScaleMetric(climbForwardClearance));
                climbTarget.y = obstacleBounds.max.y + 0.04f;
                return climbTarget;
            }

            float extent = ProjectBoundsExtent(obstacleBounds, direction);
            Vector3 vaultTarget = obstacleBounds.center + direction * (extent + controllerRadius + ScaleMetric(vaultForwardClearance));
            vaultTarget.y = SampleTraversalGroundY(vaultTarget, obstacleBounds.max.y);
            return vaultTarget;
        }

        private static float ProjectBoundsExtent(Bounds bounds, Vector3 direction)
        {
            Vector3 normalized = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
            return Mathf.Abs(Vector3.Dot(normalized, Vector3.right)) * bounds.extents.x
                   + Mathf.Abs(Vector3.Dot(normalized, Vector3.up)) * bounds.extents.y
                   + Mathf.Abs(Vector3.Dot(normalized, Vector3.forward)) * bounds.extents.z;
        }

        private float SampleTraversalGroundY(Vector3 position, float fallbackY)
        {
            Vector3 origin = position + Vector3.up * (ScaleMetric(vaultMaxHeight) + GetControllerWorldHeight());
            float distance = ScaleMetric(vaultMaxHeight) + GetControllerWorldHeight() + 4f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, traversalMask, QueryTriggerInteraction.Ignore)
                && !IsSelfHit(hit.transform))
            {
                return hit.point.y + 0.04f;
            }

            return Mathf.Max(transform.position.y, fallbackY) + 0.04f;
        }

        private float SampleWallRunGroundY(float fallbackY)
        {
            Vector3 origin = transform.position + Vector3.up * GetControllerWorldHeight();
            float distance = GetControllerWorldHeight() + ScaleMetric(14f);
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, traversalMask, QueryTriggerInteraction.Ignore)
                && !IsSelfHit(hit.transform))
            {
                return hit.point.y + 0.04f;
            }

            return fallbackY;
        }

        private float SampleWallClimbTopY(Vector3 landingPosition, float fallbackTopY)
        {
            Vector3 origin = landingPosition + Vector3.up * (GetControllerWorldHeight() + ScaleMetric(wallClimbTopProbeHeight));
            float distance = GetControllerWorldHeight() + ScaleMetric(wallClimbTopProbeHeight + 1f);
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, traversalMask, QueryTriggerInteraction.Ignore)
                && !IsSelfHit(hit.transform)
                && hit.normal.y > 0.35f)
            {
                return hit.point.y + 0.04f;
            }

            return fallbackTopY + 0.04f;
        }

        private bool IsSelfHit(Transform hitTransform)
        {
            return hitTransform != null && (hitTransform == transform || hitTransform.IsChildOf(transform));
        }

        private void ApplyMovement(float deltaTime)
        {
            if (IsObstacleTraversing)
            {
                ApplyTraversalMovement(deltaTime);
                return;
            }

            bool isAirborne = MobilityState == CharacterMobilityState.Jumping
                              || MobilityState == CharacterMobilityState.Falling
                              || (MobilityState == CharacterMobilityState.JumpCharge && jumpChargeStartedAirborne)
                              || (MobilityState == CharacterMobilityState.MobilityTapWindow && mobilityTapStartedAirborne)
                              || (MobilityState == CharacterMobilityState.CombatRoll && combatRollStartedAirborne);
            bool isWallRunning = MobilityState == CharacterMobilityState.WallRun;
            if (!isAirborne && !isWallRunning && characterController != null && characterController.enabled && IsControllerSupported(0.03f) && verticalVelocity < 0f)
            {
                verticalVelocity = ScaleMetric(groundedStickForce);
            }
            else if (isWallRunning)
            {
                verticalVelocity = wallRunIsVertical ? ScaleMetric(wallClimbUpSpeed) : ScaleMetric(wallRunGravity);
            }
            else
            {
                verticalVelocity += ScaleMetric(gravity) * deltaTime;
            }

            Vector3 motion = PlanarVelocity;
            motion.y = verticalVelocity;
            if (isWallRunning)
            {
                motion += -wallNormal * ScaleMetric(wallRunStickForce);
            }

            if (characterController != null && characterController.enabled)
            {
                CollisionFlags flags = characterController.Move(motion * deltaTime);
                if (isWallRunning)
                {
                    flags |= ApplyWallRunLatch(deltaTime);
                }

                bool groundedAfterMove = (flags & CollisionFlags.Below) != 0;
                if (isAirborne)
                {
                    jumpHasLeftGround |= !groundedAfterMove;
                    if (jumpHasLeftGround && groundedAfterMove && verticalVelocity <= 0f)
                    {
                        CompleteJumpLanding();
                    }
                }

                if ((flags & CollisionFlags.Below) != 0 && verticalVelocity < 0f)
                {
                    verticalVelocity = ScaleMetric(groundedStickForce);
                }

                return;
            }

            transform.position += motion * deltaTime;
        }

        private bool IsControllerSupported(float extraDistance)
        {
            if (characterController == null || !characterController.enabled)
            {
                return false;
            }

            if (characterController.isGrounded)
            {
                return true;
            }

            Bounds bounds = characterController.bounds;
            float radius = Mathf.Max(0.05f, GetControllerWorldRadius() * 0.85f);
            float distance = Mathf.Max(0.01f, bounds.extents.y + ScaleMetric(extraDistance));
            RaycastHit[] hits = Physics.SphereCastAll(bounds.center, radius, Vector3.down, distance, traversalMask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (IsSelfHit(hit.transform) || hit.normal.y < 0.35f)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private CollisionFlags ApplyWallRunLatch(float deltaTime)
        {
            if (characterController == null
                || !characterController.enabled
                || wallNormal.sqrMagnitude <= 0.0001f)
            {
                return CollisionFlags.None;
            }

            Vector3 currentCenter = characterController.bounds.center;
            Vector3 desiredCenter = wallRunSurfacePoint + wallNormal * (GetControllerWorldRadius() + ScaleMetric(wallRunContactOffset));
            desiredCenter.y = wallRunIsVertical ? currentCenter.y : wallRunTargetCenterY;

            Vector3 delta = desiredCenter - currentCenter;
            Vector3 horizontalCorrection = Vector3.ProjectOnPlane(delta, Vector3.up);
            Vector3 verticalCorrection = Vector3.up * delta.y;
            if (horizontalCorrection.sqrMagnitude <= 0.000001f && Mathf.Abs(verticalCorrection.y) <= 0.000001f)
            {
                return CollisionFlags.None;
            }

            float horizontalBlend = 1f - Mathf.Exp(-wallRunLatchSharpness * deltaTime);
            float verticalBlend = 1f - Mathf.Exp(-wallRunHeightLatchSharpness * deltaTime);
            horizontalCorrection = Vector3.ClampMagnitude(horizontalCorrection, ScaleMetric(wallProbeDistance + wallRunContactOffset)) * horizontalBlend;
            verticalCorrection = Vector3.up * Mathf.Clamp(verticalCorrection.y * verticalBlend, -GetControllerWorldHeight(), GetControllerWorldHeight());
            return characterController.Move(horizontalCorrection + verticalCorrection);
        }

        private void ApplyTraversalMovement(float deltaTime)
        {
            mobilityStateTimer += deltaTime;
            float duration = Mathf.Max(0.001f, activeTraversalDuration > 0.001f ? activeTraversalDuration : (IsClimbing ? climbDuration : vaultDuration));
            float t = Mathf.Clamp01(mobilityStateTimer / duration);
            float arcHeight = activeTraversalArcHeight > 0.001f ? activeTraversalArcHeight : (IsClimbing ? climbArcHeight : vaultArcHeight);
            float arc = Mathf.Sin(t * Mathf.PI) * ScaleMetric(arcHeight) * (hasVaultContactPosition ? 0.35f : 1f);
            Vector3 desiredPosition = ResolveTraversalPathPosition(t);
            desiredPosition.y += arc;
            Vector3 delta = desiredPosition - transform.position;

            if (characterController != null && characterController.enabled)
            {
                characterController.Move(delta);
            }
            else
            {
                transform.position = desiredPosition;
            }

            if (t >= 1f)
            {
                if (characterController != null && characterController.enabled)
                {
                    characterController.Move(vaultEndPosition - transform.position);
                }
                else
                {
                    transform.position = vaultEndPosition;
                }

                verticalVelocity = ScaleMetric(groundedStickForce);
                if (activeTraversalUsesSlide)
                {
                    Vector3 rollDirection = lockedMobilityDirection.sqrMagnitude > 0.0001f ? lockedMobilityDirection.normalized : ResolveMobilityDirection();
                    PlanarVelocity = rollDirection * Mathf.Max(ScaleMetric(combatRollSpeed), PlanarVelocity.magnitude);
                    activeTraversalUsesSlide = false;
                    BeginCombatRoll(parkourSlideVaultRollSpeedBonus);
                    return;
                }

                ReturnToFeet();
            }
        }

        private Vector3 ResolveTraversalPathPosition(float normalizedTime)
        {
            float t = Mathf.Clamp01(normalizedTime);
            if (!hasVaultContactPosition)
            {
                return Vector3.Lerp(vaultStartPosition, vaultEndPosition, SmoothStep01(t));
            }

            float contactTime = IsClimbing ? 0.58f : 0.46f;
            if (t <= contactTime)
            {
                float localTime = contactTime > 0.001f ? t / contactTime : 1f;
                return Vector3.Lerp(vaultStartPosition, vaultContactPosition, SmoothStep01(localTime));
            }

            float exitDuration = Mathf.Max(0.001f, 1f - contactTime);
            float exitTime = (t - contactTime) / exitDuration;
            return Vector3.Lerp(vaultContactPosition, vaultEndPosition, SmoothStep01(exitTime));
        }

        private float SmoothStep01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private Vector3 GetTraversalProbeOrigin()
        {
            if (characterController != null && characterController.enabled)
            {
                Bounds bounds = characterController.bounds;
                return bounds.center + Vector3.up * (bounds.extents.y * 0.08f);
            }

            return transform.position + Vector3.up * ScaleMetric(1f);
        }

        private float GetControllerWorldRadius()
        {
            if (characterController == null)
            {
                return ScaleMetric(0.35f);
            }

            Bounds bounds = characterController.bounds;
            return Mathf.Max(0.05f, Mathf.Max(bounds.extents.x, bounds.extents.z));
        }

        private float GetControllerWorldHeight()
        {
            if (characterController == null)
            {
                return ScaleMetric(1.85f);
            }

            return Mathf.Max(0.1f, characterController.bounds.size.y);
        }

        private float GetControllerFootY()
        {
            if (characterController == null)
            {
                return transform.position.y;
            }

            return characterController.bounds.min.y;
        }

        private float GetCurrentControllerCenterY()
        {
            if (characterController == null || !characterController.enabled)
            {
                return transform.position.y;
            }

            return characterController.bounds.center.y;
        }

        private float ScaleMetric(float value)
        {
            if (!scaleMovementWithTransform)
            {
                return value;
            }

            Vector3 scale = transform.lossyScale;
            float metric = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            return value * Mathf.Max(0.01f, metric);
        }

        private Vector3 CameraRelativeDirection(Vector2 inputVector)
        {
            if (inputVector.sqrMagnitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            Camera camera = worldCamera != null ? worldCamera : Camera.main;
            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;

            if (camera != null)
            {
                forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);
                right = Vector3.ProjectOnPlane(camera.transform.right, Vector3.up);

                if (forward.sqrMagnitude <= 0.0001f)
                {
                    forward = Vector3.forward;
                }

                if (right.sqrMagnitude <= 0.0001f)
                {
                    right = Vector3.right;
                }

                forward.Normalize();
                right.Normalize();
            }

            Vector3 direction = right * inputVector.x + forward * inputVector.y;
            return direction.sqrMagnitude > 1f ? direction.normalized : direction;
        }

        private void UpdateAimDirection()
        {
            if (input == null)
            {
                return;
            }

            Vector3 resolvedDirection;
            if (input.AimMode == AimInputMode.WorldDirection)
            {
                resolvedDirection = CameraRelativeDirection(input.AimValue);
                if (resolvedDirection.sqrMagnitude > 0.0001f)
                {
                    AimDirection = resolvedDirection.normalized;
                    HasAimDirection = true;
                }

                return;
            }

            if (input.AimMode == AimInputMode.ScreenPoint && TryGetScreenAimDirection(input.AimValue, out resolvedDirection))
            {
                AimDirection = resolvedDirection;
                HasAimDirection = true;
                return;
            }

            HasAimDirection = AimDirection.sqrMagnitude > 0.0001f;
        }

        private bool TryGetScreenAimDirection(Vector2 screenPosition, out Vector3 direction)
        {
            direction = Vector3.zero;

            Camera camera = worldCamera != null ? worldCamera : Camera.main;
            if (camera == null)
            {
                return false;
            }

            Ray ray = camera.ScreenPointToRay(screenPosition);
            if (TryRaycastAimSurface(ray, out Vector3 point) || TryIntersectAimPlane(ray, out point))
            {
                Vector3 toPoint = point - transform.position;
                toPoint.y = 0f;

                if (toPoint.sqrMagnitude > 0.0001f)
                {
                    direction = toPoint.normalized;
                    return true;
                }
            }

            return false;
        }

        private bool TryRaycastAimSurface(Ray ray, out Vector3 point)
        {
            point = Vector3.zero;
            RaycastHit[] hits = Physics.RaycastAll(ray, aimRayDistance, aimSurfaceMask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
            {
                return false;
            }

            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                Transform hitTransform = hits[i].transform;
                if (hitTransform != null && (hitTransform == transform || hitTransform.IsChildOf(transform)))
                {
                    continue;
                }

                point = hits[i].point;
                return true;
            }

            return false;
        }

        private bool TryIntersectAimPlane(Ray ray, out Vector3 point)
        {
            Plane plane = new Plane(Vector3.up, new Vector3(0f, fallbackAimPlaneY, 0f));
            if (plane.Raycast(ray, out float enter))
            {
                point = ray.GetPoint(enter);
                return true;
            }

            point = Vector3.zero;
            return false;
        }

        private void UpdateFacing(float deltaTime)
        {
            Transform root = facingRoot != null ? facingRoot : transform;
            if (!TryResolveBodyFacingDirection(out Vector3 facingDirection, out float sharpness))
            {
                return;
            }

            BodyFacingDirection = facingDirection.normalized;

            Quaternion targetRotation = MobilityState == CharacterMobilityState.WallRun
                ? ResolveWallRunVisualRotation(facingDirection)
                : Quaternion.LookRotation(facingDirection, Vector3.up);
            if (MobilityState == CharacterMobilityState.WallRun)
            {
                sharpness = wallRunVisualRotationSharpness;
            }

            float blend = 1f - Mathf.Exp(-sharpness * deltaTime);
            root.rotation = Quaternion.Slerp(root.rotation, targetRotation, blend);
        }

        private bool TryResolveBodyFacingDirection(out Vector3 facingDirection, out float sharpness)
        {
            facingDirection = Vector3.zero;
            sharpness = movementRotationSharpness;

            if ((MobilityState == CharacterMobilityState.CombatRoll
                    || MobilityState == CharacterMobilityState.Jumping
                    || MobilityState == CharacterMobilityState.WallRun
                    || IsObstacleTraversing)
                && lockedMobilityDirection.sqrMagnitude > 0.0001f)
            {
                facingDirection = lockedMobilityDirection;
                sharpness = MobilityState == CharacterMobilityState.WallRun ? wallRunVisualRotationSharpness : movementRotationSharpness;
                return true;
            }

            if (AimHeld && facingMode != CharacterFacingMode.MovementOnly && HasAimDirection && AimDirection.sqrMagnitude > 0.0001f)
            {
                facingDirection = AimDirection;
                sharpness = aimRotationSharpness;
                return true;
            }

            Vector3 planarMove = Vector3.ProjectOnPlane(PlanarVelocity, Vector3.up);
            bool hasMovementFacing = facingMode != CharacterFacingMode.AimOnly
                                     && planarMove.sqrMagnitude > lowerBodyMoveThreshold * lowerBodyMoveThreshold;

            if (separateAimFromLocomotion)
            {
                if (hasMovementFacing)
                {
                    facingDirection = planarMove.normalized;
                    sharpness = movementRotationSharpness;
                    return true;
                }

                if (lowerBodyAimsWhenIdle && facingMode != CharacterFacingMode.MovementOnly && HasAimDirection)
                {
                    facingDirection = AimDirection;
                    sharpness = aimRotationSharpness;
                    return true;
                }
            }
            else if (facingMode != CharacterFacingMode.MovementOnly && HasAimDirection)
            {
                facingDirection = AimDirection;
                sharpness = aimRotationSharpness;
                return true;
            }

            if (hasMovementFacing)
            {
                facingDirection = planarMove.normalized;
                sharpness = movementRotationSharpness;
                return true;
            }

            if (facingMode == CharacterFacingMode.AimOnly && HasAimDirection)
            {
                facingDirection = AimDirection;
                sharpness = aimRotationSharpness;
                return true;
            }

            return false;
        }

        private Quaternion ResolveWallRunVisualRotation(Vector3 fallbackDirection)
        {
            if (wallRunIsVertical && wallNormal.sqrMagnitude > 0.0001f)
            {
                return Quaternion.LookRotation(-wallNormal.normalized, Vector3.up);
            }

            Vector3 forward = wallRunDirection.sqrMagnitude > 0.0001f ? wallRunDirection.normalized : fallbackDirection.normalized;
            float oscillation = Mathf.Sin(mobilityStateTimer * Mathf.PI * 2f * wallRunVisualLeanFrequency) * wallRunVisualLeanVariance;
            float lean = (wallRunVisualLeanDegrees + oscillation) * WallRunSide;
            Vector3 tiltedUp = Quaternion.AngleAxis(lean, forward) * Vector3.up;
            return Quaternion.LookRotation(forward, tiltedUp);
        }

        private void CacheStandingControllerPose()
        {
            if (characterController == null)
            {
                return;
            }

            standingControllerHeight = characterController.height;
            standingControllerCenter = characterController.center;
            hasControllerPoseDefaults = true;
        }

        private void UpdateControllerPose(float deltaTime)
        {
            if (characterController == null || !characterController.enabled)
            {
                return;
            }

            if (!hasControllerPoseDefaults)
            {
                CacheStandingControllerPose();
            }

            bool lowProfile = MobilityState == CharacterMobilityState.Prone
                || MobilityState == CharacterMobilityState.CombatRoll;
            float targetHeight = lowProfile ? proneControllerHeight : standingControllerHeight;
            Vector3 targetCenter = lowProfile ? proneControllerCenter : standingControllerCenter;
            float step = Mathf.Clamp01(controllerPoseBlendSpeed * deltaTime);

            characterController.height = Mathf.Lerp(characterController.height, targetHeight, step);
            characterController.center = Vector3.Lerp(characterController.center, targetCenter, step);
        }
    }
}
