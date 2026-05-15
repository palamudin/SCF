using System.IO;
using UnityEditor;
using UnityEngine;

namespace SCF.Gameplay.Editor
{
    [InitializeOnLoad]
    internal static class SCFWeaponRuntimeCatalogBootstrap
    {
        private const string CatalogFolder = "Assets/Resources/SCF";
        private const string CatalogPath = CatalogFolder + "/SCFWeaponRuntimeCatalog.asset";
        private const string RailgunPath = "Assets/SCF/2.8 rail-gun prototype_Texture_Packed.blend";
        private const string RailgunFireClipPath = "Assets/SCF/Audio/kalsstockmedia-a-large-explosive-laser-gun-shot-scifi-410622.mp3";

        static SCFWeaponRuntimeCatalogBootstrap()
        {
            EditorApplication.delayCall += EnsureCatalog;
        }

        [MenuItem("SCF/Weapons/Rebuild Runtime Catalog")]
        private static void EnsureCatalog()
        {
            Directory.CreateDirectory(CatalogFolder);

            SCFWeaponRuntimeCatalog catalog = AssetDatabase.LoadAssetAtPath<SCFWeaponRuntimeCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<SCFWeaponRuntimeCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            GameObject railgun = AssetDatabase.LoadAssetAtPath<GameObject>(RailgunPath);
            AudioClip fireClip = AssetDatabase.LoadAssetAtPath<AudioClip>(RailgunFireClipPath);

            SerializedObject serializedCatalog = new SerializedObject(catalog);
            serializedCatalog.FindProperty("railgunPrototype").objectReferenceValue = railgun;
            serializedCatalog.FindProperty("railgunFireClip").objectReferenceValue = fireClip;
            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }
    }
}
