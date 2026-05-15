using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace SCF.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SCFPhotonSession : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        private const byte StateEventCode = 41;
        private const byte ShotEventCode = 42;
        private const byte DamageEventCode = 43;
        private const int GuiWindowId = 42041;

        [Header("Session")]
        [SerializeField] private bool showPanel = true;
        [SerializeField] private string gameVersion = "SCF-prototype-01";
        [SerializeField] private string roomName = "SCF_TestRange";
        [SerializeField, Range(2, 8)] private byte maxPlayers = 2;
        [SerializeField] private bool offsetLocalPlayerOnJoin = true;
        [SerializeField, Min(0f)] private float spawnSpacing = 3f;

        [Header("Local Player")]
        [SerializeField] private Transform localPlayerRoot;
        [SerializeField] private IsometricCharacterMotor localMotor;
        [SerializeField] private SCFCharacterVisualSlot localVisualSlot;
        [SerializeField] private SCFWeaponVisualSlot localWeaponSlot;
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField, Min(1f)] private float railgunDamage = 34f;

        [Header("Sync")]
        [SerializeField, Range(2f, 30f)] private float stateSendRate = 12f;
        [SerializeField, Min(0.1f)] private float remoteMoveSharpness = 16f;
        [SerializeField, Min(0.1f)] private float remoteTurnSharpness = 18f;
        [SerializeField, Min(0.1f)] private float remoteHeight = 1.8f;

        [Header("GUI")]
        [SerializeField] private Rect panelRect = new Rect(16f, 510f, 290f, 170f);
        [SerializeField] private Vector2 collapsedPanelSize = new Vector2(112f, 30f);

        private readonly Dictionary<int, RemoteAvatar> remoteAvatars = new Dictionary<int, RemoteAvatar>();
        private readonly Dictionary<int, float> knownHealth = new Dictionary<int, float>();
        private SCFWeaponVisualSlot subscribedWeaponSlot;
        private float localHealth;
        private float nextStateSendTime;
        private bool localSpawnOffsetApplied;
        private Material remoteBlueMaterial;
        private Material remoteRedMaterial;
        private Material beamMaterial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindFirstSceneObject<SCFPhotonSession>() != null || FindFirstSceneObject<IsometricCharacterMotor>() == null)
            {
                return;
            }

            GameObject session = new GameObject("SCF_PhotonSession");
            session.AddComponent<SCFPhotonSession>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            localHealth = maxHealth;
            ResolveLocalReferences();
        }

        public override void OnEnable()
        {
            base.OnEnable();
            ResolveLocalReferences();
        }

        public override void OnDisable()
        {
            SubscribeToWeaponSlot(null);
            base.OnDisable();
        }

        private void Update()
        {
            ResolveLocalReferences();
            TickStateSend();
            TickRemoteAvatars();
        }

        private void OnGUI()
        {
            if (!showPanel)
            {
                Rect openRect = new Rect(panelRect.x, panelRect.y, collapsedPanelSize.x, collapsedPanelSize.y);
                if (GUI.Button(openRect, "Network"))
                {
                    showPanel = true;
                }

                return;
            }

            panelRect = GUILayout.Window(GuiWindowId, panelRect, DrawPanel, "SCF Multiplayer");
        }

        private void DrawPanel(int windowId)
        {
            GUILayout.Label("State: " + PhotonNetwork.NetworkClientState);
            GUILayout.Label("Room: " + (PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.Name + " (" + PhotonNetwork.CurrentRoom.PlayerCount + "/" + PhotonNetwork.CurrentRoom.MaxPlayers + ")" : "none"));
            GUILayout.Label("Health: " + Mathf.CeilToInt(localHealth) + " / " + Mathf.CeilToInt(maxHealth));
            foreach (KeyValuePair<int, float> health in knownHealth)
            {
                if (PhotonNetwork.InRoom && health.Key != PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    GUILayout.Label("Actor " + health.Key + ": " + Mathf.CeilToInt(health.Value));
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                GUI.enabled = !PhotonNetwork.IsConnected;
                if (GUILayout.Button("Connect", GUILayout.Height(28f)))
                {
                    Connect();
                }

                GUI.enabled = PhotonNetwork.IsConnected && !PhotonNetwork.InRoom;
                if (GUILayout.Button("Join", GUILayout.Height(28f)))
                {
                    JoinOrCreateRoom();
                }

                GUI.enabled = PhotonNetwork.IsConnected;
                if (GUILayout.Button("Leave", GUILayout.Height(28f)))
                {
                    LeaveOrDisconnect();
                }

                GUI.enabled = true;
            }

            using (new GUILayout.HorizontalScope())
            {
                roomName = GUILayout.TextField(roomName, GUILayout.MinWidth(110f));
                if (GUILayout.Button("Slide", GUILayout.Width(58f), GUILayout.Height(24f)))
                {
                    showPanel = false;
                }
            }

            GUILayout.Label("Remote soldiers: " + remoteAvatars.Count);
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 22f));
        }

        private void Connect()
        {
            if (PhotonNetwork.IsConnected)
            {
                JoinOrCreateRoom();
                return;
            }

            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.LocalPlayer.NickName = "SCF_" + UnityEngine.Random.Range(1000, 9999);
            PhotonNetwork.ConnectUsingSettings();
        }

        private void JoinOrCreateRoom()
        {
            if (!PhotonNetwork.IsConnectedAndReady)
            {
                return;
            }

            RoomOptions options = new RoomOptions
            {
                MaxPlayers = maxPlayers,
                IsOpen = true,
                IsVisible = true
            };
            PhotonNetwork.JoinOrCreateRoom(string.IsNullOrWhiteSpace(roomName) ? "SCF_TestRange" : roomName, options, TypedLobby.Default);
        }

        private void LeaveOrDisconnect()
        {
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
                return;
            }

            PhotonNetwork.Disconnect();
        }

        public override void OnConnectedToMaster()
        {
            JoinOrCreateRoom();
        }

        public override void OnJoinedRoom()
        {
            localHealth = maxHealth;
            knownHealth[PhotonNetwork.LocalPlayer.ActorNumber] = localHealth;
            OffsetLocalPlayerOnce();
            foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
            {
                if (!player.IsLocal)
                {
                    EnsureRemoteAvatar(player.ActorNumber);
                }
            }

            SendState(true);
        }

        public override void OnLeftRoom()
        {
            localSpawnOffsetApplied = false;
            ClearRemoteAvatars();
        }

        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            EnsureRemoteAvatar(newPlayer.ActorNumber);
            SendState(true);
        }

        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            DestroyRemoteAvatar(otherPlayer.ActorNumber);
        }

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code == StateEventCode)
            {
                HandleStateEvent(photonEvent.CustomData as object[]);
            }
            else if (photonEvent.Code == ShotEventCode)
            {
                HandleShotEvent(photonEvent.CustomData as object[]);
            }
            else if (photonEvent.Code == DamageEventCode)
            {
                HandleDamageEvent(photonEvent.CustomData as object[]);
            }
        }

        private void ResolveLocalReferences()
        {
            if (localMotor == null)
            {
                localMotor = FindFirstSceneObject<IsometricCharacterMotor>();
            }

            if (localPlayerRoot == null && localMotor != null)
            {
                localPlayerRoot = localMotor.transform;
            }

            if (localVisualSlot == null && localPlayerRoot != null)
            {
                localVisualSlot = localPlayerRoot.GetComponent<SCFCharacterVisualSlot>();
            }

            if (localWeaponSlot == null && localPlayerRoot != null)
            {
                localWeaponSlot = localPlayerRoot.GetComponent<SCFWeaponVisualSlot>();
            }

            SubscribeToWeaponSlot(localWeaponSlot);
        }

        private void SubscribeToWeaponSlot(SCFWeaponVisualSlot slot)
        {
            if (subscribedWeaponSlot == slot)
            {
                return;
            }

            if (subscribedWeaponSlot != null)
            {
                subscribedWeaponSlot.RailgunFired -= OnLocalRailgunFired;
            }

            subscribedWeaponSlot = slot;
            if (subscribedWeaponSlot != null)
            {
                subscribedWeaponSlot.RailgunFired += OnLocalRailgunFired;
            }
        }

        private void TickStateSend()
        {
            if (!PhotonNetwork.InRoom || localPlayerRoot == null || Time.time < nextStateSendTime)
            {
                return;
            }

            SendState(false);
            nextStateSendTime = Time.time + 1f / Mathf.Max(1f, stateSendRate);
        }

        private void SendState(bool reliable)
        {
            if (!PhotonNetwork.InRoom || localPlayerRoot == null)
            {
                return;
            }

            object[] payload =
            {
                PhotonNetwork.LocalPlayer.ActorNumber,
                localPlayerRoot.position,
                localPlayerRoot.rotation,
                localHealth
            };

            RaiseEventOptions options = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
            PhotonNetwork.RaiseEvent(StateEventCode, payload, options, reliable ? SendOptions.SendReliable : SendOptions.SendUnreliable);
        }

        private void HandleStateEvent(object[] data)
        {
            if (data == null || data.Length < 4)
            {
                return;
            }

            int actor = Convert.ToInt32(data[0]);
            if (actor == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                return;
            }

            RemoteAvatar avatar = EnsureRemoteAvatar(actor);
            avatar.TargetPosition = (Vector3)data[1];
            avatar.TargetRotation = (Quaternion)data[2];
            avatar.Health = Convert.ToSingle(data[3]);
            knownHealth[actor] = avatar.Health;
        }

        private void OnLocalRailgunFired(SCFRailgunShot shot)
        {
            if (!PhotonNetwork.InRoom)
            {
                return;
            }

            object[] shotPayload =
            {
                PhotonNetwork.LocalPlayer.ActorNumber,
                shot.Muzzle,
                shot.Impact
            };
            PhotonNetwork.RaiseEvent(ShotEventCode, shotPayload, new RaiseEventOptions { Receivers = ReceiverGroup.Others }, SendOptions.SendReliable);

            int targetActor = ResolveActorFromCollider(shot.HitCollider);
            if (targetActor > 0)
            {
                SendDamage(targetActor, railgunDamage);
            }
        }

        private void HandleShotEvent(object[] data)
        {
            if (data == null || data.Length < 3)
            {
                return;
            }

            int actor = Convert.ToInt32(data[0]);
            if (actor == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                return;
            }

            SpawnNetworkBeam((Vector3)data[1], (Vector3)data[2]);
        }

        private void SendDamage(int targetActor, float damage)
        {
            object[] payload =
            {
                targetActor,
                PhotonNetwork.LocalPlayer.ActorNumber,
                damage
            };
            PhotonNetwork.RaiseEvent(DamageEventCode, payload, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
        }

        private void HandleDamageEvent(object[] data)
        {
            if (data == null || data.Length < 3)
            {
                return;
            }

            int targetActor = Convert.ToInt32(data[0]);
            float damage = Convert.ToSingle(data[2]);
            if (targetActor == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                localHealth = Mathf.Max(0f, localHealth - damage);
                knownHealth[targetActor] = localHealth;
                if (localHealth <= 0.001f)
                {
                    localHealth = maxHealth;
                    knownHealth[targetActor] = localHealth;
                }

                SendState(true);
                return;
            }

            RemoteAvatar avatar;
            if (remoteAvatars.TryGetValue(targetActor, out avatar))
            {
                avatar.Health = Mathf.Max(0f, avatar.Health - damage);
                knownHealth[targetActor] = avatar.Health;
            }
        }

        private RemoteAvatar EnsureRemoteAvatar(int actorNumber)
        {
            RemoteAvatar avatar;
            if (remoteAvatars.TryGetValue(actorNumber, out avatar))
            {
                return avatar;
            }

            GameObject root = new GameObject("SCF_RemoteSoldier_" + actorNumber);
            root.transform.position = localPlayerRoot != null ? localPlayerRoot.position + Vector3.right * spawnSpacing : Vector3.zero;
            root.transform.rotation = Quaternion.identity;

            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.height = remoteHeight;
            collider.radius = Mathf.Max(0.2f, remoteHeight * 0.18f);
            collider.center = Vector3.up * (remoteHeight * 0.5f);

            SCFNetworkHitbox hitbox = root.AddComponent<SCFNetworkHitbox>();
            hitbox.ActorNumber = actorNumber;

            CreateRemoteVisual(root.transform, actorNumber);
            avatar = new RemoteAvatar(root.transform)
            {
                TargetPosition = root.transform.position,
                TargetRotation = root.transform.rotation,
                Health = maxHealth
            };
            remoteAvatars.Add(actorNumber, avatar);
            knownHealth[actorNumber] = maxHealth;
            return avatar;
        }

        private GameObject CreateRemoteVisual(Transform parent, int actorNumber)
        {
            GameObject source = localVisualSlot != null ? localVisualSlot.ActiveVisual : null;
            GameObject visual;
            if (source != null)
            {
                visual = Instantiate(source, parent);
                visual.name = "SCF_RemoteSoldierVisual_" + actorNumber;
                StripRemoteVisualBehavioursAndColliders(visual);
                return visual;
            }

            visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "SCF_RemoteSoldierProxy_" + actorNumber;
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = Vector3.up * (remoteHeight * 0.5f);
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = new Vector3(0.75f, remoteHeight * 0.5f, 0.75f);
            Collider collider = visual.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = actorNumber % 2 == 0 ? ResolveRemoteBlueMaterial() : ResolveRemoteRedMaterial();
            }

            return visual;
        }

        private static void StripRemoteVisualBehavioursAndColliders(GameObject visual)
        {
            MonoBehaviour[] behaviours = visual.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] != null)
                {
                    behaviours[i].enabled = false;
                }
            }

            Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    Destroy(colliders[i]);
                }
            }
        }

        private void TickRemoteAvatars()
        {
            float moveBlend = 1f - Mathf.Exp(-remoteMoveSharpness * Time.deltaTime);
            float turnBlend = 1f - Mathf.Exp(-remoteTurnSharpness * Time.deltaTime);
            foreach (RemoteAvatar avatar in remoteAvatars.Values)
            {
                if (avatar.Root == null)
                {
                    continue;
                }

                avatar.Root.position = Vector3.Lerp(avatar.Root.position, avatar.TargetPosition, moveBlend);
                avatar.Root.rotation = Quaternion.Slerp(avatar.Root.rotation, avatar.TargetRotation, turnBlend);
            }
        }

        private int ResolveActorFromCollider(Collider hitCollider)
        {
            if (hitCollider == null)
            {
                return -1;
            }

            SCFNetworkHitbox hitbox = hitCollider.GetComponentInParent<SCFNetworkHitbox>();
            return hitbox != null ? hitbox.ActorNumber : -1;
        }

        private void SpawnNetworkBeam(Vector3 start, Vector3 end)
        {
            GameObject beamObject = new GameObject("SCF_NetworkRailgunBeam");
            LineRenderer line = beamObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = 0.045f;
            line.endWidth = 0.012f;
            line.numCapVertices = 4;
            line.sharedMaterial = ResolveBeamMaterial();
            line.startColor = new Color(0.35f, 0.95f, 1f, 0.95f);
            line.endColor = new Color(1f, 1f, 1f, 0.08f);
            Destroy(beamObject, 0.14f);
        }

        private void OffsetLocalPlayerOnce()
        {
            if (!offsetLocalPlayerOnJoin || localSpawnOffsetApplied || localPlayerRoot == null)
            {
                return;
            }

            int index = Mathf.Max(0, PhotonNetwork.LocalPlayer.ActorNumber - 1);
            localPlayerRoot.position += Vector3.right * (index * spawnSpacing);
            localSpawnOffsetApplied = true;
        }

        private void DestroyRemoteAvatar(int actorNumber)
        {
            RemoteAvatar avatar;
            if (remoteAvatars.TryGetValue(actorNumber, out avatar) && avatar.Root != null)
            {
                Destroy(avatar.Root.gameObject);
            }

            remoteAvatars.Remove(actorNumber);
            knownHealth.Remove(actorNumber);
        }

        private void ClearRemoteAvatars()
        {
            List<int> actors = new List<int>(remoteAvatars.Keys);
            for (int i = 0; i < actors.Count; i++)
            {
                DestroyRemoteAvatar(actors[i]);
            }
        }

        private Material ResolveRemoteBlueMaterial()
        {
            if (remoteBlueMaterial == null)
            {
                remoteBlueMaterial = new Material(Shader.Find("HDRP/Lit") != null ? Shader.Find("HDRP/Lit") : Shader.Find("Standard"));
                remoteBlueMaterial.color = new Color(0.2f, 0.45f, 1f, 1f);
            }

            return remoteBlueMaterial;
        }

        private Material ResolveRemoteRedMaterial()
        {
            if (remoteRedMaterial == null)
            {
                remoteRedMaterial = new Material(Shader.Find("HDRP/Lit") != null ? Shader.Find("HDRP/Lit") : Shader.Find("Standard"));
                remoteRedMaterial.color = new Color(1f, 0.25f, 0.18f, 1f);
            }

            return remoteRedMaterial;
        }

        private Material ResolveBeamMaterial()
        {
            if (beamMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                beamMaterial = new Material(shader != null ? shader : Shader.Find("Unlit/Color"));
                beamMaterial.color = Color.white;
            }

            return beamMaterial;
        }

        private static T FindFirstSceneObject<T>() where T : UnityEngine.Object
        {
#if UNITY_2023_1_OR_NEWER
            return UnityEngine.Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
#else
            return UnityEngine.Object.FindObjectOfType<T>(true);
#endif
        }

        private sealed class RemoteAvatar
        {
            public readonly Transform Root;
            public Vector3 TargetPosition;
            public Quaternion TargetRotation;
            public float Health;

            public RemoteAvatar(Transform root)
            {
                Root = root;
            }
        }
    }

    public sealed class SCFNetworkHitbox : MonoBehaviour
    {
        public int ActorNumber;
    }
}
