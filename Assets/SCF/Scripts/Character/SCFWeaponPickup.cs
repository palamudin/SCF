using UnityEngine;

namespace SCF.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SCFWeaponPickup : MonoBehaviour
    {
        [SerializeField] private GameObject weaponPrefab;
        [SerializeField] private string displayName = "Railgun";
        [SerializeField] private bool consumeOnPickup = true;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Vector3 visualLocalOffset = new Vector3(0f, 1.05f, 0f);
        [SerializeField, Min(0.1f)] private float pickupRadius = 1.15f;
        [SerializeField] private bool bobVisual = true;
        [SerializeField, Min(0f)] private float bobAmplitude = 0.16f;
        [SerializeField, Min(0.01f)] private float bobFrequency = 1.2f;
        [SerializeField] private bool spinVisual = true;
        [SerializeField] private float spinDegreesPerSecond = 58f;
        [SerializeField] private bool createStand;
        [SerializeField] private Vector3 standLocalOffset = new Vector3(0f, 0.42f, 0f);
        [SerializeField] private Vector3 standLocalScale = new Vector3(0.35f, 0.42f, 0.35f);

        private Vector3 visualBaseLocalPosition;
        private SCFWeaponVisualSlot ignoredPicker;
        private float ignorePickerUntilTime;

        public GameObject WeaponPrefab => weaponPrefab;
        public string DisplayName => displayName;

        private void Awake()
        {
            EnsureRuntimeState();
        }

        private void OnValidate()
        {
            pickupRadius = Mathf.Max(0.1f, pickupRadius);
            bobFrequency = Mathf.Max(0.01f, bobFrequency);
        }

        private void Update()
        {
            if (visualRoot == null)
            {
                return;
            }

            if (bobVisual)
            {
                float bob = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
                visualRoot.localPosition = visualBaseLocalPosition + Vector3.up * bob;
            }

            if (spinVisual && Mathf.Abs(spinDegreesPerSecond) > 0.001f)
            {
                visualRoot.Rotate(Vector3.up, spinDegreesPerSecond * Time.deltaTime, Space.World);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TryPickup(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryPickup(other);
        }

        public void Configure(GameObject prefab, string pickupName, bool consume, bool showStand)
        {
            weaponPrefab = prefab;
            displayName = !string.IsNullOrWhiteSpace(pickupName)
                ? pickupName
                : (prefab != null ? prefab.name : displayName);
            consumeOnPickup = consume;
            createStand = showStand;
            EnsureRuntimeState();
        }

        public void IgnorePicker(SCFWeaponVisualSlot picker, float seconds)
        {
            ignoredPicker = picker;
            ignorePickerUntilTime = Time.time + Mathf.Max(0f, seconds);
        }

        public static SCFWeaponPickup SpawnPickup(
            GameObject prefab,
            Vector3 worldPosition,
            Quaternion worldRotation,
            string pickupName,
            bool consume,
            bool showStand)
        {
            if (prefab == null)
            {
                return null;
            }

            GameObject pickupObject = new GameObject(string.IsNullOrWhiteSpace(pickupName) ? "SCF_WeaponPickup" : pickupName);
            pickupObject.transform.position = worldPosition;
            pickupObject.transform.rotation = worldRotation;
            SCFWeaponPickup pickup = pickupObject.AddComponent<SCFWeaponPickup>();
            pickup.Configure(prefab, prefab.name, consume, showStand);
            return pickup;
        }

        private void EnsureRuntimeState()
        {
            EnsurePhysics();
            EnsureVisual();
            EnsureStand();
        }

        private void EnsurePhysics()
        {
            SphereCollider trigger = GetComponent<SphereCollider>();
            if (trigger == null)
            {
                trigger = gameObject.AddComponent<SphereCollider>();
            }

            trigger.isTrigger = true;
            trigger.radius = pickupRadius;
            trigger.center = Vector3.up * Mathf.Max(0.5f, visualLocalOffset.y * 0.5f);

            Rigidbody body = GetComponent<Rigidbody>();
            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody>();
            }

            body.isKinematic = true;
            body.useGravity = false;
        }

        private void EnsureVisual()
        {
            if (visualRoot == null)
            {
                Transform existing = transform.Find("SCF_WeaponPickupVisual");
                visualRoot = existing;
            }

            if (visualRoot == null && weaponPrefab != null)
            {
                GameObject visual = Instantiate(weaponPrefab, transform);
                visual.name = "SCF_WeaponPickupVisual";
                visualRoot = visual.transform;
                RemoveVisualColliders(visual);
                RemoveTuningHandles(visual);
            }

            if (visualRoot == null)
            {
                return;
            }

            visualRoot.localPosition = visualLocalOffset;
            visualBaseLocalPosition = visualRoot.localPosition;
        }

        public void SetVisualLocalOffset(Vector3 localOffset)
        {
            visualLocalOffset = localOffset;
            if (visualRoot != null)
            {
                visualRoot.localPosition = visualLocalOffset;
                visualBaseLocalPosition = visualRoot.localPosition;
            }

            EnsurePhysics();
        }

        private void EnsureStand()
        {
            Transform existing = transform.Find("SCF_WeaponPickupStand");
            if (!createStand)
            {
                if (existing != null)
                {
                    DestroyUnityObject(existing.gameObject);
                }

                return;
            }

            if (existing != null)
            {
                return;
            }

            GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stand.name = "SCF_WeaponPickupStand";
            stand.transform.SetParent(transform, false);
            stand.transform.localPosition = standLocalOffset;
            stand.transform.localRotation = Quaternion.identity;
            stand.transform.localScale = standLocalScale;
            Collider standCollider = stand.GetComponent<Collider>();
            if (standCollider != null)
            {
                DestroyUnityObject(standCollider);
            }
        }

        private void TryPickup(Collider other)
        {
            if (weaponPrefab == null || other == null)
            {
                return;
            }

            SCFWeaponVisualSlot picker = other.GetComponentInParent<SCFWeaponVisualSlot>();
            if (picker == null || (picker == ignoredPicker && Time.time < ignorePickerUntilTime))
            {
                return;
            }

            picker.EquipWeaponPrefab(weaponPrefab);
            if (consumeOnPickup)
            {
                DestroyUnityObject(gameObject);
            }
        }

        private static void RemoveVisualColliders(GameObject root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                DestroyUnityObject(colliders[i]);
            }
        }

        private static void RemoveTuningHandles(GameObject root)
        {
            SCFWeaponTuningHandle[] handles = root.GetComponentsInChildren<SCFWeaponTuningHandle>(true);
            for (int i = 0; i < handles.Length; i++)
            {
                DestroyUnityObject(handles[i]);
            }
        }

        private static void DestroyUnityObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
