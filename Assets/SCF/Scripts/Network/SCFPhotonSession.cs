using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

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
        [SerializeField] private SCFMotionSelector localMotionSelector;
        [SerializeField] private IsometricPlayerInput localInput;
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField, Min(1f)] private float railgunDamage = 34f;

        [Header("Damage")]
        [SerializeField, Min(0.1f)] private float playerCollisionRadius = 0.85f;
        [SerializeField, Min(0f)] private float playerCollisionMinRelativeSpeed = 1.2f;
        [SerializeField, Min(0f)] private float playerCollisionDamage = 7f;
        [SerializeField, Min(0f)] private float playerCollisionDamageCooldown = 0.65f;
        [SerializeField, Range(0f, 1f)] private float playerCollisionMomentumProtection = 0.65f;
        [SerializeField, Min(0f)] private float deathLeaveDelay = 1.25f;
        [SerializeField, Min(0.05f)] private float proceduralDeathDuration = 0.45f;
        [SerializeField] private bool leaveRoomOnDeath = true;
        [SerializeField] private bool disconnectOnDeath;

        [Header("Death Echo")]
        [SerializeField] private bool spawnDeathEcho = true;
        [SerializeField, Min(1f)] private float deathEchoLifetime = 60f;
        [SerializeField, Min(0.1f)] private float deathEchoDisturbedLifetime = 6f;
        [SerializeField, Min(0.1f)] private float deathEchoFadeDuration = 8f;

        [Header("Sync")]
        [SerializeField, Range(2f, 30f)] private float stateSendRate = 12f;
        [SerializeField, Min(0.1f)] private float remoteMoveSharpness = 16f;
        [SerializeField, Min(0.1f)] private float remoteTurnSharpness = 18f;
        [SerializeField, Min(0.1f)] private float remoteHeight = 1.8f;

        [Header("GUI")]
        [SerializeField] private Rect panelRect = new Rect(16f, 510f, 290f, 170f);
        [SerializeField] private bool showInputBindingPanel = true;
        [SerializeField] private Rect inputBindingPanelRect = new Rect(16f, 688f, 290f, 132f);
        [SerializeField] private Vector2 collapsedPanelSize = new Vector2(112f, 30f);

        private readonly Dictionary<int, RemoteAvatar> remoteAvatars = new Dictionary<int, RemoteAvatar>();
        private readonly Dictionary<int, float> knownHealth = new Dictionary<int, float>();
        private SCFWeaponVisualSlot subscribedWeaponSlot;
        private float localHealth;
        private float nextStateSendTime;
        private bool localDead;
        private float localDeathLeaveTime;
        private SCFNetworkDeathCue localDeathCue;
        private Transform hiddenLocalDeathVisual;
        private bool localSpawnOffsetApplied;
        private Material remoteBlueMaterial;
        private Material remoteRedMaterial;
        private Material beamMaterial;
        private SCFInputBinding pendingInputBinding;

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
            TickDeathLeave();
            TickStateSend();
            TickRemoteAvatars();
            TickPlayerCollisionDamage();
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
            if (showInputBindingPanel)
            {
                inputBindingPanelRect.x = panelRect.x;
                inputBindingPanelRect.y = panelRect.yMax + 8f;
                inputBindingPanelRect.width = panelRect.width;
                inputBindingPanelRect = GUILayout.Window(GuiWindowId + 1, inputBindingPanelRect, DrawInputBindingPanel, "SCF Controls");
            }
            else
            {
                Rect controlsOpenRect = new Rect(panelRect.x, panelRect.yMax + 8f, collapsedPanelSize.x, collapsedPanelSize.y);
                if (GUI.Button(controlsOpenRect, "Controls"))
                {
                    showInputBindingPanel = true;
                }
            }
        }

        private void DrawPanel(int windowId)
        {
            GUILayout.Label("State: " + PhotonNetwork.NetworkClientState);
            GUILayout.Label("Room: " + (PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.Name + " (" + PhotonNetwork.CurrentRoom.PlayerCount + "/" + PhotonNetwork.CurrentRoom.MaxPlayers + ")" : "none"));
            GUILayout.Label("Health: " + Mathf.CeilToInt(localHealth) + " / " + Mathf.CeilToInt(maxHealth) + (localDead ? " DOWN" : ""));
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

        private void DrawInputBindingPanel(int windowId)
        {
            ResolveLocalReferences();
            if (localInput == null)
            {
                GUILayout.Label("Input: none");
                if (GUILayout.Button("Hide", GUILayout.Width(70f)))
                {
                    showInputBindingPanel = false;
                }

                GUI.DragWindow(new Rect(0f, 0f, 10000f, 22f));
                return;
            }

            DrawBindingRow(SCFInputBinding.Jump);
            DrawBindingRow(SCFInputBinding.WalkToggle);
            DrawBindingRow(SCFInputBinding.Slide);

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset", GUILayout.Width(70f), GUILayout.Height(23f)))
                {
                    localInput.ResetFallbackBindings();
                    pendingInputBinding = SCFInputBinding.None;
                }

                if (GUILayout.Button("Hide", GUILayout.Width(70f), GUILayout.Height(23f)))
                {
                    showInputBindingPanel = false;
                    pendingInputBinding = SCFInputBinding.None;
                }

                if (pendingInputBinding != SCFInputBinding.None)
                {
                    GUILayout.Label("Press key or Esc");
                }
            }

            CapturePendingBinding(Event.current);
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 22f));
        }

        private void DrawBindingRow(SCFInputBinding binding)
        {
            string bindingName = IsometricPlayerInput.FormatBindingName(binding);
            string keyName = IsometricPlayerInput.FormatBindingKey(localInput.GetFallbackBinding(binding));
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(bindingName + ": " + keyName, GUILayout.Width(150f));
                if (GUILayout.Button(pendingInputBinding == binding ? "..." : "Rebind", GUILayout.Width(82f), GUILayout.Height(22f)))
                {
                    pendingInputBinding = binding;
                }
            }
        }

        private void CapturePendingBinding(Event current)
        {
            if (pendingInputBinding == SCFInputBinding.None || current == null || current.type != EventType.KeyDown)
            {
                return;
            }

            if (current.keyCode == KeyCode.Escape)
            {
                pendingInputBinding = SCFInputBinding.None;
                current.Use();
                return;
            }

            Key key;
            if (IsometricPlayerInput.TryConvertKeyCode(current.keyCode, out key))
            {
                localInput.SetFallbackBinding(pendingInputBinding, key);
                pendingInputBinding = SCFInputBinding.None;
                current.Use();
            }
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
            localDead = false;
            localDeathLeaveTime = 0f;
            localHealth = maxHealth;
            knownHealth[PhotonNetwork.LocalPlayer.ActorNumber] = localHealth;
            ResetLocalDeathCue();
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
            localDead = false;
            localDeathLeaveTime = 0f;
            localSpawnOffsetApplied = false;
            ClearRemoteAvatars();
            ResetLocalDeathCue();
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

            if (localMotionSelector == null && localPlayerRoot != null)
            {
                localMotionSelector = localPlayerRoot.GetComponent<SCFMotionSelector>();
            }

            if (localInput == null && localPlayerRoot != null)
            {
                localInput = localPlayerRoot.GetComponent<IsometricPlayerInput>();
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
                localHealth,
                localMotionSelector != null ? localMotionSelector.SelectedMotionIndex : -1,
                localMotor != null ? localMotor.PlanarVelocity : Vector3.zero,
                localMotor != null ? localMotor.PlanarVelocity.magnitude : 0f,
                localDead
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
            avatar.MotionIndex = data.Length > 4 ? Convert.ToInt32(data[4]) : -1;
            avatar.TargetVelocity = data.Length > 5 ? (Vector3)data[5] : Vector3.zero;
            avatar.MotionSpeed = data.Length > 6 ? Convert.ToSingle(data[6]) : avatar.TargetVelocity.magnitude;
            avatar.Dead = (data.Length > 7 && Convert.ToBoolean(data[7])) || avatar.Health <= 0.001f;
            knownHealth[actor] = avatar.Health;
            if (avatar.Dead)
            {
                PlayRemoteDeathCue(avatar);
            }
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

            NotifyDeathEchoShot(shot.HitCollider);
            ProbeDeathEchoShot(shot.Muzzle, shot.Impact);
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

            Vector3 start = (Vector3)data[1];
            Vector3 end = (Vector3)data[2];
            SpawnNetworkBeam(start, end);
            ProbeDeathEchoShot(start, end);
        }

        private void SendDamage(int targetActor, float damage)
        {
            if (targetActor <= 0 || damage <= 0.001f || !PhotonNetwork.InRoom)
            {
                return;
            }

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
            int sourceActor = Convert.ToInt32(data[1]);
            float damage = Convert.ToSingle(data[2]);
            if (targetActor == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                if (localDead)
                {
                    return;
                }

                localHealth = Mathf.Max(0f, localHealth - damage);
                knownHealth[targetActor] = localHealth;
                if (localHealth <= 0.001f)
                {
                    HandleLocalDeath(sourceActor);
                    return;
                }

                SendState(true);
                return;
            }

            RemoteAvatar avatar;
            if (remoteAvatars.TryGetValue(targetActor, out avatar))
            {
                avatar.Health = Mathf.Max(0f, avatar.Health - damage);
                avatar.Dead = avatar.Health <= 0.001f;
                knownHealth[targetActor] = avatar.Health;
                if (avatar.Dead)
                {
                    PlayRemoteDeathCue(avatar);
                }
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
            collider.isTrigger = true;

            SCFNetworkHitbox hitbox = root.AddComponent<SCFNetworkHitbox>();
            hitbox.ActorNumber = actorNumber;

            GameObject visual = CreateRemoteVisual(root.transform, actorNumber);
            avatar = new RemoteAvatar(root.transform)
            {
                VisualRoot = visual != null ? visual.transform : root.transform,
                TargetPosition = root.transform.position,
                TargetRotation = root.transform.rotation,
                Health = maxHealth,
                MotionPlayback = new RemoteMotionPlayback(visual, ResolveMotionDatabase())
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

                Vector3 previousPosition = avatar.Root.position;
                avatar.Root.position = Vector3.Lerp(avatar.Root.position, avatar.TargetPosition, moveBlend);
                avatar.Root.rotation = Quaternion.Slerp(avatar.Root.rotation, avatar.TargetRotation, turnBlend);
                avatar.EstimatedVelocity = Time.deltaTime > 0.0001f ? (avatar.Root.position - previousPosition) / Time.deltaTime : Vector3.zero;
                if (avatar.MotionPlayback != null)
                {
                    avatar.MotionPlayback.ConfigureDatabase(ResolveMotionDatabase());
                    avatar.MotionPlayback.Tick(avatar.MotionIndex, avatar.MotionSpeed, avatar.Dead);
                }
            }
        }

        private void TickPlayerCollisionDamage()
        {
            if (!PhotonNetwork.InRoom || localDead || localPlayerRoot == null)
            {
                return;
            }

            int localActor = PhotonNetwork.LocalPlayer.ActorNumber;
            Vector3 localVelocity = localMotor != null ? localMotor.PlanarVelocity : Vector3.zero;
            float radiusSqr = playerCollisionRadius * playerCollisionRadius;

            foreach (KeyValuePair<int, RemoteAvatar> pair in remoteAvatars)
            {
                int remoteActor = pair.Key;
                RemoteAvatar avatar = pair.Value;
                if (avatar == null || avatar.Root == null || avatar.Dead || avatar.Health <= 0.001f)
                {
                    continue;
                }

                if (localActor > remoteActor || Time.time < avatar.NextCollisionDamageTime)
                {
                    continue;
                }

                Vector3 delta = localPlayerRoot.position - avatar.Root.position;
                delta.y = 0f;
                if (delta.sqrMagnitude > radiusSqr)
                {
                    continue;
                }

                Vector3 remoteVelocity = avatar.TargetVelocity.sqrMagnitude > 0.0001f ? avatar.TargetVelocity : avatar.EstimatedVelocity;
                float relativeSpeed = (localVelocity - remoteVelocity).magnitude;
                if (relativeSpeed < playerCollisionMinRelativeSpeed)
                {
                    continue;
                }

                float localMomentum = localVelocity.magnitude;
                float remoteMomentum = remoteVelocity.magnitude;
                float totalMomentum = Mathf.Max(0.001f, localMomentum + remoteMomentum);
                float localDamageShare = Mathf.Lerp(0.5f, remoteMomentum / totalMomentum, playerCollisionMomentumProtection);
                float impactScale = Mathf.Clamp(relativeSpeed / Mathf.Max(0.1f, playerCollisionMinRelativeSpeed), 0.35f, 2.5f);
                float totalDamage = playerCollisionDamage * impactScale;

                SendDamage(localActor, totalDamage * localDamageShare);
                SendDamage(remoteActor, totalDamage * (1f - localDamageShare));
                avatar.NextCollisionDamageTime = Time.time + playerCollisionDamageCooldown;
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
            if (remoteAvatars.TryGetValue(actorNumber, out avatar))
            {
                if (avatar.MotionPlayback != null)
                {
                    avatar.MotionPlayback.Destroy();
                }

                if (avatar.Root != null)
                {
                    Destroy(avatar.Root.gameObject);
                }
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

        private SCFMotionDatabase ResolveMotionDatabase()
        {
            if (localMotionSelector != null)
            {
                return localMotionSelector.Database;
            }

            SCFMotionSelector selector = FindFirstSceneObject<SCFMotionSelector>();
            return selector != null ? selector.Database : null;
        }

        private void HandleLocalDeath(int sourceActor)
        {
            if (localDead)
            {
                return;
            }

            localDead = true;
            localHealth = 0f;
            knownHealth[PhotonNetwork.LocalPlayer.ActorNumber] = localHealth;
            Transform cueTarget = localVisualSlot != null && localVisualSlot.ActiveVisual != null ? localVisualSlot.ActiveVisual.transform : localPlayerRoot;
            SpawnDeathEcho(cueTarget != null ? cueTarget.gameObject : null, localPlayerRoot);
            HideLocalDeathVisual(cueTarget);
            localDeathLeaveTime = Time.unscaledTime + deathLeaveDelay;
            SendState(true);
        }

        private void TickDeathLeave()
        {
            if (!localDead || !leaveRoomOnDeath || localDeathLeaveTime <= 0f || Time.unscaledTime < localDeathLeaveTime)
            {
                return;
            }

            localDeathLeaveTime = 0f;
            if (disconnectOnDeath)
            {
                PhotonNetwork.Disconnect();
            }
            else if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }
        }

        private SCFNetworkDeathCue PlayDeathCue(Transform target)
        {
            if (target == null)
            {
                return null;
            }

            SCFNetworkDeathCue cue = target.GetComponent<SCFNetworkDeathCue>();
            if (cue == null)
            {
                cue = target.gameObject.AddComponent<SCFNetworkDeathCue>();
            }

            cue.Play(proceduralDeathDuration);
            return cue;
        }

        private void PlayRemoteDeathCue(RemoteAvatar avatar)
        {
            if (avatar == null || avatar.DeathCue != null)
            {
                return;
            }

            Transform target = avatar.VisualRoot != null ? avatar.VisualRoot : avatar.Root;
            if (avatar.DeathEcho == null)
            {
                avatar.DeathEcho = SpawnDeathEcho(target != null ? target.gameObject : null, avatar.Root);
                SetRenderersVisible(target, false);
            }

            avatar.DeathCue = PlayDeathCue(target);
            if (avatar.MotionPlayback != null)
            {
                avatar.MotionPlayback.Stop();
            }
        }

        private void ResetLocalDeathCue()
        {
            if (localDeathCue != null)
            {
                localDeathCue.ResetPose();
                localDeathCue = null;
            }

            if (hiddenLocalDeathVisual != null)
            {
                SetRenderersVisible(hiddenLocalDeathVisual, true);
                hiddenLocalDeathVisual = null;
            }
        }

        private SCFDeathEcho SpawnDeathEcho(GameObject sourceVisual, Transform fallbackRoot)
        {
            if (!spawnDeathEcho)
            {
                return null;
            }

            return SCFDeathEcho.Spawn(sourceVisual, fallbackRoot, deathEchoLifetime, deathEchoDisturbedLifetime, deathEchoFadeDuration, proceduralDeathDuration);
        }

        private void HideLocalDeathVisual(Transform target)
        {
            if (target == null)
            {
                return;
            }

            hiddenLocalDeathVisual = target;
            SetRenderersVisible(hiddenLocalDeathVisual, false);
        }

        private void NotifyDeathEchoShot(Collider hitCollider)
        {
            if (hitCollider == null)
            {
                return;
            }

            SCFDeathEcho echo = hitCollider.GetComponentInParent<SCFDeathEcho>();
            if (echo != null)
            {
                echo.NotifyWeaponHit();
            }
        }

        private void ProbeDeathEchoShot(Vector3 start, Vector3 end)
        {
            Vector3 delta = end - start;
            float distance = delta.magnitude;
            if (distance <= 0.001f)
            {
                return;
            }

            RaycastHit[] hits = Physics.RaycastAll(start, delta / distance, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            if (hits == null || hits.Length == 0)
            {
                return;
            }

            for (int i = 0; i < hits.Length; i++)
            {
                NotifyDeathEchoShot(hits[i].collider);
            }
        }

        private static void SetRenderersVisible(Transform root, bool visible)
        {
            if (root == null)
            {
                return;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = visible;
                }
            }
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
            public Transform VisualRoot;
            public Vector3 TargetPosition;
            public Quaternion TargetRotation;
            public Vector3 TargetVelocity;
            public Vector3 EstimatedVelocity;
            public int MotionIndex = -1;
            public float MotionSpeed;
            public float Health;
            public bool Dead;
            public float NextCollisionDamageTime;
            public RemoteMotionPlayback MotionPlayback;
            public SCFNetworkDeathCue DeathCue;
            public SCFDeathEcho DeathEcho;

            public RemoteAvatar(Transform root)
            {
                Root = root;
            }
        }

        private sealed class RemoteMotionPlayback
        {
            private readonly Animator animator;
            private SCFMotionDatabase database;
            private PlayableGraph graph;
            private AnimationClipPlayable clipPlayable;
            private int activeMotionIndex = -1;

            public RemoteMotionPlayback(GameObject visual, SCFMotionDatabase database)
            {
                this.database = database;
                animator = visual != null ? visual.GetComponentInChildren<Animator>(true) : null;
                if (animator != null)
                {
                    animator.applyRootMotion = false;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }
            }

            public void ConfigureDatabase(SCFMotionDatabase targetDatabase)
            {
                if (database == null && targetDatabase != null)
                {
                    database = targetDatabase;
                }
            }

            public void Tick(int motionIndex, float motionSpeed, bool dead)
            {
                if (dead)
                {
                    Stop();
                    return;
                }

                if (animator == null || database == null || motionIndex < 0)
                {
                    return;
                }

                if (activeMotionIndex != motionIndex)
                {
                    SwitchToMotion(motionIndex);
                }

                if (!clipPlayable.IsValid() || !database.TryGetClip(activeMotionIndex, out SCFMotionClipData clipData))
                {
                    return;
                }

                float speed = 1f;
                if (clipData.MotionType == SCFMotionType.Locomotion && clipData.AveragePlanarSpeed > 0.1f)
                {
                    speed = Mathf.Clamp(motionSpeed / clipData.AveragePlanarSpeed, 0.75f, 1.35f);
                }

                clipPlayable.SetSpeed(speed);
                double duration = Mathf.Max(0.01f, clipData.Duration);
                double time = clipPlayable.GetTime();
                if (clipData.Looping)
                {
                    if (time >= duration || time < 0d)
                    {
                        clipPlayable.SetTime(time - Math.Floor(time / duration) * duration);
                    }
                }
                else if (time > duration)
                {
                    clipPlayable.SetTime(duration);
                    clipPlayable.SetSpeed(0d);
                }
            }

            public void Stop()
            {
                Destroy();
                activeMotionIndex = -1;
            }

            public void Destroy()
            {
                if (graph.IsValid())
                {
                    graph.Destroy();
                }

                clipPlayable = default;
            }

            private void SwitchToMotion(int motionIndex)
            {
                if (!database.TryGetClip(motionIndex, out SCFMotionClipData clipData))
                {
                    return;
                }

                Destroy();
                graph = PlayableGraph.Create("SCF_RemoteMotion_" + animator.gameObject.name);
                graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
                AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "RemoteMotion", animator);
                clipPlayable = AnimationClipPlayable.Create(graph, clipData.Clip);
                clipPlayable.SetApplyFootIK(true);
                clipPlayable.SetDuration(Mathf.Max(0.01f, clipData.Duration));
                clipPlayable.SetTime(0d);
                output.SetSourcePlayable(clipPlayable);
                graph.Play();
                activeMotionIndex = motionIndex;
            }
        }
    }

    public sealed class SCFNetworkHitbox : MonoBehaviour
    {
        public int ActorNumber;
    }
}
