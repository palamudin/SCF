using UnityEngine;

namespace SCF.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SCFPlaytestUtilityPanel : MonoBehaviour
    {
        private const string UtilityObjectName = "SCF_PlaytestUtilityPanel";
        private const string ConcreteTerrainLayerPath = "SCF/Terrain/SCF_ConcreteTerrainLayer";

        [Header("Respawn")]
        [SerializeField] private string playerObjectName = "SCF_Player";
        [SerializeField] private string spawnPointName = "SCF_PlayerSpawn";
        [SerializeField] private bool snapRespawnToGround = true;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField, Min(0f)] private float groundProbeHeight = 40f;
        [SerializeField, Min(0f)] private float groundProbeDistance = 120f;
        [SerializeField, Min(0f)] private float groundLift = 0.08f;

        [Header("Terrain Surface")]
        [SerializeField] private bool applyConcreteSurfaceOnStart = true;
        [SerializeField] private TerrainLayer concreteTerrainLayer;
        [SerializeField, Min(1f)] private float minimumGroundExtent = 8f;
        [SerializeField, Min(0.01f)] private float maximumGroundThickness = 1.25f;
        [SerializeField] private Color fallbackConcreteColor = new Color(0.48f, 0.48f, 0.45f, 1f);

        [Header("GUI")]
        [SerializeField] private bool visible = true;
        [SerializeField] private Rect windowRect = new Rect(0f, 12f, 136f, 86f);

        private Transform playerRoot;
        private IsometricCharacterMotor motor;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation = Quaternion.identity;
        private bool hasSpawnPose;
        private Material concreteRuntimeMaterial;
        private Texture2D fallbackConcreteTexture;
        private static int nextWindowId = 41000;
        private int windowId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<SCFPlaytestUtilityPanel>() != null)
            {
                return;
            }

            new GameObject(UtilityObjectName).AddComponent<SCFPlaytestUtilityPanel>();
        }

        private void Awake()
        {
            if (windowId == 0)
            {
                windowId = ++nextWindowId;
            }
        }

        private void Start()
        {
            ResolvePlayer();
            CaptureSpawnPose();

            if (applyConcreteSurfaceOnStart)
            {
                ApplyConcreteSurface();
            }
        }

        private void OnDestroy()
        {
            if (concreteRuntimeMaterial != null)
            {
                Destroy(concreteRuntimeMaterial);
            }

            if (fallbackConcreteTexture != null)
            {
                Destroy(fallbackConcreteTexture);
            }
        }

        private void OnGUI()
        {
            if (!visible)
            {
                Rect openRect = ResolveAnchoredRect(new Rect(0f, 12f, 64f, 28f));
                if (GUI.Button(openRect, "Tools"))
                {
                    visible = true;
                }

                return;
            }

            windowRect = ResolveAnchoredRect(windowRect);
            windowRect = GUILayout.Window(windowId, windowRect, DrawWindow, "Playtest");
        }

        private void DrawWindow(int id)
        {
            if (GUILayout.Button("Reset", GUILayout.Height(28f)))
            {
                RespawnPlayer();
            }

            if (GUILayout.Button("Concrete", GUILayout.Height(24f)))
            {
                ApplyConcreteSurface();
            }

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Slide", GUILayout.Width(58f), GUILayout.Height(22f)))
                {
                    visible = false;
                }
            }

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 22f));
        }

        public void RespawnPlayer()
        {
            ResolvePlayer();
            if (playerRoot == null)
            {
                return;
            }

            if (!hasSpawnPose)
            {
                CaptureSpawnPose();
            }

            Vector3 targetPosition = spawnPosition;
            Quaternion targetRotation = spawnRotation;
            Transform spawnPoint = GameObject.Find(spawnPointName)?.transform;
            if (spawnPoint != null)
            {
                targetPosition = spawnPoint.position;
                targetRotation = spawnPoint.rotation;
            }

            if (snapRespawnToGround)
            {
                targetPosition = SnapToGround(targetPosition);
            }

            if (motor != null)
            {
                motor.RespawnAt(targetPosition, targetRotation);
            }
            else
            {
                CharacterController controller = playerRoot.GetComponent<CharacterController>();
                bool controllerWasEnabled = controller != null && controller.enabled;
                if (controllerWasEnabled)
                {
                    controller.enabled = false;
                }

                playerRoot.SetPositionAndRotation(targetPosition, targetRotation);

                if (controllerWasEnabled)
                {
                    controller.enabled = true;
                }
            }
        }

        public void ApplyConcreteSurface()
        {
            TerrainLayer layer = ResolveConcreteTerrainLayer();
            ApplyTerrainLayer(layer);
            ApplyConcreteMaterialToGroundMeshes(layer != null ? layer.diffuseTexture : null);
        }

        private void ResolvePlayer()
        {
            if (motor == null)
            {
                motor = FindAnyObjectByType<IsometricCharacterMotor>();
            }

            if (playerRoot == null)
            {
                GameObject namedPlayer = GameObject.Find(playerObjectName);
                playerRoot = namedPlayer != null ? namedPlayer.transform : motor != null ? motor.transform : null;
            }

            if (motor == null && playerRoot != null)
            {
                motor = playerRoot.GetComponent<IsometricCharacterMotor>();
            }
        }

        private void CaptureSpawnPose()
        {
            Transform spawnPoint = GameObject.Find(spawnPointName)?.transform;
            if (spawnPoint != null)
            {
                spawnPosition = spawnPoint.position;
                spawnRotation = spawnPoint.rotation;
                hasSpawnPose = true;
                return;
            }

            if (playerRoot == null)
            {
                return;
            }

            spawnPosition = playerRoot.position;
            spawnRotation = playerRoot.rotation;
            hasSpawnPose = true;
        }

        private Vector3 SnapToGround(Vector3 position)
        {
            Vector3 origin = position + Vector3.up * groundProbeHeight;
            float distance = groundProbeHeight + groundProbeDistance;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, groundMask, QueryTriggerInteraction.Ignore))
            {
                return hit.point + Vector3.up * groundLift;
            }

            return position;
        }

        private TerrainLayer ResolveConcreteTerrainLayer()
        {
            if (concreteTerrainLayer != null)
            {
                return concreteTerrainLayer;
            }

            concreteTerrainLayer = Resources.Load<TerrainLayer>(ConcreteTerrainLayerPath);
            return concreteTerrainLayer;
        }

        private void ApplyTerrainLayer(TerrainLayer layer)
        {
            if (layer == null)
            {
                return;
            }

            Terrain[] terrains = Terrain.activeTerrains;
            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain terrain = terrains[i];
                if (terrain == null || terrain.terrainData == null)
                {
                    continue;
                }

                terrain.terrainData.terrainLayers = new[] { layer };
                terrain.Flush();
            }
        }

        private void ApplyConcreteMaterialToGroundMeshes(Texture diffuseTexture)
        {
            Material material = ResolveConcreteRuntimeMaterial(diffuseTexture);
            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer candidate = renderers[i];
                if (candidate == null)
                {
                    continue;
                }

                if (LooksLikeGroundMesh(candidate))
                {
                    candidate.sharedMaterial = material;
                }
            }
        }

        private bool LooksLikeGroundMesh(Renderer candidate)
        {
            Bounds bounds = candidate.bounds;
            if (bounds.size.x < minimumGroundExtent || bounds.size.z < minimumGroundExtent || bounds.size.y > maximumGroundThickness)
            {
                return false;
            }

            string lowerName = candidate.name.ToLowerInvariant();
            return lowerName.Contains("ground")
                   || lowerName.Contains("terrain")
                   || lowerName.Contains("plane")
                   || lowerName.Contains("floor");
        }

        private Material ResolveConcreteRuntimeMaterial(Texture diffuseTexture)
        {
            if (concreteRuntimeMaterial == null)
            {
                Shader shader = Shader.Find("HDRP/Lit")
                    ?? Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Standard")
                    ?? Shader.Find("Unlit/Texture")
                    ?? Shader.Find("Unlit/Color");
                concreteRuntimeMaterial = new Material(shader)
                {
                    name = "SCF_RuntimeConcreteGround"
                };
            }

            Texture texture = diffuseTexture != null ? diffuseTexture : ResolveFallbackConcreteTexture();
            concreteRuntimeMaterial.mainTexture = texture;
            concreteRuntimeMaterial.mainTextureScale = Vector2.one * 6f;

            if (concreteRuntimeMaterial.HasProperty("_BaseColorMap"))
            {
                concreteRuntimeMaterial.SetTexture("_BaseColorMap", texture);
            }

            if (concreteRuntimeMaterial.HasProperty("_BaseColor"))
            {
                concreteRuntimeMaterial.SetColor("_BaseColor", Color.white);
            }

            if (concreteRuntimeMaterial.HasProperty("_Color"))
            {
                concreteRuntimeMaterial.SetColor("_Color", Color.white);
            }

            return concreteRuntimeMaterial;
        }

        private Texture2D ResolveFallbackConcreteTexture()
        {
            if (fallbackConcreteTexture != null)
            {
                return fallbackConcreteTexture;
            }

            fallbackConcreteTexture = new Texture2D(32, 32, TextureFormat.RGBA32, true)
            {
                name = "SCF_FallbackConcreteTexture",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };

            Color darker = fallbackConcreteColor * 0.75f;
            darker.a = 1f;
            for (int y = 0; y < fallbackConcreteTexture.height; y++)
            {
                for (int x = 0; x < fallbackConcreteTexture.width; x++)
                {
                    bool joint = x % 16 == 0 || y % 16 == 0;
                    float grain = Mathf.PerlinNoise(x * 0.37f, y * 0.37f) * 0.12f;
                    Color color = joint ? darker : fallbackConcreteColor + new Color(grain, grain, grain, 0f);
                    color.a = 1f;
                    fallbackConcreteTexture.SetPixel(x, y, color);
                }
            }

            fallbackConcreteTexture.Apply();
            return fallbackConcreteTexture;
        }

        private Rect ResolveAnchoredRect(Rect rect)
        {
            rect.x = Mathf.Clamp(Screen.width - rect.width - 12f, 0f, Mathf.Max(0f, Screen.width - rect.width));
            rect.y = Mathf.Clamp(rect.y, 0f, Mathf.Max(0f, Screen.height - rect.height));
            return rect;
        }
    }
}
