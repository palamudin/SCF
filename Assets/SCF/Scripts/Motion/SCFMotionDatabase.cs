using System;
using UnityEngine;

namespace SCF.Gameplay
{
    public enum SCFMotionType
    {
        Idle,
        Locomotion,
        Jump,
        CombatRoll,
        WallRun,
        Vault,
        Climb,
        VaultSlide
    }

    [Flags]
    public enum SCFMotionTags
    {
        None = 0,
        Grounded = 1 << 0,
        Airborne = 1 << 1,
        Locomotion = 1 << 2,
        Action = 1 << 3,
        Loop = 1 << 4,
        Jump = 1 << 5,
        CombatRoll = 1 << 6,
        WallRun = 1 << 7,
        Vault = 1 << 8,
        Climb = 1 << 9,
        VaultSlide = 1 << 10
    }

    [Serializable]
    public struct SCFTrajectoryPoint
    {
        [SerializeField] private float time;
        [SerializeField] private Vector3 localPosition;
        [SerializeField] private float facingYaw;

        public float Time => time;
        public Vector3 LocalPosition => localPosition;
        public float FacingYaw => facingYaw;

        public SCFTrajectoryPoint(float time, Vector3 localPosition, float facingYaw)
        {
            this.time = time;
            this.localPosition = localPosition;
            this.facingYaw = facingYaw;
        }
    }

    [Serializable]
    public struct SCFMotionFrame
    {
        [SerializeField] private float clipTime;
        [SerializeField] private float normalizedTime;
        [SerializeField] private Vector3 rootVelocity;
        [SerializeField] private SCFTrajectoryPoint[] trajectory;

        public float ClipTime => clipTime;
        public float NormalizedTime => normalizedTime;
        public Vector3 RootVelocity => rootVelocity;
        public SCFTrajectoryPoint[] Trajectory => trajectory;

        public SCFMotionFrame(float clipTime, float normalizedTime, Vector3 rootVelocity, SCFTrajectoryPoint[] trajectory)
        {
            this.clipTime = clipTime;
            this.normalizedTime = normalizedTime;
            this.rootVelocity = rootVelocity;
            this.trajectory = trajectory;
        }
    }

    [Serializable]
    public sealed class SCFMotionClipData
    {
        [SerializeField] private string id;
        [SerializeField] private AnimationClip clip;
        [SerializeField] private SCFMotionType motionType;
        [SerializeField] private SCFMotionTags tags;
        [SerializeField] private bool looping;
        [SerializeField] private float duration;
        [SerializeField] private float averagePlanarSpeed;
        [SerializeField] private Vector3 averageVelocity;
        [SerializeField] private Vector2 movementDirection;
        [SerializeField] private SCFMotionFrame[] frames;

        public string Id => string.IsNullOrWhiteSpace(id) && clip != null ? clip.name : id;
        public AnimationClip Clip => clip;
        public SCFMotionType MotionType => motionType;
        public SCFMotionTags Tags => tags;
        public bool Looping => looping;
        public float Duration => duration;
        public float AveragePlanarSpeed => averagePlanarSpeed;
        public Vector3 AverageVelocity => averageVelocity;
        public Vector2 MovementDirection => movementDirection;
        public SCFMotionFrame[] Frames => frames;
        public bool IsValid => clip != null;
        public string DisplayName => clip != null ? clip.name : Id;

        public SCFMotionClipData(
            string id,
            AnimationClip clip,
            SCFMotionType motionType,
            SCFMotionTags tags,
            bool looping,
            float duration,
            float averagePlanarSpeed,
            Vector3 averageVelocity,
            SCFMotionFrame[] frames)
            : this(id, clip, motionType, tags, looping, duration, averagePlanarSpeed, averageVelocity, ResolveDirection(averageVelocity), frames)
        {
        }

        public SCFMotionClipData(
            string id,
            AnimationClip clip,
            SCFMotionType motionType,
            SCFMotionTags tags,
            bool looping,
            float duration,
            float averagePlanarSpeed,
            Vector3 averageVelocity,
            Vector2 movementDirection,
            SCFMotionFrame[] frames)
        {
            this.id = id;
            this.clip = clip;
            this.motionType = motionType;
            this.tags = tags;
            this.looping = looping;
            this.duration = duration;
            this.averagePlanarSpeed = averagePlanarSpeed;
            this.averageVelocity = averageVelocity;
            this.movementDirection = movementDirection.sqrMagnitude > 0.0001f ? movementDirection.normalized : Vector2.zero;
            this.frames = frames;
        }

        public bool HasTag(SCFMotionTags tag)
        {
            return tag == SCFMotionTags.None || (tags & tag) == tag;
        }

        private static Vector2 ResolveDirection(Vector3 velocity)
        {
            Vector2 direction = new Vector2(velocity.x, velocity.z);
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.zero;
        }
    }

    [CreateAssetMenu(fileName = "SCF_MotionDatabase", menuName = "SCF/Motion Database")]
    public sealed class SCFMotionDatabase : ScriptableObject
    {
        [SerializeField, Min(0.01f)] private float poseInterval = 0.1f;
        [SerializeField] private float[] trajectoryTimes = { 0f, 0.33f, 0.66f, 1f };
        [SerializeField] private SCFMotionClipData[] clips = Array.Empty<SCFMotionClipData>();

