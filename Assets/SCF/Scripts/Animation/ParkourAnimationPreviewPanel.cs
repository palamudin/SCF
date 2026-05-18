using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SCF.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ParkourAnimationPreviewPanel : MonoBehaviour
    {
        private static readonly string[] DefaultClipFolders =
        {
            "Assets/SCF/Animation",
            "Assets/SCF/MovementAni",
            "Assets/SCF/ThirdParty/DynamicParkour/Animations",
            "Assets/RedNotRed/3D Adaptive Parkour System/Animations/Player",
            "Assets/TPS Shooter (Military style)/Animations/Humanoid/EquipedAnimations",
            "Assets/TPS Shooter (Military style)/Animations/Humanoid/FreehandsAnimations"
        };

        [SerializeField] private Animator previewAnimator;
        [SerializeField] private List<AnimationClip> clips = new List<AnimationClip>();
        [SerializeField] private bool showPanel;
        [SerializeField] private KeyCode toggleKey = KeyCode.F7;
        [SerializeField] private bool loopPreview = true;
        [SerializeField] private float playbackSpeed = 1f;
        [SerializeField] private bool pauseScfMotionWhilePreview = true;
        [SerializeField] private Rect windowRect = new Rect(286f, 78f, 380f, 620f);
        [SerializeField] private Rect collapsedRect = new Rect(286f, 78f, 108f, 30f);

        private PlayableGraph graph;
        private AnimationClipPlayable clipPlayable;
        private AnimationClip currentClip;
        private Behaviour[] pausedAnimationDrivers = Array.Empty<Behaviour>();
        private bool[] pausedAnimationDriverStates = Array.Empty<bool>();
        private Vector2 scrollPosition;
        private string searchText = string.Empty;
        private static int nextWindowId = 32000;
        private int windowId;

        private void Reset()
        {
            previewAnimator = GetComponentInChildren<Animator>(true);
        }

        private void Awake()
        {
            if (windowId == 0)
            {
                windowId = ++nextWindowId;
            }

            if (previewAnimator == null)
            {
                previewAnimator = GetComponentInChildren<Animator>(true);
            }

#if UNITY_EDITOR
            if (clips.Count == 0)
            {
                LoadCandidateClipsFromProject();
            }
#endif
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                showPanel = !showPanel;
            }

            if (!clipPlayable.IsValid() || currentClip == null)
            {
                return;
            }

            clipPlayable.SetSpeed(Mathf.Max(0f, playbackSpeed));
            if (currentClip.length <= 0f || clipPlayable.GetTime() < currentClip.length)
            {
                return;
            }

            if (loopPreview)
            {
                clipPlayable.SetTime(0d);
            }
            else
            {
                clipPlayable.SetTime(currentClip.length);
                clipPlayable.SetSpeed(0d);
            }
        }

        private void OnDisable()
        {
            StopPreview();
        }

        private void OnGUI()
        {
            if (!showPanel)
            {
                if (GUI.Button(collapsedRect, "Animations"))
                {
                    showPanel = true;
                }

                return;
            }

            ClampWindowToScreen();
            windowRect = GUILayout.Window(windowId, windowRect, DrawWindow, "SCF Animation Browser");
        }

        public void SetPreviewAnimator(Animator animator)
        {
            showPanel = false;
            if (previewAnimator == animator)
            {
                return;
            }

            StopPreview();
            previewAnimator = animator;
        }

        public void Configure(Animator animator, IEnumerable<AnimationClip> animationClips)
        {
            previewAnimator = animator;
            showPanel = false;
            clips.Clear();

            if (animationClips != null)
            {
                foreach (AnimationClip clip in animationClips)
                {
                    if (clip != null && !clips.Contains(clip))
                    {
                        clips.Add(clip);
                    }
                }
            }

            SortClips();
        }

#if UNITY_EDITOR
        [ContextMenu("Load Candidate Clips From Project")]
        public void LoadCandidateClipsFromProject()
        {
            clips.Clear();

            string[] clipGuids = AssetDatabase.FindAssets("t:AnimationClip", DefaultClipFolders);
            for (int i = 0; i < clipGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(clipGuids[i]);
                AddClipsFromAsset(path);
            }

            SortClips();
        }

        private void AddClipsFromAsset(string path)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip clip
                    && !clip.name.StartsWith("__preview", StringComparison.OrdinalIgnoreCase)
                    && !clips.Contains(clip))
                {
                    clips.Add(clip);
                }
            }
        }
