using UnityEngine;

namespace SCF.Gameplay
{
        [DisallowMultipleComponent]
        [RequireComponent(typeof(Camera))]
        public sealed class IsometricCameraFollow : MonoBehaviour
        {
            [SerializeField] private Transform target;
            [SerializeField] private bool useLookAtComposition = true;
            [SerializeField] private Vector3 offset = new Vector3(-9.75f, 8.45f, -9.75f);
            [SerializeField] private Vector3 targetCenterOffset = new Vector3(0f, 1.05f, 0f);
            [SerializeField, Min(1f)] private float cameraDistance = 17.4f;
            [SerializeField] private Vector3 rotationEuler = new Vector3(61f, 45f, 0f);
            [SerializeField, Min(0.1f)] private float followSharpness = 12f;
            [SerializeField, Min(0.1f)] private float rotationSharpness = 18f;
            [SerializeField] private bool useOrthographic;
            [SerializeField, Min(1f)] private float orthographicSize = 7.25f;
            [SerializeField, Range(1f, 179f)] private float fieldOfView = 42f;
            [SerializeField, Min(0.01f)] private float nearClipPlane = 0.3f;
            [SerializeField, Min(1f)] private float farClipPlane = 1000f;

        private Camera cachedCamera;

        public Vector3 Offset
        {
            get => offset;
            set => offset = value;
        }

        public Vector3 RotationEuler
        {
            get => rotationEuler;
            set => rotationEuler = value;
        }

        public float FollowSharpness
        {
            get => followSharpness;
            set => followSharpness = Mathf.Max(0.1f, value);
        }

        public float RotationSharpness
        {
            get => rotationSharpness;
            set => rotationSharpness = Mathf.Max(0.1f, value);
        }

        public bool UseOrthographic
        {
            get => useOrthographic;
            set
            {
                useOrthographic = value;
                ApplyCameraProjection();
            }
        }

        public float OrthographicSize
        {
            get => orthographicSize;
            set
            {
                orthographicSize = Mathf.Max(1f, value);
                ApplyCameraProjection();
            }
        }

        public float FieldOfView
        {
            get => fieldOfView;
            set
            {
                fieldOfView = Mathf.Clamp(value, 1f, 179f);
                ApplyCameraProjection();
            }
        }

        public float CameraDistance
        {
            get => cameraDistance;
            set => cameraDistance = Mathf.Max(1f, value);
        }

        public Vector3 TargetCenterOffset
        {
            get => targetCenterOffset;
            set => targetCenterOffset = value;
        }

        private void Reset()
        {
            cachedCamera = GetComponent<Camera>();
            ApplyCameraProjection();
        }

        private void Awake()
        {
            cachedCamera = GetComponent<Camera>();
            ApplyCameraProjection();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            float positionBlend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            float rotationBlend = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
            Quaternion desiredRotation = Quaternion.Euler(rotationEuler);
            Vector3 desiredPosition = ResolveDesiredPosition(desiredRotation);

            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionBlend);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationBlend);
            ApplyCameraProjection();
        }

        public void Configure(Transform followTarget)
        {
            target = followTarget;
            cachedCamera = GetComponent<Camera>();
            SnapToTarget();
            ApplyCameraProjection();
        }

        public void ApplyTacticalShooterPreset()
        {
            useLookAtComposition = true;
            offset = new Vector3(-9.75f, 8.45f, -9.75f);
            targetCenterOffset = new Vector3(0f, 1.05f, 0f);
            cameraDistance = 17.4f;
            rotationEuler = new Vector3(61f, 45f, 0f);
            followSharpness = 12f;
            rotationSharpness = 18f;
            useOrthographic = false;
            orthographicSize = 7.25f;
            fieldOfView = 42f;
            nearClipPlane = 0.3f;
            farClipPlane = 1000f;
            ApplyCameraProjection();
        }

        public void SnapToTarget()
        {
            Quaternion desiredRotation = Quaternion.Euler(rotationEuler);
            if (target != null)
            {
                transform.position = ResolveDesiredPosition(desiredRotation);
            }

            transform.rotation = desiredRotation;
            ApplyCameraProjection();
        }

        private Vector3 ResolveDesiredPosition(Quaternion desiredRotation)
        {
            if (target == null)
            {
                return transform.position;
            }

            if (!useLookAtComposition)
            {
                return target.position + offset;
            }

            Vector3 focusPoint = target.position + targetCenterOffset;
            Vector3 cameraForward = desiredRotation * Vector3.forward;
            return focusPoint - cameraForward * cameraDistance;
        }

        private void ApplyCameraProjection()
        {
            if (cachedCamera == null)
            {
                return;
            }

            cachedCamera.orthographic = useOrthographic;
            cachedCamera.fieldOfView = fieldOfView;
            cachedCamera.nearClipPlane = nearClipPlane;
            cachedCamera.farClipPlane = Mathf.Max(nearClipPlane + 1f, farClipPlane);
            if (useOrthographic)
            {
                cachedCamera.orthographicSize = orthographicSize;
            }
        }
    }
}
