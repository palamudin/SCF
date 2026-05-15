using UnityEngine;
using UnityEditor;

namespace Atlas.Unity {

    [CustomEditor(typeof(AtlasUnityPreviewVolume))]
    [CanEditMultipleObjects]
    public class AtlasUnityPreviewerEditor : Editor {

        private GUIContent[] toolbarContents;
        private ToolBarMode toolbarMode = ToolBarMode.Edit;

        private SerializedProperty terrainSource;
        private SerializedProperty autoCenter;
        private SerializedProperty editing;
        private SerializedProperty editedOnce;
        private SerializedProperty previewRenderResolution;
        private SerializedProperty terrainUpdateMode;
        private SerializedProperty size;
        private SerializedProperty previewAccuracyFromSource;
        private SerializedProperty previewAccuracy;
        private SerializedProperty drawScaleReferences;
        private SerializedProperty scaleReferenceType;

        private GUIContent startEditGUIContent;
        private GUIContent stopEditGUIContent;

        private void OnEnable() {

            toolbarContents = new GUIContent[] {
                new GUIContent(EditorGUIUtility.FindTexture("editicon.sml"),"edit"),
                new GUIContent(EditorGUIUtility.FindTexture("ClothInspector.SettingsTool"),"settings"),
            };

            terrainSource = serializedObject.FindProperty("terrainSource");
            autoCenter = serializedObject.FindProperty("autoCenter");
            editing = serializedObject.FindProperty("editing");
            editedOnce = serializedObject.FindProperty("editedOnce");
            previewRenderResolution = serializedObject.FindProperty("previewRenderResolution");
            terrainUpdateMode = serializedObject.FindProperty("terrainUpdateMode");
            size = serializedObject.FindProperty("volumeSize");
            previewAccuracyFromSource = serializedObject.FindProperty("previewAccuracyFromSource");
            previewAccuracy = serializedObject.FindProperty("previewAccuracy");
            drawScaleReferences = serializedObject.FindProperty("drawScaleReferences");
            scaleReferenceType = serializedObject.FindProperty("scaleReferenceType");

            startEditGUIContent = new GUIContent(AtlasUtils.LoadIcon("Packages/com.atlas.atlas-terrain-editor/Runtime/Icons/Atlas_activationtool_icon.tiff", "l:Atlas_activationtool_icon"), "start editing");
            stopEditGUIContent = new GUIContent(AtlasUtils.LoadIcon("Packages/com.atlas.atlas-terrain-editor/Runtime/Icons/Atlas_activationtool_icon_off.tiff", "l:Atlas_activationtool_icon_off"), "stop editing");

        }

        public override void OnInspectorGUI() {

            #region header

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(editing.boolValue ? stopEditGUIContent : startEditGUIContent, GUILayout.Width(40), GUILayout.Height(40))) {

                if ((editing.boolValue && (target as AtlasUnityPreviewVolume).FirstTimeRenderProceed()) || editing.boolValue == false) {

                    AtlasUtils.RememberTerrainDrawStates(!editing.boolValue);

                    DisableTargets();

                    editing.boolValue = !editing.boolValue;

                    editedOnce.boolValue = true;

                    serializedObject.ApplyModifiedProperties();

                    EnableTargets();

                }

            }

            //if (GUILayout.Button(new GUIContent(EditorGUIUtility.FindTexture("AssetStore Icon"), "Atlas store"), GUILayout.Width(40), GUILayout.Height(40))) {
            //
            //    AtlasMenuItems.AtlasStore();
            //
            //}

            Border.Draw(GUILayoutUtility.GetLastRect());

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (!editing.boolValue) {

                if (targets.Length == 1 && editedOnce.boolValue == false) { EditorGUILayout.HelpBox("Press [Edit] to start sculpting in real time inside the preview volume.", MessageType.Info); }

            }

#endregion

#region dont edit in playmode //might wana kill this
            if (editing.boolValue && Application.isPlaying) {

                editing.boolValue = false;

                serializedObject.ApplyModifiedProperties();

            }
#endregion

            var needToDisableAndEnable = false;

            if (editing.boolValue) {

#region toolbar

                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                toolbarMode = (ToolBarMode)GUILayout.SelectionGrid((int)toolbarMode, toolbarContents, toolbarContents.Length);

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

#endregion

#region edit
                if (toolbarMode == ToolBarMode.Edit) {

                    EditorGUI.BeginChangeCheck();

                    Border.Start();

                    EditorGUILayout.PropertyField(previewRenderResolution);

                    EditorGUILayout.PropertyField(terrainUpdateMode);

                    EditorGUILayout.Space(5);

                    Border.End();

                    if (EditorGUI.EndChangeCheck()) {

                        needToDisableAndEnable = true;

                    }

                    EditorGUILayout.Space(5);

                    Border.Start();

                    if (targets.Length == 1 && (target as AtlasUnityPreviewVolume).needEye) {

                        EditorGUILayout.Space(5);

                        EditorGUILayout.PropertyField(autoCenter);

                    }

                    EditorGUI.BeginChangeCheck();

                    EditorGUILayout.PropertyField(drawScaleReferences);

                    if (drawScaleReferences.boolValue) {

                        EditorGUI.indentLevel++;

                        EditorGUILayout.PropertyField(scaleReferenceType);

                        EditorGUI.indentLevel--;

                    }

                    if (EditorGUI.EndChangeCheck()) {

                        foreach (var i in targets) { (i as AtlasUnityPreviewVolume).atlasScaleReferenceDrawer.ForceDraw((AtlasScaleReferenceDrawer.ScaleReferenceType)scaleReferenceType.enumValueIndex); }

                    }

                    Border.End(1);

                    if (terrainUpdateMode.enumValueIndex == (int)TerrainHeightmapSyncControl.HeightOnly) {

                        GUILayout.Space(10);

                        EditorGUILayout.HelpBox("Terrain update mode set to 'HeightOnly' while it has higher performance.\nIt will not be updating collision for your terrain.\n\nThese issues only exist while editing within the preview volume and do not reflect onto your final terrain.", MessageType.Warning);

                    }

                    if (terrainUpdateMode.enumValueIndex == (int)TerrainHeightmapSyncControl.None) {

                        GUILayout.Space(10);

                        EditorGUILayout.HelpBox("Terrain update mode set to 'None' while it has higher performance.\nIt will not be updating collision for your terrain.\nIt may also show culling artifacts.\n\nThese issues only exist while editing within the preview volume and do not reflect onto your final terrain.", MessageType.Warning);

                    }

                }
#endregion

#region settings
                if (toolbarMode == ToolBarMode.Settings) {

                    EditorGUI.BeginChangeCheck();

                    Border.Start();

                    EditorGUILayout.PropertyField(size);

                    Border.End();

                    EditorGUILayout.Space(5);

                    Border.Start();

                    EditorGUILayout.PropertyField(terrainSource);

                    if (terrainSource.objectReferenceValue != null) {

                        EditorGUILayout.PropertyField(previewAccuracyFromSource);

                        if (previewAccuracyFromSource.boolValue == false) {

                            EditorGUILayout.PropertyField(previewAccuracy);

                        }

                    } else {

                        EditorGUILayout.PropertyField(previewAccuracy);

                    }

                    Border.End(1);

                    if (EditorGUI.EndChangeCheck()) {

                        needToDisableAndEnable = true;

                    }

                }
#endregion

            } else {

                EditorGUILayout.Space(5);

                Border.Start();

                EditorGUILayout.PropertyField(size);

                Border.End(0);

                EditorGUILayout.Space(5);

                Border.Start();

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(drawScaleReferences);

                if (drawScaleReferences.boolValue) {

                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(scaleReferenceType);

                    EditorGUI.indentLevel--;

                }

                if (EditorGUI.EndChangeCheck()) {

                    foreach (var i in targets) { (i as AtlasUnityPreviewVolume).atlasScaleReferenceDrawer.ForceDraw((AtlasScaleReferenceDrawer.ScaleReferenceType)scaleReferenceType.enumValueIndex); }

                }

                Border.End(1);

                if (PlayerSettings.colorSpace == ColorSpace.Gamma) {

                    GUILayout.Space(10);

                    EditorGUILayout.HelpBox("Your project is set to the 'Gamma' color space.\n\nWhile Gamma works the stamps might give unwanted results and the Atlas sample scenes will look different.\n\nYou can find the color space setting under Player Settings.", MessageType.Warning);

                }

            }

            if (serializedObject.hasModifiedProperties) {

                if (needToDisableAndEnable) {

                    DisableTargets();

                }

                serializedObject.ApplyModifiedProperties();

                if (needToDisableAndEnable) {

                    EnableTargets();

                }

            }

        }

        private void OnSceneGUI() {

            EyeHandle(target);

        }

        private void DisableTargets() {

            if (targets != null) {

                foreach (var i in targets) {

                    var atlasTerra = i as AtlasUnityPreviewVolume;

                    atlasTerra.OnDisable();

                }

            }

        }

        private void EnableTargets() {

            if (targets != null) {

                foreach (var i in targets) {

                    var atlasTerra = i as AtlasUnityPreviewVolume;

                    atlasTerra.OnEnable();

                }

            }

        }

        private void EyeHandle(Object target) {

            var previewVolume = target as AtlasUnityPreviewVolume;

            if (previewVolume.needEye && previewVolume.eyeGameObject != null) {

                EditorGUI.BeginChangeCheck();

                if (Event.current.type == EventType.MouseUp) {

                    AtlasStamper.QueRender();

                }

                var eyeLocalPosition = previewVolume.transform.InverseTransformPoint(Handles.Slider2D(previewVolume.transform.TransformPoint(previewVolume.eyeLocalPosition), Vector3.up, Vector3.forward, Vector3.right, HandleUtility.GetHandleSize(previewVolume.transform.TransformPoint(previewVolume.eyeLocalPosition)) * 0.2f, Handles.CircleHandleCap, 0));

                if (EditorGUI.EndChangeCheck()) {

                    previewVolume.SetEyeLocalPosition(eyeLocalPosition);

                }

            }

        }

        public bool HasFrameBounds() { return true; }

        public Bounds OnGetFrameBounds() {

            if ((target as AtlasUnityPreviewVolume).eyeGameObject == null) {

                return new Bounds((target as AtlasUnityPreviewVolume).transform.position + Vector3.Scale((target as AtlasUnityPreviewVolume).size, new Vector3(0.5f, 0, 0.5f)), (target as AtlasUnityPreviewVolume).size * 0.5f);

            } else {

                return new Bounds((target as AtlasUnityPreviewVolume).eyeGameObject.transform.position + Vector3.Scale((target as AtlasUnityPreviewVolume).eyeSize, new Vector3(0.5f, 0, 0.5f)), (target as AtlasUnityPreviewVolume).eyeSize * 0.5f);

            }

        }


        private enum ToolBarMode {

            Edit,
            Settings,

        }

    }

    public static class Border {

        private static float rectStartHeight;

        public static void Start() {

            GUILayout.Space(0);

            rectStartHeight = GUILayoutUtility.GetLastRect().y + 3;

        }

        public static void End(int colorIndex = 0) {

            var r = GUILayoutUtility.GetLastRect();

            var rectEndHeight = r.y + r.height;

            r = new Rect(4, rectStartHeight, 2, rectEndHeight - rectStartHeight);

            Draw(r, AtlasUtils.IndexToColor(colorIndex));

        }

        public static void End(Color color) {

            var r = GUILayoutUtility.GetLastRect();

            var rectEndHeight = r.y + r.height;

            r = new Rect(4, rectStartHeight, 2, rectEndHeight - rectStartHeight);

            Draw(r, color);

        }

        public static void Draw(Rect r, int colorIndex = 0) {

            Draw(r, AtlasUtils.IndexToColor(colorIndex));

        }

        private static void Draw(Rect r, Color color) {

            var border = 1;

            EditorGUI.DrawRect(new Rect(r.x, r.y, border, r.height), color);
            EditorGUI.DrawRect(new Rect(r.x + r.width - border, r.y, border, r.height), color);
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, border), color);
            EditorGUI.DrawRect(new Rect(r.x, r.y + r.height - border, r.width, border), color);

        }

    }

}
