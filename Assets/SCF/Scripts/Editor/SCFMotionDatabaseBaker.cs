#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using SCF.Gameplay;
using UnityEditor;
using UnityEngine;

namespace SCF.EditorTools
{
    public static class SCFMotionDatabaseBaker
    {
        public const string DatabaseFolder = "Assets/SCF/Animation";
        public const string DatabasePath = DatabaseFolder + "/SCF_BaseMotionDatabase.asset";
        public const string HumanoidDatabasePath = DatabaseFolder + "/SCF_HumanoidMotionDatabase.asset";
        private const string ParkourPlayerAnimationFolder = "Assets/RedNotRed/3D Adaptive Parkour System/Animations/Player";
        private const string DynamicParkourAnimationFolder = "Assets/SCF/ThirdParty/DynamicParkour/Animations";
        private const string TpsEquippedWalkPath = "Assets/TPS Shooter (Military style)/Animations/Humanoid/EquipedAnimations/Walk/walk.fbx";
        private const string TpsEquippedRunPath = "Assets/TPS Shooter (Military style)/Animations/Humanoid/EquipedAnimations/Run/run.fbx";
        private const string TpsEquippedWalkForwardClipName = "walk_fwd";

        private const float PoseInterval = 0.1f;
        private static readonly float[] TrajectoryTimes = { 0f, 0.33f, 0.66f, 1f };

        [MenuItem("SCF/Motion/Rebuild Base Motion Database")]
        public static void RebuildBaseMotionDatabaseMenu()
        {
            SCFMotionDatabase database = BuildBaseDatabase();
            BuildHumanoidDatabase();
            Selection.activeObject = database;
            Debug.Log("SCF base motion database rebuilt at " + DatabasePath + ".");
        }

