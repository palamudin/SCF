using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SCF.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SCFWeaponSelectionPanel : MonoBehaviour
    {
        private static readonly string[] DefaultWeaponPrefabFolders =
        {
            "Assets/SCF/Prefabs/Weapons"
        };

        [SerializeField] private SCFWeaponVisualSlot weaponSlot;
        [SerializeField] private List<GameObject> weaponPrefabs = new List<GameObject>();
        [SerializeField] private bool visible = true;
        [SerializeField] private Key toggleKey = Key.F6;
        [SerializeField] private Rect windowRect = new Rect(286f, 78f, 360f, 360f);
        [SerializeField] private Rect collapsedRect = new Rect(286f, 78f, 104f, 30f);

        private Vector2 scroll;
        private string searchText = string.Empty;
        private static int nextWindowId = 34000;
        private int windowId;

        private void Awake()
        {
            if (windowId == 0)
            {
                windowId = ++nextWindowId;
            }

            if (weaponSlot == null)
            {
                weaponSlot = GetComponent<SCFWeaponVisualSlot>();
            }

#if UNITY_EDITOR
            if (weaponPrefabs.Count == 0)
            {
                LoadWeaponPrefabsFromProject();
            }
#endif
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard[toggleKey].wasPressedThisFrame)
            {
                visible = !visible;
            }
        }

        public void Configure(SCFWeaponVisualSlot slot)
        {
            weaponSlot = slot;
            visible = true;
        }

#if UNITY_EDITOR
        [ContextMenu("Load Weapon Prefabs From Project")]
        public void LoadWeaponPrefabsFromProject()
        {
            weaponPrefabs.Clear();
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", DefaultWeaponPrefabFolders);
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && !weaponPrefabs.Contains(prefab))
                {
                    weaponPrefabs.Add(prefab);
                }
            }

            weaponPrefabs.Sort((left, right) => string.Compare(left != null ? left.name : string.Empty, right != null ? right.name : string.Empty, StringComparison.OrdinalIgnoreCase));
        }
#endif

        private void OnGUI()
        {
            if (!visible)
            {
                ClampCollapsedToScreen();
                if (GUI.Button(collapsedRect, "Weapons"))
                {
                    visible = true;
                }

                return;
            }

            ClampWindowToScreen();
            windowRect = GUILayout.Window(windowId, windowRect, DrawWindow, "SCF Weapon Browser");
        }

        private void DrawWindow(int windowId)
        {
            DrawHeader();
            DrawToolbar();
            DrawWeaponList();
            GUILayout.Label("Toggle " + toggleKey);
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 22f));
        }

        private void DrawHeader()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Weapon Selection");
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Slide", GUILayout.Width(58f), GUILayout.Height(22f)))
                {
                    visible = false;
                }
            }

            string activeName = weaponSlot != null ? weaponSlot.ActiveWeaponName : "none";
            GUILayout.Label("Active: " + activeName);
        }

        private void DrawToolbar()
        {
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Unequip", GUILayout.Height(26f)))
                {
                    weaponSlot?.UnequipWeapon();
                }

                if (GUILayout.Button("Default", GUILayout.Height(26f)))
                {
                    weaponSlot?.EquipDefaultRailgun();
                }

                if (GUILayout.Button("Reload", GUILayout.Height(26f)))
                {
#if UNITY_EDITOR
                    LoadWeaponPrefabsFromProject();
#endif
                }
            }

            searchText = GUILayout.TextField(searchText ?? string.Empty, GUI.skin.FindStyle("ToolbarSeachTextField") ?? GUI.skin.textField);
        }

        private void DrawWeaponList()
        {
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(Mathf.Max(120f, windowRect.height - 132f)));
            for (int i = 0; i < weaponPrefabs.Count; i++)
            {
                GameObject prefab = weaponPrefabs[i];
                if (prefab == null || !MatchesSearch(prefab))
                {
                    continue;
                }

                bool isSelected = weaponSlot != null && weaponSlot.SelectedWeaponPrototype == prefab;
                Color previousColor = GUI.backgroundColor;
                if (isSelected)
                {
                    GUI.backgroundColor = new Color(0.55f, 0.82f, 1f, 1f);
                }

                if (GUILayout.Button(prefab.name, GUILayout.Height(30f)))
                {
                    weaponSlot?.EquipWeaponPrefab(prefab);
                }

                GUI.backgroundColor = previousColor;
            }

            GUILayout.EndScrollView();
        }

        private bool MatchesSearch(GameObject prefab)
        {
            return string.IsNullOrWhiteSpace(searchText)
                   || prefab.name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void ClampWindowToScreen()
        {
            windowRect.width = Mathf.Clamp(windowRect.width, 240f, Mathf.Max(240f, Screen.width - 16f));
            windowRect.height = Mathf.Clamp(windowRect.height, 180f, Mathf.Max(180f, Screen.height - 16f));
            windowRect.x = Mathf.Clamp(windowRect.x, 0f, Mathf.Max(0f, Screen.width - windowRect.width));
            windowRect.y = Mathf.Clamp(windowRect.y, 0f, Mathf.Max(0f, Screen.height - windowRect.height));
        }

        private void ClampCollapsedToScreen()
        {
            collapsedRect.x = Mathf.Clamp(collapsedRect.x, 0f, Mathf.Max(0f, Screen.width - collapsedRect.width));
            collapsedRect.y = Mathf.Clamp(collapsedRect.y, 0f, Mathf.Max(0f, Screen.height - collapsedRect.height));
        }
    }
}
