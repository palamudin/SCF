using UnityEngine;

namespace SCF.Gameplay
{
    public sealed class SCFWeaponRuntimeCatalog : ScriptableObject
    {
        [SerializeField] private GameObject railgunPrototype;
        [SerializeField] private GameObject railgunWeaponPrefab;
        [SerializeField] private SCFWeaponRigProfile railgunRigProfile;
        [SerializeField] private AudioClip railgunFireClip;

        public GameObject RailgunPrototype => railgunPrototype;
        public GameObject RailgunWeaponPrefab => railgunWeaponPrefab != null ? railgunWeaponPrefab : railgunPrototype;
        public SCFWeaponRigProfile RailgunRigProfile => railgunRigProfile;
        public AudioClip RailgunFireClip => railgunFireClip;
    }
}
