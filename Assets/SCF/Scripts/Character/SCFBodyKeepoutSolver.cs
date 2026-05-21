using System;
using System.Collections.Generic;
using UnityEngine;

namespace SCF.Gameplay
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(90)]
    public sealed class SCFBodyKeepoutSolver : MonoBehaviour
    {
        private const string DynamicRootName = "SCF_DynamicBodyRig";
        private const string KeepoutRootName = "SCF_NoGoZones";
        private const string CarryRootName = "SCF_DynamicCarryPoints";

        private const string HipsKeepoutName = "SCF_NoGo_Hips";
        private const string CoreKeepoutName = "SCF_NoGo_Core";
        private const string ChestKeepoutName = "SCF_NoGo_Chest";
        private const string HeadKeepoutName = "SCF_NoGo_Head";

        public const string IdleWeaponPlacementPointName = "Idle";
        public const string HipWeaponPlacementPointName = "HipGun";
        public const string ChestWeaponPlacementPointName = "ShoulderGun";
        public const string RunWeaponPlacementPointName = "RunGun";
        public const string SprintWeaponPlacementPointName = "SprintGun";
        public const string OneHandWeaponPlacementPointName = "Gun";

        public const string IdleRightGripCarryPointName = "SCF_RightGrip_Idle";
        public const string HipRightGripCarryPointName = "SCF_RightGrip_HipAim";
        public const string ChestRightGripCarryPointName = "SCF_RightGrip_ShoulderAim";
        public const string RunRightGripCarryPointName = "SCF_RightGrip_Run";
        public const string SprintRightGripCarryPointName = "SCF_RightGrip_Sprint";
        public const string OneHandRightGripCarryPointName = "SCF_RightGrip_OneHand";

        [Serializable]
        private struct BodyCarryPreset
        {
            public Vector3 bodyOffset01;
            public Vector3 eulerOffset;

            public BodyCarryPreset(Vector3 bodyOffset01, Vector3 eulerOffset)
            {
                this.bodyOffset01 = bodyOffset01;
                this.eulerOffset = eulerOffset;
            }
        }

        private struct KeepoutVolume
        {
            public Transform transform;
            public float radius;
        }

        [Header("Build")]
        [SerializeField] private Animator animator;
        [SerializeField] private bool autoBuild = true;
        [SerializeField] private bool refreshEveryLateUpdate = true;

        [Header("Generated Keep-Out Zones")]
        [SerializeField] private bool enableKeepoutSolving = true;
        [SerializeField, Range(0f, 0.25f)] private float armTargetClearance = 0.045f;
        [SerializeField, Range(0f, 1f)] private float keepoutSolveWeight = 1f;
        [SerializeField, Range(1, 4)] private int keepoutSolveIterations = 2;
        [SerializeField, Range(0.05f, 0.8f)] private float hipsRadius01 = 0.19f;
        [SerializeField, Range(0.05f, 0.8f)] private float coreRadius01 = 0.25f;
        [SerializeField, Range(0.05f, 0.8f)] private float chestRadius01 = 0.29f;
        [SerializeField, Range(0.03f, 0.5f)] private float headRadius01 = 0.13f;

        [Header("Generated Carry Presets")]
        [SerializeField] private bool generateLegacyRightGripCarryPoints;
        [SerializeField] private bool useRendererBoundsForBodySurface = true;
        [SerializeField, Range(0.05f, 0.8f)] private float fallbackBodySurfaceRightRadius01 = 0.16f;
        [SerializeField, Range(0.03f, 0.8f)] private float fallbackBodySurfaceForwardRadius01 = 0.11f;
        [SerializeField, Range(-0.05f, 0.2f)] private float bodySurfaceClearance = 0.01f;
        [SerializeField, Range(0f, 1f)] private float bodyContactKeepoutWeight = 0.25f;
        [SerializeField, Range(0.25f, 1f)] private float bodyContactKeepoutRadiusScale = 0.42f;
        [SerializeField, Range(0f, 0.15f)] private float bodyContactKeepoutClearance;
        [SerializeField] private BodyCarryPreset idleCarry = new BodyCarryPreset(new Vector3(0.28f, 0.36f, 0.95f), Vector3.zero);
        [SerializeField] private BodyCarryPreset hipCarry = new BodyCarryPreset(new Vector3(0.28f, 0.37f, 0.95f), Vector3.zero);
        [SerializeField] private BodyCarryPreset chestCarry = new BodyCarryPreset(new Vector3(0.30f, 0.62f, 0.95f), Vector3.zero);
        [SerializeField] private BodyCarryPreset runCarry = new BodyCarryPreset(new Vector3(0.28f, 0.49f, 0.95f), Vector3.zero);
        [SerializeField] private BodyCarryPreset sprintCarry = new BodyCarryPreset(new Vector3(0.25f, 0.40f, 0.85f), Vector3.zero);
        [SerializeField] private BodyCarryPreset oneHandCarry = new BodyCarryPreset(new Vector3(0.55f, 0.58f, 0.85f), Vector3.zero);

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private Color hipsColor = new Color(1f, 0.2f, 0.1f, 0.28f);
        [SerializeField] private Color coreColor = new Color(1f, 0.75f, 0.05f, 0.24f);
        [SerializeField] private Color chestColor = new Color(0.1f, 0.45f, 1f, 0.25f);
        [SerializeField] private Color headColor = new Color(0.65f, 0.2f, 1f, 0.24f);
        [SerializeField] private Color carryPointColor = new Color(0.05f, 1f, 0.75f, 0.9f);

        private readonly List<KeepoutVolume> keepoutVolumes = new List<KeepoutVolume>(4);

        private Transform dynamicRoot;
        private Transform keepoutRoot;
        private Transform carryRoot;
        private Transform hipsKeepout;
        private Transform coreKeepout;
        private Transform chestKeepout;
        private Transform headKeepout;

        private Transform hipsBone;
        private Transform chestBone;
        private Transform headBone;
        private Transform leftShoulderBone;
        private Transform rightShoulderBone;

        private Transform idleWeaponPoint;
        private Transform hipWeaponPoint;
        private Transform chestWeaponPoint;
        private Transform runWeaponPoint;
        private Transform sprintWeaponPoint;
        private Transform oneHandWeaponPoint;

        private Transform idleRightGripPoint;
        private Transform hipRightGripPoint;
        private Transform chestRightGripPoint;
        private Transform runRightGripPoint;
        private Transform sprintRightGripPoint;
        private Transform oneHandRightGripPoint;

        private Vector3 bodyOrigin;
        private Vector3 bodyRight = Vector3.right;
        private Vector3 bodyUp = Vector3.up;
        private Vector3 bodyForward = Vector3.forward;
        private float bodyHeight = 1.75f;
        private float shoulderWidth = 0.45f;
        private float bodySurfaceRightRadius = 0.18f;
        private float bodySurfaceForwardRadius = 0.12f;
        private int lastRefreshFrame = -1;

        public Transform IdleWeaponPoint => idleWeaponPoint;
        public Transform HipWeaponPoint => hipWeaponPoint;
        public Transform ChestWeaponPoint => chestWeaponPoint;
        public Transform RunWeaponPoint => runWeaponPoint;
        public Transform SprintWeaponPoint => sprintWeaponPoint;
        public Transform OneHandWeaponPoint => oneHandWeaponPoint;

        public Transform IdleRightGripPoint => idleRightGripPoint;
        public Transform HipRightGripPoint => hipRightGripPoint;
        public Transform ChestRightGripPoint => chestRightGripPoint;
        public Transform RunRightGripPoint => runRightGripPoint;
        public Transform SprintRightGripPoint => sprintRightGripPoint;
        public Transform OneHandRightGripPoint => oneHandRightGripPoint;

        public bool OwnsGeneratedCarryPoint(Transform point)
        {
            return point != null && carryRoot != null && point.IsChildOf(carryRoot);
        }

        private void Awake()
        {
            if (autoBuild)
            {
                Configure(animator);
            }
        }

        private void LateUpdate()
        {
            if (refreshEveryLateUpdate)
            {
                RefreshFromBones();
            }
        }

        public void Configure(Animator sourceAnimator)
        {
            animator = sourceAnimator != null ? sourceAnimator : GetComponentInChildren<Animator>();
            BuildGeneratedRig();
            CacheBones();
            RefreshFromBones(true);
        }

        public Vector3 ResolveArmTarget(Vector3 shoulderPosition, Vector3 targetPosition, bool rightHand, out bool adjusted)
        {
            adjusted = false;
            if (!enableKeepoutSolving || keepoutVolumes.Count == 0 || keepoutSolveWeight <= 0.001f)
            {
                return targetPosition;
            }

            RefreshFromBones();

            Vector3 originalTarget = targetPosition;
            Vector3 solvedTarget = targetPosition;
            float clearance = Mathf.Max(0f, armTargetClearance);

            int iterations = Mathf.Max(1, keepoutSolveIterations);
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                for (int i = 0; i < keepoutVolumes.Count; i++)
                {
                    KeepoutVolume volume = keepoutVolumes[i];
                    if (volume.transform == null)
                    {
                        continue;
                    }

                    solvedTarget = PushTargetAwayFromVolume(shoulderPosition, solvedTarget, volume.transform.position, volume.radius + clearance, rightHand);
                }
            }

            if ((solvedTarget - originalTarget).sqrMagnitude > 0.000001f)
            {
                adjusted = true;
                return Vector3.Lerp(originalTarget, solvedTarget, Mathf.Clamp01(keepoutSolveWeight));
            }

            return targetPosition;
        }

        public Vector3 ResolveJointPosition(Vector3 jointPosition, bool rightSide, out bool adjusted)
        {
            adjusted = false;
            if (!enableKeepoutSolving || keepoutVolumes.Count == 0 || keepoutSolveWeight <= 0.001f)
            {
                return jointPosition;
            }

            RefreshFromBones();

            Vector3 solved = jointPosition;
            float clearance = Mathf.Max(0f, armTargetClearance);
            for (int i = 0; i < keepoutVolumes.Count; i++)
            {
                KeepoutVolume volume = keepoutVolumes[i];
                if (volume.transform == null)
                {
                    continue;
                }

                solved = PushPointAwayFromVolume(solved, volume.transform.position, volume.radius + clearance, rightSide);
            }

            float sideSign = rightSide ? 1f : -1f;
            float sideDistance = Vector3.Dot(solved - bodyOrigin, bodyRight) * sideSign;
            float minSideDistance = Mathf.Max(0.04f, shoulderWidth * 0.18f);
            if (sideDistance < minSideDistance)
            {
                solved += bodyRight * sideSign * (minSideDistance - sideDistance);
            }

            if ((solved - jointPosition).sqrMagnitude > 0.000001f)
            {
                adjusted = true;
                return Vector3.Lerp(jointPosition, solved, Mathf.Clamp01(keepoutSolveWeight));
            }

            return jointPosition;
        }

        [ContextMenu("Capture Current Carry Points As Presets")]
        public void CaptureCurrentCarryPointsAsPresets()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (hipsBone == null || chestBone == null || headBone == null)
            {
                CacheBones();
            }

            ResolveBodyBasis();

            idleCarry = CaptureCarryPreset(idleWeaponPoint, idleCarry);
            hipCarry = CaptureCarryPreset(hipWeaponPoint, hipCarry);
            chestCarry = CaptureCarryPreset(chestWeaponPoint, chestCarry);
            runCarry = CaptureCarryPreset(runWeaponPoint, runCarry);
            sprintCarry = CaptureCarryPreset(sprintWeaponPoint, sprintCarry);
            oneHandCarry = CaptureCarryPreset(oneHandWeaponPoint, oneHandCarry);
        }

        public void RefreshFromBones()
        {
            RefreshFromBones(false);
        }

        private void RefreshFromBones(bool force)
        {
            if (!force && lastRefreshFrame == Time.frameCount)
            {
                return;
            }

            lastRefreshFrame = Time.frameCount;

            if (dynamicRoot == null)
            {
                BuildGeneratedRig();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (hipsBone == null || chestBone == null || headBone == null)
            {
                CacheBones();
            }

            ResolveBodyBasis();
            RefreshKeepoutVolumes();
            RefreshCarryPoints();
        }

        private void BuildGeneratedRig()
        {
            Transform parent = animator != null ? animator.transform : transform;
            if (dynamicRoot != null && dynamicRoot.parent != parent)
            {
                dynamicRoot.SetParent(parent, false);
            }

            dynamicRoot = dynamicRoot != null ? dynamicRoot : EnsureChild(parent, DynamicRootName);
            dynamicRoot.localPosition = Vector3.zero;
            dynamicRoot.localRotation = Quaternion.identity;
            dynamicRoot.localScale = Vector3.one;

            keepoutRoot = EnsureChild(dynamicRoot, KeepoutRootName);
            carryRoot = EnsureChild(dynamicRoot, CarryRootName);

            hipsKeepout = EnsureChild(keepoutRoot, HipsKeepoutName);
            coreKeepout = EnsureChild(keepoutRoot, CoreKeepoutName);
            chestKeepout = EnsureChild(keepoutRoot, ChestKeepoutName);
            headKeepout = EnsureChild(keepoutRoot, HeadKeepoutName);

            idleWeaponPoint = EnsureChild(carryRoot, IdleWeaponPlacementPointName);
            hipWeaponPoint = EnsureChild(carryRoot, HipWeaponPlacementPointName);
            chestWeaponPoint = EnsureChild(carryRoot, ChestWeaponPlacementPointName);
            runWeaponPoint = EnsureChild(carryRoot, RunWeaponPlacementPointName);
            sprintWeaponPoint = EnsureChild(carryRoot, SprintWeaponPlacementPointName);
            oneHandWeaponPoint = EnsureChild(carryRoot, OneHandWeaponPlacementPointName);

            if (generateLegacyRightGripCarryPoints)
            {
                idleRightGripPoint = EnsureChild(carryRoot, IdleRightGripCarryPointName);
                hipRightGripPoint = EnsureChild(carryRoot, HipRightGripCarryPointName);
                chestRightGripPoint = EnsureChild(carryRoot, ChestRightGripCarryPointName);
                runRightGripPoint = EnsureChild(carryRoot, RunRightGripCarryPointName);
                sprintRightGripPoint = EnsureChild(carryRoot, SprintRightGripCarryPointName);
                oneHandRightGripPoint = EnsureChild(carryRoot, OneHandRightGripCarryPointName);
            }
            else
            {
                DestroyLegacyRightGripCarryPoints();
            }
        }

        private void DestroyLegacyRightGripCarryPoints()
        {
            DestroyChild(carryRoot, IdleRightGripCarryPointName);
            DestroyChild(carryRoot, HipRightGripCarryPointName);
            DestroyChild(carryRoot, ChestRightGripCarryPointName);
            DestroyChild(carryRoot, RunRightGripCarryPointName);
            DestroyChild(carryRoot, SprintRightGripCarryPointName);
            DestroyChild(carryRoot, OneHandRightGripCarryPointName);

            idleRightGripPoint = null;
            hipRightGripPoint = null;
            chestRightGripPoint = null;
            runRightGripPoint = null;
            sprintRightGripPoint = null;
            oneHandRightGripPoint = null;
        }

        private void CacheBones()
        {
            Transform root = animator != null ? animator.transform : transform;
            if (animator != null && animator.avatar != null && animator.isHuman)
            {
                hipsBone = FirstNonNull(animator.GetBoneTransform(HumanBodyBones.Hips), root);
                chestBone = FirstNonNull(
                    animator.GetBoneTransform(HumanBodyBones.UpperChest),
                    animator.GetBoneTransform(HumanBodyBones.Chest),
                    animator.GetBoneTransform(HumanBodyBones.Spine),
                    hipsBone,
                    root);
                headBone = FirstNonNull(
                    animator.GetBoneTransform(HumanBodyBones.Head),
                    animator.GetBoneTransform(HumanBodyBones.Neck),
                    chestBone,
                    root);
                leftShoulderBone = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
                rightShoulderBone = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
                return;
            }

            hipsBone = FirstNonNull(FindBone(root, "hips", "pelvis", "ccbasehip"), root);
            chestBone = FirstNonNull(
                FindBone(root, "upperchest", "upper_chest", "spine2", "spine02", "spine_02", "ccbasespine02"),
                FindBone(root, "chest", "spine1", "spine01", "spine_01", "ccbasespine01"),
                FindBone(root, "spine", "ccbasespine"),
                hipsBone,
                root);
            headBone = FirstNonNull(FindBone(root, "head", "ccbasehead"), FindBone(root, "neck", "ccbaseneck"), chestBone, root);
            leftShoulderBone = FindBone(root, "leftshoulder", "lshoulder", "shoulder_l", "l_shoulder", "leftclavicle", "lclavicle", "clavicle_l", "l_clavicle", "ccbaselclavicle");
            rightShoulderBone = FindBone(root, "rightshoulder", "rshoulder", "shoulder_r", "r_shoulder", "rightclavicle", "rclavicle", "clavicle_r", "r_clavicle", "ccbaserclavicle");
        }

        private void ResolveBodyBasis()
        {
            Transform root = animator != null ? animator.transform : transform;
            Vector3 hipsPosition = hipsBone != null ? hipsBone.position : root.position;
            Vector3 chestPosition = chestBone != null ? chestBone.position : hipsPosition + root.up * 0.85f;
            Vector3 headPosition = headBone != null ? headBone.position : chestPosition + root.up * 0.45f;

            bodyOrigin = hipsPosition;
            bodyUp = ResolveAxis(headPosition - hipsPosition, root.up, Vector3.up);
            Vector3 referenceForward = ResolveAxis(Vector3.ProjectOnPlane(root.forward, bodyUp), root.forward, Vector3.forward);
            Vector3 referenceRight = ResolveAxis(Vector3.Cross(bodyUp, referenceForward), root.right, Vector3.right);

            if (leftShoulderBone != null && rightShoulderBone != null)
            {
                bodyRight = ResolveAxis(rightShoulderBone.position - leftShoulderBone.position, root.right, Vector3.right);
                shoulderWidth = Mathf.Max(0.2f, Vector3.Distance(leftShoulderBone.position, rightShoulderBone.position));
            }
            else
            {
                bodyRight = ResolveAxis(chestBone != null ? chestBone.right : root.right, root.right, Vector3.right);
                shoulderWidth = Mathf.Max(0.2f, Vector3.Distance(hipsPosition, chestPosition) * 0.55f);
            }

            bodyRight = Vector3.ProjectOnPlane(bodyRight, bodyUp);
            if (bodyRight.sqrMagnitude <= 0.0001f)
            {
                bodyRight = referenceRight;
            }
            else
            {
                bodyRight.Normalize();
            }

            if (Vector3.Dot(bodyRight, referenceRight) < 0f)
            {
                bodyRight = -bodyRight;
            }

            bodyForward = Vector3.Cross(bodyRight, bodyUp);
            if (bodyForward.sqrMagnitude <= 0.0001f)
            {
                bodyForward = referenceForward;
            }
            else
            {
                bodyForward.Normalize();
            }

            if (Vector3.Dot(bodyForward, referenceForward) < 0f)
            {
                bodyForward = referenceForward;
                bodyRight = ResolveAxis(Vector3.Cross(bodyUp, bodyForward), referenceRight, Vector3.right);
            }

            float measuredHeight = Vector3.Distance(hipsPosition, headPosition);
            bodyHeight = Mathf.Max(0.65f, measuredHeight * 1.18f);
            ResolveBodySurfaceRadii(root);
        }

        private void ResolveBodySurfaceRadii(Transform root)
        {
            float fallbackRight = Mathf.Max(0.06f, bodyHeight * fallbackBodySurfaceRightRadius01);
            float fallbackForward = Mathf.Max(0.04f, bodyHeight * fallbackBodySurfaceForwardRadius01);
            float measuredRight = fallbackRight;
            float measuredForward = fallbackForward;
            bool hasRendererSample = false;

            if (useRendererBoundsForBodySurface && root != null)
            {
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (ShouldIgnoreRendererForBodySurface(renderer))
                    {
                        continue;
                    }

                    Bounds bounds = renderer.bounds;
                    Vector3 centerOffset = bounds.center - bodyOrigin;
                    float vertical = Vector3.Dot(centerOffset, bodyUp);
                    if (vertical < bodyHeight * 0.08f || vertical > bodyHeight * 0.72f)
                    {
                        continue;
                    }

                    Vector3 extents = bounds.extents;
                    float rightReach = Mathf.Abs(Vector3.Dot(Vector3.right * extents.x, bodyRight))
                                       + Mathf.Abs(Vector3.Dot(Vector3.up * extents.y, bodyRight))
                                       + Mathf.Abs(Vector3.Dot(Vector3.forward * extents.z, bodyRight));
                    float forwardReach = Mathf.Abs(Vector3.Dot(Vector3.right * extents.x, bodyForward))
                                         + Mathf.Abs(Vector3.Dot(Vector3.up * extents.y, bodyForward))
                                         + Mathf.Abs(Vector3.Dot(Vector3.forward * extents.z, bodyForward));

                    measuredRight = Mathf.Max(measuredRight, rightReach);
                    measuredForward = Mathf.Max(measuredForward, forwardReach);
                    hasRendererSample = true;
                }
            }

            float minRight = Mathf.Max(0.05f, shoulderWidth * 0.18f);
            float maxRight = Mathf.Max(minRight, shoulderWidth * 0.62f);
            float minForward = Mathf.Max(0.035f, shoulderWidth * 0.11f);
            float maxForward = Mathf.Max(minForward, shoulderWidth * 0.46f);
            bodySurfaceRightRadius = Mathf.Clamp(hasRendererSample ? measuredRight : fallbackRight, minRight, maxRight);
            bodySurfaceForwardRadius = Mathf.Clamp(hasRendererSample ? measuredForward : fallbackForward, minForward, maxForward);
        }

        private bool ShouldIgnoreRendererForBodySurface(Renderer renderer)
        {
            if (renderer == null || renderer.transform == null)
            {
                return true;
            }

            if (dynamicRoot != null && renderer.transform.IsChildOf(dynamicRoot))
            {
                return true;
            }

            string compactName = CompactName(renderer.name);
            return compactName.Contains("railgun")
                   || compactName.Contains("shotgun")
                   || compactName.Contains("weapon")
                   || compactName.Contains("muzzle")
                   || compactName.Contains("grip")
                   || compactName.Contains("buttstock");
        }

        private void RefreshKeepoutVolumes()
        {
            Vector3 hips = BodyPoint(new Vector3(0f, 0.08f, 0f));
            Vector3 core = BodyPoint(new Vector3(0f, 0.32f, 0.01f));
            Vector3 chest = BodyPoint(new Vector3(0f, 0.55f, 0.015f));
            Vector3 head = headBone != null ? headBone.position : BodyPoint(new Vector3(0f, 0.84f, 0f));

            SetWorldPose(hipsKeepout, hips, Quaternion.LookRotation(bodyForward, bodyUp));
            SetWorldPose(coreKeepout, core, Quaternion.LookRotation(bodyForward, bodyUp));
            SetWorldPose(chestKeepout, chest, Quaternion.LookRotation(bodyForward, bodyUp));
            SetWorldPose(headKeepout, head, Quaternion.LookRotation(bodyForward, bodyUp));

            keepoutVolumes.Clear();
            AddVolume(hipsKeepout, ResolveRadius(hipsRadius01));
            AddVolume(coreKeepout, ResolveRadius(coreRadius01));
            AddVolume(chestKeepout, ResolveRadius(chestRadius01));
            AddVolume(headKeepout, ResolveRadius(headRadius01));
        }

        private void RefreshCarryPoints()
        {
            ApplyCarryPreset(idleWeaponPoint, idleCarry);
            ApplyCarryPreset(hipWeaponPoint, hipCarry);
            ApplyCarryPreset(chestWeaponPoint, chestCarry);
            ApplyCarryPreset(runWeaponPoint, runCarry);
            ApplyCarryPreset(sprintWeaponPoint, sprintCarry);
            ApplyCarryPreset(oneHandWeaponPoint, oneHandCarry);

            if (generateLegacyRightGripCarryPoints)
            {
                MirrorPose(idleWeaponPoint, idleRightGripPoint);
                MirrorPose(hipWeaponPoint, hipRightGripPoint);
                MirrorPose(chestWeaponPoint, chestRightGripPoint);
                MirrorPose(runWeaponPoint, runRightGripPoint);
                MirrorPose(sprintWeaponPoint, sprintRightGripPoint);
                MirrorPose(oneHandWeaponPoint, oneHandRightGripPoint);
            }
            else
            {
                DestroyLegacyRightGripCarryPoints();
                idleRightGripPoint = null;
                hipRightGripPoint = null;
                chestRightGripPoint = null;
                runRightGripPoint = null;
                sprintRightGripPoint = null;
                oneHandRightGripPoint = null;
            }
        }

        private void ApplyCarryPreset(Transform point, BodyCarryPreset preset)
        {
            if (point == null)
            {
                return;
            }

            Quaternion bodyRotation = Quaternion.LookRotation(bodyForward, bodyUp);
            SetWorldPose(point, ResolveBodySurfaceContactPoint(preset), bodyRotation * Quaternion.Euler(preset.eulerOffset));
        }

        private BodyCarryPreset CaptureCarryPreset(Transform point, BodyCarryPreset fallback)
        {
            if (point == null)
            {
                return fallback;
            }

            Quaternion bodyRotation = Quaternion.LookRotation(bodyForward, bodyUp);
            Vector3 offset = point.position - bodyOrigin;
            float safeHeight = Mathf.Max(0.0001f, bodyHeight);
            Vector3 bodyOffset01 = new Vector3(
                Vector3.Dot(offset, bodyRight) / safeHeight,
                Vector3.Dot(offset, bodyUp) / safeHeight,
                Vector3.Dot(offset, bodyForward) / safeHeight);
            Quaternion localRotation = Quaternion.Inverse(bodyRotation) * point.rotation;
            return new BodyCarryPreset(bodyOffset01, NormalizeEuler(localRotation.eulerAngles));
        }

        private void MirrorPose(Transform source, Transform target)
        {
            if (source == null || target == null)
            {
                return;
            }

            SetWorldPose(target, source.position, source.rotation);
        }

        private Vector3 BodyPoint(Vector3 bodyOffset01)
        {
            return bodyOrigin
                   + bodyRight * (bodyOffset01.x * bodyHeight)
                   + bodyUp * (bodyOffset01.y * bodyHeight)
                   + bodyForward * (bodyOffset01.z * bodyHeight);
        }

        private Vector3 ResolveBodySurfaceContactPoint(BodyCarryPreset preset)
        {
            float height01 = Mathf.Clamp01(preset.bodyOffset01.y);
            Vector3 center = bodyOrigin + bodyUp * (height01 * bodyHeight);
            Vector3 radialDirection = bodyRight * preset.bodyOffset01.x + bodyForward * preset.bodyOffset01.z;
            radialDirection = Vector3.ProjectOnPlane(radialDirection, bodyUp);
            if (radialDirection.sqrMagnitude <= 0.0001f)
            {
                radialDirection = bodyRight * 0.25f + bodyForward;
            }

            radialDirection.Normalize();
            float right = Vector3.Dot(radialDirection, bodyRight);
            float forward = Vector3.Dot(radialDirection, bodyForward);
            float rightRadius = Mathf.Max(0.001f, bodySurfaceRightRadius);
            float forwardRadius = Mathf.Max(0.001f, bodySurfaceForwardRadius);
            float denominator = (right * right) / (rightRadius * rightRadius) + (forward * forward) / (forwardRadius * forwardRadius);
            float surfaceRadius = denominator > 0.0001f ? 1f / Mathf.Sqrt(denominator) : rightRadius;
            Vector3 surfacePoint = center + radialDirection * Mathf.Max(0f, surfaceRadius + bodySurfaceClearance);
            return ResolveBodyContactPoint(surfacePoint);
        }

        private Vector3 ResolveBodyContactPoint(Vector3 point)
        {
            if (!enableKeepoutSolving || keepoutVolumes.Count == 0 || bodyContactKeepoutWeight <= 0.001f)
            {
                return point;
            }

            Vector3 solved = point;
            float clearance = Mathf.Max(0f, bodyContactKeepoutClearance);
            float radiusScale = Mathf.Clamp(bodyContactKeepoutRadiusScale, 0.25f, 1f);
            for (int i = 0; i < keepoutVolumes.Count; i++)
            {
                KeepoutVolume volume = keepoutVolumes[i];
                if (volume.transform == null)
                {
                    continue;
                }

                solved = PushBodyContactAwayFromVolume(solved, volume.transform.position, volume.radius * radiusScale + clearance);
            }

            return Vector3.Lerp(point, solved, Mathf.Clamp01(bodyContactKeepoutWeight));
        }

        private Vector3 PushBodyContactAwayFromVolume(Vector3 point, Vector3 volumeCenter, float radius)
        {
            Vector3 offset = point - volumeCenter;
            float distance = offset.magnitude;
            if (distance >= radius)
            {
                return point;
            }

            Vector3 pushDirection = Vector3.ProjectOnPlane(offset, bodyUp);
            if (pushDirection.sqrMagnitude <= 0.0001f)
            {
                pushDirection = bodyRight * 0.45f + bodyForward;
            }

            return volumeCenter + pushDirection.normalized * radius;
        }

        private float ResolveRadius(float radius01)
        {
            return Mathf.Max(0.08f, Mathf.Lerp(shoulderWidth, bodyHeight, 0.25f) * radius01);
        }

        private void AddVolume(Transform volumeTransform, float radius)
        {
            keepoutVolumes.Add(new KeepoutVolume
            {
                transform = volumeTransform,
                radius = radius
            });
        }

        private Vector3 PushTargetAwayFromVolume(Vector3 shoulderPosition, Vector3 targetPosition, Vector3 volumeCenter, float radius, bool rightHand)
        {
            Vector3 shoulderToTarget = targetPosition - shoulderPosition;
            float segmentLengthSq = shoulderToTarget.sqrMagnitude;
            if (segmentLengthSq <= 0.000001f)
            {
                return PushPointAwayFromVolume(targetPosition, volumeCenter, radius, rightHand);
            }

            float t = Mathf.Clamp01(Vector3.Dot(volumeCenter - shoulderPosition, shoulderToTarget) / segmentLengthSq);
            Vector3 closestPoint = shoulderPosition + shoulderToTarget * t;
            Vector3 fromCenter = closestPoint - volumeCenter;
            float distance = fromCenter.magnitude;
            if (distance >= radius)
            {
                return PushPointAwayFromVolume(targetPosition, volumeCenter, radius, rightHand);
            }

            Vector3 pushDirection = ResolvePushDirection(fromCenter, rightHand);
            float pushAmount = radius - distance;
            float targetInfluence = Mathf.Lerp(0.55f, 1f, t);
            return PushPointAwayFromVolume(targetPosition + pushDirection * pushAmount * targetInfluence, volumeCenter, radius, rightHand);
        }

        private Vector3 PushPointAwayFromVolume(Vector3 point, Vector3 volumeCenter, float radius, bool rightHand)
        {
            Vector3 offset = point - volumeCenter;
            float distance = offset.magnitude;
            if (distance >= radius)
            {
                return point;
            }

            Vector3 pushDirection = ResolvePushDirection(offset, rightHand);
            return volumeCenter + pushDirection * radius;
        }

        private Vector3 ResolvePushDirection(Vector3 offset, bool rightHand)
        {
            Vector3 planarOffset = Vector3.ProjectOnPlane(offset, bodyUp);
            if (planarOffset.sqrMagnitude > 0.0001f)
            {
                return planarOffset.normalized;
            }

            Vector3 side = rightHand ? bodyRight : -bodyRight;
            Vector3 fallback = side + bodyForward * 0.25f;
            return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : side;
        }

        private static void SetWorldPose(Transform target, Vector3 position, Quaternion rotation)
        {
            if (target == null)
            {
                return;
            }

            target.SetPositionAndRotation(position, rotation);
            target.localScale = Vector3.one;
        }

        private static Transform EnsureChild(Transform parent, string childName)
        {
            Transform child = parent != null ? parent.Find(childName) : null;
            if (child != null)
            {
                return child;
            }

            GameObject childObject = new GameObject(childName);
            child = childObject.transform;
            if (parent != null)
            {
                child.SetParent(parent, false);
            }

            return child;
        }

        private static void DestroyChild(Transform parent, string childName)
        {
            if (parent == null || string.IsNullOrWhiteSpace(childName))
            {
                return;
            }

            Transform child = parent.Find(childName);
            if (child == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
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

        private static Vector3 ResolveAxis(Vector3 preferred, Vector3 fallback, Vector3 finalFallback)
        {
            if (preferred.sqrMagnitude > 0.0001f)
            {
                return preferred.normalized;
            }

            if (fallback.sqrMagnitude > 0.0001f)
            {
                return fallback.normalized;
            }

            return finalFallback.normalized;
        }

        private static Transform FindBone(Transform root, params string[] names)
        {
            if (root == null || names == null || names.Length == 0)
            {
                return null;
            }

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                string compact = CompactName(children[i].name);
                for (int n = 0; n < names.Length; n++)
                {
                    if (compact == CompactName(names[n]))
                    {
                        return children[i];
                    }
                }
            }

            return null;
        }

        private static string CompactName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace(".", string.Empty)
                .ToLowerInvariant();
        }

        private static Vector3 NormalizeEuler(Vector3 euler)
        {
            euler.x = NormalizeAngle(euler.x);
            euler.y = NormalizeAngle(euler.y);
            euler.z = NormalizeAngle(euler.z);
            return euler;
        }

        private static float NormalizeAngle(float angle)
        {
            while (angle > 180f)
            {
                angle -= 360f;
            }

            while (angle < -180f)
            {
                angle += 360f;
            }

            return angle;
        }

        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos)
            {
                return;
            }

            DrawVolume(hipsKeepout, ResolveRadius(hipsRadius01), hipsColor);
            DrawVolume(coreKeepout, ResolveRadius(coreRadius01), coreColor);
            DrawVolume(chestKeepout, ResolveRadius(chestRadius01), chestColor);
            DrawVolume(headKeepout, ResolveRadius(headRadius01), headColor);
            DrawCarryPoint(idleWeaponPoint);
            DrawCarryPoint(hipWeaponPoint);
            DrawCarryPoint(chestWeaponPoint);
            DrawCarryPoint(runWeaponPoint);
            DrawCarryPoint(sprintWeaponPoint);
            DrawCarryPoint(oneHandWeaponPoint);
        }

        private static void DrawVolume(Transform volume, float radius, Color color)
        {
            if (volume == null)
            {
                return;
            }

            Gizmos.color = color;
            Gizmos.DrawSphere(volume.position, radius);
            Gizmos.color = new Color(color.r, color.g, color.b, 0.85f);
            Gizmos.DrawWireSphere(volume.position, radius);
        }

        private void DrawCarryPoint(Transform point)
        {
            if (point == null)
            {
                return;
            }

            Gizmos.color = carryPointColor;
            Gizmos.DrawSphere(point.position, 0.035f);
            Gizmos.DrawRay(point.position, point.forward * 0.18f);
        }
    }
}
