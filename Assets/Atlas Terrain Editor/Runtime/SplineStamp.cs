using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Atlas.Unity {

    public class SplineStamp : StampBase {

        [Tooltip("Width of the spline.")]
        public float width = 10f;
        [Tooltip("How many times the stamp will repeat itself along the spline.")]
        public float tiling = 1f;
        [Tooltip("Starting fade of the spline where a value of 0 means no fading.")]
        [Range(0, 1)]
        public float fadeStart = 0.1f;
        [Tooltip("Ending fade of the spline where a value of 1 means no fading.")]
        [Range(0, 1)]
        public float fadeEnd = 0.9f;
        [Tooltip("When enabled the spline will be a closed loop.")]
        public bool closed = false;
        [Tooltip("Quality of the spline while editing, when we stop editing the terrain maximum quality automatically applies.")]
        public SplineQuality quality;
        public SplineQualitySettings qualitySettings;
        public SplineToMeshQualitySettings meshExportSettings;
        public EditMode editMode;
        public List<SplinePoint> points;

        private List<SplineValue> splineValues;
        private SplineQuality beforeRenderForExportSplineQuality;
        private int lastRenderTick;
        private AtlasStamper lastRenderBase;
        private SplineQualitySettings splineQualitySettings {
            get {
                switch (quality) {
                    case SplineQuality.FastPreview: return SplineQualitySettings.splineQualitySettings[0];
                    case SplineQuality.AccuratePreview: return SplineQualitySettings.splineQualitySettings[1];
                    case SplineQuality.Export: return SplineQualitySettings.splineQualitySettings[2];
                    default:
                        return qualitySettings;
                }
            }
        }
        private bool exporting = false;


        private void OnDrawGizmosSelected() {

            if (points == null || points.Count < 2) {

                return;

            }

            Gizmos.matrix = transform.localToWorldMatrix;

            for (var i = 1; i < points.Count; i++) {

                Gizmos.DrawLine(points[i - 1].point, points[i].point);

            }

            if (closed) {

                Gizmos.DrawLine(points[0].point, points[points.Count - 1].point);

            }

            SplineUtils.GetSides(points, width, closed, transform.up, out var leftPoints, out var rightPoints);

            Gizmos.color = new Color(1, 1, 1, 0.2f);

            for (var i = 1; i < points.Count; i++) {

                Gizmos.DrawLine(leftPoints[i - 1], leftPoints[i]);

                Gizmos.DrawLine(rightPoints[i - 1], rightPoints[i]);

            }

            if (closed) {

                Gizmos.DrawLine(leftPoints[0], leftPoints[leftPoints.Count - 1]);

                Gizmos.DrawLine(rightPoints[0], rightPoints[rightPoints.Count - 1]);

            }

        }

        private void Update() {

            if (transform.hasChanged && meshExportSettings.useSplitting) {

                transform.hasChanged = false;

                CreateMeshIfCreated();

            }

        }

        private void OnDestroy() {
            


        }

        public void SmoothPointsHeight() {

            if (points == null || points.Count < 2) { return; }

            if (closed) {

                for (var i = 0; i < points.Count; i++) {

                    var i0 = i == 0 ? (points.Count - 1) : i - 1;
                    var i1 = i == (points.Count - 1) ? 0 : i + 1;

                    var p0 = points[i0].point.y;
                    var p1 = points[i].point.y;
                    var p2 = points[i1].point.y;

                    p1 = Mathf.Lerp(p1, Mathf.Lerp(p0, p2, 0.5f), 0.5f);

                    points[i] = new SplinePoint(new Vector3(points[i].point.x, p1, points[i].point.z), points[i].scale);

                }

            } else {

                for (var i = 1; i < points.Count - 1; i++) {

                    var i0 = i;
                    var i1 = i + 1;

                    var p0 = points[i0].point.y;
                    var p1 = points[i].point.y;
                    var p2 = points[i1].point.y;

                    p1 = Mathf.Lerp(p1, Mathf.Lerp(p0, p2, 0.5f), 0.5f);

                    points[i] = new SplinePoint(new Vector3(points[i].point.x, p1, points[i].point.z), points[i].scale);

                }

            }

            AtlasStamper.QueRender();

        }

        public void SmoothPointsLateral() {

            if (points == null || points.Count < 2) { return; }

            if (closed) {

                for (var i = 0; i < points.Count; i++) {

                    var i0 = i == 0 ? (points.Count - 1) : i - 1;
                    var i1 = i == (points.Count - 1) ? 0 : i + 1;

                    var p0 = new Vector2(points[i0].point.x, points[i0].point.z);
                    var p1 = new Vector2(points[i].point.x, points[i].point.z);
                    var p2 = new Vector2(points[i1].point.x, points[i1].point.z);

                    p1 = Vector2.Lerp(p1, Vector2.Lerp(p0, p2, 0.5f), 0.5f);

                    points[i] = new SplinePoint(new Vector3(p1.x, points[i].point.y, p1.y), points[i].scale);

                }

            } else {

                for (var i = 1; i < points.Count - 1; i++) {

                    var i0 = i - 1;
                    var i1 = i + 1;

                    var p0 = new Vector2(points[i0].point.x, points[i0].point.z);
                    var p1 = new Vector2(points[i].point.x, points[i].point.z);
                    var p2 = new Vector2(points[i1].point.x, points[i1].point.z);

                    p1 = Vector2.Lerp(p1, Vector2.Lerp(p0, p2, 0.5f), 0.5f);

                    points[i] = new SplinePoint(new Vector3(p1.x, points[i].point.y, p1.y), points[i].scale);

                }

            }

            AtlasStamper.QueRender();

        }

        public void SmoothScale() {

            if (points == null || points.Count < 2) { return; }

            if (closed) {

                for (var i = 0; i < points.Count; i++) {

                    var i0 = i == 0 ? (points.Count - 1) : i - 1;
                    var i1 = i == (points.Count - 1) ? 0 : i + 1;

                    var p0 = points[i0].scale;
                    var p1 = points[i].scale;
                    var p2 = points[i1].scale;

                    p1 = Mathf.Lerp(p1, Mathf.Lerp(p0, p2, 0.5f), 0.5f);

                    points[i] = new SplinePoint(points[i].point, p1);

                }

            } else {

                for (var i = 1; i < points.Count - 1; i++) {

                    var i0 = i - 1;
                    var i1 = i + 1;

                    var p0 = points[i0].scale;
                    var p1 = points[i].scale;
                    var p2 = points[i1].scale;

                    p1 = Mathf.Lerp(p1, Mathf.Lerp(p0, p2, 0.5f), 0.5f);

                    points[i] = new SplinePoint(points[i].point, p1);

                }

            }

            AtlasStamper.QueRender();

        }

        public void Subdevide() {

            if (closed) {

                for (var i = points.Count - 1; i >= 0; i--) {

                    var c = new SplinePoint((points[i].point + points[(i + 1) % points.Count].point) * 0.5f, points[i].scale);

                    points.Insert(i + 1, c);

                }

            } else {


                for (var i = points.Count - 2; i >= 0; i--) {

                    var c = new SplinePoint((points[i].point + points[i + 1].point) * 0.5f, points[i].scale); ;

                    points.Insert(i + 1, c);

                }

            }

            AtlasStamper.QueRender();

        }

        public void UnSubdevide() {

            if (closed) {

                for (var i = points.Count - 1; i >= 0; i--) {

                    if (i % 2 == 1) {

                        points.RemoveAt(i);

                    }

                }

            } else {

                for (var i = points.Count - 2; i >= 1; i--) {

                    if (i % 2 == 0) {

                        points.RemoveAt(i);

                    }

                }

            }

            AtlasStamper.QueRender();

        }

        public void DropToFloor() {

            if (points == null || points.Count < 2) {

                return;

            }

            for (var i = 0; i < points.Count; i++) {

                var point = transform.TransformPoint(points[i].point);

                point.y = 10000;

                if (Physics.Raycast(point, Vector3.down, out var hit, 20000)) {

                    points[i] = new SplinePoint(transform.InverseTransformPoint(new Vector3(point.x, hit.point.y, point.z)), points[i].scale);

                }

            }

            AtlasStamper.QueRender();

        }


        public override bool MayDrawIcon(out string path) {

            //path = "Packages/com.atlas.atlas-terrain-editor/Runtime/Icons/Atlas_spline_icon.tif";

            path = "AtlasSplineIcon.tif";

            return true;

        }

        public override void OnBeforeRenderForExport() {

            beforeRenderForExportSplineQuality = quality;

            quality = SplineQuality.Export;

            exporting = true;

        }

        public override void OnAfterRenderForExport() {

            quality = beforeRenderForExportSplineQuality;

            exporting = false;

        }

        public override bool MayRender() {

            return !(stamp == null || points == null || points.Count < 2);

        }

        public override void DrawMesh(AtlasStamper stampTerrainBase, DrawMeshType drawMeshType, bool forMask = false) {

            if (lastRenderTick != Time.frameCount || lastRenderBase != stampTerrainBase || splineValues == null || exporting) {

                lastRenderTick = Time.frameCount;

                lastRenderBase = stampTerrainBase;

                GetSplineValues(stampTerrainBase, points, closed, splineQualitySettings, out splineValues, 7);

            }

            GL.Begin(GL.QUADS);

            var tiling = Vector2.one;

            if (tileMask) {

                tiling = stampTiling;

            } else {

                if (drawMeshType != DrawMeshType.Mask && drawMeshType != DrawMeshType.RoadMask) {

                    tiling = stampTiling;

                }

            }

            var drawMeshForMask = drawMeshType == DrawMeshType.Mask || drawMeshType == DrawMeshType.RoadMask;

            foreach (var i in splineValues) {

                foreach (var p in i.widthPoints) {

                    GL.MultiTexCoord2(0, drawMeshForMask ? 0.5f : p.uv2 * tiling.x, p.f1 * tiling.y);
                    GL.MultiTexCoord2(1, p.p2f.y, p.p2t.y);
                    GL.MultiTexCoord2(2, p.fadef2, p.fadef2);
                    GL.Vertex3(p.p2f.x, p.p2f.z, 0);

                    GL.MultiTexCoord2(0, drawMeshForMask ? 0.5f : p.uv2 * tiling.x, p.f2 * tiling.y);
                    GL.MultiTexCoord2(1, p.p3f.y, p.p3t.y);
                    GL.MultiTexCoord2(2, p.fadef2, p.fadef2);
                    GL.Vertex3(p.p3f.x, p.p3f.z, 0);

                    GL.MultiTexCoord2(0, drawMeshForMask ? 0.5f : p.uv1 * tiling.x, p.f2 * tiling.y);
                    GL.MultiTexCoord2(1, p.p1f.y, p.p1t.y);
                    GL.MultiTexCoord2(2, p.fadef1, p.fadef1);
                    GL.Vertex3(p.p1f.x, p.p1f.z, 0);

                    GL.MultiTexCoord2(0, drawMeshForMask ? 0.5f : p.uv1 * tiling.x, p.f1 * tiling.y);
                    GL.MultiTexCoord2(1, p.p0f.y, p.p0t.y);
                    GL.MultiTexCoord2(2, p.fadef1, p.fadef1);
                    GL.Vertex3(p.p0f.x, p.p0f.z, 0);

                }

            }

            GL.End();

        }



        public void CreateMesh() {

            var allSplinePoints = SplineUtils.GetSplinePoints(points, closed, meshExportSettings.maxDistanceBetweenPoints, meshExportSettings.maxPoints);

            var splinePointsPerSector = new List<KeyValuePair<string, List<SplinePoint>>>();

            if (meshExportSettings.useSplitting && allSplinePoints.Count > 2) {

                var preSector = gameObject.name + "-splinemesh-" + CalcSplinePointSectorForMeshSplitting(allSplinePoints[0], meshExportSettings.splitGridOffset, Mathf.Max(10, meshExportSettings.splitGridSize)) + "_" + (splinePointsPerSector.Count + 1);

                splinePointsPerSector.Add(new KeyValuePair<string, List<SplinePoint>>(preSector, new List<SplinePoint>()));

                var cc = 0;

                foreach (var i in allSplinePoints) {

                    var sector = gameObject.name + "-splinemesh-" + CalcSplinePointSectorForMeshSplitting(i, meshExportSettings.splitGridOffset, Mathf.Max(10, meshExportSettings.splitGridSize)) + "_" + splinePointsPerSector.Count;

                    if (sector != preSector && cc < allSplinePoints.Count - 2) {

                        splinePointsPerSector[splinePointsPerSector.Count - 1].Value.Add(i);

                        preSector = gameObject.name + "-splinemesh-" + CalcSplinePointSectorForMeshSplitting(i, meshExportSettings.splitGridOffset, Mathf.Max(10, meshExportSettings.splitGridSize)) + "_" + (splinePointsPerSector.Count + 1);

                        splinePointsPerSector.Add(new KeyValuePair<string, List<SplinePoint>>(sector, new List<SplinePoint>()));

                    }

                    splinePointsPerSector[splinePointsPerSector.Count - 1].Value.Add(i);

                    cc++;

                }

            } else {

                splinePointsPerSector.Add(new KeyValuePair<string, List<SplinePoint>>(gameObject.name + "-splinemesh", allSplinePoints));

            }

            var allTransforms = gameObject.GetComponentsInChildren<Transform>();

            foreach (var i in allTransforms) {

                if (i == transform) { continue; }

                var found = false;

                foreach (var ii in splinePointsPerSector) {

                    if (ii.Key == i.gameObject.name) {

                        found = true;

                        break;

                    }

                }

                if (found == false) {

                    DestroyMesh(i.gameObject);

                    //GameObject.DestroyImmediate(i.gameObject, false);

                }

            }

            var meshes = new List<Mesh>();

            for (var splinePointsPerSectorIndex = 0; splinePointsPerSectorIndex < splinePointsPerSector.Count; splinePointsPerSectorIndex++) {


                var keyValuePair = splinePointsPerSector[splinePointsPerSectorIndex];

                var sector = keyValuePair.Key;

                var splinePoints = keyValuePair.Value;


                var isClosed = closed && splinePointsPerSector.Count == 1;

                var isClosedButSplit = closed && splinePointsPerSector.Count > 1;


                var name = sector;

                var tr = transform.Find(name);

                GameObject go;

                if (tr == null) {

                    go = new GameObject(name);
                    go.transform.parent = gameObject.transform;
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localEulerAngles = Vector3.zero;
                    go.transform.localScale = Vector3.one;

                } else {

                    go = tr.gameObject;

                    var meshFilter = go.GetComponent<MeshFilter>();

                    if (meshFilter != null && meshFilter.sharedMesh != null) {

                         GameObject.DestroyImmediate(meshFilter.sharedMesh, true);

                    }

                }

                var meshWidth = Mathf.Abs(meshExportSettings.width);

                SplineUtils.GetSides(splinePoints, meshWidth, isClosed, transform.up, out var leftPoints, out var rightPoints);

                List<Vector3> vertices = new List<Vector3>();
                List<Vector2> uv = new List<Vector2>();
                List<Color> colors = new List<Color>();
                List<int> triangles = new List<int>();

                for (var i = 0; i <= (isClosed ? splinePoints.Count : (splinePoints.Count - 1)); i++) {

                    var index = i % splinePoints.Count;

                    var factorifrom = (float)index / (splinePoints.Count - 1);
                    var uv1 = CalcUV(factorifrom, meshExportSettings.tiling);
                    var fade1 = CalcFade(isClosed, meshExportSettings.fadeStart, meshExportSettings.fadeEnd, factorifrom);
                    var worldpoint0 = leftPoints[index];
                    var worldpoint1 = rightPoints[index];

                    if (isClosedButSplit && splinePointsPerSectorIndex == splinePointsPerSector.Count - 1 && i == splinePoints.Count - 1) {

                        SplineUtils.GetSides(splinePointsPerSector[0].Value, meshWidth, isClosed, transform.up, out var newLeftPoints, out var newRightPoints);

                        worldpoint0 = newLeftPoints[0];
                        worldpoint1 = newRightPoints[0];

                    }

                    for (var ii = 0; ii <= (meshExportSettings.widthSegments - 1); ii++) {

                        var factoriifrom = (float)ii / (meshExportSettings.widthSegments - 1);
                        var point0 = Vector3.Lerp(worldpoint0, worldpoint1, factoriifrom);
                        var edge0 = 0f;

                        if (meshExportSettings.widthSegments > 3) {

                            if (ii == 0) {

                                point0 += Vector3.down * meshExportSettings.edgePushDown;
                                edge0 = 1;

                            } else if (ii == meshExportSettings.widthSegments - 1) {

                                point0 += Vector3.down * meshExportSettings.edgePushDown;
                                edge0 = 1;

                            } else {

                                point0 += Vector3.up * meshExportSettings.midPushUp;
                                edge0 = 0;

                            }

                        }

                        vertices.Add(go.transform.InverseTransformPoint(transform.TransformPoint(point0)));
                        uv.Add(new Vector2(factoriifrom, uv1));
                        colors.Add(new Color(0, 0, edge0, fade1));

                    }

                }

                for (var i = 0; i < (isClosed ? splinePoints.Count : (splinePoints.Count - 1)); i++) {

                    var index = i % splinePoints.Count;
                    var indexPlusOne = (i + 1) % splinePoints.Count;

                    for (var ii = 0; ii < (meshExportSettings.widthSegments - 1); ii++) {

                        triangles.Add((index * meshExportSettings.widthSegments) + ii);
                        triangles.Add((index * meshExportSettings.widthSegments) + ii + 1);
                        triangles.Add((indexPlusOne * meshExportSettings.widthSegments) + ii);


                        triangles.Add((indexPlusOne * meshExportSettings.widthSegments) + ii + 1);
                        triangles.Add((indexPlusOne * meshExportSettings.widthSegments) + ii);
                        triangles.Add((index * meshExportSettings.widthSegments) + ii + 1);

                    }

                }

                var mf = go.GetComponent<MeshFilter>();

                if (mf == null) {

                    mf = go.AddComponent<MeshFilter>();

                }

                var m = new Mesh() { name = gameObject.name + "_splinemesh", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32, };

                var mr = go.GetComponent<MeshRenderer>();

                if (mr == null) {

                    mr = go.AddComponent<MeshRenderer>();

                }

                mr.sharedMaterial = meshExportSettings.material;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                m.vertices = vertices.ToArray();
                m.uv = uv.ToArray();
                m.colors = colors.ToArray();
                m.triangles = triangles.ToArray();
                m.RecalculateBounds();
                m.RecalculateNormals();
                m.RecalculateTangents();

                mf.sharedMesh = m;

                meshes.Add(m);

            }

        }

        public void CreateMeshIfCreated() {

            var allTransforms = transform.GetComponentsInChildren<Transform>();

            foreach (var i in allTransforms) {

                if (i == transform) { continue; }

                if (i.gameObject.name.Contains("-splinemesh")) {

                    CreateMesh();

                    break;

                }

            }

        }

        public void ClearMesh() {

            var allTransforms = transform.GetComponentsInChildren<Transform>();

            foreach (var i in allTransforms) {

                if (i == transform) { continue; }

                if (i.gameObject.name.Contains("-splinemesh")) {

                    DestroyMesh(i.gameObject);

                }

            }

        }

        private void DestroyMesh(GameObject meshGameObject) {

            var meshFilter = meshGameObject.GetComponent<MeshFilter>();

            if (meshFilter != null && meshFilter.sharedMesh != null) {

                GameObject.DestroyImmediate(meshFilter.sharedMesh);

            }

            GameObject.DestroyImmediate(meshGameObject, false);

        }


        public void GetSplineValues(AtlasStamper stampTerrainBase, List<SplinePoint> points, bool closed, SplineQualitySettings splineQualitySettings, out List<SplineValue> splineValues, int minPoints) {

            splineValues = new List<SplineValue>();

            var lastPoints = SplineUtils.GetSplinePoints(points, closed, splineQualitySettings.maxLength, splineQualitySettings.maxDistanceBetweenPoints, minPoints);

            SplineUtils.GetSides(lastPoints, width, closed, transform.up, out var lastLeftPoints, out var lastRightPoints);

            for (var i = 0; i < (closed ? lastPoints.Count : (lastPoints.Count - 1)); i++) {

                var nexti = (i + 1) % lastPoints.Count;

                var factorifrom = (float)i / (lastPoints.Count - 1);
                var factorito = (float)(i + 1) / (lastPoints.Count - 1);

                var uv1 = CalcUV(factorifrom, tiling);
                var uv2 = CalcUV(factorito, tiling);

                var fade1 = CalcFade(closed, fadeStart, fadeEnd, factorifrom);
                var fade2 = CalcFade(closed, fadeStart, fadeEnd, factorito);

                var worldpoint0 = lastLeftPoints[i];
                var worldpoint1 = lastRightPoints[i];
                var worldpoint2 = lastLeftPoints[nexti];
                var worldpoint3 = lastRightPoints[nexti];

                var newSplineValue = new SplineValue();

                for (var ii = 0; ii < (splineQualitySettings.widthSegments - 1); ii++) {

                    var factoriifrom = (float)ii / (splineQualitySettings.widthSegments - 1);
                    var factoriito = (float)(ii + 1) / (splineQualitySettings.widthSegments - 1);

                    var point0 = Vector3.Lerp(worldpoint0, worldpoint1, factoriifrom);
                    var point1 = Vector3.Lerp(worldpoint0, worldpoint1, factoriito);
                    var point2 = Vector3.Lerp(worldpoint2, worldpoint3, factoriifrom);
                    var point3 = Vector3.Lerp(worldpoint2, worldpoint3, factoriito);

                    var point0from = AtlasUtils.LocalPointToTerrainRelativePoint(this, point0, stampTerrainBase);
                    var point1from = AtlasUtils.LocalPointToTerrainRelativePoint(this, point1, stampTerrainBase);
                    var point2from = AtlasUtils.LocalPointToTerrainRelativePoint(this, point2, stampTerrainBase);
                    var point3from = AtlasUtils.LocalPointToTerrainRelativePoint(this, point3, stampTerrainBase);

                    var point0to = AtlasUtils.LocalPointToTerrainRelativePoint(this, point0 + (Vector3.up * size.y), stampTerrainBase);
                    var point1to = AtlasUtils.LocalPointToTerrainRelativePoint(this, point1 + (Vector3.up * size.y), stampTerrainBase);
                    var point2to = AtlasUtils.LocalPointToTerrainRelativePoint(this, point2 + (Vector3.up * size.y), stampTerrainBase);
                    var point3to = AtlasUtils.LocalPointToTerrainRelativePoint(this, point3 + (Vector3.up * size.y), stampTerrainBase);

                    var newSplinePoint = new SplineValue.SplinePoint {
                        uv1 = uv1,
                        uv2 = uv2,
                        fadef1 = fade1,
                        fadef2 = fade2,
                        f1 = factoriifrom,
                        f2 = factoriito,
                        p0f = point0from,
                        p1f = point1from,
                        p2f = point2from,
                        p3f = point3from,
                        p0t = point0to,
                        p1t = point1to,
                        p2t = point2to,
                        p3t = point3to
                    };

                    newSplineValue.widthPoints.Add(newSplinePoint);

                }

                splineValues.Add(newSplineValue);

            }

        }

        public float CalcUV(float f, float tiling) {

            return f * tiling;

        }

        public float CalcFade(bool closed, float fadeStart, float fadeEnd, float f) {

            return closed ? 1 : (fadeEnd == 1 ? 1 : Mathf.InverseLerp(1f - 0.001f, fadeEnd, f)) * (fadeStart == 0 ? 1 : Mathf.InverseLerp(0f + 0.001f, fadeStart, f));

        }

        public string CalcSplinePointSectorForMeshSplitting(SplinePoint splinePoint, Vector2 offset, float size) {

            var position = transform.TransformPoint(splinePoint.point);

            position.x -= offset.x;
            position.z -= offset.y;

            return Mathf.FloorToInt(position.x / size).ToString() + "_" + Mathf.FloorToInt(position.z / size).ToString();

        }


        [Serializable]
        public struct SplinePoint {

            public Vector3 point;
            public float scale;

            public SplinePoint(Vector3 point, float scale) {
                this.point = point;
                this.scale = scale;
            }

        }

        public enum SplineQuality {
            FastPreview,
            AccuratePreview,
            Export,
            Custom,
        }

        public enum EditMode {
            Add,
            Move,
            Scale,
        }

        [Serializable]
        public class SplineQualitySettings {

            public float maxLength = 5f;
            public int maxDistanceBetweenPoints = 100;
            public int widthSegments = 5;

            public static SplineQualitySettings[] splineQualitySettings = new SplineQualitySettings[] {

                new SplineQualitySettings(){
                    maxLength = 5,
                    maxDistanceBetweenPoints = 50,
                    widthSegments = 3,
                },

                new SplineQualitySettings(){
                    maxLength = 1,
                    maxDistanceBetweenPoints = 300,
                    widthSegments = 8,
                },

                new SplineQualitySettings(){
                    maxLength = 0.1f,
                    maxDistanceBetweenPoints = 1000,
                    widthSegments = 20,
                },

            };

        }

        [Serializable]
        public class SplineToMeshQualitySettings {

            [Tooltip("Material of the mesh that will be generated.")]
            public Material material;
            [Tooltip("Width of the spline.")]
            public float width = 7;
            [Tooltip("Amount by which UV will tile.")]
            public float tiling = 100;
            [Space(10)]
            [Tooltip("Preferred distance between vertices along the length.")]
            [Range(0.1f, 100)]
            public float maxDistanceBetweenPoints = 1f;
            [Tooltip("Maximum vertices count along the length.")]
            public int maxPoints = 10000;
            [Space(10)]
            [Tooltip("Vertex count along the width.")]
            [Range(4, 21)]
            public int widthSegments = 5;
            [Tooltip("How far the edge vertices should be pushed down.\nEdge data is applied on the vertex color (B) channel.")]
            public float edgePushDown = 0.2f;
            [Tooltip("How far the middle vertices should be pushed up.")]
            public float midPushUp = 0.015f;
            [Tooltip("Starting fade of the spline where a value of 0 means no fading.\nFade data is applied on the vertex color (A) channel.")]
            [Range(0, 1)]
            public float fadeStart = 0.1f;
            [Tooltip("Ending fade of the spline where a value of 1 means no fading.\nFade data is applied on the vertex color (A) channel.")]
            [Range(0, 1)]
            public float fadeEnd = 0.9f;
            [Space(10)]
            [Tooltip("Split the mesh via grid.")]
            public bool useSplitting = false;
            [Tooltip("Split grid size.")]
            public float splitGridSize = 1000;
            [Tooltip("Split grid offset in world space.")]
            public Vector2 splitGridOffset = Vector2.zero;

        }

        public class SplineValue {

            public List<SplinePoint> widthPoints = new List<SplinePoint>();

            public class SplinePoint {

                public float uv1;
                public float uv2;
                public float fadef1;
                public float fadef2;
                public float f1;
                public float f2;

                public Vector3 p0f;
                public Vector3 p1f;
                public Vector3 p2f;
                public Vector3 p3f;

                public Vector3 p0t;
                public Vector3 p1t;
                public Vector3 p2t;
                public Vector3 p3t;

            }

        }

        public enum NodeMovementMode {
            DragGizmo,
            TransformationGizmo
        }

    }

    public static class SplineUtils {

        private static List<Vector2> segmentRanges = new List<Vector2>();


        public static List<SplineStamp.SplinePoint> GetSplinePoints(List<SplineStamp.SplinePoint> points, bool closed, float maxDistanceBetweenPoints = 1f, int maxPoints = 1000, int minPoints = 0) {

            //clamp maxDistanceBetweenPoints

            maxDistanceBetweenPoints = Mathf.Max(0.1f, maxDistanceBetweenPoints);

            //return empty list if we cant get spline points

            if (points == null || points.Count < 1) {

                return new List<SplineStamp.SplinePoint>();

            }


            //return single element list if we have 1 points

            if (points.Count == 1) {

                return new List<SplineStamp.SplinePoint>() {
                    points[0],
                };

            }


            //return straight line if we have 2 points

            if (points.Count == 2 && minPoints <= 2) {

                return new List<SplineStamp.SplinePoint>() {
                    points[0],
                    points[1],
                };

            }


            //calculate total length and calc the total points needed

            var length = GetLength(points, closed);

            for (var i = 1; i < points.Count; i++) {

                length += Vector3.Distance(points[i - 1].point, points[i].point);

            }

            if (closed) {

                length += Vector3.Distance(points[0].point, points[points.Count - 1].point);

            }

            var itterations = Mathf.Max(Mathf.Min(maxPoints, length / maxDistanceBetweenPoints), minPoints);


            //build and return list of points

            var returnPoints = new List<SplineStamp.SplinePoint>();

            for (var i = 0; i < itterations; i++) {

                var f = i * (1f / itterations);

                returnPoints.Add(GetSplinePoint(points, closed, f));

            }

            return returnPoints;

        }

        public static Vector3[] GetPoints(List<SplineStamp.SplinePoint> points, bool closed, float maxDistanceBetweenPoints = 1f, int maxPoints = 1000) {

            var splinePoints = GetSplinePoints(points, closed, maxDistanceBetweenPoints, maxPoints);

            var splinePointsVector3 = new Vector3[splinePoints.Count];

            for (var i = 0; i < splinePoints.Count; i++) {

                splinePointsVector3[i] = splinePoints[i].point;

            }

            return splinePointsVector3;

        }

        public static SplineStamp.SplinePoint GetSplinePoint(List<SplineStamp.SplinePoint> points, bool closed, float f) {

            //return empty point if we cant calc the spline point

            if (points == null || points.Count < 1) {

                return new SplineStamp.SplinePoint();

            }


            //return the first spline point if we have 1 spline point

            if (points.Count == 1) {

                return points[0];

            }


            //cycle f

            if (f > 1) {

                f = f % 1f;

            }

            if (f < 0) {

                f = (Mathf.Ceil(Mathf.Abs(f)) - Mathf.Abs(f)) % 1f;

            }


            //lerp linear if we have 2 spline points

            if (points.Count == 2) {

                return new SplineStamp.SplinePoint(Vector3.Lerp(points[0].point, points[1].point, f), Mathf.Lerp(points[0].scale, points[1].scale, f));

            }


            //retrun start and end cases when not closed

            if (!closed) {

                if (f == 0) {

                    return points[0];

                } else if (f == 1) {

                    return points[points.Count - 1];

                }

            }


            //calculate total length and create a list of star and end distances per segment between points

            var length = 0f;

            segmentRanges.Clear();

            for (var i = 1; i < points.Count; i++) {

                var segmentLength = Vector3.Distance(points[i - 1].point, points[i].point);

                segmentRanges.Add(new Vector2(length, length + segmentLength));

                length += segmentLength;

            }

            if (closed) {

                var segmentLength = Vector3.Distance(points[0].point, points[points.Count - 1].point);

                segmentRanges.Add(new Vector2(length, length + segmentLength));

                length += segmentLength;

            }


            //select the correct segment and calculate the intermediat lerp factor

            var selectedLength = length * f;

            var selectedIndex = 0;

            var selectedFactor = 0f;

            for (var i = 0; i < segmentRanges.Count; i++) {

                if (selectedLength >= segmentRanges[i].x && selectedLength < segmentRanges[i].y) {

                    selectedIndex = i;

                    selectedFactor = Mathf.InverseLerp(segmentRanges[i].x, segmentRanges[i].y, selectedLength);

                    break;

                }

            }

            var selectedIndexPlusOne = (selectedIndex + 1) % points.Count;


            //calculate the points needed to do a bezier lerp

            var pointStart = points[selectedIndex].point;

            var pointEnd = points[selectedIndexPlusOne].point;

            var forwardStart = GetSplineForward(points, selectedIndex, closed);

            var forwardEnd = GetSplineForward(points, selectedIndexPlusOne, closed);

            var startEndDistance = Vector3.Distance(pointStart, pointEnd);

            var midStart = pointStart + forwardStart * startEndDistance * 0.333f;

            var midEnd = pointEnd - forwardEnd * startEndDistance * 0.333f;


            //create and return a new spline point

            var newPoint = LerpBezier(pointStart, midStart, midEnd, pointEnd, selectedFactor);

            var newScale = LerpBezierScale(points[selectedIndex].scale, points[selectedIndexPlusOne].scale, selectedFactor);

            return new SplineStamp.SplinePoint(newPoint, newScale);

        }

        public static Vector3 GetPoint(List<SplineStamp.SplinePoint> points, bool closed, float f) {

            return GetSplinePoint(points, closed, f).point;

        }

        public static Vector3 GetForward(List<SplineStamp.SplinePoint> points, bool closed, float f) {

            //return world forward if we cant calculate forward

            if (points == null || points.Count < 2) {

                return Vector3.forward;

            }


            //return linear forward if we have 2 points

            if (points.Count == 2) {

                return (points[1].point - points[0].point).normalized;

            }


            //cycle f

            if (f > 1) {

                f = f % 1f;

            }

            if (f < 0) {

                f = (Mathf.Ceil(Mathf.Abs(f)) - Mathf.Abs(f)) % 1f;

            }


            //return start and end cases when not closed

            if (!closed) {

                if (f == 0) {

                    return GetSplineForward(points, 0, closed);

                } else if (f == 1) {

                    return GetSplineForward(points, points.Count - 1, closed);

                }

            }


            //return forward interpolated between 3 points

            var pMin = GetSplinePoint(points, closed, f - 0.01f).point;
            var pMid = GetSplinePoint(points, closed, f).point;
            var pNex = GetSplinePoint(points, closed, f + 0.01f).point;

            return Vector3.Lerp((pMid - pMin).normalized, (pNex - pMid).normalized, 0.5f).normalized;

        }

        public static void GetSides(List<SplineStamp.SplinePoint> points, float width, bool closed, Vector3 up, out List<Vector3> leftRail, out List<Vector3> rightRail) {

            //return empty lists if we can't calculate side rails

            leftRail = new List<Vector3>();

            rightRail = new List<Vector3>();

            if (points == null || points.Count < 2) {

                return;

            }


            //build rail lists

            var upNormalized = up.normalized;

            for (var i = 0; i < points.Count; i++) {

                var point = points[i].point;

                var scale = points[i].scale;

                var forward = GetSplineForward(points, i, closed);

                var right = Vector3.Cross(forward, upNormalized);

                leftRail.Add(point - right * scale * width * 0.5f);

                rightRail.Add(point + right * scale * width * 0.5f);

            }

        }

        public static float GetLength(List<SplineStamp.SplinePoint> points, bool closed) {

            //return 0 of we can't calculate the length

            if (points == null || points.Count < 2) {

                return 0f;

            }


            //calc and return length

            var length = 0f;

            for (var i = 1; i < points.Count; i++) {

                length += Vector3.Distance(points[i - 1].point, points[i].point);

            }

            if (closed) {

                length += Vector3.Distance(points[0].point, points[points.Count - 1].point);

            }

            return length;

        }


        private static Vector3 GetSplineForward(List<SplineStamp.SplinePoint> points, int index, bool closed) {

            //return world forward if we can't calculate forward direction

            if (points == null || points.Count < 1) {

                return Vector3.forward;

            }


            //return linear forward direction if we have 2 points

            if (points.Count == 2) {

                return (points[1].point - points[0].point).normalized;

            }


            //return the forward directions for the start and end nodes

            if (!closed) {

                if (index == 0) {

                    return (points[1].point - points[0].point).normalized;

                } else if (index == points.Count - 1) {

                    return (points[points.Count - 1].point - points[points.Count - 2].point).normalized;

                }

            }


            //calculate the forward direction between 3 nodes

            var pMin = points[index == 0 ? points.Count - 1 : index - 1].point;
            var pMid = points[index].point;
            var pNex = points[index == points.Count - 1 ? 0 : index + 1].point;

            return Vector3.Lerp((pMid - pMin).normalized, (pNex - pMid).normalized, 0.5f).normalized;

        }

        private static Vector3 LerpBezier(Vector3 p0, Vector3 f0, Vector3 f1, Vector3 p1, float f) {

            var pp0 = Vector3.Lerp(p0, f0, f);
            var pp1 = Vector3.Lerp(f0, f1, f);
            var pp2 = Vector3.Lerp(f1, p1, f);

            var ppp0 = Vector3.Lerp(pp0, pp1, f);
            var ppp1 = Vector3.Lerp(pp1, pp2, f);

            return Vector3.Lerp(ppp0, ppp1, f);

        }

        private static float LerpBezierScale(float s0, float s1, float f) {

            return Mathf.Lerp(s0, s1, f);

        }

    }

}