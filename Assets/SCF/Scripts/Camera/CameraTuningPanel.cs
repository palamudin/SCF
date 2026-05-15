using System;
using UnityEngine;

namespace SCF.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [RequireComponent(typeof(IsometricCameraFollow))]
    public sealed class CameraTuningPanel : MonoBehaviour
    {
        [SerializeField] private Camera tunedCamera;
        [SerializeField] private IsometricCameraFollow cameraFollow;
        [SerializeField] private bool showPanel = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F9;
        [SerializeField] private Rect windowRect = new Rect(392f, 16f, 360f, 520f);

        private static int nextWindowId = 31000;
        private int windowId;

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
            if (windowId == 0)
            {
                windowId = ++nextWindowId;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                showPanel = !showPanel;
            }
        }

        private void OnGUI()
        {
            if (!showPanel)
            {
                return;
            }

            ResolveReferences();
            ClampWindowToScreen();
            windowRect = GUILayout.Window(windowId, windowRect, DrawWindow, "Camera Tuning");
        }

        public void Configure(Camera camera, IsometricCameraFollow follow)
        {
            tunedCamera = camera;
            cameraFollow = follow;
        }

        private void DrawWindow(int windowId)
        {
            if (tunedCamera == null || cameraFollow == null)
            {
                GUILayout.Label("Camera tuning target missing.");
                GUI.DragWindow(new Rect(0f, 0f, 10000f, 22f));
                return;
            }

            DrawVector3("Offset", cameraFollow.Offset, new Vector3(-24f, 2f, -24f), new Vector3(24f, 28f, 24f), value =>
            {
                cameraFollow.Offset = value;
            });

            DrawVector3("Rotation", cameraFollow.RotationEuler, new Vector3(30f, -180f, -35f), new Vector3(85f, 180f, 35f), value =>
            {
                cameraFollow.RotationEuler = value;
            });

            bool useOrthographic = GUILayout.Toggle(cameraFollow.UseOrthographic, "Orthographic");
            if (useOrthographic != cameraFollow.UseOrthographic)
            {
                cameraFollow.UseOrthographic = useOrthographic;
            }

            cameraFollow.OrthographicSize = DrawSlider("Ortho Size", cameraFollow.OrthographicSize, 2.5f, 12f);
            tunedCamera.fieldOfView = DrawSlider("FOV", tunedCamera.fieldOfView, 25f, 85f);

            cameraFollow.FollowSharpness = DrawSlider("Follow", cameraFollow.FollowSharpness, 1f, 40f);
            cameraFollow.RotationSharpness = DrawSlider("Turn Follow", cameraFollow.RotationSharpness, 1f, 40f);
            tunedCamera.nearClipPlane = DrawSlider("Near Clip", tunedCamera.nearClipPlane, 0.03f, 2f);
            tunedCamera.farClipPlane = DrawSlider("Far Clip", tunedCamera.farClipPlane, 80f, 1500f);

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Preset", GUILayout.Height(28f)))
                {
                    cameraFollow.ApplyTacticalShooterPreset();
                }

                if (GUILayout.Button("Snap", GUILayout.Height(28f)))
                {
                    cameraFollow.SnapToTarget();
                }

                if (GUILayout.Button("Copy", GUILayout.Height(28f)))
                {
                    GUIUtility.systemCopyBuffer = BuildSettingsSummary();
                }
            }

            GUILayout.Label("Toggle " + toggleKey + ".");
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 22f));
        }

        private void DrawVector3(string label, Vector3 value, Vector3 min, Vector3 max, Action<Vector3> apply)
        {
            GUILayout.Space(4f);
            GUILayout.Label(label + "  " + FormatVector(value));
            value.x = DrawSlider("X", value.x, min.x, max.x);
            value.y = DrawSlider("Y", value.y, min.y, max.y);
            value.z = DrawSlider("Z", value.z, min.z, max.z);
            apply(value);
        }

        private float DrawSlider(string label, float value, float min, float max)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(76f));
                value = GUILayout.HorizontalSlider(value, min, max);
                string typed = GUILayout.TextField(value.ToString("0.00"), GUILayout.Width(54f));
                if (float.TryParse(typed, out float parsed))
                {
                    value = Mathf.Clamp(parsed, min, max);
                }
            }

            return value;
        }

        private string BuildSettingsSummary()
        {
            Vector3 offset = cameraFollow.Offset;
            Vector3 rotation = cameraFollow.RotationEuler;
            return "offset=" + FormatVector(offset)
                + " rotation=" + FormatVector(rotation)
                + " orthographic=" + cameraFollow.UseOrthographic
                + " orthoSize=" + cameraFollow.OrthographicSize.ToString("0.00")
                + " fov=" + tunedCamera.fieldOfView.ToString("0.00");
        }

        private string FormatVector(Vector3 value)
        {
            return "(" + value.x.ToString("0.00") + ", " + value.y.ToString("0.00") + ", " + value.z.ToString("0.00") + ")";
        }

        private void ResolveReferences()
        {
            if (tunedCamera == null)
            {
                tunedCamera = GetComponent<Camera>();
            }

            if (cameraFollow == null)
            {
                cameraFollow = GetComponent<IsometricCameraFollow>();
            }
        }

        private void ClampWindowToScreen()
        {
            float maxWidth = Mathf.Max(300f, Screen.width - 24f);
            float maxHeight = Mathf.Max(300f, Screen.height - 24f);
            windowRect.width = Mathf.Clamp(windowRect.width, 300f, maxWidth);
            windowRect.height = Mathf.Clamp(windowRect.height, 300f, maxHeight);
            windowRect.x = Mathf.Clamp(windowRect.x, 0f, Mathf.Max(0f, Screen.width - windowRect.width));
            windowRect.y = Mathf.Clamp(windowRect.y, 0f, Mathf.Max(0f, Screen.height - windowRect.height));
        }
    }
}
