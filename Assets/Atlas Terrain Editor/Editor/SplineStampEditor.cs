using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace Atlas.Unity {

    [CustomEditor(typeof(SplineStamp)), CanEditMultipleObjects]
    public class SplineStampEditor : Editor {

        private GUIContent[] toolbarContents;
        private GUIContent[] actionBarContents;
        private ToolBarMode toolbarMode = 0;

        private SerializedProperty size;
        private SerializedProperty closed;
        private SerializedProperty width;
        private SerializedProperty tiling;
        private SerializedProperty fadeStart;
        private SerializedProperty fadeEnd;
        private SerializedProperty quality;
        private SerializedProperty qualitySettings;
        private SerializedProperty meshExportSettings;
        private SerializedProperty roadBlending;

        private void OnEnable() {

            toolbarContents = new GUIContent[] {
                new GUIContent(EditorGUIUtility.FindTexture("editicon.sml"),"edit"),
                new GUIContent(EditorGUIUtility.FindTexture("ClothInspector.PaintTool"),"layers"),
                new GUIContent(EditorGUIUtility.FindTexture("ClothInspector.SettingsTool"),"settings"),
            };

            actionBarContents = new GUIContent[] {
                new GUIContent("",AtlasUtils.LoadIcon("Packages/com.atlas.atlas-terrain-editor/Runtime/Icons/Atlas_subdivide_icon.tiff","l:Atlas_subdivide_icon"),"Adds a new node between each node."),
                new GUIContent("",AtlasUtils.LoadIcon("Packages/com.atlas.atlas-terrain-editor/Runtime/Icons/Atlas_unsubdivide_icon.tiff","l:Atlas_unsubdivide_icon"),"Removes every other node."),
                new GUIContent("",AtlasUtils.LoadIcon("Packages/com.atlas.atlas-terrain-editor/Runtime/Icons/Atlas_smooth_height_icon.tiff","l:Atlas_smooth_height_icon"),"Averages all nodes height wise for a smoother result. Subdivision before smoothing is recomended."),
                new GUIContent("",AtlasUtils.LoadIcon("Packages/com.atlas.atlas-terrain-editor/Runtime/Icons/Atlas_smooth_latheral_icon.tiff","l:Atlas_smooth_latheral_icon"),"Averages all nodes latheral wise for a smoother result. Subdivision before smoothing is recomended."),
                new GUIContent("",AtlasUtils.LoadIcon("Packages/com.atlas.atlas-terrain-editor/Runtime/Icons/Atlas_smooth_scaling_icon.tiff","l:Atlas_smooth_scaling_icon"),"Averages the scaling value of all nodes for a smoother result. Subdivision before smoothing is recomended."),
                new GUIContent("",AtlasUtils.LoadIcon("Packages/com.atlas.atlas-terrain-editor/Runtime/Icons/Atlas_drop_to_floor_icon.tiff","l:Atlas_drop_to_floor_icon"),"Drops all nodes to the floor.\n\nSet heightmap opacity to 0 before dropping to floor, otherwise the spline will drop to its own height."),
            };

            size = serializedObject.FindProperty("size");
            closed = serializedObject.FindProperty("closed");
            width = serializedObject.FindProperty("width");
            tiling = serializedObject.FindProperty("tiling");
            fadeStart = serializedObject.FindProperty("fadeStart");
            fadeEnd = serializedObject.FindProperty("fadeEnd");
            quality = serializedObject.FindProperty("quality");
            qualitySettings = serializedObject.FindProperty("qualitySettings");
            meshExportSettings = serializedObject.FindProperty("meshExportSettings");
            roadBlending = serializedObject.FindProperty("roadBlending");

        }

        public override void OnInspectorGUI() {

            DrawToolBar();

            EditorGUILayout.Space(5);

            if (toolbarMode == ToolBarMode.Edit) {

                EditorGUILayout.PropertyField(size);

                EditorGUILayout.Space(10);

                EditorGUILayout.HelpBox(@"drag nodes for lateral movement
hold [ctrl] and drag nodes for free movement
hold [shift] + [left click] to add a point
hold [shift] + [right click] to remove a point
hold [ctrl] + [shift] and drag nodes for scaling", MessageType.Info);

                EditorGUILayout.Space(10);

                DrawActionBar();

                GUILayout.Space(20);

                Border.Start();

                EditorGUILayout.PropertyField(quality);
                if ((SplineStamp.SplineQuality)quality.enumValueIndex == SplineStamp.SplineQuality.Custom) {
                    EditorGUILayout.PropertyField(qualitySettings);
                }

                Border.End(0);

            }

            if (toolbarMode == ToolBarMode.Paint) {

                bool thesameStampAssetInAllTargets = true;

                var stampAsset = (target as StampBase).stamp;

                foreach (var i in targets) {

                    var stampBase = i as StampBase;

                    if (stampBase.stamp != stampAsset) {

                        thesameStampAssetInAllTargets = false;

                        break;

                    }

                }

                if (thesameStampAssetInAllTargets) {

                    StampBaseEditor.DrawBaseStamp(serializedObject, false);

                } else {

                    EditorGUILayout.HelpBox("Multi edit only supported when thesame stamp asset is selected", MessageType.Info);

                }

            }

            if (toolbarMode == ToolBarMode.Settings) {

                Border.Start();

                EditorGUILayout.PropertyField(closed);
                EditorGUILayout.PropertyField(width);
                EditorGUILayout.PropertyField(tiling);
                EditorGUILayout.PropertyField(fadeStart);
                EditorGUILayout.PropertyField(fadeEnd);
                EditorGUILayout.PropertyField(roadBlending);

                Border.End(0);

                GUILayout.Space(10);

                Border.Start();

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(meshExportSettings);

                if (EditorGUI.EndChangeCheck()) {

                    serializedObject.ApplyModifiedProperties();

                    foreach (var i in targets) { (i as SplineStamp).CreateMeshIfCreated(); }

                }

                if (meshExportSettings.isExpanded) {

                    GUILayout.Space(10);

                    EditorGUI.indentLevel++;

                    GUILayout.BeginHorizontal();

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Create", GUILayout.MaxWidth(100))) { foreach (var i in targets) { (i as SplineStamp).CreateMesh(); } }

                    if (GUILayout.Button("Clear", GUILayout.MaxWidth(100))) { foreach (var i in targets) { (i as SplineStamp).ClearMesh(); } }

                    GUILayout.EndHorizontal();

                    EditorGUI.indentLevel--;
                }

                Border.End(10);

            }


            if (serializedObject.hasModifiedProperties) {

                serializedObject.ApplyModifiedProperties();

                AtlasStamper.QueRender();

            }

        }

        protected virtual void OnSceneGUI() {

            var o = (SplineStamp)target;

            if (Selection.activeGameObject != o.gameObject) { return; }

            UpdateSizeAndCenter(o);

            var handleScale = 0.1f;

            UpdatePointCreation(o, handleScale);

            DrawPoints(o, handleScale);

            Handles.matrix = Matrix4x4.identity;

            DrawMeshSplit();

        }


        private void DrawToolBar() {

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            toolbarMode = (ToolBarMode)GUILayout.SelectionGrid((int)toolbarMode, toolbarContents, toolbarContents.Length);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

        }

        private void DrawActionBar() {

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            for (var i = 0; i < actionBarContents.Length; i++) {

                if (GUILayout.Button(actionBarContents[i], GUILayout.MaxWidth(35), GUILayout.MinWidth(35), GUILayout.MaxHeight(25), GUILayout.MinHeight(25))) {

                    switch (i) {

                        case 0:

                            Undo.RecordObjects(targets, "Subdevide splines.");

                            foreach (var ii in targets) {

                                (ii as SplineStamp).Subdevide();

                                PrefabUtility.RecordPrefabInstancePropertyModifications(ii as SplineStamp);

                            }

                            break;

                        case 1:

                            Undo.RecordObjects(targets, "Unsubdevide splines.");

                            foreach (var ii in targets) {

                                (ii as SplineStamp).UnSubdevide();

                                PrefabUtility.RecordPrefabInstancePropertyModifications(ii as SplineStamp);

                            }

                            break;

                        case 2:

                            Undo.RecordObjects(targets, "Smooth spline points height.");

                            foreach (var ii in targets) {

                                (ii as SplineStamp).SmoothPointsHeight();

                                PrefabUtility.RecordPrefabInstancePropertyModifications(ii as SplineStamp);

                            }

                            break;

                        case 3:

                            Undo.RecordObjects(targets, "Smooth spline points latheral.");

                            foreach (var ii in targets) {

                                (ii as SplineStamp).SmoothPointsLateral();

                                PrefabUtility.RecordPrefabInstancePropertyModifications(ii as SplineStamp);

                            }

                            break;

                        case 4:

                            Undo.RecordObjects(targets, "Smooth spline points scale.");

                            foreach (var ii in targets) {

                                (ii as SplineStamp).SmoothScale();

                                PrefabUtility.RecordPrefabInstancePropertyModifications(ii as SplineStamp);

                            }

                            break;

                        case 5:

                            Undo.RecordObjects(targets, "Drop spline points to floor.");

                            foreach (var ii in targets) {

                                (ii as SplineStamp).DropToFloor();

                                PrefabUtility.RecordPrefabInstancePropertyModifications(ii as SplineStamp);

                            }

                            break;

                    }

                    foreach (var ii in targets) { (ii as SplineStamp).CreateMeshIfCreated(); }

                }

            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

        }

        private void UpdatePointCreation(SplineStamp o, float handleScale) {

            if (!Event.current.control) {

                if (Event.current.shift) {

                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

                    if (Event.current.type == EventType.MouseDown && Event.current.button == 0) {

                        if (RaycastLines(o, o.points, handleScale, out var index, out var pl)) {

                            Undo.RecordObject(o, "Spline Insert Point");

                            InsertPoint(o, index, pl);

                            PrefabUtility.RecordPrefabInstancePropertyModifications(o);

                            AtlasStamper.QueRender();

                            return;

                        } else if (Raycast(o, out var p)) {

                            Undo.RecordObject(o, "Spline Add Point");

                            AddPoint(o, p);

                            PrefabUtility.RecordPrefabInstancePropertyModifications(o);

                            AtlasStamper.QueRender();

                            return;

                        }


                    }

                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

                    if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && RaycastPoints(o, o.points, handleScale, out var index2)) {

                        Undo.RecordObject(o, "Spline Remove Point");

                        RemovePoint(o, index2);

                        PrefabUtility.RecordPrefabInstancePropertyModifications(o);

                        AtlasStamper.QueRender();

                        return;

                    }

                }

            }

        }

        private void DrawPoints(SplineStamp o, float handleScale) {

            if (o.points == null) { return; }

            var points = new List<SplineStamp.SplinePoint>();

            points.AddRange(o.points);

            EditorGUI.BeginChangeCheck();

            if (Event.current.shift && Event.current.control) {

                for (var i = 0; i < points.Count; i++) {

                    var worldPoint = o.transform.TransformPoint(points[i].point);

                    points[i] = new SplineStamp.SplinePoint(points[i].point, Handles.ScaleSlider(points[i].scale, worldPoint, Vector3.up, Quaternion.identity, HandleUtility.GetHandleSize(worldPoint) * handleScale * 9, 0));

                }

            } else {

                if (Event.current.control) {

                    for (var i = 0; i < points.Count; i++) {

                        var worldPoint = o.transform.TransformPoint(points[i].point);

                        if (i == points.Count - 1) {

                            var fmh_417_138_639143231886628274 = Quaternion.identity; points[i] = new SplineStamp.SplinePoint(o.transform.InverseTransformPoint(Handles.FreeMoveHandle(worldPoint, HandleUtility.GetHandleSize(worldPoint) * handleScale * 2f, Vector3.zero, Handles.CubeHandleCap)), points[i].scale);

                        } else {

                            var fmh_421_138_639143231886642213 = Quaternion.identity; points[i] = new SplineStamp.SplinePoint(o.transform.InverseTransformPoint(Handles.FreeMoveHandle(worldPoint, HandleUtility.GetHandleSize(worldPoint) * handleScale * 2f, Vector3.zero, Handles.SphereHandleCap)), points[i].scale);

                        }

                    }

                } else {

                    for (var i = 0; i < points.Count; i++) {

                        var worldPoint = o.transform.TransformPoint(points[i].point);

                        if (i == points.Count - 1) {

                            points[i] = new SplineStamp.SplinePoint(o.transform.InverseTransformPoint(Handles.Slider2D(worldPoint, Vector3.up, Vector3.forward, Vector3.right, HandleUtility.GetHandleSize(worldPoint) * handleScale, Handles.RectangleHandleCap, 0)), points[i].scale);

                        } else {

                            points[i] = new SplineStamp.SplinePoint(o.transform.InverseTransformPoint(Handles.Slider2D(worldPoint, Vector3.up, Vector3.forward, Vector3.right, HandleUtility.GetHandleSize(worldPoint) * handleScale, Handles.CircleHandleCap, 0)), points[i].scale);

                        }

                    }

                }

            }

            if (EditorGUI.EndChangeCheck()) {

                Undo.RecordObject(o, "Atlas Spline Change");

                o.points.Clear();

                o.points.AddRange(points);

                o.CreateMeshIfCreated();

                PrefabUtility.RecordPrefabInstancePropertyModifications(o);

                AtlasStamper.QueRender();

            }

        }

        private bool Raycast(SplineStamp o, out Vector3 point) {

            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

            var plane = new Plane(Vector3.up, o.transform.position);

            if (plane.Raycast(ray, out var dist)) {

                point = ray.origin + (ray.direction * dist);

                return true;

            }

            point = Vector3.zero;

            return false;

        }

        private bool RaycastPoints(SplineStamp o, List<SplineStamp.SplinePoint> points, float handleScale, out int index) {

            index = -1;

            if (points == null) { return false; }

            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

            for (var i = 0; i < points.Count; i++) {

                var plane = new Plane(Vector3.up, o.transform.TransformPoint(points[i].point));

                if (plane.Raycast(ray, out var dist)) {

                    var point = ray.origin + (ray.direction * dist);

                    var distFromOrigin = Vector3.Distance(o.transform.TransformPoint(points[i].point), point);

                    if (distFromOrigin < HandleUtility.GetHandleSize(points[i].point) * handleScale) {

                        index = i;

                        return true;

                    }

                }


            }

            return false;

        }

        private bool RaycastLines(SplineStamp o, List<SplineStamp.SplinePoint> points, float handleScale, out int index, out Vector3 p) {

            index = -1;

            p = Vector3.zero;

            if (points == null) { return false; }

            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

            for (var i = 0; i < points.Count - 1; i++) {

                var p1 = o.transform.TransformPoint(points[i].point);
                var p2 = o.transform.TransformPoint(points[i + 1].point);

                if (ClosestPointsOnTwoLines(out var pl1, out var pl2, p1, p2 - p1, ray.origin, ray.direction)) {

                    var dist = Vector3.Distance(pl1, pl2);

                    if (dist < HandleUtility.GetHandleSize(pl1) * handleScale * 2 && ProjectPointOnLineSegment(p1, p2, pl2)) {

                        index = i + 1;

                        p = pl1;

                        return true;

                    }

                }

            }

            return false;

        }

        private bool ClosestPointsOnTwoLines(out Vector3 closestPointLine1, out Vector3 closestPointLine2, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2) {

            closestPointLine1 = Vector3.zero;
            closestPointLine2 = Vector3.zero;

            var a = Vector3.Dot(lineVec1, lineVec1);
            var b = Vector3.Dot(lineVec1, lineVec2);
            var e = Vector3.Dot(lineVec2, lineVec2);

            var d = (a * e) - (b * b);

            if (d != 0.0f) {

                var r = linePoint1 - linePoint2;
                var c = Vector3.Dot(lineVec1, r);
                var f = Vector3.Dot(lineVec2, r);

                var s = ((b * f) - (c * e)) / d;
                var t = ((a * f) - (c * b)) / d;

                closestPointLine1 = linePoint1 + (lineVec1 * s);
                closestPointLine2 = linePoint2 + (lineVec2 * t);

                return true;

            } else {

                return false;
            }
        }

        private bool ProjectPointOnLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point) {

            var vector = linePoint2 - linePoint1;

            var projectedPoint = ProjectPointOnLine(linePoint1, vector.normalized, point);

            var side = PointOnWhichSideOfLineSegment(linePoint1, linePoint2, projectedPoint);

            return side == 0 ? true : false;
        }

        private Vector3 ProjectPointOnLine(Vector3 linePoint, Vector3 lineVec, Vector3 point) {

            var linePointToPoint = point - linePoint;

            var t = Vector3.Dot(linePointToPoint, lineVec);

            return linePoint + (lineVec * t);
        }

        private int PointOnWhichSideOfLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point) {

            var lineVec = linePoint2 - linePoint1;
            var pointVec = point - linePoint1;

            var dot = Vector3.Dot(pointVec, lineVec);

            return dot > 0 ? pointVec.magnitude <= lineVec.magnitude ? 0 : 2 : 1;

        }

        private void InsertPoint(SplineStamp o, int index, Vector3 p) {

            if (o.points == null || index > o.points.Count) { return; }

            o.points.Insert(index, new SplineStamp.SplinePoint(o.transform.InverseTransformPoint(p), 1));

        }

        private void AddPoint(SplineStamp o, Vector3 p) {

            if (o.points == null) { o.points = new List<SplineStamp.SplinePoint>(); }

            o.points.Add(new SplineStamp.SplinePoint(o.transform.InverseTransformPoint(p), 1));
        }

        private void RemovePoint(SplineStamp o, int index) {

            if (o.points == null) { return; }

            o.points.RemoveAt(index);

        }

        private void UpdateSizeAndCenter(SplineStamp o) {

            o.center = Vector3.zero;

            if (o.points == null || o.points.Count < 2) { return; }

            var aa = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var bb = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            foreach (var i in o.points) {

                aa.x = Mathf.Min(aa.x, i.point.x);
                aa.z = Mathf.Min(aa.z, i.point.z);

                bb.x = Mathf.Max(bb.x, i.point.x);
                bb.z = Mathf.Max(bb.z, i.point.z);

            }

            o.center = new Vector3(aa.x + ((bb.x - aa.x) * 0.5f), 0, aa.z + ((bb.z - aa.z) * 0.5f));
            o.size = new Vector3(bb.x - aa.x, o.size.y, bb.z - aa.z);

        }

        private void DrawMeshSplit() {

            var view = SceneView.currentDrawingSceneView;

            if (view != null) {

                if (meshExportSettings.isExpanded && meshExportSettings.FindPropertyRelative("useSplitting").boolValue) {

                    var size = meshExportSettings.FindPropertyRelative("splitGridSize").floatValue;

                    var offset = meshExportSettings.FindPropertyRelative("splitGridOffset").vector2Value;

                    var flatSize = new Vector3(size, 0, size);

                    var plane = new Plane(Vector3.up, Vector3.zero);

                    var ray = new Ray(view.camera.transform.position, view.camera.transform.forward);

                    float distance;

                    if (plane.Raycast(ray, out distance)) {

                        var center = ray.origin + (ray.direction * distance);

                        center.x = Mathf.FloorToInt(center.x / size) * size;
                        center.y = 0f;
                        center.z = Mathf.FloorToInt(center.z / size) * size;

                        var position = center + new Vector3(offset.x, 0, offset.y);

                        for (var x = -2; x <= 2; x++) {

                            for (var y = -2; y <= 2; y++) {

                                var gridOffset = new Vector3(x * size, 0, y * size);

                                Handles.DrawWireCube(gridOffset + position + (flatSize * 0.5f), flatSize);

                            }

                        }

                    }

                }

            }

        }

        private enum ToolBarMode {
            Edit,
            Paint,
            Settings,
        }

    }

}
