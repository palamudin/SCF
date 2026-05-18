using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SCF.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SCFCharacterSelectionPanel : MonoBehaviour
    {
        private const string SoldierDisplayName = "TPS Soldier";
        private const string ExperimentalSoldierDisplayName = "soldierExp";
        private const string FrankDisplayName = "Parkour Frank";

        [SerializeField] private SCFCharacterVisualSlot visualSlot;
        [SerializeField] private SCFCharacterCandidate[] candidates;
        [SerializeField] private bool visible = true;
        [SerializeField] private Key toggleKey = Key.F8;
        [SerializeField] private Vector2 panelPosition = new Vector2(16f, 78f);
        [SerializeField] private Vector2 panelSize = new Vector2(260f, 420f);
        [SerializeField] private Vector2 collapsedButtonSize = new Vector2(92f, 30f);

        private Vector2 scroll;
        private int selectedIndex = -1;

        private void Awake()
        {
            if (visualSlot == null)
            {
                visualSlot = GetComponent<SCFCharacterVisualSlot>();
            }

            NormalizeCandidateList();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard[toggleKey].wasPressedThisFrame)
            {
                visible = !visible;
            }
        }

        public void Configure(SCFCharacterVisualSlot slot, SCFCharacterCandidate[] characterCandidates)
        {
            visualSlot = slot;
            candidates = characterCandidates;
            NormalizeCandidateList();
        }

        private void OnGUI()
        {
            if (!visible)
            {
                Rect openRect = new Rect(panelPosition, collapsedButtonSize);
                if (GUI.Button(openRect, "Chars"))
                {
                    visible = true;
                }

                return;
            }

            Rect panelRect = new Rect(panelPosition, panelSize);
            GUILayout.BeginArea(panelRect, GUI.skin.window);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Char Selection");
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Slide", GUILayout.Width(58f), GUILayout.Height(22f)))
                {
                    visible = false;
                }
            }

            string activeName = visualSlot != null && !string.IsNullOrWhiteSpace(visualSlot.ActiveCharacterName)
                ? visualSlot.ActiveCharacterName
                : "none";
            GUILayout.Label("Active: " + activeName);

            scroll = GUILayout.BeginScrollView(scroll);
            if (candidates != null)
            {
                for (int i = 0; i < candidates.Length; i++)
                {
                    SCFCharacterCandidate candidate = candidates[i];
                    if (candidate == null || candidate.Prefab == null || IsBlockedCandidate(candidate))
                    {
                        continue;
                    }

                    GUI.enabled = i != selectedIndex;
                    if (GUILayout.Button(candidate.DisplayName, GUILayout.Height(30f)))
                    {
                        selectedIndex = i;
                        if (visualSlot != null)
                        {
                            visualSlot.ApplyCandidate(candidate);
                        }
                    }
                    GUI.enabled = true;
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Label("Toggle F8");
            GUILayout.EndArea();
        }

        private static bool IsBlockedCandidate(SCFCharacterCandidate candidate)
        {
            string displayName = candidate.DisplayName ?? string.Empty;
            string prefabName = candidate.Prefab != null ? candidate.Prefab.name : string.Empty;
            return !IsAllowedCandidate(displayName)
                   || displayName.IndexOf("FullPlayer", System.StringComparison.OrdinalIgnoreCase) >= 0
                   || prefabName.IndexOf("FullPlayer", System.StringComparison.OrdinalIgnoreCase) >= 0
                   || IsBlockedFighter(displayName)
                   || IsBlockedFighter(prefabName);
        }

        private void NormalizeCandidateList()
        {
            if (candidates == null || candidates.Length == 0)
            {
                return;
            }

            SCFCharacterCandidate soldier = FindCandidate(SoldierDisplayName);
            if (soldier == null)
            {
                soldier = FindCandidate("Soldier");
            }

            SCFCharacterCandidate experimentalSoldier = FindCandidate(ExperimentalSoldierDisplayName);
            if (experimentalSoldier == null && soldier != null)
            {
                experimentalSoldier = new SCFCharacterCandidate(
                    ExperimentalSoldierDisplayName,
                    soldier.Prefab,
                    soldier.LocalPosition,
                    soldier.LocalEulerAngles,
                    soldier.LocalScale);
            }

            SCFCharacterCandidate frank = FindCandidate(FrankDisplayName);
            if (frank == null)
            {
                frank = FindCandidate("Frank");
            }

            List<SCFCharacterCandidate> filtered = new List<SCFCharacterCandidate>(3);
            AddCandidateIfValid(filtered, soldier);
            AddCandidateIfValid(filtered, experimentalSoldier);
            AddCandidateIfValid(filtered, frank);

            candidates = filtered.ToArray();
            if (selectedIndex >= candidates.Length)
            {
                selectedIndex = -1;
            }
        }

        private SCFCharacterCandidate FindCandidate(string name)
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                SCFCharacterCandidate candidate = candidates[i];
                if (candidate == null)
                {
                    continue;
                }

                string displayName = candidate.DisplayName ?? string.Empty;
                string prefabName = candidate.Prefab != null ? candidate.Prefab.name : string.Empty;
                if (displayName.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0
                    || prefabName.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void AddCandidateIfValid(List<SCFCharacterCandidate> target, SCFCharacterCandidate candidate)
        {
            if (candidate == null || candidate.Prefab == null || target.Contains(candidate))
            {
                return;
            }

            target.Add(candidate);
        }

        private static bool IsAllowedCandidate(string displayName)
        {
            return string.Equals(displayName, SoldierDisplayName, System.StringComparison.OrdinalIgnoreCase)
                   || string.Equals(displayName, "Soldier", System.StringComparison.OrdinalIgnoreCase)
                   || string.Equals(displayName, ExperimentalSoldierDisplayName, System.StringComparison.OrdinalIgnoreCase)
                   || string.Equals(displayName, FrankDisplayName, System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsBlockedFighter(string value)
        {
            return string.Equals(value, "Fighter_08", System.StringComparison.OrdinalIgnoreCase)
                   || string.Equals(value, "Fighter_09", System.StringComparison.OrdinalIgnoreCase)
                   || string.Equals(value, "Fighter_10", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
