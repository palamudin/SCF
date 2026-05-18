using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SCF.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SCFWeaponTuningPanel : MonoBehaviour
    {
        private const float LegacyPositionRangeCeiling = 3f;
        private const float ExpandedDefaultPositionRange = 12.5f;
        private const float ExpandedMaxPositionRange = 30f;

        [SerializeField] private SCFWeaponVisualSlot weaponSlot;
        [SerializeField] private bool visible;
        [SerializeField] private Key toggleKey = Key.F7;
        [SerializeField] private Rect windowRect = new Rect(652f, 78f, 440f, 650f);
        [SerializeField] private Rect collapsedRect = new Rect(652f, 78f, 112f, 30f);
        [SerializeField, Min(0.05f)] private float positionRange = 12.5f;
        [SerializeField, Min(0.1f)] private float weaponScaleMax = 2f;
        [SerializeField] private bool showSocket = true;
        [SerializeField] private bool showMuzzle = true;
        [SerializeField] private bool showRotations = true;
        [SerializeField] private SCFWeaponFitPoseSlot activePoseSlot = SCFWeaponFitPoseSlot.Neutral;
        [SerializeField] private bool poseTuningMode;

        private Vector2 scroll;
        private static int nextWindowId = 36000;
        private int windowId;
        private SCFWeaponFitPoseSlot lastSyncedPoseSlot;
        private bool lastSyncedPoseTuningMode;

        private void OnValidate()
        {
            UpgradeLegacySliderRanges();
        }

        private void Awake()
        {
            UpgradeLegacySliderRanges();

            if (windowId == 0)
            {
                windowId = ++nextWindowId;
            }

            if (weaponSlot == null)
            {
                weaponSlot = GetComponent<SCFWeaponVisualSlot>();
            }
        }

        private void UpgradeLegacySliderRanges()
        {
            if (positionRange <= LegacyPositionRangeCeiling)
            {
                positionRange = ExpandedDefaultPositionRange;
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

        public void Configure(SCFWeaponVisualSlot slot)
        {
            weaponSlot = slot;
            visible = false;
        }

        private void OnGUI()
        {
            if (!visible)
            {
                ClampCollapsedToScreen();
                if (GUI.Button(collapsedRect, "Weapon Fit"))
                {
                    visible = true;
                }

                return;
            }

            ClampWindowToScreen();
            windowRect = GUILayout.Window(windowId, windowRect, DrawWindow, "SCF Weapon Fit Tuner");
        }

        private void DrawWindow(int id)
        {
            DrawHeader();
            DrawToolbar();

            if (weaponSlot == null)
            {
                GUILayout.Label("No SCFWeaponVisualSlot found.");
                GUI.DragWindow(new Rect(0f, 0f, 10000f, 22f));
                return;
            }

            weaponSlot.RefreshWeaponTuningTargets();

            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(Mathf.Max(260f, windowRect.height - 162f)));
            if (showSocket)
            {
                DrawReadOnlyTransformBlock("SOCKET / SCF_ChestWeaponSocket", weaponSlot.WeaponSocketTransform);
            }

            DrawTransformBlock("WEAPON / SCF_Selected_Railgun", weaponSlot.ActiveWeaponTransform, true, true);
            DrawTransformBlock("RIGHT HAND GRIP / SCF_RightPistolGrip", weaponSlot.RightGripTransform, true, false);
            DrawTransformBlock("LEFT HAND GRIP / SCF_LeftUnderbarrelGrip", weaponSlot.LeftGripTransform, true, false);
            DrawTransformBlock("RIGHT ELBOW ORIENTATOR / SCF_RightElbowHint", weaponSlot.RightElbowHintTransform, true, false);
            DrawTransformBlock("LEFT ELBOW ORIENTATOR / SCF_LeftElbowHint", weaponSlot.LeftElbowHintTransform, true, false);

            if (showMuzzle)
            {
                DrawTransformBlock("MUZZLE / SCF_RailgunMuzzleTarget", weaponSlot.RailgunMuzzleTransform, true, false);
            }

            GUILayout.EndScrollView();
            GUILayout.Label("Toggle " + toggleKey);
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 22f));
        }

        private void DrawHeader()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Active: " + (weaponSlot != null ? weaponSlot.ActiveWeaponName : "none"));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Slide", GUILayout.Width(58f), GUILayout.Height(22f)))
                {
                    visible = false;
                }
            }
        }

        private void DrawToolbar()
        {
            DrawPoseToolbar();

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Copy File", GUILayout.Height(26f)))
                {
                    CopyPanelValues();
                }

                if (GUILayout.Button("Capture File", GUILayout.Height(26f)))
                {
                    SaveSnapshot();
                }

                if (GUILayout.Button("Slot Capture", GUILayout.Height(26f)))
                {
                    weaponSlot?.CaptureCurrentRailgunTuning();
                    SaveSnapshot();
                }

                if (GUILayout.Button("Grip Defaults", GUILayout.Height(26f)))
                {
                    weaponSlot?.ApplySavedRailgunGripTargets();
                }
            }

            positionRange = DrawScalarSlider("Position Range", positionRange, 0.05f, ExpandedMaxPositionRange);
            weaponScaleMax = DrawScalarSlider("Weapon Scale Max", weaponScaleMax, 0.25f, 4f);
            showSocket = GUILayout.Toggle(showSocket, "Show socket");
            showMuzzle = GUILayout.Toggle(showMuzzle, "Show muzzle");
            showRotations = GUILayout.Toggle(showRotations, "Show rotations");
        }

        private void DrawPoseToolbar()
        {
            GUILayout.Space(4f);
            GUILayout.Label("Pose Slot", GUI.skin.box);
            using (new GUILayout.HorizontalScope())
            {
                DrawPoseSlotButton(SCFWeaponFitPoseSlot.Neutral, "Neutral");
                DrawPoseSlotButton(SCFWeaponFitPoseSlot.AimWalk, "Aim Walk");
                DrawPoseSlotButton(SCFWeaponFitPoseSlot.AimRun, "Aim Run");
            }

            bool nextTuningMode = GUILayout.Toggle(poseTuningMode, "Pose tuning lock");
            if (nextTuningMode != poseTuningMode)
            {
                poseTuningMode = nextTuningMode;
                SyncPoseTuningMode(true);
            }

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Pose", GUILayout.Height(26f)))
                {
                    weaponSlot?.ApplyWeaponFitPose(activePoseSlot);
                }

                if (GUILayout.Button("Capture Pose", GUILayout.Height(26f)))
                {
                    weaponSlot?.CaptureCurrentWeaponFitPose(activePoseSlot);
                }

                if (GUILayout.Button("Pose File", GUILayout.Height(26f)))
                {
                    string path = weaponSlot != null ? weaponSlot.RecordWeaponFitPoseSnapshot(activePoseSlot) : string.Empty;
                    Debug.Log("SCF weapon pose snapshot saved: " + path);
                }
            }

            SyncPoseTuningMode(false);
        }

        private void DrawPoseSlotButton(SCFWeaponFitPoseSlot slot, string label)
        {
            string text = activePoseSlot == slot ? "[" + label + "]" : label;
            if (!GUILayout.Button(text, GUILayout.Height(26f)))
            {
                return;
            }

            activePoseSlot = slot;
            weaponSlot?.ApplyWeaponFitPose(activePoseSlot);
            SyncPoseTuningMode(true);
        }

        private void SyncPoseTuningMode(bool force)
        {
            if (weaponSlot == null)
            {
                return;
            }

            if (!force && lastSyncedPoseSlot == activePoseSlot && lastSyncedPoseTuningMode == poseTuningMode)
            {
                return;
            }

            weaponSlot.SetWeaponFitPoseTuningMode(activePoseSlot, poseTuningMode);
            lastSyncedPoseSlot = activePoseSlot;
            lastSyncedPoseTuningMode = poseTuningMode;
        }

        private void DrawTransformBlock(string label, Transform target, bool includeRotation, bool includeScale)
        {
            GUILayout.Space(8f);
            GUILayout.Label(label, GUI.skin.box);
            if (target == null)
            {
                GUILayout.Label("Missing target.");
                return;
            }

            string parentName = target.parent != null ? target.parent.name : "none";
            GUILayout.Label("Object: " + target.name + " | Parent: " + parentName);

            target.localPosition = DrawVector3Sliders("Local Position", target.localPosition, -positionRange, positionRange);

            if (includeRotation && showRotations)
            {
                Vector3 euler = NormalizeEuler(target.localEulerAngles);
                euler = DrawVector3Sliders("Local Rotation", euler, -180f, 180f);
                target.localRotation = Quaternion.Euler(euler);
            }

            if (includeScale)
            {
                target.localScale = DrawVector3Sliders("Local Scale", target.localScale, 0.01f, weaponScaleMax);
            }

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Zero Pos", GUILayout.Height(22f)))
                {
                    target.localPosition = Vector3.zero;
                }

                if (includeRotation && GUILayout.Button("Zero Rot", GUILayout.Height(22f)))
                {
                    target.localRotation = Quaternion.identity;
                }

                if (includeScale && GUILayout.Button("Scale 1", GUILayout.Height(22f)))
                {
                    target.localScale = Vector3.one;
                }
            }
        }

        private void DrawReadOnlyTransformBlock(string label, Transform target)
        {
            GUILayout.Space(8f);
            GUILayout.Label(label, GUI.skin.box);
            if (target == null)
            {
                GUILayout.Label("Missing target.");
                return;
            }

            string parentName = target.parent != null ? target.parent.name : "none";
            GUILayout.Label("Object: " + target.name + " | Parent: " + parentName);
            GUILayout.Label("Local Position  " + FormatVector(target.localPosition));
            GUILayout.Label("Local Rotation  " + FormatVector(NormalizeEuler(target.localEulerAngles)));
            GUILayout.Label("Local Scale  " + FormatVector(target.localScale));
        }

        private Vector3 DrawVector3Sliders(string label, Vector3 value, float min, float max)
        {
            GUILayout.Label(label + "  " + FormatVector(value));
            value.x = DrawAxisSlider("X", value.x, min, max);
            value.y = DrawAxisSlider("Y", value.y, min, max);
            value.z = DrawAxisSlider("Z", value.z, min, max);
            return value;
        }

        private float DrawAxisSlider(string axis, float value, float min, float max)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(axis, GUILayout.Width(16f));
                GUILayout.Label(FormatFloat(value), GUILayout.Width(62f));
                value = GUILayout.HorizontalSlider(value, min, max);
            }

            return value;
        }

        private float DrawScalarSlider(string label, float value, float min, float max)
        {
            GUILayout.Label(label + "  " + FormatFloat(value));
            return GUILayout.HorizontalSlider(value, min, max);
        }

        private void CopyPanelValues()
        {
            string path = weaponSlot != null ? weaponSlot.RecordWeaponFitSnapshot("SCF_WeaponFitPanelCopy") : string.Empty;
            Debug.Log("SCF weapon fit copied to clipboard and file: " + path);
        }

        private void SaveSnapshot()
        {
            string path = weaponSlot != null ? weaponSlot.RecordWeaponFitSnapshot("SCF_WeaponFitPanel") : string.Empty;
            Debug.Log("SCF weapon fit snapshot saved: " + path);
        }

        private string BuildPanelValuesText()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("SCF weapon fit tuning");
            builder.AppendLine("Captured at: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            builder.AppendLine("Active weapon: " + (weaponSlot != null ? weaponSlot.ActiveWeaponName : "none"));
            AppendTransform(builder, "SOCKET / SCF_ChestWeaponSocket", weaponSlot != null ? weaponSlot.WeaponSocketTransform : null);
            AppendTransform(builder, "WEAPON / SCF_Selected_Railgun", weaponSlot != null ? weaponSlot.ActiveWeaponTransform : null);
            AppendTransform(builder, "RIGHT HAND GRIP / SCF_RightPistolGrip", weaponSlot != null ? weaponSlot.RightGripTransform : null);
            AppendTransform(builder, "LEFT HAND GRIP / SCF_LeftUnderbarrelGrip", weaponSlot != null ? weaponSlot.LeftGripTransform : null);
            AppendTransform(builder, "RIGHT ELBOW ORIENTATOR / SCF_RightElbowHint", weaponSlot != null ? weaponSlot.RightElbowHintTransform : null);
            AppendTransform(builder, "LEFT ELBOW ORIENTATOR / SCF_LeftElbowHint", weaponSlot != null ? weaponSlot.LeftElbowHintTransform : null);
            AppendTransform(builder, "MUZZLE / SCF_RailgunMuzzleTarget", weaponSlot != null ? weaponSlot.RailgunMuzzleTransform : null);
            return builder.ToString();
        }

        private static void AppendTransform(StringBuilder builder, string label, Transform target)
        {
            builder.AppendLine(label);
            if (target == null)
            {
                builder.AppendLine("  missing");
                return;
            }

            builder.AppendLine("  object = " + target.name);
            builder.AppendLine("  parent = " + (target.parent != null ? target.parent.name : "none"));
            builder.AppendLine("  localPosition = " + FormatVector(target.localPosition));
            builder.AppendLine("  localEulerAngles = " + FormatVector(NormalizeEuler(target.localEulerAngles)));
            builder.AppendLine("  localScale = " + FormatVector(target.localScale));
        }

        private void ClampWindowToScreen()
        {
            windowRect.width = Mathf.Clamp(windowRect.width, 320f, Mathf.Max(320f, Screen.width - 16f));
            windowRect.height = Mathf.Clamp(windowRect.height, 240f, Mathf.Max(240f, Screen.height - 16f));
            windowRect.x = Mathf.Clamp(windowRect.x, 0f, Mathf.Max(0f, Screen.width - windowRect.width));
            windowRect.y = Mathf.Clamp(windowRect.y, 0f, Mathf.Max(0f, Screen.height - windowRect.height));
        }

        private void ClampCollapsedToScreen()
        {
            collapsedRect.x = Mathf.Clamp(collapsedRect.x, 0f, Mathf.Max(0f, Screen.width - collapsedRect.width));
            collapsedRect.y = Mathf.Clamp(collapsedRect.y, 0f, Mathf.Max(0f, Screen.height - collapsedRect.height));
        }

        private static Vector3 NormalizeEuler(Vector3 eulerAngles)
        {
            return new Vector3(NormalizeEulerAxis(eulerAngles.x), NormalizeEulerAxis(eulerAngles.y), NormalizeEulerAxis(eulerAngles.z));
        }

        private static float NormalizeEulerAxis(float value)
        {
            value %= 360f;
            if (value > 180f)
            {
                value -= 360f;
            }
            else if (value < -180f)
            {
                value += 360f;
            }

            return value;
        }

        private static string FormatVector(Vector3 value)
        {
            return "new Vector3(" + FormatFloat(value.x) + "f, " + FormatFloat(value.y) + "f, " + FormatFloat(value.z) + "f)";
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }
    }
}
