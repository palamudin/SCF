#if UNITY_EDITOR
using SCF.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SCF.EditorTools
{
    public static class SCFMobilityTestSceneSetup
    {
        private const string TestRootName = "SCF_MobilityTestBuildings";
        private const string MaterialsFolder = "Assets/SCF/Materials";
        private const string FrankModelPath = "Assets/RedNotRed/3D Adaptive Parkour System/Player/Frank.fbx";

        [MenuItem("SCF/Setup/Add Mobility Test Buildings")]
        public static void AddMobilityTestBuildings()
        {
            AddMobilityTestBuildings(true);
        }

        public static GameObject AddMobilityTestBuildings(bool saveScene)
        {
            GameObject oldRoot = GameObject.Find(TestRootName);
            if (oldRoot != null)
            {
                Undo.DestroyObjectImmediate(oldRoot);
            }

            GameObject root = new GameObject(TestRootName);
            Undo.RegisterCreatedObjectUndo(root, "Create SCF mobility test buildings");

            GameObject player = GameObject.Find("SCF_Player");
            Vector3 origin = player != null ? player.transform.position : Vector3.zero;
            float scale = ResolvePlayerScale(player);

            Material buildingMaterial = EnsureMaterial("SCF_TestBuilding_HDRP", new Color(0.26f, 0.29f, 0.31f, 1f));
            Material wallRunMaterial = EnsureMaterial("SCF_WallRunSurface_HDRP", new Color(0.16f, 0.34f, 0.48f, 1f));

            CreateBlock(root.transform, "WallRun_East_Face", origin + new Vector3(12f, 0f, 0f) * scale, new Vector3(1.8f, 5.8f, 18f) * scale, wallRunMaterial);
            CreateBlock(root.transform, "WallRun_West_Face", origin + new Vector3(-12f, 0f, 0f) * scale, new Vector3(1.8f, 5.8f, 18f) * scale, wallRunMaterial);
            CreateBlock(root.transform, "WallRun_North_Face", origin + new Vector3(0f, 0f, 17f) * scale, new Vector3(20f, 5.8f, 1.8f) * scale, wallRunMaterial);

            CreateBlock(root.transform, "Building_Block_A", origin + new Vector3(22f, 0f, 18f) * scale, new Vector3(8f, 7f, 8f) * scale, buildingMaterial);
            CreateBlock(root.transform, "Building_Block_B", origin + new Vector3(-23f, 0f, 15f) * scale, new Vector3(10f, 6f, 7f) * scale, buildingMaterial);
            CreateBlock(root.transform, "Building_Block_C", origin + new Vector3(21f, 0f, -18f) * scale, new Vector3(7f, 8f, 11f) * scale, buildingMaterial);

            CreateFrankHeightObstacles(root.transform, origin, ResolveFrankReferenceHeight(player));

            if (saveScene)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
            }

            Selection.activeGameObject = root;
            Debug.Log("SCF mobility test buildings added around the player for wall-run and scale testing.");
            return root;
        }

        private static void CreateFrankHeightObstacles(Transform parent, Vector3 origin, float frankHeight)
        {
            float height = Mathf.Clamp(frankHeight, 1.2f, 2.4f);
            Material hipMaterial = EnsureMaterial("SCF_Obstacle_Hip_HDRP", new Color(0.28f, 0.42f, 0.30f, 1f));
            Material chestMaterial = EnsureMaterial("SCF_Obstacle_Chest_HDRP", new Color(0.46f, 0.38f, 0.22f, 1f));
            Material shoulderMaterial = EnsureMaterial("SCF_Obstacle_Shoulder_HDRP", new Color(0.43f, 0.28f, 0.42f, 1f));
            Material headMaterial = EnsureMaterial("SCF_Obstacle_Head_HDRP", new Color(0.48f, 0.25f, 0.25f, 1f));

            CreateObstacle(parent, "Frank_Hip_Height_Obstacle", origin + new Vector3(-4.5f, 0f, 7f), height * 0.52f, hipMaterial);
            CreateObstacle(parent, "Frank_Chest_Height_Obstacle", origin + new Vector3(-1.5f, 0f, 7f), height * 0.72f, chestMaterial);
            CreateObstacle(parent, "Frank_Shoulder_Height_Obstacle", origin + new Vector3(1.5f, 0f, 7f), height * 0.86f, shoulderMaterial);
            CreateObstacle(parent, "Frank_Head_Height_Obstacle", origin + new Vector3(4.5f, 0f, 7f), height, headMaterial);
        }

        private static void CreateObstacle(Transform parent, string name, Vector3 center, float height, Material material)
        {
            Vector3 size = new Vector3(2.1f, Mathf.Max(0.1f, height), 1.1f);
            CreateBlock(parent, name + "_" + height.ToString("0.00") + "m", center, size, material);
        }

        private static void CreateBlock(Transform parent, string name, Vector3 center, Vector3 size, Material material)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(block, "Create SCF test building");
            block.name = name;
            block.transform.SetParent(parent, true);

            float groundY = SampleGroundY(center);
            block.transform.position = new Vector3(center.x, groundY + size.y * 0.5f, center.z);
            block.transform.localRotation = Quaternion.identity;
            block.transform.localScale = size;

            Renderer renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            GameObjectUtility.SetStaticEditorFlags(block, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
        }

        private static float SampleGroundY(Vector3 position)
        {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                return terrain.SampleHeight(position) + terrain.transform.position.y;
            }

            Vector3 rayOrigin = position + Vector3.up * 500f;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 1000f, ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.point.y;
            }

            return position.y;
        }

        private static float ResolvePlayerScale(GameObject player)
        {
            if (player == null)
            {
                return 1f;
            }

            Vector3 scale = player.transform.lossyScale;
            return Mathf.Max(1f, Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        }

        private static float ResolveFrankReferenceHeight(GameObject player)
        {
            if (player != null)
            {
                SCFCharacterVisualSlot visualSlot = player.GetComponent<SCFCharacterVisualSlot>();
                if (visualSlot != null && visualSlot.ActiveVisual != null && TryResolveRendererHeight(visualSlot.ActiveVisual, out float activeHeight))
                {
                    return activeHeight;
                }
            }

            GameObject frankAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FrankModelPath);
            if (frankAsset == null)
            {
                return 1.82f;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(frankAsset) as GameObject;
            if (instance == null)
            {
                return 1.82f;
            }

            instance.hideFlags = HideFlags.HideAndDontSave;
            float height = TryResolveRendererHeight(instance, out float assetHeight) ? assetHeight : 1.82f;
            Object.DestroyImmediate(instance);
            return height;
        }

        private static bool TryResolveRendererHeight(GameObject root, out float height)
        {
            height = 0f;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool initialized = false;
            Bounds bounds = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (!initialized)
                {
                    bounds = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (!initialized)
            {
                return false;
            }

            height = bounds.size.y;
            return height > 0.1f;
        }

        private static Material EnsureMaterial(string materialName, Color baseColor)
        {
            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            {
                AssetDatabase.CreateFolder("Assets/SCF", "Materials");
            }

            string path = MaterialsFolder + "/" + materialName + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("HDRP/Lit") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader);
            material.name = materialName;

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", baseColor);
            }

            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
#endif
