#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Builder;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SCF.EditorTools
{
    public static class SCFDynamicBuildingSceneSetup
    {
        private const string DistrictRootName = "SCF_DynamicBuildingDistrict";
        private const string GeneratorRootName = "_SCF_DynamicBuildingGenerators";
        private const string MobilityRootName = "SCF_MobilityTestBuildings";
        private const string BuildingAssetRoot = "Assets/RedNotRed/AdvancedProceduralBuildingGenerator";
        private const string ScenePath = "Assets/Realistic Terrain Collection/Terrains/High Resolution Terrain Scene.unity";

        [MenuItem("SCF/Setup/Add Dynamic Building District")]
        public static void AddDynamicBuildingDistrict()
        {
            AddDynamicBuildingDistrict(true);
        }

        public static GameObject AddDynamicBuildingDistrict(bool saveScene)
        {
            EnsureUsableScene();
            RepairGeneratorMaterials();
            RealignExistingMobilityBlocks();

            GameObject oldDistrict = GameObject.Find(DistrictRootName);
            if (oldDistrict != null)
            {
                Undo.DestroyObjectImmediate(oldDistrict);
            }

            GameObject root = new GameObject(DistrictRootName);
            Undo.RegisterCreatedObjectUndo(root, "Create SCF dynamic building district");

            GameObject generatorRoot = new GameObject(GeneratorRootName);
            generatorRoot.transform.SetParent(root.transform, false);
            generatorRoot.hideFlags = HideFlags.HideInHierarchy;

            DynamicBuildingAssets assets = LoadAssets();
            Vector3 center = ResolveDistrictCenter();

            BuildingSpec[] specs =
            {
                new BuildingSpec("SCF_Dyn_Warehouse_West", new Vector2(-34f, -22f), new Vector2Int(18, 14), new [] { 4, 4 }, 0f, assets.Concrete),
                new BuildingSpec("SCF_Dyn_Office_North", new Vector2(30f, 26f), new Vector2Int(12, 12), new [] { 4, 3, 3, 3 }, 90f, assets.Bricks),
                new BuildingSpec("SCF_Dyn_Lab_East", new Vector2(36f, -18f), new Vector2Int(22, 12), new [] { 4, 4 }, 0f, assets.Concrete),
                new BuildingSpec("SCF_Dyn_Tower_South", new Vector2(-28f, 30f), new Vector2Int(10, 10), new [] { 4, 3, 3, 3, 3 }, 45f, assets.Bricks),
                new BuildingSpec("SCF_Dyn_Rooftop_Run_Block", new Vector2(4f, 54f), new Vector2Int(18, 16), new [] { 4, 4, 4 }, -20f, assets.Concrete)
            };

            foreach (BuildingSpec spec in specs)
            {
                CreateBuilding(root.transform, generatorRoot.transform, assets, spec, center);
            }

            CreateTraversalPads(root.transform, assets.Road, center);
            FixRenderersUnder(root.transform, assets.Fallback);

            if (saveScene)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
            }

            Selection.activeGameObject = root;
            Debug.Log("SCF dynamic building district generated and snapped to the current terrain.");
            return root;
        }

        private static void EnsureUsableScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.isLoaded)
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
        }

        private static DynamicBuildingAssets LoadAssets()
        {
            DynamicBuildingAssets assets = new DynamicBuildingAssets
            {
                Concrete = Load<Material>(BuildingAssetRoot + "/Materials/BuildingMaterials/Concrete.mat"),
                Bricks = Load<Material>(BuildingAssetRoot + "/Materials/BuildingMaterials/Bricks.mat"),
                Transparent = Load<Material>(BuildingAssetRoot + "/Materials/BuildingMaterials/Transparent.mat"),
                Road = Load<Material>(BuildingAssetRoot + "/Materials/Road/Road.mat"),
                Window = Load<GameObject>(BuildingAssetRoot + "/Prefabs/Window/Window.prefab"),
                Column = Load<GameObject>(BuildingAssetRoot + "/Prefabs/Column/Column.prefab"),
                Roof = Load<GameObject>(BuildingAssetRoot + "/Prefabs/Roof/Roof.prefab")
            };

            assets.WindowRooms = new[]
            {
                Load<Material>(BuildingAssetRoot + "/Materials/BuildingMaterials/Windows/Window.mat"),
                Load<Material>(BuildingAssetRoot + "/Materials/BuildingMaterials/Windows/Window 1.mat"),
                Load<Material>(BuildingAssetRoot + "/Materials/BuildingMaterials/Windows/Window 2.mat"),
                Load<Material>(BuildingAssetRoot + "/Materials/BuildingMaterials/Windows/Window 3.mat"),
                Load<Material>(BuildingAssetRoot + "/Materials/BuildingMaterials/Windows/Window 4.mat")
            };

            assets.RoofMods = new[]
            {
                Load<GameObject>(BuildingAssetRoot + "/Prefabs/Roof Mods/Cabinet/Cabinet.prefab"),
                Load<GameObject>(BuildingAssetRoot + "/Prefabs/Roof Mods/Chimney/Chimney.prefab"),
                Load<GameObject>(BuildingAssetRoot + "/Prefabs/Roof Mods/Ventilation1/Ventilation1.prefab"),
                Load<GameObject>(BuildingAssetRoot + "/Prefabs/Roof Mods/Ventilation3/Ventilation3.prefab"),
                Load<GameObject>(BuildingAssetRoot + "/Prefabs/Roof Mods/Hut1/Hut1.prefab")
            };

            assets.Fallback = assets.Concrete ?? EnsureFallbackMaterial();
            if (assets.Concrete == null)
            {
                assets.Concrete = assets.Fallback;
            }

            if (assets.Bricks == null)
            {
                assets.Bricks = assets.Fallback;
            }

            if (assets.Transparent == null)
            {
                assets.Transparent = assets.Fallback;
            }

            if (assets.Road == null)
            {
                assets.Road = assets.Fallback;
            }

            return assets;
        }

        private static void CreateBuilding(Transform root, Transform generatorRoot, DynamicBuildingAssets assets, BuildingSpec spec, Vector3 center)
        {
            GameObject generatorObject = new GameObject("_Generator_" + spec.Name);
            generatorObject.transform.SetParent(generatorRoot, false);
            generatorObject.hideFlags = HideFlags.HideInHierarchy;

            GenerateBuilding generator = generatorObject.AddComponent<GenerateBuilding>();
            generator.Name = spec.Name;
            generator.AutoSize = true;
            generator.Interior = false;
            generator.CreateLOD = GenerateBuilding.lod.None;
            generator.WindowModel = assets.Window;
            generator.Column = assets.Column;
            generator.CentralColumn = assets.Column;
            generator.Roof = spec.UseRoof ? assets.Roof : null;
            generator.RoofMods = FilterNulls(assets.RoofMods);
            generator.BuildingMaterial = spec.Material != null ? spec.Material : assets.Fallback;
            generator.TransparentMaterial = assets.Transparent;
            generator.Rooms = FilterNulls(assets.WindowRooms);
            generator.Floors = BuildFloors(spec, generator.Rooms);

            Vector3 position = center + new Vector3(spec.Offset.x, 0f, spec.Offset.y);
            position.y = SampleGroundY(position);

            IEnumerator routine = generator.CreateBuilding(position);
            int steps = 0;
            while (routine.MoveNext())
            {
                steps++;
                if (steps > 25000)
                {
                    Debug.LogWarning("Stopped dynamic building generation early for " + spec.Name + " after too many editor steps.");
                    break;
                }
            }

            if (generator.Building != null)
            {
                Transform building = generator.Building;
                building.name = spec.Name;
                building.SetParent(root, true);
                building.rotation = Quaternion.Euler(0f, spec.RotationY, 0f);
                SnapRendererBaseToGround(building);
                MarkStatic(building);
            }
        }

        private static List<GenerateBuilding.Floor> BuildFloors(BuildingSpec spec, Material[] roomMaterials)
        {
            List<GenerateBuilding.Floor> floors = new List<GenerateBuilding.Floor>();
            foreach (int height in spec.FloorHeights)
            {
                floors.Add(new GenerateBuilding.Floor
                {
                    width = spec.Size.x,
                    height = height,
                    lenght = spec.Size.y,
                    WindowsX = Mathf.Max(2, spec.Size.x / 4),
                    WindowsY = 1,
                    WindowsZ = Mathf.Max(2, spec.Size.y / 4),
                    WindowsOffset = 0.08f,
                    NordWindow = true,
                    SouthWindow = true,
                    EastWindow = true,
                    WestWindow = true,
                    NordColumn = true,
                    SouthColumn = true,
                    EastColumn = true,
                    WestColumn = true,
                    Deform = false,
                    WindowMaterials = new List<Material>(roomMaterials)
                });
            }

            return floors;
        }

        private static void CreateTraversalPads(Transform root, Material roadMaterial, Vector3 center)
        {
            CreatePad(root, "SCF_Dyn_Street_Cross_NS", center + new Vector3(0f, 0f, 4f), new Vector3(8f, 0.18f, 94f), roadMaterial);
            CreatePad(root, "SCF_Dyn_Street_Cross_EW", center + new Vector3(0f, 0f, 4f), new Vector3(94f, 0.18f, 8f), roadMaterial);
            CreatePad(root, "SCF_Dyn_Wallrun_Practice_Strip", center + new Vector3(18f, 0f, 51f), new Vector3(28f, 0.2f, 5f), roadMaterial);
        }

        private static void CreatePad(Transform parent, string name, Vector3 center, Vector3 size, Material material)
        {
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(pad, "Create SCF district pad");
            pad.name = name;
            pad.transform.SetParent(parent, true);

            float groundY = SampleGroundY(center);
            pad.transform.position = new Vector3(center.x, groundY + size.y * 0.5f + 0.03f, center.z);
            pad.transform.localScale = size;

            Renderer renderer = pad.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            MarkStatic(pad.transform);
        }

        private static void RealignExistingMobilityBlocks()
        {
            GameObject root = GameObject.Find(MobilityRootName);
            if (root == null)
            {
                return;
            }

            foreach (Transform child in root.transform)
            {
                SnapRendererBaseToGround(child);
            }
        }

        private static void SnapRendererBaseToGround(Transform target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float groundY = SampleGroundY(target.position);
            target.position += Vector3.up * (groundY - bounds.min.y);
        }

        private static float SampleGroundY(Vector3 position)
        {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                return terrain.SampleHeight(position) + terrain.transform.position.y;
            }

            Vector3 rayOrigin = position + Vector3.up * 600f;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 1200f, ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.point.y;
            }

            return position.y;
        }

        private static Vector3 ResolveDistrictCenter()
        {
            GameObject player = GameObject.Find("SCF_Player");
            if (player != null)
            {
                return player.transform.position;
            }

            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null && terrain.terrainData != null)
            {
                Vector3 size = terrain.terrainData.size;
                return terrain.transform.position + new Vector3(size.x * 0.5f, 0f, size.z * 0.5f);
            }

            return Vector3.zero;
        }

        private static void FixRenderersUnder(Transform root, Material fallback)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (NeedsRepair(materials[i]))
                    {
                        materials[i] = fallback;
                        changed = true;
                    }
                }

                if (changed)
                {
                    renderer.sharedMaterials = materials;
                }
            }
        }

        private static void RepairGeneratorMaterials()
        {
            string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { BuildingAssetRoot });
            foreach (string guid in materialGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!NeedsRepair(material))
                {
                    continue;
                }

                Color color = material != null && material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
                Texture texture = material != null && material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
                Shader shader = Shader.Find("HDRP/Lit") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (material == null || shader == null)
                {
                    continue;
                }

                material.shader = shader;
                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", color);
                }
                else if (material.HasProperty("_Color"))
                {
                    material.SetColor("_Color", color);
                }

                if (texture != null)
                {
                    if (material.HasProperty("_BaseColorMap"))
                    {
                        material.SetTexture("_BaseColorMap", texture);
                    }
                    else if (material.HasProperty("_MainTex"))
                    {
                        material.SetTexture("_MainTex", texture);
                    }
                }

                EditorUtility.SetDirty(material);
            }

            AssetDatabase.SaveAssets();
        }

        private static bool NeedsRepair(Material material)
        {
            if (material == null || material.shader == null)
            {
                return true;
            }

            string shaderName = material.shader.name;
            return shaderName == "Hidden/InternalErrorShader" ||
                   shaderName == "Standard" ||
                   shaderName.StartsWith("Legacy Shaders/");
        }

        private static void MarkStatic(Transform root)
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                GameObjectUtility.SetStaticEditorFlags(transform.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
            }
        }

        private static Material EnsureFallbackMaterial()
        {
            const string folder = "Assets/SCF/Materials";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/SCF", "Materials");
            }

            const string path = folder + "/SCF_DynamicBuilding_Fallback_HDRP.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("HDRP/Lit") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader);
            material.name = "SCF_DynamicBuilding_Fallback_HDRP";
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", new Color(0.38f, 0.4f, 0.38f, 1f));
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", new Color(0.38f, 0.4f, 0.38f, 1f));
            }

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static T Load<T>(string path) where T : Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static T[] FilterNulls<T>(IEnumerable<T> values) where T : Object
        {
            List<T> filtered = new List<T>();
            foreach (T value in values)
            {
                if (value != null)
                {
                    filtered.Add(value);
                }
            }

            return filtered.ToArray();
        }

        private sealed class DynamicBuildingAssets
        {
            public Material Concrete;
            public Material Bricks;
            public Material Transparent;
            public Material Road;
            public Material Fallback;
            public Material[] WindowRooms;
            public GameObject Window;
            public GameObject Column;
            public GameObject Roof;
            public GameObject[] RoofMods;
        }

        private readonly struct BuildingSpec
        {
            public readonly string Name;
            public readonly Vector2 Offset;
            public readonly Vector2Int Size;
            public readonly int[] FloorHeights;
            public readonly float RotationY;
            public readonly Material Material;
            public readonly bool UseRoof;

            public BuildingSpec(string name, Vector2 offset, Vector2Int size, int[] floorHeights, float rotationY, Material material, bool useRoof = false)
            {
                Name = name;
                Offset = offset;
                Size = size;
                FloorHeights = floorHeights;
                RotationY = rotationY;
                Material = material;
                UseRoof = useRoof;
            }
        }
    }
}
#endif
