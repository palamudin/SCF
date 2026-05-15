using UnityEngine;

namespace SCF.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(IsometricCharacterMotor))]
    [DefaultExecutionOrder(90)]
    public sealed class SCFClimbHandContactIK : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private IsometricCharacterMotor motor;
        [SerializeField] private Animator animator;

        [Header("Climb Hands")]
        [SerializeField] private bool activeForCurrentCharacter;
        [SerializeField, Min(0.1f)] private float blendSharpness = 16f;
        [SerializeField, Range(0.45f, 0.95f)] private float handBaseHeight = 0.74f;
        [SerializeField, Range(0.04f, 0.3f)] private float handSideSpacing = 0.16f;
        [SerializeField, Range(0f, 0.22f)] private float handVerticalStride = 0.12f;
        [SerializeField, Min(0f)] private float handSurfaceOffset = 0.035f;
        [SerializeField, Min(0.05f)] private float handProbeDistance = 0.45f;

        [Header("Debug")]
        [SerializeField] private float currentWeight;

        private Transform leftClavicle;
        private Transform rightClavicle;
        private Transform leftUpperArm;
        private Transform rightUpperArm;
        private Transform leftForearm;
        private Transform rightForearm;
        private Transform leftHand;
        private Transform rightHand;

        private void Reset()
        {
            motor = GetComponent<IsometricCharacterMotor>();
            animator = GetComponentInChildren<Animator>();
        }

        private void Awake()
        {
            ResolveReferences();
            CacheGenericBones();
        }

        public void Configure(IsometricCharacterMotor characterMotor, Animator targetAnimator, bool enabledForCharacter)
        {
            motor = characterMotor;
            animator = targetAnimator;
            activeForCurrentCharacter = enabledForCharacter;
            CacheGenericBones();
        }

        private void Update()
        {
            ResolveReferences();
            float targetWeight = CanApplyHandContact() ? 1f : 0f;
            float blend = 1f - Mathf.Exp(-blendSharpness * Time.deltaTime);
            currentWeight = Mathf.Lerp(currentWeight, targetWeight, blend);
        }

        private void LateUpdate()
        {
            if (currentWeight <= 0.001f || animator == null || animator.isHuman || motor == null)
            {
                return;
            }

            if (leftHand == null && rightHand == null)
            {
                CacheGenericBones();
            }

            if (!TryResolveWallBasis(out Vector3 normal, out Vector3 right))
            {
                return;
            }

            ApplyGenericHand(leftClavicle, leftUpperArm, leftForearm, leftHand, -1f, 0f, normal, right);
            ApplyGenericHand(rightClavicle, rightUpperArm, rightForearm, rightHand, 1f, 0.5f, normal, right);
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (currentWeight <= 0.001f || animator == null || !animator.isHuman || motor == null)
            {
                ClearIkWeights();
                return;
            }

            if (!TryResolveWallBasis(out Vector3 normal, out Vector3 right))
            {
                ClearIkWeights();
                return;
            }

            ApplyHumanoidHand(AvatarIKGoal.LeftHand, -1f, 0f, normal, right);
            ApplyHumanoidHand(AvatarIKGoal.RightHand, 1f, 0.5f, normal, right);
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

        private void CacheGenericBones()
        {
            Transform root = animator != null ? animator.transform : transform;
            leftClavicle = FindFirstBone(root, "leftclavicle", "lclavicle", "clavicle_l", "l_clavicle", "ccbaselclavicle");
            rightClavicle = FindFirstBone(root, "rightclavicle", "rclavicle", "clavicle_r", "r_clavicle", "ccbaserclavicle");
            leftUpperArm = FindFirstBone(root, "leftupperarm", "lupperarm", "upperarm_l", "l_upperarm", "ccbaselupperarm");
            rightUpperArm = FindFirstBone(root, "rightupperarm", "rupperarm", "upperarm_r", "r_upperarm", "ccbaserupperarm");
            leftForearm = FindFirstBone(root, "leftforearm", "lforearm", "forearm_l", "l_forearm", "ccbaselforearm");
            rightForearm = FindFirstBone(root, "rightforearm", "rforearm", "forearm_r", "r_forearm", "ccbaserforearm");
            leftHand = FindFirstBone(root, "lefthand", "lhand", "hand_l", "l_hand", "ccbaselhand");
            rightHand = FindFirstBone(root, "righthand", "rhand", "hand_r", "r_hand", "ccbaserhand");
        }

        private bool CanApplyHandContact()
        {
            return activeForCurrentCharacter
                   && motor != null
                   && motor.TraversalProfile == SCFTraversalProfile.Parkour
                   && motor.IsWallClimbingUp
                   && animator != null
                   && animator.isActiveAndEnabled;
        }

        private bool TryResolveWallBasis(out Vector3 normal, out Vector3 right)
        {
            normal = Vector3.ProjectOnPlane(motor.WallNormal, Vector3.up);
            right = Vector3.zero;
            if (normal.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            normal.Normalize();
            right = Vector3.Cross(Vector3.up, normal);
            if (right.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            right.Normalize();
            return true;
        }

        private void ApplyHumanoidHand(AvatarIKGoal goal, float side, float phaseOffset, Vector3 normal, Vector3 right)
        {
            if (!TryResolveHandTarget(side, phaseOffset, normal, right, out Vector3 target, out Quaternion rotation, out float handWeight))
            {
                return;
            }

            animator.SetIKPositionWeight(goal, handWeight);
            animator.SetIKRotationWeight(goal, handWeight * 0.8f);
            animator.SetIKPosition(goal, target);
            animator.SetIKRotation(goal, rotation);
        }

        private void ApplyGenericHand(Transform clavicle, Transform upperArm, Transform forearm, Transform hand, float side, float phaseOffset, Vector3 normal, Vector3 right)
        {
            if (hand == null
                || !TryResolveHandTarget(side, phaseOffset, normal, right, out Vector3 target, out Quaternion rotation, out float handWeight))
            {
                return;
            }

            RotateBoneTowardTarget(clavicle, hand, target, handWeight * 0.18f);
            RotateBoneTowardTarget(upperArm, hand, target, handWeight * 0.42f);
            RotateBoneTowardTarget(forearm, hand, target, handWeight * 0.65f);
            hand.rotation = Quaternion.Slerp(hand.rotation, rotation, handWeight * 0.45f);
        }

        private bool TryResolveHandTarget(float side, float phaseOffset, Vector3 normal, Vector3 right, out Vector3 target, out Quaternion rotation, out float handWeight)
        {
            target = Vector3.zero;
            rotation = Quaternion.identity;
            handWeight = 0f;
            if (motor == null)
            {
                return false;
            }

            float phase = Mathf.Repeat(motor.WallRunCycle01 + phaseOffset, 1f);
            float reach01 = Mathf.Clamp01(Mathf.Sin(phase * Mathf.PI * 2f) * 0.5f + 0.5f);
            float height = Mathf.Max(0.1f, motor.ControllerWorldHeight);
            target = motor.WallSurfacePoint;
            target.y = motor.ControllerFootY + height * (handBaseHeight + handVerticalStride * (reach01 - 0.5f));
            target += right * (side * height * handSideSpacing);

            Vector3 probeOrigin = target + normal * handProbeDistance;
            if (Physics.Raycast(probeOrigin, -normal, out RaycastHit hit, handProbeDistance * 2f, motor.TraversalMask, QueryTriggerInteraction.Ignore))
            {
                target = hit.point;
            }

            target += normal * handSurfaceOffset;
            rotation = Quaternion.LookRotation(-normal, Vector3.up);
            handWeight = currentWeight * Mathf.Lerp(0.65f, 1f, reach01);
            return true;
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

        private void ClearIkWeights()
        {
            if (animator == null || !animator.isHuman)
            {
                return;
            }

            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
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
