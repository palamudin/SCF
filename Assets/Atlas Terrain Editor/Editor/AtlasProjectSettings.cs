using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Unity {

    class AtlasProjectSettings : ScriptableObject {

        public const string path = "Assets/Atlas Terrain Editor/AtlasProjectSettings.asset";

        [Tooltip("Remove stamps from the build.\nAtlas won't work at runtime when this is enabled, but it will make the build size smaller by not including the textures assigned to the stamps.")]
        [SerializeField]
        public bool stripStampsFromBuild;

        internal static AtlasProjectSettings GetOrCreateSettings() {

            var settings = AssetDatabase.LoadAssetAtPath<AtlasProjectSettings>(path);

            if (settings == null) {

                settings = ScriptableObject.CreateInstance<AtlasProjectSettings>();
                settings.stripStampsFromBuild = false;
                AssetDatabase.CreateAsset(settings, path);
                AssetDatabase.SaveAssets();

            }

            return settings;

        }

        internal static SerializedObject GetSerializedSettings() {

            return new SerializedObject(GetOrCreateSettings());

        }

    }

    class AtlasProjectSettingsProvider : SettingsProvider {

        private SerializedObject settings;

        class Styles {
            public static GUIContent numbstripStampsFromBuilder = new GUIContent("Strip Stamps From Build");
        }

        const string path = "Assets/Atlas Terrain Editor/AtlasProjectSettings.asset";

        public AtlasProjectSettingsProvider(string path, SettingsScope scope = SettingsScope.User) : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement) {

            settings = AtlasProjectSettings.GetSerializedSettings();

        }

        public override void OnGUI(string searchContext) {

            EditorGUILayout.PropertyField(settings.FindProperty("stripStampsFromBuild"), Styles.numbstripStampsFromBuilder);

            settings.ApplyModifiedProperties();

        }

        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider() {

            var provider = new AtlasProjectSettingsProvider("Project/Atlas", SettingsScope.Project);

            provider.keywords = GetSearchKeywordsFromGUIContentProperties<Styles>();

            return provider;

        }
    }

}