using UnityEngine;
using UnityEngine.InputSystem;

namespace SCF.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SCFCharacterSelectionPanel : MonoBehaviour
    {
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
            return displayName.IndexOf("FullPlayer", System.StringComparison.OrdinalIgnoreCase) >= 0
                   || prefabName.IndexOf("FullPlayer", System.StringComparison.OrdinalIgnoreCase) >= 0
                   || IsBlockedFighter(displayName)
                   || IsBlockedFighter(prefabName);
        }

        private static bool IsBlockedFighter(string value)
        {
            return string.Equals(value, "Fighter_08", System.StringComparison.OrdinalIgnoreCase)
                   || string.Equals(value, "Fighter_09", System.StringComparison.OrdinalIgnoreCase)
                   || string.Equals(value, "Fighter_10", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