        public float PoseInterval => poseInterval;
        public float[] TrajectoryTimes => trajectoryTimes;
        public SCFMotionClipData[] Clips => clips;
        public int Count => clips != null ? clips.Length : 0;

        public bool TryGetClip(int index, out SCFMotionClipData clipData)
        {
            if (clips != null && index >= 0 && index < clips.Length)
            {
                clipData = clips[index];
                return clipData != null && clipData.IsValid;
            }

            clipData = null;
            return false;
        }

        public int FindFirst(SCFMotionType motionType)
        {
            if (clips == null)
            {
                return -1;
            }

            for (int i = 0; i < clips.Length; i++)
            {
                SCFMotionClipData clipData = clips[i];
                if (clipData != null && clipData.IsValid && clipData.MotionType == motionType)
                {
                    return i;
                }
            }

            return -1;
        }

        public int FindFastestLocomotion()
        {
            int bestIndex = -1;
            float bestSpeed = float.NegativeInfinity;

            if (clips == null)
            {
                return bestIndex;
            }

            for (int i = 0; i < clips.Length; i++)
            {
                SCFMotionClipData clipData = clips[i];
                if (clipData == null || !clipData.IsValid)
                {
                    continue;
                }

                if (clipData.MotionType != SCFMotionType.Locomotion && clipData.MotionType != SCFMotionType.Idle)
                {
                    continue;
                }

                if (clipData.AveragePlanarSpeed > bestSpeed)
                {
                    bestSpeed = clipData.AveragePlanarSpeed;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        public int FindFastestLocomotion(out float bestSpeed)
        {
            int bestIndex = -1;
            bestSpeed = float.NegativeInfinity;

            if (clips == null)
            {
                return bestIndex;
            }

            for (int i = 0; i < clips.Length; i++)
            {
                SCFMotionClipData clipData = clips[i];
                if (clipData == null || !clipData.IsValid)
                {
                    continue;
                }

                if (clipData.MotionType != SCFMotionType.Locomotion && clipData.MotionType != SCFMotionType.Idle)
                {
                    continue;
                }

                if (clipData.AveragePlanarSpeed > bestSpeed)
                {
                    bestSpeed = clipData.AveragePlanarSpeed;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        public int FindBestLocomotion(float desiredPlanarSpeed, int currentIndex, float currentClipBias, out float bestCost)
        {
            return FindBestLocomotion(Vector2.zero, desiredPlanarSpeed, currentIndex, currentClipBias, out bestCost);
        }

        public int FindBestLocomotion(Vector2 desiredDirection, float desiredPlanarSpeed, int currentIndex, float currentClipBias, out float bestCost)
        {
            int bestIndex = -1;
            bestCost = float.PositiveInfinity;

            if (clips == null)
            {
                return bestIndex;
            }

            float speed = Mathf.Max(0f, desiredPlanarSpeed);
            Vector2 direction = desiredDirection.sqrMagnitude > 0.0001f ? desiredDirection.normalized : Vector2.zero;
            for (int i = 0; i < clips.Length; i++)
            {
                SCFMotionClipData clipData = clips[i];
                if (clipData == null || !clipData.IsValid)
                {
                    continue;
                }

                if (clipData.MotionType != SCFMotionType.Locomotion && clipData.MotionType != SCFMotionType.Idle)
                {
                    continue;
                }

                float cost = Mathf.Abs(clipData.AveragePlanarSpeed - speed);
                cost += DirectionCost(direction, clipData.MovementDirection);
                if (i == currentIndex)
                {
                    cost -= Mathf.Max(0f, currentClipBias);
                }

                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        public int FindBestByType(SCFMotionType motionType, Vector2 desiredDirection, int currentIndex, float currentClipBias, out float bestCost)
        {
            int bestIndex = -1;
            bestCost = float.PositiveInfinity;

            if (clips == null)
            {
                return bestIndex;
            }

            Vector2 direction = desiredDirection.sqrMagnitude > 0.0001f ? desiredDirection.normalized : Vector2.zero;
            for (int i = 0; i < clips.Length; i++)
            {
                SCFMotionClipData clipData = clips[i];
                if (clipData == null || !clipData.IsValid || clipData.MotionType != motionType)
                {
                    continue;
                }

                float cost = DirectionCost(direction, clipData.MovementDirection);
                if (i == currentIndex)
                {
                    cost -= Mathf.Max(0f, currentClipBias);
                }

                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private static float DirectionCost(Vector2 desiredDirection, Vector2 clipDirection)
        {
            if (desiredDirection.sqrMagnitude <= 0.0001f || clipDirection.sqrMagnitude <= 0.0001f)
            {
                return 0f;
            }

            float dot = Mathf.Clamp(Vector2.Dot(desiredDirection.normalized, clipDirection.normalized), -1f, 1f);
            return (1f - dot) * 2.25f;
        }

#if UNITY_EDITOR
        public void SetBakedData(float newPoseInterval, float[] newTrajectoryTimes, SCFMotionClipData[] newClips)
        {
            poseInterval = Mathf.Max(0.01f, newPoseInterval);
            trajectoryTimes = newTrajectoryTimes != null && newTrajectoryTimes.Length > 0
                ? newTrajectoryTimes
                : new[] { 0f, 0.33f, 0.66f, 1f };
            clips = newClips ?? Array.Empty<SCFMotionClipData>();
        }
#endif
    }
}
