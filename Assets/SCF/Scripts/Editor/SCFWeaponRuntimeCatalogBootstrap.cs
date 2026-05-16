using System.IO;
using UnityEditor;
using UnityEngine;

namespace SCF.Gameplay.Editor
{
    [InitializeOnLoad]
    internal static class SCFWeaponRuntimeCatalogBootstrap
    {
        private const string CatalogFolder = "Assets/Resources/SCF";
        private const string ProfileFolder = "Assets/Resources/SCF/Weapons";
        private const string PrefabFolder = "Assets/SCF/Prefabs/Weapons";
        private const string CatalogPath = CatalogFolder + "/SCFWeaponRuntimeCatalog.asset";
        private const string RailgunProfilePath = ProfileFolder + "/SCF_RailgunRigProfile.asset";
        private const string RailgunPrefabPath = PrefabFolder + "/SCF_Railgun_Weapon_Fiddled Variant.prefab";
        private const string RailgunPath = "Assets/SCF/2.8 rail-gun prototype_Texture_Packed.blend";
        private const string RailgunFireClipPath = "Assets/SCF/Audio/kalsstockmedia-a-large-explosive-laser-gun-shot-scifi-410622.mp3";

        static SCFWeaponRuntimeCatalogBootstrap()
        {
            EditorApplication.delayCall += RebuildRuntimeCatalog;
        }

        [MenuItem("SCF/Weapons/Rebuild Runtime Catalog")]
        private static void RebuildRuntimeCatalogFromMenu()
        {
            RebuildRuntimeCatalog();
        }

        public static void RebuildRuntimeCatalog()
        {
            Directory.CreateDirectory(CatalogFolder);
            Directory.CreateDirectory(ProfileFolder);
            Directory.CreateDirectory(PrefabFolder);

            SCFWeaponRuntimeCatalog catalog = AssetDatabase.LoadAssetAtPath<SCFWeaponRuntimeCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<SCFWeaponRuntimeCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            GameObject railgun = AssetDatabase.LoadAssetAtPath<GameObject>(RailgunPath);
            GameObject railgunPrefab = EnsureRailgunWeaponPrefab(railgun);
            AudioClip fireClip = AssetDatabase.LoadAssetAtPath<AudioClip>(RailgunFireClipPath);
            SCFWeaponRigProfile railgunProfile = EnsureRailgunRigProfile(railgunPrefab, fireClip);

            SerializedObject serializedCatalog = new SerializedObject(catalog);
            serializedCatalog.FindProperty("railgunPrototype").objectReferenceValue = railgun;
            serializedCatalog.FindProperty("railgunWeaponPrefab").objectReferenceValue = railgunPrefab;
            serializedCatalog.FindProperty("railgunRigProfile").objectReferenceValue = railgunProfile;
            serializedCatalog.FindProperty("railgunFireClip").objectReferenceValue = fireClip;
            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        private static GameObject EnsureRailgunWeaponPrefab(GameObject railgunSource)
        {
            if (railgunSource == null)
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(RailgunPrefabPath);
            }

            bool loadedExistingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RailgunPrefabPath) != null;
            GameObject prefabRoot = loadedExistingPrefab
                ? PrefabUtility.LoadPrefabContents(RailgunPrefabPath)
                : new GameObject("SCF_Railgun_Weapon");

            try
            {
                EnsureModelChild(prefabRoot.transform, railgunSource);
                EnsureAnchor(prefabRoot.transform, "SCF_RightPistolGrip",
                    new Vector3(-0.15f, 0.03f, -0.01f),
                    Quaternion.Euler(-32.43002f, 86.09999f, -81.39999f));
                EnsureAnchor(prefabRoot.transform, "SCF_LeftUnderbarrelGrip",
                    new Vector3(0.57f, -0.04f, -0.01f),
                    Quaternion.Euler(0f, 115.46f, 147.62f));
                EnsureAnchor(prefabRoot.transform, "SCF_RailgunMuzzleTarget",
                    new Vector3(0.96f, 0.12f, 0f),
                    Quaternion.Euler(0f, 90f, 0f));
                EnsureAnchor(prefabRoot.transform, "SCF_RightElbowHint",
                    new Vector3(-2.93f, -1.6f, -0.42355f),
                    Quaternion.identity);
                EnsureAnchor(prefabRoot.transform, "SCF_LeftElbowHint",
                    new Vector3(0.37f, -0.23f, 0.17f),
                    Quaternion.identity);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, RailgunPrefabPath);
            }
            finally
            {
                if (loadedExistingPrefab)
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
                else
                {
                    Object.DestroyImmediate(prefabRoot);
                }
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(RailgunPrefabPath);
        }

        private static SCFWeaponRigProfile EnsureRailgunRigProfile(GameObject railgunPrefab, AudioClip fireClip)
        {
            SCFWeaponRigProfile profile = AssetDatabase.LoadAssetAtPath<SCFWeaponRigProfile>(RailgunProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<SCFWeaponRigProfile>();
                AssetDatabase.CreateAsset(profile, RailgunProfilePath);
            }

            SerializedObject serializedProfile = new SerializedObject(profile);
            serializedProfile.FindProperty("weaponId").stringValue = "railgun";
            serializedProfile.FindProperty("weaponPrefab").objectReferenceValue = railgunPrefab;
            serializedProfile.FindProperty("fireClip").objectReferenceValue = fireClip;
            serializedProfile.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void EnsureModelChild(Transform root, GameObject railgunSource)
        {
            Transform existing = root.Find("SCF_RailgunModel");
            if (existing != null)
            {
                return;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(railgunSource) as GameObject;
            if (instance == null)
            {
                instance = Object.Instantiate(railgunSource);
            }

            instance.name = "SCF_RailgunModel";
            instance.transform.SetParent(root, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
        }

        private static void EnsureAnchor(Transform root, string anchorName, Vector3 localPosition, Quaternion localRotation)
        {
            Transform anchor = root.Find(anchorName);
            if (anchor != null)
            {
                return;
            }

            anchor = new GameObject(anchorName).transform;
            anchor.SetParent(root, false);
            anchor.localPosition = localPosition;
            anchor.localRotation = localRotation;
            anchor.localScale = Vector3.one;
        }

        private static Vector3 SocketSpaceToWeaponLocal(Vector3 socketSpacePosition)
        {
            Vector3 weaponPosition = DefaultWeaponLocalPosition();
            Quaternion weaponRotation = Quaternion.Euler(-4.95f, -90.25f, 25.36f);
            float weaponScale = 0.8f;
            return Quaternion.Inverse(weaponRotation) * ((socketSpacePosition - weaponPosition) / weaponScale);
        }

        private static Quaternion SocketSpaceToWeaponLocalRotation(Vector3 socketSpaceEulerAngles)
        {
            Quaternion weaponRotation = Quaternion.Euler(-4.95f, -90.25f, 25.36f);
            return Quaternion.Inverse(weaponRotation) * Quaternion.Euler(socketSpaceEulerAngles);
        }

        private static Vector3 DefaultWeaponLocalPosition()
        {
            Vector3 rightGripSocket = new Vector3(0f, -0.08f, -0.14f);
            Vector3 rightFromAnchor = new Vector3(0.0332f, -0.2147f, -0.8692f) * 0.27f;
            Vector3 weaponAnchorSocket = rightGripSocket - rightFromAnchor;
            return new Vector3(-0.058564f, 0.051804f, 0.081334f) - weaponAnchorSocket;
        }
    }
}