        public static SCFMotionDatabase BuildBaseDatabase()
        {
            EnsureFolder(DatabaseFolder);

            SCFMotionDatabase database = AssetDatabase.LoadAssetAtPath<SCFMotionDatabase>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<SCFMotionDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            List<SCFMotionClipData> clips = new List<SCFMotionClipData>();

            AnimationClip idle = FindFirstClip(
                ParkourPlayerAnimationFolder + "/Idle.anim",
                "Assets/StarterAssets/ThirdPersonController/Character/Animations/Stand--Idle.anim.fbx",
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/FreehandsAnimations/Idle/Idle.fbx",
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/EquipedAnimations/Idle/idle.fbx");
            AnimationClip walk = FindFirstClip(
                "Assets/StarterAssets/ThirdPersonController/Character/Animations/Locomotion--Walk_N.anim.fbx",
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/FreehandsAnimations/Walk/walk.fbx",
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/EquipedAnimations/Walk/walk.fbx");
            AnimationClip run = FindFirstClip(
                ParkourPlayerAnimationFolder + "/Run.anim",
                "Assets/StarterAssets/ThirdPersonController/Character/Animations/Locomotion--Run_N.anim.fbx",
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/FreehandsAnimations/Run/run.fbx",
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/EquipedAnimations/Run/run.fbx");

            AddMotion(clips, "Idle", idle, SCFMotionType.Idle, SCFMotionTags.Grounded | SCFMotionTags.Loop, true, 0f, Vector2.zero);
            AddMotion(clips, "Walk", walk, SCFMotionType.Locomotion, SCFMotionTags.Grounded | SCFMotionTags.Locomotion | SCFMotionTags.Loop, true, 2.6f, Vector2.up);
            AddMotion(clips, "Run", run, SCFMotionType.Locomotion, SCFMotionTags.Grounded | SCFMotionTags.Locomotion | SCFMotionTags.Loop, true, 5.8f, Vector2.up);
            AddMotion(clips, "Run Forward Left", FindFirstClip(ParkourPlayerAnimationFolder + "/RunForwardLeft.anim"), SCFMotionType.Locomotion, SCFMotionTags.Grounded | SCFMotionTags.Locomotion | SCFMotionTags.Loop, true, 5.8f, new Vector2(-1f, 1f));
            AddMotion(clips, "Run Forward Right", FindFirstClip(ParkourPlayerAnimationFolder + "/RunForwardRight.anim"), SCFMotionType.Locomotion, SCFMotionTags.Grounded | SCFMotionTags.Locomotion | SCFMotionTags.Loop, true, 5.8f, new Vector2(1f, 1f));
            AddMotion(clips, "Run Left", FindFirstClip(ParkourPlayerAnimationFolder + "/RunLeft.anim"), SCFMotionType.Locomotion, SCFMotionTags.Grounded | SCFMotionTags.Locomotion | SCFMotionTags.Loop, true, 5.4f, Vector2.left);
            AddMotion(clips, "Run Right", FindFirstClip(ParkourPlayerAnimationFolder + "/RunRight.anim"), SCFMotionType.Locomotion, SCFMotionTags.Grounded | SCFMotionTags.Locomotion | SCFMotionTags.Loop, true, 5.4f, Vector2.right);
            AddMotion(clips, "Run Backward", FindFirstClip(
                ParkourPlayerAnimationFolder + "/RunBack.anim",
                "Assets/StarterAssets/ThirdPersonController/Character/Animations/Locomotion--Run_S.anim.fbx"),
                SCFMotionType.Locomotion, SCFMotionTags.Grounded | SCFMotionTags.Locomotion | SCFMotionTags.Loop, true, 5.2f, Vector2.down);
            AddMotion(clips, "Run Backward Left", FindFirstClip(ParkourPlayerAnimationFolder + "/RunBackLeft.anim"), SCFMotionType.Locomotion, SCFMotionTags.Grounded | SCFMotionTags.Locomotion | SCFMotionTags.Loop, true, 5.2f, new Vector2(-1f, -1f));
            AddMotion(clips, "Run Backward Right", FindFirstClip(ParkourPlayerAnimationFolder + "/RunBackRight.anim"), SCFMotionType.Locomotion, SCFMotionTags.Grounded | SCFMotionTags.Locomotion | SCFMotionTags.Loop, true, 5.2f, new Vector2(1f, -1f));
            AddMotion(clips, "Jump", FindFirstClip(
                ParkourPlayerAnimationFolder + "/Jump.anim",
                ParkourPlayerAnimationFolder + "/HopUp.anim",
                "Assets/SCF/Animation/Jump.anim",
                "Assets/StarterAssets/ThirdPersonController/Character/Animations/Jump--Jump.anim.fbx",
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/FreehandsAnimations/Jump/jump.fbx",
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/EquipedAnimations/Jump/jump start.FBX"),
                SCFMotionType.Jump,
                SCFMotionTags.Airborne | SCFMotionTags.Action | SCFMotionTags.Jump,
                false,
                5.5f,
                Vector2.up);
            AddMotion(clips, "Combat Roll", FindFirstClip(
                ParkourPlayerAnimationFolder + "/Roll.anim",
                "Assets/SCF/Animation/Roll.anim",
                "Assets/SCF/Animation/Slide.anim",
                "Assets/SCF/Animation/Vault.anim"),
                SCFMotionType.CombatRoll,
                SCFMotionTags.Grounded | SCFMotionTags.Action | SCFMotionTags.CombatRoll,
                false,
                8.4f,
                Vector2.up);
            AddMotion(clips, "Back Step", FindFirstClip(ParkourPlayerAnimationFolder + "/HopDown.anim"),
                SCFMotionType.CombatRoll,
                SCFMotionTags.Grounded | SCFMotionTags.Action | SCFMotionTags.CombatRoll,
                false,
                6.2f,
                Vector2.down);
            AddMotion(clips, "Dodge Left", FindFirstClip(ParkourPlayerAnimationFolder + "/HopLeft.anim"),
                SCFMotionType.CombatRoll,
                SCFMotionTags.Grounded | SCFMotionTags.Action | SCFMotionTags.CombatRoll,
                false,
                6.2f,
                Vector2.left);
            AddMotion(clips, "Dodge Right", FindFirstClip(ParkourPlayerAnimationFolder + "/HopRight.anim"),
                SCFMotionType.CombatRoll,
                SCFMotionTags.Grounded | SCFMotionTags.Action | SCFMotionTags.CombatRoll,
                false,
                6.2f,
                Vector2.right);
            AddMotion(clips, "Vault Slide", FindFirstClip(
                ParkourPlayerAnimationFolder + "/Slide.anim",
                "Assets/SCF/Animation/Slide.anim",
                DynamicParkourAnimationFolder + "/Slide.fbx",
                DynamicParkourAnimationFolder + "/Jumping Crouch.fbx"),
                SCFMotionType.VaultSlide,
                SCFMotionTags.Grounded | SCFMotionTags.Action | SCFMotionTags.Vault | SCFMotionTags.VaultSlide,
                false,
                6.8f,
                Vector2.up);
            AddMotion(clips, "Wall Run Left", FindFirstClip(
                ParkourPlayerAnimationFolder + "/RunLeft.anim",
                ParkourPlayerAnimationFolder + "/WallRunLeft.anim"),
                SCFMotionType.WallRun,
                SCFMotionTags.Airborne | SCFMotionTags.Action | SCFMotionTags.WallRun | SCFMotionTags.Loop,
                true,
                7.4f,
                Vector2.left);
            AddMotion(clips, "Wall Run Right", FindFirstClip(
                ParkourPlayerAnimationFolder + "/RunRight.anim",
                ParkourPlayerAnimationFolder + "/WallRunRight.anim"),
                SCFMotionType.WallRun,
                SCFMotionTags.Airborne | SCFMotionTags.Action | SCFMotionTags.WallRun | SCFMotionTags.Loop,
                true,
                7.4f,
                Vector2.right);
            AddMotion(clips, "Vault", FindFirstClip(
                ParkourPlayerAnimationFolder + "/Vault.anim",
                ParkourPlayerAnimationFolder + "/Vault2.anim",
                "Assets/SCF/Animation/Vault.anim",
                "Assets/SCF/Animation/Vault2.anim",
                ParkourPlayerAnimationFolder + "/HopDown.anim"),
                SCFMotionType.Vault,
                SCFMotionTags.Grounded | SCFMotionTags.Action | SCFMotionTags.Vault,
                false,
                5.2f,
                Vector2.up);
            AddMotion(clips, "Climb", FindFirstClip(
                ParkourPlayerAnimationFolder + "/ClimbUp.anim",
                ParkourPlayerAnimationFolder + "/ClimbingUp.anim",
                ParkourPlayerAnimationFolder + "/FlyOver.anim",
                ParkourPlayerAnimationFolder + "/Vault2.anim"),
                SCFMotionType.Climb,
                SCFMotionTags.Grounded | SCFMotionTags.Action | SCFMotionTags.Climb,
                false,
                3.2f,
                Vector2.up);

            Undo.RecordObject(database, "Bake SCF motion database");
            database.SetBakedData(PoseInterval, TrajectoryTimes, clips.ToArray());
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            return database;
        }

        public static SCFMotionDatabase BuildHumanoidDatabase()
        {
            EnsureFolder(DatabaseFolder);

            SCFMotionDatabase database = AssetDatabase.LoadAssetAtPath<SCFMotionDatabase>(HumanoidDatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<SCFMotionDatabase>();
                AssetDatabase.CreateAsset(database, HumanoidDatabasePath);
            }

            List<SCFMotionClipData> clips = new List<SCFMotionClipData>();

            AnimationClip idle = FindFirstClip(
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/EquipedAnimations/Idle/idle aiming.fbx",
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/EquipedAnimations/Idle/idle.fbx",
                DynamicParkourAnimationFolder + "/Idle.fbx",
                "Assets/StarterAssets/ThirdPersonController/Character/Animations/Stand--Idle.anim.fbx",
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/FreehandsAnimations/Idle/Idle.fbx");
            AnimationClip walk = FindClipInAsset(TpsEquippedWalkPath, TpsEquippedWalkForwardClipName) ?? FindFirstClip(
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/EquipedAnimations/Walk/walk.fbx",
                DynamicParkourAnimationFolder + "/Walk.fbx",
                "Assets/StarterAssets/ThirdPersonController/Character/Animations/Locomotion--Walk_N.anim.fbx",
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/FreehandsAnimations/Walk/walk.fbx");
            AnimationClip run = FindClipInAsset(TpsEquippedRunPath, "run") ?? FindFirstClip(
                TpsEquippedRunPath,
                DynamicParkourAnimationFolder + "/Run.fbx",
                "Assets/StarterAssets/ThirdPersonController/Character/Animations/Locomotion--Run_N.anim.fbx",
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/FreehandsAnimations/Run/run.fbx");
            AnimationClip runBack = FindFirstClip(
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/EquipedAnimations/Run/run.fbx",
                DynamicParkourAnimationFolder + "/Run To Stop.fbx",
                "Assets/StarterAssets/ThirdPersonController/Character/Animations/Locomotion--Run_S.anim.fbx",
                "Assets/StarterAssets/ThirdPersonController/Character/Animations/Locomotion--Run_N.anim.fbx",
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/FreehandsAnimations/Run/run.fbx");
            AnimationClip jump = FindFirstClip(
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/EquipedAnimations/Jump/jump start.FBX",
                DynamicParkourAnimationFolder + "/Jump.fbx",
                DynamicParkourAnimationFolder + "/Big Jump.fbx",
                "Assets/StarterAssets/ThirdPersonController/Character/Animations/Jump--Jump.anim.fbx",
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/FreehandsAnimations/Jump/jump.fbx");
            AnimationClip vault = FindFirstClip(
                DynamicParkourAnimationFolder + "/VaultFence.fbx",
                DynamicParkourAnimationFolder + "/Step Up.fbx",
                DynamicParkourAnimationFolder + "/ReachHigh.fbx",
                "Assets/StarterAssets/ThirdPersonController/Character/Animations/Jump--Jump.anim.fbx");
            AnimationClip climb = FindFirstClip(
                DynamicParkourAnimationFolder + "/Freehang Climb.fbx",
                DynamicParkourAnimationFolder + "/Braced Hang Climb.fbx",
                DynamicParkourAnimationFolder + "/ReachHigh.fbx",
                DynamicParkourAnimationFolder + "/Step Up.fbx",
                "Assets/StarterAssets/ThirdPersonController/Character/Animations/Jump--Jump.anim.fbx");
            AnimationClip slide = FindFirstClip(
                DynamicParkourAnimationFolder + "/Slide.fbx",
                DynamicParkourAnimationFolder + "/Jumping Crouch.fbx",
                DynamicParkourAnimationFolder + "/Crouched To Standing.fbx");
            AnimationClip crouchForward = FindFirstClip(
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/FreehandsAnimations/Crouch/Crouch_Forward.fbx",
                "Assets/TPS Shooter (Military style)/Animations/Humanoid/FreehandsAnimations/Crouch/Crouch_Locomotion.fbx",
                "Assets/StarterAssets/ThirdPersonController/Character/Animations/Jump--InAir.anim.fbx",
                "Assets/StarterAssets/ThirdPersonController/Character/Animations/Jump--Jump.anim.fbx");
            AnimationClip rollFallback = slide != null ? slide : crouchForward;
            AnimationClip aimWalkForward = FindClipInAsset(TpsEquippedWalkPath, "walk_fwd") ?? walk;
            AnimationClip aimWalkBackward = aimWalkForward;
            AnimationClip aimWalkLeft = FindClipInAsset(TpsEquippedWalkPath, "walk_45_left") ?? aimWalkForward;
            AnimationClip aimWalkRight = FindClipInAsset(TpsEquippedWalkPath, "walk_45_right") ?? aimWalkForward;
            AnimationClip aimRunForward = FindClipInAsset(TpsEquippedRunPath, "run") ?? run;
            AnimationClip aimRunBackward = FindClipInAsset(TpsEquippedWalkPath, "walk_bwd") ?? aimWalkForward;
            AnimationClip walkLeft = FindClipInAsset(TpsEquippedWalkPath, "walk_left");
            AnimationClip walkRight = FindClipInAsset(TpsEquippedWalkPath, "walk_right");
            AnimationClip aimRunLeft = walkLeft != null ? walkLeft : aimWalkLeft;
            AnimationClip aimRunRight = walkRight != null ? walkRight : aimWalkRight;

            AddMotion(clips, "Humanoid Idle", idle, SCFMotionType.Idle, SCFMotionTags.Grounded | SCFMotionTags.Loop, true, 0f, Vector2.zero);
            AddMotion(clips, "Humanoid Walk", walk, SCFMotionType.Locomotion, SCFMotionTags.Grounded | SCFMotionTags.Locomotion | SCFMotionTags.Loop, true, 2.6f, Vector2.up);
            AddMotion(clips, "Humanoid Run", run, SCFMotionType.Locomotion, SCFMotionTags.Grounded | SCFMotionTags.Locomotion | SCFMotionTags.Loop, true, 5.8f, Vector2.up);
            AddMotion(clips, "Humanoid Run Forward Left", run, SCFMotionType.Locomotion, SCFMotionTags.Grounded | SCFMotionTags.Locomotion | SCFMotionTags.Loop, true, 5.8f, new Vector2(-1f, 1f));
            AddMotion(clips, "Humanoid Run Forward Right", run, SCFMotionType.Locomotion, SCFMotionTags.Grounded | SCFMotionTags.Locomotion | SCFMotionTags.Loop, true, 5.8f, new Vector2(1f, 1f));
            AddMotion(clips, "Humanoid Run Left", walkLeft != null ? walkLeft : run, SCFMotionType.Locomotion, SCFMotionTags.Grounded | SCFMotionTags.Locomotion | SCFMotionTags.Loop, true, 5.2f, Vector2.left, true);
            AddMotion(clips, "Humanoid Run Right", walkRight != null ? walkRight : run, SCFMotionType.Locomotion, SCFMotionTags.Grounded | SCFMotionTags.Locomotion | SCFMotionTags.Loop, true, 5.2f, Vector2.right, true);
            AddMotion(clips, "Humanoid Run Backward", runBack, SCFMotionType.Locomotion, SCFMotionTags.Grounded | SCFMotionTags.Locomotion | SCFMotionTags.Loop, true, 5.2f, Vector2.down);
            AddMotion(clips, "Humanoid Run Backward Left", runBack, SCFMotionType.Locomotion, SCFMotionTags.Grounded | SCFMotionTags.Locomotion | SCFMotionTags.Loop, true, 5.2f, new Vector2(-1f, -1f));
            AddMotion(clips, "Humanoid Run Backward Right", runBack, SCFMotionType.Locomotion, SCFMotionTags.Grounded | SCFMotionTags.Locomotion | SCFMotionTags.Loop, true, 5.2f, new Vector2(1f, -1f));
            AddMotion(clips, "Humanoid Aim Walk Forward", aimWalkForward, SCFMotionType.Locomotion, SCFMotionTags.Grounded | SCFMotionTags.Locomotion | SCFMotionTags.Loop | SCFMotionTags.Aim | SCFMotionTags.AimWalk, true, 2.6f, Vector2.up, true);
            AddMotion(clips, "Humanoid Aim Walk Backward", aimWalkBackward, SCFMotionType.Locomotion, SCFMotionTags.Grounded | SCFMotionTags.Locomotion | SCFMotionTags.Loop | SCFMotionTags.Aim | SCFMotionTags.AimWalk, true, 2.6f, Vector2.down, true);
            AddMotion(clips, "Humanoid Aim Walk Left", aimWalkLeft, SCFMotionType.Locomotion, SCFMotionTags.Grounded | SCFMotionTags.Locomotion | SCFMotionTags.Loop | SCFMotionTags.Aim | SCFMotionTags.AimWalk, true, 2.6f, Vector2.left, true);
            AddMotion(clips, "Humanoid Aim Walk Right", aimWalkRight, SCFMotionType.Locomotion, SCFMotionTags.Grounded | SCFMotionTags.Locomotion | SCFMotionTags.Loop | SCFMotionTags.Aim | SCFMotionTags.AimWalk, true, 2.6f, Vector2.right, true);
            AddMotion(clips, "Humanoid Aim Run Forward", aimRunForward, SCFMotionType.Locomotion, SCFMotionTags.Grounded | SCFMotionTags.Locomotion | SCFMotionTags.Loop | SCFMotionTags.Aim | SCFMotionTags.AimRun, true, 5.2f, Vector2.up, true);
            AddMotion(clips, "Humanoid Aim Run Backward", aimRunBackward, SCFMotionType.Locomotion, SCFMotionTags.Grounded | SCFMotionTags.Locomotion | SCFMotionTags.Loop | SCFMotionTags.Aim | SCFMotionTags.AimRun, true, 5.2f, Vector2.down, true);
            AddMotion(clips, "Humanoid Aim Run Left", aimRunLeft, SCFMotionType.Locomotion, SCFMotionTags.Grounded | SCFMotionTags.Locomotion | SCFMotionTags.Loop | SCFMotionTags.Aim | SCFMotionTags.AimRun, true, 5.2f, Vector2.left, true);
            AddMotion(clips, "Humanoid Aim Run Right", aimRunRight, SCFMotionType.Locomotion, SCFMotionTags.Grounded | SCFMotionTags.Locomotion | SCFMotionTags.Loop | SCFMotionTags.Aim | SCFMotionTags.AimRun, true, 5.2f, Vector2.right, true);
            AddMotion(clips, "Humanoid Jump", jump, SCFMotionType.Jump, SCFMotionTags.Airborne | SCFMotionTags.Action | SCFMotionTags.Jump, false, 5.5f, Vector2.up);
            AddMotion(clips, "Humanoid Slide Roll", rollFallback, SCFMotionType.CombatRoll, SCFMotionTags.Grounded | SCFMotionTags.Action | SCFMotionTags.CombatRoll, false, 6.2f, Vector2.up);
            AddMotion(clips, "Humanoid Back Step Fallback", runBack, SCFMotionType.CombatRoll, SCFMotionTags.Grounded | SCFMotionTags.Action | SCFMotionTags.CombatRoll, false, 6.2f, Vector2.down);
            AddMotion(clips, "Humanoid Dodge Left Fallback", rollFallback, SCFMotionType.CombatRoll, SCFMotionTags.Grounded | SCFMotionTags.Action | SCFMotionTags.CombatRoll, false, 6.2f, Vector2.left);
            AddMotion(clips, "Humanoid Dodge Right Fallback", rollFallback, SCFMotionType.CombatRoll, SCFMotionTags.Grounded | SCFMotionTags.Action | SCFMotionTags.CombatRoll, false, 6.2f, Vector2.right);
            AddMotion(clips, "Humanoid Vault Slide", rollFallback, SCFMotionType.VaultSlide, SCFMotionTags.Grounded | SCFMotionTags.Action | SCFMotionTags.Vault | SCFMotionTags.VaultSlide, false, 6.8f, Vector2.up);
            AddMotion(clips, "Humanoid Wall Run Left Fallback", run, SCFMotionType.WallRun, SCFMotionTags.Airborne | SCFMotionTags.Action | SCFMotionTags.WallRun | SCFMotionTags.Loop, true, 7.4f, Vector2.left);
            AddMotion(clips, "Humanoid Wall Run Right Fallback", run, SCFMotionType.WallRun, SCFMotionTags.Airborne | SCFMotionTags.Action | SCFMotionTags.WallRun | SCFMotionTags.Loop, true, 7.4f, Vector2.right);
            AddMotion(clips, "Humanoid Vault Fence", vault, SCFMotionType.Vault, SCFMotionTags.Grounded | SCFMotionTags.Action | SCFMotionTags.Vault, false, 5.2f, Vector2.up);
            AddMotion(clips, "Humanoid Ledge Climb", climb, SCFMotionType.Climb, SCFMotionTags.Grounded | SCFMotionTags.Action | SCFMotionTags.Climb, false, 3.2f, Vector2.up);

            Undo.RecordObject(database, "Bake SCF humanoid motion database");
            database.SetBakedData(PoseInterval, TrajectoryTimes, clips.ToArray());
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            return database;
        }

        private static void AddMotion(
            List<SCFMotionClipData> clips,
            string id,
            AnimationClip clip,
            SCFMotionType motionType,
            SCFMotionTags tags,
            bool looping,
            float fallbackPlanarSpeed,
            Vector2 movementDirection,
            bool forceFallbackVelocity = false)
        {
            if (clip == null)
            {
                if (id != "Run Backward")
                {
                    Debug.LogWarning("SCF motion database skipped missing clip: " + id + ".");
                }

                return;
            }

            Vector3 averageVelocity = clip.averageSpeed;
            averageVelocity.y = 0f;
            float averagePlanarSpeed = averageVelocity.magnitude;
            if (forceFallbackVelocity || averagePlanarSpeed <= 0.05f)
            {
                averagePlanarSpeed = Mathf.Max(0f, fallbackPlanarSpeed);
                Vector2 direction = movementDirection.sqrMagnitude > 0.0001f ? movementDirection.normalized : Vector2.up;
                averageVelocity = new Vector3(direction.x, 0f, direction.y) * averagePlanarSpeed;
            }

            float duration = Mathf.Max(0.01f, clip.length);
            SCFMotionFrame[] frames = BuildFrames(duration, averageVelocity);
            clips.Add(new SCFMotionClipData(id, clip, motionType, tags, looping, duration, averagePlanarSpeed, averageVelocity, movementDirection, frames));
        }

        private static SCFMotionFrame[] BuildFrames(float duration, Vector3 averageVelocity)
        {
            int count = Mathf.Max(1, Mathf.CeilToInt(duration / PoseInterval) + 1);
            SCFMotionFrame[] frames = new SCFMotionFrame[count];

            for (int i = 0; i < count; i++)
            {
                float clipTime = Mathf.Min(duration, i * PoseInterval);
                float normalizedTime = duration > 0f ? Mathf.Clamp01(clipTime / duration) : 0f;
                SCFTrajectoryPoint[] trajectory = new SCFTrajectoryPoint[TrajectoryTimes.Length];
                for (int t = 0; t < TrajectoryTimes.Length; t++)
                {
                    float trajectoryTime = TrajectoryTimes[t];
                    trajectory[t] = new SCFTrajectoryPoint(trajectoryTime, averageVelocity * trajectoryTime, 0f);
                }

                frames[i] = new SCFMotionFrame(clipTime, normalizedTime, averageVelocity, trajectory);
            }

            return frames;
        }

        private static AnimationClip FindFirstClip(params string[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                AnimationClip clip = LoadClip(paths[i]);
                if (clip != null)
                {
                    return clip;
                }
            }

            return null;
        }

        private static AnimationClip LoadClip(string path)
        {
            AnimationClip directClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (directClip != null)
            {
                return directClip;
            }

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip clip && !clip.name.StartsWith("__preview", System.StringComparison.OrdinalIgnoreCase))
                {
                    return clip;
                }
            }

            return FindClipByName(path);
        }

        private static AnimationClip FindClipByName(string path)
        {
            string clipName = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrEmpty(clipName))
            {
                return null;
            }

            string[] guids = AssetDatabase.FindAssets(clipName + " t:AnimationClip", new[] { ParkourPlayerAnimationFolder, DynamicParkourAnimationFolder, DatabaseFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!string.Equals(Path.GetFileNameWithoutExtension(assetPath), clipName, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
                if (clip != null)
                {
                    return clip;
                }
            }

            return null;
        }

        private static AnimationClip FindClipInAsset(string assetPath, string clipName)
        {
            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(clipName))
            {
                return null;
            }

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip clip
                    && !clip.name.StartsWith("__preview", System.StringComparison.OrdinalIgnoreCase)
                    && ClipNameMatches(clip.name, clipName))
                {
                    return clip;
                }
            }

            return null;
        }

        private static bool ClipNameMatches(string actualName, string requestedName)
        {
            if (string.Equals(actualName, requestedName, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            int pipeIndex = actualName.LastIndexOf('|');
            if (pipeIndex >= 0 && pipeIndex < actualName.Length - 1)
            {
                string strippedName = actualName.Substring(pipeIndex + 1);
                return string.Equals(strippedName, requestedName, System.StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
#endif
