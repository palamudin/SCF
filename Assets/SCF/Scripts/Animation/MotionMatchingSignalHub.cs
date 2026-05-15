using UnityEngine;
using MxM;

namespace SCF.Gameplay
{
    public enum MxMRootMotionApplication
    {
        Ignore,
        ApplyRotationOnly,
        ApplyTranslationAndRotation
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(IsometricCharacterMotor))]
    public sealed class MotionMatchingSignalHub : MonoBehaviour, IMxMRootMotion
    {
        [SerializeField] private IsometricCharacterMotor motor;
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private MxMRootMotionApplication rootMotionApplication = MxMRootMotionApplication.Ignore;
        [SerializeField] private float rootMotionScale = 1f;

        public Vector3 DesiredVelocity => motor != null ? motor.DesiredVelocity : Vector3.zero;
        public Vector3 PlanarVelocity => motor != null ? motor.PlanarVelocity : Vector3.zero;
        public Vector3 DesiredFacing => ResolveDesiredFacing();
        public bool IsSprinting => motor != null && motor.SprintHeld;

        private void Reset()
        {
            motor = GetComponent<IsometricCharacterMotor>();
            animator = GetComponentInChildren<Animator>();
            characterController = GetComponent<CharacterController>();
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

            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }
        }

        public void Configure(Animator targetAnimator)
        {
            animator = targetAnimator;
        }

        private Vector3 ResolveDesiredFacing()
        {
            if (motor == null)
            {
                return transform.forward;
            }

            if (motor.SeparateAimFromLocomotion && motor.BodyFacingDirection.sqrMagnitude > 0.0001f)
            {
                return motor.BodyFacingDirection;
            }

            return motor.HasAimDirection ? motor.AimDirection : transform.forward;
        }

        public void HandleRootMotion(
            Vector3 rootPosition,
            Quaternion rootRotation,
            Vector3 warp,
            Quaternion warpRotation,
            float deltaTime)
        {
            if (rootMotionApplication == MxMRootMotionApplication.Ignore)
            {
                return;
            }

            Quaternion rotationDelta = rootRotation * warpRotation;
            if (rootMotionApplication == MxMRootMotionApplication.ApplyRotationOnly)
            {
                transform.rotation *= rotationDelta;
                return;
            }

            Vector3 delta = (rootPosition + warp) * rootMotionScale;
            if (characterController != null && characterController.enabled)
            {
                characterController.Move(delta);
            }
            else
            {
                transform.position += delta;
            }

            transform.rotation *= rotationDelta;
        }

        public void HandleAngularErrorWarping(Quaternion warpRotation)
        {
            if (rootMotionApplication != MxMRootMotionApplication.Ignore)
            {
                transform.rotation *= warpRotation;
            }
        }

        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }

        public void SetRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
        }

        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
        }

        public void Translate(Vector3 delta)
        {
            if (rootMotionApplication == MxMRootMotionApplication.Ignore)
            {
                return;
            }

            if (characterController != null && characterController.enabled)
            {
                characterController.Move(delta);
            }
            else
            {
                transform.position += delta;
            }
        }

        public void Rotate(Vector3 axis, float angle)
        {
            if (rootMotionApplication != MxMRootMotionApplication.Ignore)
            {
                transform.Rotate(axis, angle, Space.World);
            }
        }

        public void FinalizeRootMotion()
        {
        }
    }
}
