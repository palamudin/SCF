using UnityEngine;

namespace SCF.Gameplay
{
    public sealed class SCFWeaponRuntimeCatalog : ScriptableObject
    {
        [SerializeField] private GameObject railgunPrototype;
        [SerializeField] private AudioClip railgunFireClip;

        public GameObject RailgunPrototype => railgunPrototype;
        public AudioClip RailgunFireClip => railgunFireClip;
    }
}
