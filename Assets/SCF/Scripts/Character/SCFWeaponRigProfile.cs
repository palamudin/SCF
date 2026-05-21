using System;
using UnityEngine;

namespace SCF.Gameplay
{
    [CreateAssetMenu(fileName = "SCFWeaponRigProfile", menuName = "SCF/Weapons/Weapon Rig Profile")]
    public sealed class SCFWeaponRigProfile : ScriptableObject
    {
        [SerializeField] private string weaponId = "railgun";
        [SerializeField] private GameObject weaponPrefab;
        [SerializeField] private AudioClip fireClip;
        [SerializeField] private SCFWeaponAnchorNames anchorNames = SCFWeaponAnchorNames.Default();
        [SerializeField] private SCFWeaponCarryWeights carryWeights = SCFWeaponCarryWeights.Default();
        [SerializeField] private SCFWeaponAnatomyLimits anatomyLimits = SCFWeaponAnatomyLimits.Default();

        public string WeaponId => weaponId;
        public GameObject WeaponPrefab => weaponPrefab;
        public AudioClip FireClip => fireClip;
        public SCFWeaponAnchorNames AnchorNames => anchorNames;
        public SCFWeaponCarryWeights CarryWeights => carryWeights;
        public SCFWeaponAnatomyLimits AnatomyLimits => anatomyLimits;
    }

    [Serializable]
    public struct SCFWeaponAnchorNames
    {
        public string rightGrip;
        public string leftGrip;
        public string muzzle;
        public string buttstock;

        public static SCFWeaponAnchorNames Default()
        {
            return new SCFWeaponAnchorNames
            {
                rightGrip = "SCF_RightPistolGrip",
                leftGrip = "SCF_LeftUnderbarrelGrip",
                muzzle = "SCF_RailgunMuzzleTarget",
                buttstock = "SCF_RailgunButtstock"
            };
        }
    }

    [Serializable]
    public struct SCFWeaponCarryWeights
    {
        [Range(0f, 1f)] public float locomotion;
        [Range(0f, 1f)] public float aim;
        [Range(0f, 1f)] public float sprint;
        [Range(0f, 1f)] public float airborne;
        [Range(0f, 1f)] public float traversal;
        [Range(0f, 1f)] public float wallRun;
        [Range(0f, 1f)] public float roll;

        public static SCFWeaponCarryWeights Default()
        {
            return new SCFWeaponCarryWeights
            {
                locomotion = 0.95f,
                aim = 1f,
                sprint = 0.86f,
                airborne = 0.72f,
                traversal = 0.5f,
                wallRun = 0.58f,
                roll = 0.18f
            };
        }
    }

    [Serializable]
    public struct SCFWeaponAnatomyLimits
    {
        [Range(0.5f, 1.05f)] public float maxReachMultiplier;
        [Range(0f, 0.5f)] public float reachSoftZone;
        [Range(0f, 1f)] public float unreachableRotationWeight;

        public static SCFWeaponAnatomyLimits Default()
        {
            return new SCFWeaponAnatomyLimits
            {
                maxReachMultiplier = 0.94f,
                reachSoftZone = 0.16f,
                unreachableRotationWeight = 0.35f
            };
        }
    }
}
