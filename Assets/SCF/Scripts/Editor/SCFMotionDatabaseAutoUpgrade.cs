#if UNITY_EDITOR
using SCF.Gameplay;
using UnityEditor;
using UnityEngine;

namespace SCF.EditorTools
{
    [InitializeOnLoad]
    public static class SCFMotionDatabaseAutoUpgrade
    {
        private const string SessionKey = "SCF.MotionDatabaseAutoUpgrade.AimLocomotion";

        static SCFMotionDatabaseAutoUpgrade()
        {
            EditorApplication.delayCall += UpgradeOncePerEditorSession;
        }

        private static void UpgradeOncePerEditorSession()
        {
            if (SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            SessionState.SetBool(SessionKey, true);

            SCFMotionDatabase humanoidDatabase = AssetDatabase.LoadAssetAtPath<SCFMotionDatabase>(SCFMotionDatabaseBaker.HumanoidDatabasePath);
            if (HasAimLocomotion(humanoidDatabase))
            {
                return;
            }

            SCFMotionDatabaseBaker.BuildHumanoidDatabase();
            Debug.Log("SCF humanoid motion database upgraded with dedicated aim locomotion clips.");
        }

        private static bool HasAimLocomotion(SCFMotionDatabase database)
        {
            if (database == null || database.Clips == null)
            {
                return false;
            }

            bool hasWalk = false;
            bool hasRun = false;
            SCFMotionClipData[] clips = database.Clips;
            for (int i = 0; i < clips.Length; i++)
            {
                SCFMotionClipData clip = clips[i];
                if (clip == null || !clip.IsValid || !clip.HasTag(SCFMotionTags.Aim))
                {
                    continue;
                }

                hasWalk |= clip.HasTag(SCFMotionTags.AimWalk);
                hasRun |= clip.HasTag(SCFMotionTags.AimRun);
                if (hasWalk && hasRun)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif
