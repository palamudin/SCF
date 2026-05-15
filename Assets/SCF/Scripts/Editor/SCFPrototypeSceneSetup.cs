#if UNITY_EDITOR
using SCF.Gameplay;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SCF.EditorTools
{
    public static class SCFPrototypeSceneSetup
    {
        private const string StarterAssetsUrl = "https://assetstore.unity.com/packages/essentials/starter-assets-thirdperson-urp-196526";
        private const string DefaultScenePath = "Assets/OutdoorsScene.unity";
        private const string StarterArmatureModelPath = "Assets/StarterAssets/ThirdPersonController/Character/Models/Armature.fbx";
        private const string StarterAnimatorControllerPath = "Assets/StarterAssets/ThirdPersonController/Character/Animations/StarterAssetsThirdPerson.controller";
        private const string PlayerMaterialsFolder = "Assets/SCF/Materials";

        [MenuItem("SCF/Setup/Open Starter Assets Page")]
        public static void OpenStarterAssetsPage()
        {
            Application.OpenURL(StarterAssetsUrl);
        }

        [MenuItem("SCF/Setup/Create Isometric Prototype Player")]
        public static void CreateIsometricPrototypePlayer()
        {
            CreateIsometricPrototypePlayer(true, false);
        }

        [MenuItem("SCF/Setup/Repair Player Visual Model")]
        public static void RepairPlayerVisualModel()
        {
            RepairPlayerVisualModel(true);
        }

        public static GameObject CreateIsometricPrototypePlayer(bool allowDialog, bool saveScene)
        {
            GameObject existing = GameObject.Find("SCF_Player");
            if (existing != null)
            {
                if (!allowDialog || !EditorUtility.DisplayDialog(
                        "Replace SCF_Player?",
                        "A SCF_Player object already exists in the scene. Select it instead of creating another one?",
                        "Select Existing",
                        "Create Another"))
                {
                    existing = null;
                }
            }

            EnsureSceneOpen();

            GameObject player = existing != null ? existing : CreatePlayerRoot();
            player.name = "SCF_Player";
            player.transform.position = Vector3.up * 0.05f;

            ConfigurePrototypePlayer(player, saveScene);

            Debug.Log("SCF prototype player is ready. Import Unity Starter Assets first if you want the humanoid PlayerArmature instead of the capsule fallback.");
            return player;
        }

        public static GameObject RepairPlayerVisualModel(bool saveScene)
        {
            EnsureSceneOpen();

            GameObject player = GameObject.Find("SCF_Player");
            if (player == null)
            {
                player = CreatePlayerRoot();
                player.name = "SCF_Player";
                player.transform.position = Vector3.up * 0.05f;
            }

            ConfigurePrototypePlayer(player, saveScene);
            Debug.Log("SCF player visual model repaired and rebound to the movement stack.");
            return player;
        }

        private static void ConfigurePrototypePlayer(GameObject player, bool saveScene)
        {
            CharacterController controller = EnsureComponent<CharacterController>(player);
            ConfigureCharacterController(controller);

            IsometricPlayerInput playerInput = EnsureComponent<IsometricPlayerInput>(player);
            EnsureComponent<CursorLockGuard>(player);
            IsometricCharacterMotor motor = EnsureComponent<IsometricCharacterMotor>(player);
            MovementAnimatorBridge animatorBridge = EnsureComponent<MovementAnimatorBridge>(player);
            MotionMatchingSignalHub signalHub = EnsureComponent<MotionMatchingSignalHub>(player);
            SCFMotionSelector motionSelector = EnsureComponent<SCFMotionSelector>(player);

            DisableStarterControllerBehaviours(player);
            SetLayerRecursively(player.transform, 0);

            Camera sceneCamera = EnsureIsometricCamera(player.transform);
            Animator animator = EnsureVisiblePlayerModel(player);
            Transform facingRoot = animator != null ? animator.transform : player.transform;

            motor.Configure(playerInput, sceneCamera, facingRoot);
            animatorBridge.Configure(animator);
            signalHub.Configure(animator);
            motionSelector.Configure(animator, SCFMotionDatabaseBaker.BuildBaseDatabase());
            SCFMotionSystemSetup.DisableLegacyMotionComponents(player);

            Selection.activeGameObject = player;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            if (saveScene)
            {
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }
        }

        private static GameObject CreatePlayerRoot()
        {
            GameObject starterPrefab = FindStarterPlayerPrefab();
            if (starterPrefab != null)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(starterPrefab) as GameObject;
                if (instance != null)
                {
                    Undo.RegisterCreatedObjectUndo(instance, "Create SCF player from Starter Assets");
                    return instance;
                }
            }

            GameObject player = new GameObject("SCF_Player");
            Undo.RegisterCreatedObjectUndo(player, "Create SCF prototype player");

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Undo.RegisterCreatedObjectUndo(visual, "Create SCF capsule visual");
            visual.name = "Prototype Visual";
            visual.transform.SetParent(player.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            visual.transform.localScale = new Vector3(0.8f, 0.95f, 0.8f);

            Collider collider = visual.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            return player;
        }

        private static GameObject FindStarterPlayerPrefab()
        {
            string[] guids = AssetDatabase.FindAssets("PlayerArmature t:Prefab");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!path.Replace("\\", "/").Contains("StarterAssets"))
                {
                    continue;
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    return prefab;
                }
            }

            return null;
        }

        private static Animator EnsureVisiblePlayerModel(GameObject player)
        {
            Renderer[] renderers = player.GetComponentsInChildren<Renderer>(true);
            if (!HasSkinnedRenderer(renderers))
            {
                InstantiateStarterArmatureModel(player);
                renderers = player.GetComponentsInChildren<Renderer>(true);
            }

            PlayerVisualMaterials materials = EnsurePlayerVisualMaterials();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                SetActiveUpToRoot(renderer.transform, player.transform);
                Undo.RecordObject(renderer, "Repair SCF player renderer");
                renderer.enabled = true;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.sharedMaterials = BuildMaterialSet(renderer.sharedMaterials.Length, renderer.name, materials);
                EditorUtility.SetDirty(renderer);
            }

            Animator animator = player.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                SetActiveUpToRoot(animator.transform, player.transform);
                Undo.RecordObject(animator, "Repair SCF player animator");
                animator.enabled = true;
                animator.applyRootMotion = false;
                EnsureStarterAnimatorAssets(animator);
                EditorUtility.SetDirty(animator);
            }

            return animator;
        }

        private static bool HasSkinnedRenderer(Renderer[] renderers)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] is SkinnedMeshRenderer)
                {
                    return true;
                }
            }

            return false;
        }

        private static void InstantiateStarterArmatureModel(GameObject player)
        {
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StarterArmatureModelPath);
            if (modelPrefab == null)
            {
                return;
            }

            GameObject visual = PrefabUtility.InstantiatePrefab(modelPrefab) as GameObject;
            if (visual == null)
            {
                return;
            }

            Undo.RegisterCreatedObjectUndo(visual, "Add Starter Assets armature visual");
            visual.name = "SCF_VisibleArmature";
            visual.transform.SetParent(player.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            Animator animator = visual.GetComponent<Animator>();
            if (animator == null)
            {
                animator = Undo.AddComponent<Animator>(visual);
            }

            Animator existingAnimator = player.GetComponent<Animator>();
            if (existingAnimator != null && existingAnimator.runtimeAnimatorController != null)
            {
                animator.avatar = existingAnimator.avatar;
                animator.runtimeAnimatorController = existingAnimator.runtimeAnimatorController;
            }

            EnsureStarterAnimatorAssets(animator);
            RemovePrototypeCapsule(player);
        }

        private static void EnsureStarterAnimatorAssets(Animator animator)
        {
            if (animator.avatar == null)
            {
                Avatar avatar = FindStarterArmatureAvatar();
                if (avatar != null)
                {
                    animator.avatar = avatar;
                }
            }

            if (animator.runtimeAnimatorController == null)
            {
                RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(StarterAnimatorControllerPath);
                if (controller != null)
                {
                    animator.runtimeAnimatorController = controller;
                }
            }
        }

        private static Avatar FindStarterArmatureAvatar()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(StarterArmatureModelPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Avatar avatar)
                {
                    return avatar;
                }
            }

            return null;
        }

        private static void RemovePrototypeCapsule(GameObject player)
        {
            Transform[] children = player.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == null || child == player.transform || child.name != "Prototype Visual")
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(child.gameObject);
                return;
            }
        }

        private static PlayerVisualMaterials EnsurePlayerVisualMaterials()
        {
            if (!AssetDatabase.IsValidFolder(PlayerMaterialsFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/SCF"))
                {
                    AssetDatabase.CreateFolder("Assets", "SCF");
                }

                AssetDatabase.CreateFolder("Assets/SCF", "Materials");
            }

            PlayerVisualMaterials materials = new PlayerVisualMaterials
            {
                Arms = EnsureMaterial("SCF_Player_Arms_HDRP", new Color(0.72f, 0.56f, 0.42f, 1f)),
                Body = EnsureMaterial("SCF_Player_Body_HDRP", new Color(0.18f, 0.24f, 0.30f, 1f)),
                Legs = EnsureMaterial("SCF_Player_Legs_HDRP", new Color(0.10f, 0.12f, 0.15f, 1f))
            };

            AssetDatabase.SaveAssets();
            return materials;
        }

        private static Material EnsureMaterial(string materialName, Color baseColor)
        {
            string path = PlayerMaterialsFolder + "/" + materialName + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = FindLitShader();
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", baseColor);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.36f);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0f);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Shader FindLitShader()
        {
            Shader shader = Shader.Find("HDRP/Lit");
            if (shader != null)
            {
                return shader;
            }

            shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                return shader;
            }

            shader = Shader.Find("Standard");
            return shader != null ? shader : Shader.Find("Diffuse");
        }

        private static Material[] BuildMaterialSet(int slotCount, string rendererName, PlayerVisualMaterials materials)
        {
            int count = Mathf.Max(1, slotCount);
            Material[] assigned = new Material[count];
            string lowerName = rendererName.ToLowerInvariant();

            for (int i = 0; i < assigned.Length; i++)
            {
                assigned[i] = PickMaterial(i, assigned.Length, lowerName, materials);
            }

            return assigned;
        }

        private static Material PickMaterial(int slotIndex, int slotCount, string lowerRendererName, PlayerVisualMaterials materials)
        {
            if (lowerRendererName.Contains("arm") || (slotCount >= 3 && slotIndex == 0))
            {
                return materials.Arms;
            }

            if (lowerRendererName.Contains("leg") || (slotCount >= 3 && slotIndex == 2))
            {
                return materials.Legs;
            }

            return materials.Body;
        }

        private static void SetActiveUpToRoot(Transform child, Transform root)
        {
            Transform current = child;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    Undo.RecordObject(current.gameObject, "Activate SCF player visual hierarchy");
                    current.gameObject.SetActive(true);
                    EditorUtility.SetDirty(current.gameObject);
                }

                if (current == root)
                {
                    break;
                }

                current = current.parent;
            }
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == null || child.gameObject.layer == layer)
                {
                    continue;
                }

                Undo.RecordObject(child.gameObject, "Set SCF player visual layer");
                child.gameObject.layer = layer;
                EditorUtility.SetDirty(child.gameObject);
            }
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            return Undo.AddComponent<T>(gameObject);
        }

        private static void ConfigureCharacterController(CharacterController controller)
        {
            Undo.RecordObject(controller, "Configure SCF character controller");
            controller.height = 1.85f;
            controller.radius = 0.34f;
            controller.center = new Vector3(0f, 0.92f, 0f);
            controller.slopeLimit = 55f;
            controller.stepOffset = 0.35f;
            controller.skinWidth = 0.04f;
            EditorUtility.SetDirty(controller);
        }

        private static Camera EnsureIsometricCamera(Transform target)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                Undo.RegisterCreatedObjectUndo(cameraObject, "Create SCF isometric camera");
                cameraObject.tag = "MainCamera";
                camera = Undo.AddComponent<Camera>(cameraObject);
            }

            IsometricCameraFollow follow = camera.GetComponent<IsometricCameraFollow>();
            if (follow == null)
            {
                follow = Undo.AddComponent<IsometricCameraFollow>(camera.gameObject);
            }

            follow.Configure(target);
            return camera;
        }

        private static void DisableStarterControllerBehaviours(GameObject player)
        {
            Behaviour[] behaviours = player.GetComponentsInChildren<Behaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                Behaviour behaviour = behaviours[i];
                if (behaviour == null || behaviour is Animator)
                {
                    continue;
                }

                string typeName = behaviour.GetType().Name;
                string typeNamespace = behaviour.GetType().Namespace ?? string.Empty;
                bool belongsToStarterController = typeNamespace.Contains("StarterAssets")
                    || typeName.Contains("ThirdPersonController")
                    || typeName.Contains("StarterAssetsInputs")
                    || typeName.Contains("BasicRigidBodyPush")
                    || typeName == "PlayerInput";

                if (!belongsToStarterController)
                {
                    continue;
                }

                Undo.RecordObject(behaviour, "Disable Starter Assets controller behaviour");
                behaviour.enabled = false;
                EditorUtility.SetDirty(behaviour);
            }
        }

        private static void EnsureSceneOpen()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && !string.IsNullOrEmpty(activeScene.path))
            {
                return;
            }

            if (File.Exists(DefaultScenePath))
            {
                EditorSceneManager.OpenScene(DefaultScenePath);
            }
        }

        private struct PlayerVisualMaterials
        {
            public Material Arms;
            public Material Body;
            public Material Legs;
        }
    }
}
#endif
