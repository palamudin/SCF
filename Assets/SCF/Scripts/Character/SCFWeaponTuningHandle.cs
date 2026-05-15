using UnityEngine;

namespace SCF.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SCFWeaponTuningHandle : MonoBehaviour
    {
        [SerializeField] private SCFWeaponVisualSlot owner;

        public void Configure(SCFWeaponVisualSlot weaponSlot)
        {
            owner = weaponSlot;
        }

        [ContextMenu("SCF/Capture Railgun Tuning")]
        private void CaptureRailgunTuning()
        {
            if (owner != null)
            {
                owner.CaptureCurrentRailgunTuning();
            }
        }

        [ContextMenu("SCF/Copy Railgun Tuning")]
        private void CopyRailgunTuning()
        {
            if (owner != null)
            {
                owner.CopyCurrentRailgunTuning();
            }
        }

        [ContextMenu("SCF/Apply Saved Grip Targets")]
        private void ApplySavedGripTargets()
        {
            if (owner != null)
            {
                owner.ApplySavedRailgunGripTargets();
            }
        }
    }
}
