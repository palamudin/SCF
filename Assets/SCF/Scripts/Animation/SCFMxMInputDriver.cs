using MxM;
using UnityEngine;

namespace SCF.Gameplay
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-40)]
    public sealed class SCFMxMInputDriver : MonoBehaviour
    {
        [SerializeField] private IsometricPlayerInput playerInput;
        [SerializeField] private IsometricCharacterMotor motor;
        [SerializeField] private MxMTrajectoryGenerator trajectoryGenerator;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private bool syncMaxSpeed = true;
        [SerializeField] private bool useStrafeFacing = true;
        [SerializeField] private float idleFacingDeadZone = 0.001f;

        private void Reset()
        {
            playerInput = GetComponent<IsometricPlayerInput>();
            motor = GetComponent<IsometricCharacterMotor>();
            trajectoryGenerator = GetComponent<MxMTrajectoryGenerator>();
            worldCamera = Camera.main;
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            ResolveReferences();
            if (trajectoryGenerator == null || playerInput == null)
            {
                return;
            }

            Vector2 moveInput = ResolveTrajectoryInput();
            trajectoryGenerator.InputVector = new Vector3(moveInput.x, 0f, moveInput.y);

            Camera camera = worldCamera != null ? worldCamera : Camera.main;
            trajectoryGenerator.RelativeCameraTransform = camera != null ? camera.transform : null;

            if (syncMaxSpeed && motor != null)
            {
                trajectoryGenerator.MaxSpeed = motor.CurrentMaxSpeed;
            }

            trajectoryGenerator.Strafing = useStrafeFacing;
            if (useStrafeFacing)
            {
                trajectoryGenerator.StrafeDirection = ResolveFacingDirection();
            }
        }

        public void Configure(IsometricPlayerInput input, IsometricCharacterMotor characterMotor, Camera camera)
        {
            playerInput = input;
            motor = characterMotor;
            worldCamera = camera;
            trajectoryGenerator = GetComponent<MxMTrajectoryGenerator>();
        }

        private void ResolveReferences()
        {
            if (playerInput == null)
            {
                playerInput = GetComponent<IsometricPlayerInput>();
            }

            if (motor == null)
            {
                motor = GetComponent<IsometricCharacterMotor>();
            }

            if (trajectoryGenerator == null)
            {
                trajectoryGenerator = GetComponent<MxMTrajectoryGenerator>();
            }
        }

        private Vector3 ResolveFacingDirection()
        {
            if (motor != null && motor.HasAimDirection)
            {
                return motor.AimDirection;
            }

            if (motor != null && motor.PlanarVelocity.sqrMagnitude > idleFacingDeadZone)
            {
                return motor.PlanarVelocity.normalized;
            }

            Vector2 moveInput = playerInput != null ? playerInput.Move : Vector2.zero;
            if (moveInput.sqrMagnitude > idleFacingDeadZone)
            {
                return new Vector3(moveInput.x, 0f, moveInput.y).normalized;
            }

            return transform.forward;
        }

        private Vector2 ResolveTrajectoryInput()
        {
            if (motor != null && motor.IsAirborne)
            {
                return Vector2.zero;
            }

            if (motor != null
                && motor.IsCombatRolling
                && motor.DesiredVelocity.sqrMagnitude > 0.001f)
            {
                return WorldDirectionToCameraInput(motor.DesiredVelocity.normalized);
            }

            return Vector2.ClampMagnitude(playerInput.Move, 1f);
        }

        private Vector2 WorldDirectionToCameraInput(Vector3 worldDirection)
        {
            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude <= 0.0001f)
            {
                return Vector2.zero;
            }

            worldDirection.Normalize();
            Camera camera = worldCamera != null ? worldCamera : Camera.main;
            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;

            if (camera != null)
            {
                forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);
                right = Vector3.ProjectOnPlane(camera.transform.right, Vector3.up);

                if (forward.sqrMagnitude > 0.0001f)
                {
                    forward.Normalize();
                }
                else
                {
                    forward = Vector3.forward;
                }

                if (right.sqrMagnitude > 0.0001f)
                {
                    right.Normalize();
                }
                else
                {
                    right = Vector3.right;
                }
            }

            return Vector2.ClampMagnitude(new Vector2(
                Vector3.Dot(worldDirection, right),
                Vector3.Dot(worldDirection, forward)), 1f);
        }
    }
}
