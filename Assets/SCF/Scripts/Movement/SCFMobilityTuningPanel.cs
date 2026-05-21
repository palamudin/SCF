using UnityEngine;

namespace SCF.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SCFMobilityTuningPanel : MonoBehaviour
    {
        private const string UtilityObjectName = "SCF_MobilityTuningPanel";
        private const float ResizeGripSize = 18f;

        [SerializeField] private IsometricCharacterMotor motor;
        [SerializeField] private bool visible = true;
        [SerializeField] private Rect windowRect = new Rect(286f, 12f, 620f, 224f);
        [SerializeField] private Rect collapsedRect = new Rect(286f, 12f, 112f, 30f);
        [SerializeField, Min(1f)] private float momentumCapMax = 135f;
        [SerializeField, Min(1f)] private float retentionMax = 10f;
        [SerializeField, Min(1f)] private float slideJumpBoostMax = 14f;
        [SerializeField, Min(1f)] private float airStartBoostMax = 10f;
        [SerializeField, Min(1f)] private float groundSteerMax = 75f;
        [SerializeField, Min(1f)] private float airSteerMax = 120f;

        private static int nextWindowId = 43000;
        private int windowId;
        private bool resizingWindow;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<SCFMobilityTuningPanel>() != null)
            {
                return;
            }

            new GameObject(UtilityObjectName).AddComponent<SCFMobilityTuningPanel>();
        }

        private void Awake()
        {
            if (windowId == 0)
            {
                windowId = ++nextWindowId;
            }

            ResolveMotor();
        }

        private void OnGUI()
        {
            if (!visible)
            {
                ClampCollapsedToScreen();
                if (GUI.Button(collapsedRect, "Mobility"))
                {
                    visible = true;
                }

                return;
            }

            ResolveMotor();
            ClampWindowToScreen();
            windowRect = GUILayout.Window(windowId, windowRect, DrawWindow, "SCF Mobility Tuning");
        }

        private void DrawWindow(int id)
        {
            if (motor == null)
            {
                GUILayout.Label("No IsometricCharacterMotor found.");
                DrawFooter();
                return;
            }

            using (new GUILayout.HorizontalScope())
            {
                using (new GUILayout.VerticalScope())
                {
                    DrawSlider("Momentum Cap", motor.SlideMomentumSpeedCap, 0f, momentumCapMax, value => motor.SlideMomentumSpeedCap = value);
                    DrawSlider("Air Retention", motor.AirSlideMomentumRetention, 0f, retentionMax, value => motor.AirSlideMomentumRetention = value);
                    DrawSlider("Slide Jump Boost", motor.SlideJumpPlanarBoost, 0f, slideJumpBoostMax, value => motor.SlideJumpPlanarBoost = value);
                }

                GUILayout.Space(12f);

                using (new GUILayout.VerticalScope())
                {
                    DrawSlider("Air Start Boost", motor.AirSlideStartBoost, 0f, airStartBoostMax, value => motor.AirSlideStartBoost = value);
                    DrawSlider("Ground Steer", motor.SlideGroundSteerSharpness, 0.1f, groundSteerMax, value => motor.SlideGroundSteerSharpness = value);
                    DrawSlider("Air Steer", motor.SlideAirSteerSharpness, 0.1f, airSteerMax, value => motor.SlideAirSteerSharpness = value);
                }
            }

            GUILayout.Space(6f);
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Game-ish", GUILayout.Height(24f)))
                {
                    ApplyGameishDefaults();
                }

                if (GUILayout.Button("Absurd", GUILayout.Height(24f)))
                {
                    ApplyAbsurdDefaults();
                }

                if (GUILayout.Button("Hide", GUILayout.Width(58f), GUILayout.Height(24f)))
                {
                    visible = false;
                }
            }

            DrawFooter();
        }

        private void DrawSlider(string label, float value, float min, float max, System.Action<float> setter)
        {
            GUILayout.Label(label + "  " + value.ToString("0.###"));
            float next = GUILayout.HorizontalSlider(value, min, max);
            if (!Mathf.Approximately(next, value))
            {
                setter(next);
            }
        }

        private void ApplyGameishDefaults()
        {
            motor.SlideMomentumSpeedCap = 28f;
            motor.AirSlideMomentumRetention = 1.04f;
            motor.SlideJumpPlanarBoost = 2.2f;
            motor.AirSlideStartBoost = 0.55f;
            motor.SlideGroundSteerSharpness = 10f;
            motor.SlideAirSteerSharpness = 18f;
        }

        private void ApplyAbsurdDefaults()
        {
            motor.SlideMomentumSpeedCap = momentumCapMax;
            motor.AirSlideMomentumRetention = 1.24f;
            motor.SlideJumpPlanarBoost = 6.5f;
            motor.AirSlideStartBoost = 2.5f;
            motor.SlideGroundSteerSharpness = 24f;
            motor.SlideAirSteerSharpness = 48f;
        }

        private void DrawFooter()
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label("Drag corner to resize. Close to park it.");
            DrawResizeGrip();
            GUI.DragWindow(new Rect(0f, 0f, Mathf.Max(0f, windowRect.width - ResizeGripSize), 22f));
        }

        private void ResolveMotor()
        {
            if (motor == null)
            {
                motor = FindAnyObjectByType<IsometricCharacterMotor>();
            }
        }

        private void DrawResizeGrip()
        {
            Rect gripRect = new Rect(
                Mathf.Max(0f, windowRect.width - ResizeGripSize),
                Mathf.Max(0f, windowRect.height - ResizeGripSize),
                ResizeGripSize,
                ResizeGripSize);

            GUI.Box(gripRect, string.Empty);

            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            Event current = Event.current;
            switch (current.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (current.button == 0 && gripRect.Contains(current.mousePosition))
                    {
                        resizingWindow = true;
                        GUIUtility.hotControl = controlId;
                        current.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (resizingWindow && GUIUtility.hotControl == controlId)
                    {
                        windowRect.width = Mathf.Clamp(current.mousePosition.x + ResizeGripSize * 0.5f, 420f, Mathf.Max(420f, Screen.width - 24f));
                        windowRect.height = Mathf.Clamp(current.mousePosition.y + ResizeGripSize * 0.5f, 176f, Mathf.Max(176f, Screen.height - 24f));
                        current.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (resizingWindow && GUIUtility.hotControl == controlId)
                    {
                        resizingWindow = false;
                        GUIUtility.hotControl = 0;
                        current.Use();
                    }
                    break;
            }
        }

        private void ClampWindowToScreen()
        {
            windowRect.width = Mathf.Clamp(windowRect.width, 420f, Mathf.Max(420f, Screen.width - 24f));
            windowRect.height = Mathf.Clamp(windowRect.height, 176f, Mathf.Max(176f, Screen.height - 24f));
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
