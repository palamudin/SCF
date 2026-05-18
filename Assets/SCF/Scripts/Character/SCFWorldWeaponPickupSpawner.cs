using UnityEngine;

namespace SCF.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SCFWorldWeaponPickupSpawner : MonoBehaviour
    {
        private const string RuntimeWeaponCatalogPath = "SCF/SCFWeaponRuntimeCatalog";

        [SerializeField] private bool spawnRailgunPickup = true;
        [SerializeField] private string pickupObjectName = "SCF_RailgunPickup";
        [SerializeField] private GameObject railgunPrefab;
        [SerializeField] private Vector3 spawnOffset = new Vector3(3.2f, 0f, 2.4f);
        [SerializeField, Min(0f)] private float hoverHeight = 1.05f;
        [SerializeField] private bool showStand = true;
        [SerializeField] private bool consumeOnPickup = true;
        [SerializeField, Min(0f)] private float groundProbeHeight = 8f;
        [SerializeField, Min(0.1f)] private float groundProbeDistance = 30f;

        private bool hasSpawned;

        private void Start()
        {
            EnsurePickup();
        }

        public void Configure(GameObject defaultRailgunPrefab)
        {
            if (railgunPrefab == null)
            {
                railgunPrefab = defaultRailgunPrefab;
            }

            if (Application.isPlaying)
            {
                EnsurePickup();
            }
        }

        [ContextMenu("SCF/Spawn Railgun Pickup")]
        public void EnsurePickup()
        {
            if (!spawnRailgunPickup || hasSpawned || FindExistingPickup() != null)
            {
                return;
            }

            GameObject prefab = ResolveRailgunPrefab();
            if (prefab == null)
            {
                return;
            }

            Vector3 spawnPosition = ResolveSpawnPosition();
            SCFWeaponPickup pickup = SCFWeaponPickup.SpawnPickup(prefab, spawnPosition, Quaternion.identity, pickupObjectName, consumeOnPickup, showStand);
            if (pickup != null)
            {
                pickup.SetVisualLocalOffset(Vector3.up * hoverHeight);
                hasSpawned = true;
            }
        }

        private SCFWeaponPickup FindExistingPickup()
        {
            SCFWeaponPickup[] pickups = FindObjectsByType<SCFWeaponPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < pickups.Length; i++)
            {
                if (pickups[i] != null && string.Equals(pickups[i].name, pickupObjectName, System.StringComparison.Ordinal))
                {
                    return pickups[i];
                }
            }

            return null;
        }

        private GameObject ResolveRailgunPrefab()
        {
            if (railgunPrefab != null)
            {
                return railgunPrefab;
            }

            SCFWeaponRuntimeCatalog catalog = Resources.Load<SCFWeaponRuntimeCatalog>(RuntimeWeaponCatalogPath);
            if (catalog != null)
            {
                railgunPrefab = catalog.RailgunWeaponPrefab;
            }

            return railgunPrefab;
        }

        private Vector3 ResolveSpawnPosition()
        {
            Vector3 position = transform.position + spawnOffset;
            float groundY = SampleGroundY(position);
            return new Vector3(position.x, groundY, position.z);
        }

        private float SampleGroundY(Vector3 position)
        {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                return terrain.SampleHeight(position) + terrain.transform.position.y;
            }

            Vector3 rayOrigin = position + Vector3.up * groundProbeHeight;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundProbeDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.point.y;
            }

            return position.y;
        }
    }
}
