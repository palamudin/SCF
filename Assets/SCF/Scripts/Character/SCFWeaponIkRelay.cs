using UnityEngine;

namespace SCF.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SCFWeaponIkRelay : MonoBehaviour
    {
        [SerializeField] private SCFWeaponVisualSlot owner;
        [SerializeField] private Animator sourceAnimator;

        private void Awake()
        {
            sourceAnimator = GetComponent<Animator>();
        }

        public void Configure(SCFWeaponVisualSlot weaponSlot)
        {
            owner = weaponSlot;
            if (sourceAnimator == null)
            {
                sourceAnimator = GetComponent<Animator>();
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (owner != null)
            {
                owner.ApplyAnimatorIK(layerIndex, sourceAnimator);
            }
        }
    }
}
