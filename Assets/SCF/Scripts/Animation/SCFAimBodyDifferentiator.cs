using UnityEngine;

namespace SCF.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(IsometricCharacterMotor))]
    [DefaultExecutionOrder(85)]
    public sealed class SCFAimBodyDifferentiator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private IsometricCharacterMotor motor;
        [SerializeField] private Animator animator;

        [Header("Body Split")]
        [SerializeField] private bool enableBodySplit = true;
        [SerializeField] private bool requireHumanoid;
        [SerializeField] private bool useGenericBoneNameFallback = true;
        [SerializeField] private bool aimTorsoDuringWallRun = true;
        [SerializeField, Range(0f, 1f)] private float torsoAimWeight = 0.75f;
        [SerializeField, Range(0f, 90f)] private float maxTorsoYaw = 65f;
        [SerializeField, Min(0.1f)] private float torsoRotationSharpness = 18f;

        [Header("Bone Weights")]
        [SerializeField, Range(0f, 1f)] private float headWeight = 0.42f;
        [SerializeField, Range(0f, 1f)] private float neckWeight = 0.2f;
        [SerializeField, Range(0f, 1f)] private float shoulderWeight = 0.16f;
        [SerializeField] private bool includeHipsInAim;
        [SerializeField, Range(0f, 1f)] private float hipsWeight = 0.08f;
        [SerializeField, Range(0f, 1f)] private float spineWeight = 0.14f;
        [SerializeField, Range(0f, 1f)] private float chestWeight = 0.28f;
        [SerializeField, Range(0f, 1f)] private float upperChestWeight = 0.24f;

        [Header("Debug")]
        [SerializeField] private float currentTorsoYaw;
        [SerializeField] private float targetTorsoYaw;

        private Transform spine;
        private Transform chest;
        private Transform upperChest;
        private Transform hips;
        private Transform neck;
        private Transform head;
        private Transform leftShoulder;
        private Transform rightShoulder;

        private void Reset()
        {
            motor = GetComponent<IsometricCharacterMotor>();
            animator = GetComponentInChildren<Animator>();
        }

        private void Awake()
        {
            ResolveReferences();
            CacheBones();
        }

        private void OnValidate()
        {
            torsoAimWeight = Mathf.Clamp01(torsoAimWeight);
            maxTorsoYaw = Mathf.Max(0f, maxTorsoYaw);
            torsoRotationSharpness = Mathf.Max(0.1f, torsoRotationSharpness);
        }

        public void Configure(IsometricCharacterMotor characterMotor, Animator targetAnimator)
        {
            motor = characterMotor;
            animator = targetAnimator;
            CacheBones();
        }

        public void SetAimTorsoDuringWallRun(bool enabled)
        {
            aimTorsoDuringWallRun = enabled;
            if (enabled)
            {
                requireHumanoid = false;
                useGenericBoneNameFallback = true;
                CacheBones();
            }
        }

        private void LateUpdate()
        {
            ResolveReferences();
            if (motor == null || animator == null || !animator.isActiveAndEnabled)
            {
                return;
            }

            if (spine == null && chest == null && upperChest == null && neck == null && head == null && leftShoulder == null && rightShoulder == null)
            {
                CacheBones();
            }

            targetTorsoYaw = ResolveTargetYaw();
            float blend = 1f - Mathf.Exp(-torsoRotationSharpness * Time.deltaTime);
            currentTorsoYaw = Mathf.LerpAngle(currentTorsoYaw, targetTorsoYaw, blend);
            ApplyTorsoYaw(currentTorsoYaw * torsoAimWeight);
        }

        private void ResolveReferences()
        {
            if (motor == null)
            {
                motor = GetComponent<IsometricCharacterMotor>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        private void CacheBones()
        {
            spine = null;
            chest = null;
            upperChest = null;
            hips = null;
            neck = null;
            head = null;
            leftShoulder = null;
            rightShoulder = null;

            if (animator == null)
            {
                return;
            }

            if (animator.avatar != null && animator.isHuman)
            {
                hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                spine = animator.GetBoneTransform(HumanBodyBones.Spine);
                chest = animator.GetBoneTransform(HumanBodyBones.Chest);
                upperChest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
                neck = animator.GetBoneTransform(HumanBodyBones.Neck);
                head = animator.GetBoneTransform(HumanBodyBones.Head);
                leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
                rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
                return;
            }

            if (requireHumanoid || !useGenericBoneNameFallback)
            {
                return;
            }

            CacheGenericBones(animator.transform);
        }

        private float ResolveTargetYaw()
        {
            if (!enableBodySplit
                || !motor.SeparateAimFromLocomotion
                || !motor.HasAimDirection
                || (motor.IsWallRunning && !aimTorsoDuringWallRun)
                || motor.IsObstacleTraversing
                || motor.IsCombatRolling)
            {
                return 0f;
            }

            Vector3 bodyForward = motor.BodyFacingDirection.sqrMagnitude > 0.0001f
                ? motor.BodyFacingDirection
                : animator.transform.forward;
            bodyForward = Vector3.ProjectOnPlane(bodyForward, Vector3.up);

            Vector3 aimForward = Vector3.ProjectOnPlane(motor.AimDirection, Vector3.up);
            if (bodyForward.sqrMagnitude <= 0.0001f || aimForward.sqrMagnitude <= 0.0001f)
            {
                return 0f;
            }

            float yaw = SignedAngleAroundAxis(bodyForward.normalized, aimForward.normalized, Vector3.up);
            return Mathf.Clamp(yaw, -maxTorsoYaw, maxTorsoYaw);
        }

        private void ApplyTorsoYaw(float yaw)
        {
            if (Mathf.Abs(yaw) <= 0.001f)
            {
                return;
            }

            if (includeHipsInAim)
            {
                ApplyBoneYaw(hips, yaw * hipsWeight);
            }

            ApplyBoneYaw(spine, yaw * spineWeight);
            ApplyBoneYaw(chest, yaw * chestWeight);
            ApplyBoneYaw(upperChest, yaw * upperChestWeight);
            ApplyBoneYaw(leftShoulder, yaw * shoulderWeight);
            ApplyBoneYaw(rightShoulder, yaw * shoulderWeight);
            ApplyBoneYaw(neck, yaw * neckWeight);
            ApplyBoneYaw(head, yaw * headWeight);
        }

        private void CacheGenericBones(Transform root)
        {
            if (root == null)
            {
                return;
            }

            hips = FindFirstBone(root, "pelvis", "hip", "ccbasepelvis", "ccbasehip");
            spine = FindFirstBone(root, "waist", "spine", "spine01", "spine_01", "ccbasespine01");
            chest = FindFirstBone(root, "chest", "spine1", "spine01", "spine_01", "spine 1", "ccbasespine01");
            upperChest = FindFirstBone(root, "upperchest", "upper_chest", "spine2", "spine02", "spine_02", "spine 2", "ccbasespine02");
            neck = FindFirstBone(root, "neck", "necktwist", "ccbaseneck");
            head = FindFirstBone(root, "head", "ccbasehead");
            leftShoulder = FindFirstBone(root, "leftshoulder", "lshoulder", "shoulder_l", "l_shoulder", "leftclavicle", "lclavicle", "clavicle_l", "l_clavicle", "ccbaselclavicle", "leftupperarm", "lupperarm", "upperarm_l", "l_upperarm", "ccbaselupperarm");
            rightShoulder = FindFirstBone(root, "rightshoulder", "rshoulder", "shoulder_r", "r_shoulder", "rightclavicle", "rclavicle", "clavicle_r", "r_clavicle", "ccbaserclavicle", "rightupperarm", "rupperarm", "upperarm_r", "r_upperarm", "ccbaserupperarm");
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

        private static void ApplyBoneYaw(Transform bone, float yaw)
        {
            if (bone == null || Mathf.Abs(yaw) <= 0.001f)
            {
                return;
            }

            bone.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * bone.rotation;
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
    }
}