#endif

        private void DrawWindow(int windowId)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(previewAnimator != null ? "Target: " + previewAnimator.name : "Target: none");
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Hide", GUILayout.Width(52f), GUILayout.Height(22f)))
                {
                    showPanel = false;
                }
            }

            GUILayout.Label(currentClip != null ? "Playing: " + currentClip.name : "Playing: none");

            using (new GUILayout.HorizontalScope())
            {
                loopPreview = GUILayout.Toggle(loopPreview, "Loop", GUILayout.Width(68f));
                GUILayout.Label("Speed", GUILayout.Width(42f));
                playbackSpeed = GUILayout.HorizontalSlider(playbackSpeed, 0f, 2f);
                GUILayout.Label(playbackSpeed.ToString("0.00"), GUILayout.Width(38f));
            }

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Stop Preview", GUILayout.Height(26f)))
                {
                    StopPreview();
                }

                if (GUILayout.Button("Reload Clips", GUILayout.Height(26f)))
                {
#if UNITY_EDITOR
                    LoadCandidateClipsFromProject();
#endif
                }
            }

            searchText = GUILayout.TextField(searchText ?? string.Empty, GUI.skin.FindStyle("ToolbarSeachTextField") ?? GUI.skin.textField);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(Mathf.Max(180f, windowRect.height - 176f)));
            for (int i = 0; i < clips.Count; i++)
            {
                AnimationClip clip = clips[i];
                if (clip == null || !MatchesSearch(clip))
                {
                    continue;
                }

                Color previousColor = GUI.backgroundColor;
                if (clip == currentClip)
                {
                    GUI.backgroundColor = new Color(0.55f, 0.82f, 1f, 1f);
                }

                if (GUILayout.Button(clip.name, GUILayout.Height(30f)))
                {
                    PlayClip(clip);
                }

                GUI.backgroundColor = previousColor;
            }

            GUILayout.EndScrollView();
            GUILayout.Label(clips.Count + " clips loaded. Toggle with " + toggleKey + ".");
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 22f));
        }

        private bool MatchesSearch(AnimationClip clip)
        {
            return string.IsNullOrWhiteSpace(searchText)
                || clip.name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void PlayClip(AnimationClip clip)
        {
            if (previewAnimator == null || clip == null)
            {
                return;
            }

            PauseAnimationDrivers();
            StopGraphOnly();

            currentClip = clip;
            graph = PlayableGraph.Create("SCF Parkour Animation Preview");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Animation", previewAnimator);
            clipPlayable = AnimationClipPlayable.Create(graph, clip);
            clipPlayable.SetApplyFootIK(true);
            clipPlayable.SetTime(0d);
            clipPlayable.SetSpeed(Mathf.Max(0f, playbackSpeed));
            output.SetSourcePlayable(clipPlayable);

            graph.Play();
        }

        private void StopPreview()
        {
            StopGraphOnly();
            currentClip = null;
            RestoreAnimationDrivers();

            if (previewAnimator != null && previewAnimator.isActiveAndEnabled)
            {
                previewAnimator.Rebind();
                previewAnimator.Update(0f);
            }
        }

        private void StopGraphOnly()
        {
            if (graph.IsValid())
            {
                graph.Destroy();
            }

            clipPlayable = default;
        }

        private void PauseAnimationDrivers()
        {
            if (!pauseScfMotionWhilePreview || pausedAnimationDrivers.Length > 0)
            {
                return;
            }

            pausedAnimationDrivers = new Behaviour[]
            {
                GetComponent<SCFMotionSelector>(),
                GetComponent<MovementAnimatorBridge>(),
                GetComponent<MotionMatchingSignalHub>()
            };
            pausedAnimationDriverStates = new bool[pausedAnimationDrivers.Length];

            for (int i = 0; i < pausedAnimationDrivers.Length; i++)
            {
                Behaviour behaviour = pausedAnimationDrivers[i];
                if (behaviour != null)
                {
                    pausedAnimationDriverStates[i] = behaviour.enabled;
                    behaviour.enabled = false;
                }
            }
        }

        private void RestoreAnimationDrivers()
        {
            for (int i = 0; i < pausedAnimationDrivers.Length; i++)
            {
                Behaviour behaviour = pausedAnimationDrivers[i];
                if (behaviour != null)
                {
                    bool wasEnabled = i < pausedAnimationDriverStates.Length && pausedAnimationDriverStates[i];
                    behaviour.enabled = wasEnabled;
                }
            }

            pausedAnimationDrivers = Array.Empty<Behaviour>();
            pausedAnimationDriverStates = Array.Empty<bool>();
        }

        private void SortClips()
        {
            clips.Sort((left, right) => string.Compare(left != null ? left.name : string.Empty,
                right != null ? right.name : string.Empty,
                StringComparison.OrdinalIgnoreCase));
        }

        private void ClampWindowToScreen()
        {
            float maxWidth = Mathf.Max(260f, Screen.width - 24f);
            float maxHeight = Mathf.Max(260f, Screen.height - 24f);
            windowRect.width = Mathf.Clamp(windowRect.width, 260f, maxWidth);
            windowRect.height = Mathf.Clamp(windowRect.height, 260f, maxHeight);
            windowRect.x = Mathf.Clamp(windowRect.x, 0f, Mathf.Max(0f, Screen.width - windowRect.width));
            windowRect.y = Mathf.Clamp(windowRect.y, 0f, Mathf.Max(0f, Screen.height - windowRect.height));
        }
    }
}
