using UnityEngine;

namespace SCF.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(IsometricCharacterMotor))]
    public sealed class MovementAnimatorBridge : MonoBehaviour
    {
        [SerializeField] private IsometricCharacterMotor motor;
        [SerializeField] private Animator animator;
        [SerializeField] private float damping = 0.1f;

        [Header("Animator Parameters")]
        [SerializeField] private string speedParameter = "Speed";
        [SerializeField] private string moveXParameter = "MoveX";
        [SerializeField] private string moveZParameter = "MoveZ";
        [SerializeField] private string aimXParameter = "AimX";
        [SerializeField] private string aimZParameter = "AimZ";
        [SerializeField] private string motionSpeedParameter = "MotionSpeed";
        [SerializeField] private string groundedParameter = "Grounded";
        [SerializeField] private string freeFallParameter = "FreeFall";
        [SerializeField] private string jumpParameter = "Jump";
        [SerializeField] private string movingParameter = "IsMoving";
        [SerializeField] private string attackingParameter = "Attack";
        [SerializeField] private string combatRollParameter = "CombatRoll";
        [SerializeField] private string combatRollTriggerParameter = "CombatRollTrigger";
        [SerializeField] private string jumpChargingParameter = "JumpCharging";
        [SerializeField] private string jumpingParameter = "Jumping";
        [SerializeField] private string jumpTriggerParameter = "JumpTrigger";
        [SerializeField] private string landTriggerParameter = "LandTrigger";
        [SerializeField] private string proneParameter = "Prone";
        [SerializeField] private string wallRunParameter = "WallRun";
        [SerializeField] private string wallRunTriggerParameter = "WallRunTrigger";
        [SerializeField] private string wallRunSideParameter = "WallRunSide";
        [SerializeField] private string vaultParameter = "Vault";
        [SerializeField] private string vaultTriggerParameter = "VaultTrigger";
        [SerializeField] private string climbParameter = "Climb";
        [SerializeField] private string climbTriggerParameter = "ClimbTrigger";
        [SerializeField] private string jumpChargeParameter = "JumpCharge01";
        [SerializeField] private string mobilityStateParameter = "MobilityState";
        [SerializeField, Min(0.01f)] private float jumpPulseDuration = 0.12f;

        private int speedHash;
        private int moveXHash;
        private int moveZHash;
        private int aimXHash;
        private int aimZHash;
        private int motionSpeedHash;
        private int groundedHash;
        private int freeFallHash;
        private int jumpHash;
        private int movingHash;
        private int attackingHash;
        private int combatRollHash;
        private int combatRollTriggerHash;
        private int jumpChargingHash;
        private int jumpingHash;
        private int jumpTriggerHash;
        private int landTriggerHash;
        private int proneHash;
        private int wallRunHash;
        private int wallRunTriggerHash;
        private int wallRunSideHash;
        private int vaultHash;
        private int vaultTriggerHash;
        private int climbHash;
        private int climbTriggerHash;
        private int jumpChargeHash;
        private int mobilityStateHash;
        private bool hasSpeed;
        private bool hasMoveX;
        private bool hasMoveZ;
        private bool hasAimX;
        private bool hasAimZ;
        private bool hasMotionSpeed;
        private bool hasGrounded;
        private bool hasFreeFall;
        private bool hasJump;
        private bool hasMoving;
        private bool hasAttacking;
        private bool hasCombatRoll;
        private bool hasCombatRollTrigger;
        private bool hasJumpCharging;
        private bool hasJumping;
        private bool hasJumpTrigger;
        private bool hasLandTrigger;
        private bool hasProne;
        private bool hasWallRun;
        private bool hasWallRunTrigger;
        private bool hasWallRunSide;
        private bool hasVault;
        private bool hasVaultTrigger;
        private bool hasClimb;
        private bool hasClimbTrigger;
        private bool hasJumpCharge;
        private bool hasMobilityState;
        private int observedCombatRollSequence;
        private int observedJumpSequence;
        private int observedLandSequence;
        private int observedWallRunSequence;
        private int observedVaultSequence;
        private int observedClimbSequence;
        private float jumpPulseTimer;

        private void Reset()
        {
            motor = GetComponent<IsometricCharacterMotor>();
            animator = GetComponentInChildren<Animator>();
        }

        private void Awake()
        {
            if (motor == null)
            {
                motor = GetComponent<IsometricCharacterMotor>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            CacheHashes();
        }

        private void LateUpdate()
        {
            if (motor == null || animator == null || !animator.isActiveAndEnabled)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            float maxSpeed = Mathf.Max(0.001f, motor.CurrentMaxSpeed);
            Transform animationSpace = animator.transform != null ? animator.transform : transform;
            Vector3 localVelocity = animationSpace.InverseTransformDirection(motor.PlanarVelocity);
            Vector3 localAim = animationSpace.InverseTransformDirection(motor.AimDirection);
            float planarSpeed = motor.PlanarVelocity.magnitude;
            float normalizedSpeed = Mathf.Clamp01(planarSpeed / maxSpeed);
            UpdateMobilityTriggers(deltaTime);
            UpdateAnimatorPlayback(deltaTime);

            if (hasSpeed)
            {
                animator.SetFloat(speedHash, planarSpeed, damping, deltaTime);
            }

            if (hasMoveX)
            {
                animator.SetFloat(moveXHash, Mathf.Clamp(localVelocity.x / maxSpeed, -1f, 1f), damping, deltaTime);
            }

            if (hasMoveZ)
            {
                animator.SetFloat(moveZHash, Mathf.Clamp(localVelocity.z / maxSpeed, -1f, 1f), damping, deltaTime);
            }

            if (hasAimX)
            {
                animator.SetFloat(aimXHash, Mathf.Clamp(localAim.x, -1f, 1f), damping, deltaTime);
            }

            if (hasAimZ)
            {
                animator.SetFloat(aimZHash, Mathf.Clamp(localAim.z, -1f, 1f), damping, deltaTime);
            }

            if (hasMotionSpeed)
            {
                animator.SetFloat(motionSpeedHash, Mathf.Max(normalizedSpeed, motor.MoveInput.magnitude), damping, deltaTime);
            }

            if (hasGrounded)
            {
                animator.SetBool(groundedHash, motor.IsGrounded);
            }

            if (hasFreeFall)
            {
                animator.SetBool(freeFallHash, motor.IsAirborne || !motor.IsGrounded);
            }

            if (hasJump)
            {
                animator.SetBool(jumpHash, jumpPulseTimer > 0f);
            }

            if (hasMoving)
            {
                animator.SetBool(movingHash, motor.PlanarVelocity.sqrMagnitude > 0.01f);
            }

            IsometricPlayerInput input = GetComponent<IsometricPlayerInput>();
            if (hasAttacking && input != null)
            {
                animator.SetBool(attackingHash, input.AttackHeld);
            }

            if (hasCombatRoll)
            {
                animator.SetBool(combatRollHash, motor.IsCombatRolling);
            }

            if (hasJumpCharging)
            {
                animator.SetBool(jumpChargingHash, motor.IsJumpCharging);
            }

            if (hasJumping)
            {
                animator.SetBool(jumpingHash, motor.IsAirborne);
            }

            if (hasProne)
            {
                animator.SetBool(proneHash, motor.IsProne);
            }

            if (hasWallRun)
            {
                animator.SetBool(wallRunHash, motor.IsWallRunning);
            }

            if (hasWallRunSide)
            {
                animator.SetFloat(wallRunSideHash, motor.WallRunSide, damping, deltaTime);
            }

            if (hasVault)
            {
                animator.SetBool(vaultHash, motor.IsVaulting);
            }

            if (hasClimb)
            {
                animator.SetBool(climbHash, motor.IsClimbing);
            }

            if (hasJumpCharge)
            {
                animator.SetFloat(jumpChargeHash, motor.JumpCharge01, damping, deltaTime);
            }

            if (hasMobilityState)
            {
                animator.SetInteger(mobilityStateHash, (int)motor.MobilityState);
            }
        }

        public void Configure(Animator targetAnimator)
        {
            animator = targetAnimator;
            CacheHashes();
        }

        private void OnDisable()
        {
            if (animator != null)
            {
                animator.speed = 1f;
            }
        }

        private void UpdateAnimatorPlayback(float deltaTime)
        {
            animator.speed = Mathf.MoveTowards(animator.speed, 1f, Mathf.Max(0.01f, deltaTime * 6f));
        }

        private void CacheHashes()
        {
            speedHash = Animator.StringToHash(speedParameter);
            moveXHash = Animator.StringToHash(moveXParameter);
            moveZHash = Animator.StringToHash(moveZParameter);
            aimXHash = Animator.StringToHash(aimXParameter);
            aimZHash = Animator.StringToHash(aimZParameter);
            motionSpeedHash = Animator.StringToHash(motionSpeedParameter);
            groundedHash = Animator.StringToHash(groundedParameter);
            freeFallHash = Animator.StringToHash(freeFallParameter);
            jumpHash = Animator.StringToHash(jumpParameter);
            movingHash = Animator.StringToHash(movingParameter);
            attackingHash = Animator.StringToHash(attackingParameter);
            combatRollHash = Animator.StringToHash(combatRollParameter);
            combatRollTriggerHash = Animator.StringToHash(combatRollTriggerParameter);
            jumpChargingHash = Animator.StringToHash(jumpChargingParameter);
            jumpingHash = Animator.StringToHash(jumpingParameter);
            jumpTriggerHash = Animator.StringToHash(jumpTriggerParameter);
            landTriggerHash = Animator.StringToHash(landTriggerParameter);
            proneHash = Animator.StringToHash(proneParameter);
            wallRunHash = Animator.StringToHash(wallRunParameter);
            wallRunTriggerHash = Animator.StringToHash(wallRunTriggerParameter);
            wallRunSideHash = Animator.StringToHash(wallRunSideParameter);
            vaultHash = Animator.StringToHash(vaultParameter);
            vaultTriggerHash = Animator.StringToHash(vaultTriggerParameter);
            climbHash = Animator.StringToHash(climbParameter);
            climbTriggerHash = Animator.StringToHash(climbTriggerParameter);
            jumpChargeHash = Animator.StringToHash(jumpChargeParameter);
            mobilityStateHash = Animator.StringToHash(mobilityStateParameter);

            hasSpeed = HasParameter(speedHash, AnimatorControllerParameterType.Float);
            hasMoveX = HasParameter(moveXHash, AnimatorControllerParameterType.Float);
            hasMoveZ = HasParameter(moveZHash, AnimatorControllerParameterType.Float);
            hasAimX = HasParameter(aimXHash, AnimatorControllerParameterType.Float);
            hasAimZ = HasParameter(aimZHash, AnimatorControllerParameterType.Float);
            hasMotionSpeed = HasParameter(motionSpeedHash, AnimatorControllerParameterType.Float);
            hasGrounded = HasParameter(groundedHash, AnimatorControllerParameterType.Bool);
            hasFreeFall = HasParameter(freeFallHash, AnimatorControllerParameterType.Bool);
            hasJump = HasParameter(jumpHash, AnimatorControllerParameterType.Bool);
            hasMoving = HasParameter(movingHash, AnimatorControllerParameterType.Bool);
            hasAttacking = HasParameter(attackingHash, AnimatorControllerParameterType.Bool);
            hasCombatRoll = HasParameter(combatRollHash, AnimatorControllerParameterType.Bool);
            hasCombatRollTrigger = HasParameter(combatRollTriggerHash, AnimatorControllerParameterType.Trigger);
            hasJumpCharging = HasParameter(jumpChargingHash, AnimatorControllerParameterType.Bool);
            hasJumping = HasParameter(jumpingHash, AnimatorControllerParameterType.Bool);
            hasJumpTrigger = HasParameter(jumpTriggerHash, AnimatorControllerParameterType.Trigger);
            hasLandTrigger = HasParameter(landTriggerHash, AnimatorControllerParameterType.Trigger);
            hasProne = HasParameter(proneHash, AnimatorControllerParameterType.Bool);
            hasWallRun = HasParameter(wallRunHash, AnimatorControllerParameterType.Bool);
            hasWallRunTrigger = HasParameter(wallRunTriggerHash, AnimatorControllerParameterType.Trigger);
            hasWallRunSide = HasParameter(wallRunSideHash, AnimatorControllerParameterType.Float);
            hasVault = HasParameter(vaultHash, AnimatorControllerParameterType.Bool);
            hasVaultTrigger = HasParameter(vaultTriggerHash, AnimatorControllerParameterType.Trigger);
            hasClimb = HasParameter(climbHash, AnimatorControllerParameterType.Bool);
            hasClimbTrigger = HasParameter(climbTriggerHash, AnimatorControllerParameterType.Trigger);
            hasJumpCharge = HasParameter(jumpChargeHash, AnimatorControllerParameterType.Float);
            hasMobilityState = HasParameter(mobilityStateHash, AnimatorControllerParameterType.Int);
        }

        private void UpdateMobilityTriggers(float deltaTime)
        {
            if (motor.CombatRollSequence != observedCombatRollSequence)
            {
                observedCombatRollSequence = motor.CombatRollSequence;
                if (hasCombatRollTrigger)
                {
                    animator.SetTrigger(combatRollTriggerHash);
                }
            }

            if (motor.JumpSequence != observedJumpSequence)
            {
                observedJumpSequence = motor.JumpSequence;
                jumpPulseTimer = jumpPulseDuration;
                if (hasJumpTrigger)
                {
                    animator.SetTrigger(jumpTriggerHash);
                }
            }

            if (motor.LandSequence != observedLandSequence)
            {
                observedLandSequence = motor.LandSequence;
                if (hasLandTrigger)
                {
                    animator.SetTrigger(landTriggerHash);
                }
            }

            if (motor.WallRunSequence != observedWallRunSequence)
            {
                observedWallRunSequence = motor.WallRunSequence;
                if (hasWallRunTrigger)
                {
                    animator.SetTrigger(wallRunTriggerHash);
                }
            }

            if (motor.VaultSequence != observedVaultSequence)
            {
                observedVaultSequence = motor.VaultSequence;
                if (hasVaultTrigger)
                {
                    animator.SetTrigger(vaultTriggerHash);
                }
            }

            if (motor.ClimbSequence != observedClimbSequence)
            {
                observedClimbSequence = motor.ClimbSequence;
                if (hasClimbTrigger)
                {
                    animator.SetTrigger(climbTriggerHash);
                }
            }

            if (jumpPulseTimer > 0f)
            {
                jumpPulseTimer -= deltaTime;
            }
        }

        private bool HasParameter(int hash, AnimatorControllerParameterType type)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return false;
            }

            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].nameHash == hash && parameters[i].type == type)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
