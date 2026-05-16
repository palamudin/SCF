using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SCF.Gameplay
{
    public struct SCFTransformSnapshotTarget
    {
        public string Label;
        public Transform Target;

        public SCFTransformSnapshotTarget(string label, Transform target)
        {
            Label = label;
            Target = target;
        }
    }

    public struct SCFVectorSnapshotTarget
    {
        public string Label;
        public GameObject Owner;
        public Vector3 Value;

        public SCFVectorSnapshotTarget(string label, GameObject owner, Vector3 value)
        {
            Label = label;
            Owner = owner;
            Value = value;
        }
    }

    public static class SCFTransformSnapshotRecorder
    {
        public const string DefaultFolderPath = "Assets/Resources/SCF/Weapons/RuntimeSnapshots";

        public static string CaptureTransforms(string snapshotName, IEnumerable<SCFTransformSnapshotTarget> targets, string folderPath = DefaultFolderPath)
        {
            List<SCFTransformSnapshotTarget> targetList = new List<SCFTransformSnapshotTarget>();
            if (targets != null)
            {
                targetList.AddRange(targets);
            }

            string content = BuildTransformSnapshotText(snapshotName, targetList);
            return WriteSnapshot(snapshotName, content, folderPath);
        }

        public static string CaptureHierarchy(string snapshotName, Transform root, IEnumerable<SCFTransformSnapshotTarget> namedTargets = null, string folderPath = DefaultFolderPath)
        {
            List<SCFTransformSnapshotTarget> targetList = new List<SCFTransformSnapshotTarget>();
            if (namedTargets != null)
            {
                targetList.AddRange(namedTargets);
            }

            string content = BuildHierarchySnapshotText(snapshotName, root, targetList);
            return WriteSnapshot(snapshotName, content, folderPath);
        }

        public static string CaptureVectors(string snapshotName, IEnumerable<SCFVectorSnapshotTarget> targets, string folderPath = DefaultFolderPath)
        {
            List<SCFVectorSnapshotTarget> targetList = new List<SCFVectorSnapshotTarget>();
            if (targets != null)
            {
                targetList.AddRange(targets);
            }

            string content = BuildVectorSnapshotText(snapshotName, targetList);
            return WriteSnapshot(snapshotName, content, folderPath);
        }

        private static string WriteSnapshot(string snapshotName, string content, string folderPath)
        {
            string safeName = SanitizeFileName(string.IsNullOrWhiteSpace(snapshotName) ? "SCF_TransformSnapshot" : snapshotName);
            string safeFolder = string.IsNullOrWhiteSpace(folderPath) ? DefaultFolderPath : folderPath;
            Directory.CreateDirectory(safeFolder);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
            string timestampedPath = Path.Combine(safeFolder, safeName + "_" + timestamp + ".txt");
            string latestPath = Path.Combine(safeFolder, safeName + "_LATEST.txt");
            File.WriteAllText(timestampedPath, content, Encoding.UTF8);
            File.WriteAllText(latestPath, content, Encoding.UTF8);

            GUIUtility.systemCopyBuffer = content;
            Debug.Log("SCF snapshot written:\n" + NormalizePath(timestampedPath) + "\n" + NormalizePath(latestPath));

#if UNITY_EDITOR
            AssetDatabase.ImportAsset(NormalizePath(timestampedPath));
            AssetDatabase.ImportAsset(NormalizePath(latestPath));
#endif

            return NormalizePath(timestampedPath);
        }

        private static string BuildTransformSnapshotText(string snapshotName, IReadOnlyList<SCFTransformSnapshotTarget> targets)
        {
            StringBuilder builder = new StringBuilder();
            AppendHeader(builder, snapshotName, "transform", targets.Count);
            for (int i = 0; i < targets.Count; i++)
            {
                SCFTransformSnapshotTarget entry = targets[i];
                Transform target = entry.Target;
                builder.AppendLine("[" + i.ToString("000", CultureInfo.InvariantCulture) + "] " + SafeLabel(entry.Label));
                if (target == null)
                {
                    builder.AppendLine("ownerPath = missing");
                    builder.AppendLine();
                    continue;
                }

                builder.AppendLine("ownerPath = " + GetHierarchyPath(target));
                builder.AppendLine("gameObject = " + target.gameObject.name);
                builder.AppendLine("parent = " + (target.parent != null ? GetHierarchyPath(target.parent) : "none"));
                builder.AppendLine("localPosition = " + FormatVector(target.localPosition));
                builder.AppendLine("localEulerAngles = " + FormatVector(NormalizeEuler(target.localEulerAngles)));
                builder.AppendLine("localScale = " + FormatVector(target.localScale));
                builder.AppendLine("worldPosition = " + FormatVector(target.position));
                builder.AppendLine("worldEulerAngles = " + FormatVector(NormalizeEuler(target.eulerAngles)));
                builder.AppendLine("lossyScale = " + FormatVector(target.lossyScale));
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string BuildHierarchySnapshotText(string snapshotName, Transform root, IReadOnlyList<SCFTransformSnapshotTarget> namedTargets)
        {
            Transform[] hierarchy = root != null ? root.GetComponentsInChildren<Transform>(true) : new Transform[0];
            StringBuilder builder = new StringBuilder();
            AppendHeader(builder, snapshotName, "hierarchy", hierarchy.Length);
            builder.AppendLine("rootPath = " + GetHierarchyPath(root));
            builder.AppendLine("namedTargetCount = " + namedTargets.Count.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine();
            builder.AppendLine("NAMED TARGETS");
            for (int i = 0; i < namedTargets.Count; i++)
            {
                AppendTransformEntry(builder, i, namedTargets[i].Label, namedTargets[i].Target, root);
            }

            builder.AppendLine("FULL HIERARCHY");
            for (int i = 0; i < hierarchy.Length; i++)
            {
                AppendTransformEntry(builder, i, hierarchy[i].name, hierarchy[i], root);
            }

            return builder.ToString();
        }

        private static void AppendTransformEntry(StringBuilder builder, int index, string label, Transform target, Transform root)
        {
            builder.AppendLine("[" + index.ToString("000", CultureInfo.InvariantCulture) + "] " + SafeLabel(label));
            if (target == null)
            {
                builder.AppendLine("ownerPath = missing");
                builder.AppendLine();
                return;
            }

            builder.AppendLine("ownerPath = " + GetHierarchyPath(target));
            builder.AppendLine("relativePath = " + GetRelativePath(root, target));
            builder.AppendLine("gameObject = " + target.gameObject.name);
            builder.AppendLine("activeSelf = " + target.gameObject.activeSelf);
            builder.AppendLine("activeInHierarchy = " + target.gameObject.activeInHierarchy);
            builder.AppendLine("layer = " + target.gameObject.layer.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("tag = " + target.gameObject.tag);
            builder.AppendLine("parent = " + (target.parent != null ? GetHierarchyPath(target.parent) : "none"));
            builder.AppendLine("parentRelativePath = " + (target.parent != null ? GetRelativePath(root, target.parent) : "none"));
            builder.AppendLine("localPosition = " + FormatVector(target.localPosition));
            builder.AppendLine("localEulerAngles = " + FormatVector(NormalizeEuler(target.localEulerAngles)));
            builder.AppendLine("localScale = " + FormatVector(target.localScale));
            builder.AppendLine("worldPosition = " + FormatVector(target.position));
            builder.AppendLine("worldEulerAngles = " + FormatVector(NormalizeEuler(target.eulerAngles)));
            builder.AppendLine("lossyScale = " + FormatVector(target.lossyScale));
            builder.AppendLine();
        }

        private static string BuildVectorSnapshotText(string snapshotName, IReadOnlyList<SCFVectorSnapshotTarget> targets)
        {
            StringBuilder builder = new StringBuilder();
            AppendHeader(builder, snapshotName, "vector", targets.Count);
            for (int i = 0; i < targets.Count; i++)
            {
                SCFVectorSnapshotTarget entry = targets[i];
                builder.AppendLine("[" + i.ToString("000", CultureInfo.InvariantCulture) + "] " + SafeLabel(entry.Label));
                builder.AppendLine("ownerPath = " + (entry.Owner != null ? GetHierarchyPath(entry.Owner.transform) : "missing"));
                builder.AppendLine("gameObject = " + (entry.Owner != null ? entry.Owner.name : "missing"));
                builder.AppendLine("value = " + FormatVector(entry.Value));
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static void AppendHeader(StringBuilder builder, string snapshotName, string captureType, int count)
        {
            builder.AppendLine("SCF transform snapshot");
            builder.AppendLine("name = " + SafeLabel(snapshotName));
            builder.AppendLine("type = " + captureType);
            builder.AppendLine("count = " + count.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("capturedLocal = " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
            builder.AppendLine("capturedUtc = " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
            builder.AppendLine("unityPlayMode = " + Application.isPlaying);
            builder.AppendLine();
        }

        private static string GetHierarchyPath(Transform target)
        {
            if (target == null)
            {
                return "missing";
            }

            Stack<string> names = new Stack<string>();
            Transform current = target;
            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return "/" + string.Join("/", names.ToArray());
        }

        private static string GetRelativePath(Transform root, Transform target)
        {
            if (target == null)
            {
                return "missing";
            }

            if (root == null)
            {
                return GetHierarchyPath(target);
            }

            if (target == root)
            {
                return ".";
            }

            Stack<string> names = new Stack<string>();
            Transform current = target;
            while (current != null && current != root)
            {
                names.Push(current.name);
                current = current.parent;
            }

            if (current != root)
            {
                return GetHierarchyPath(target);
            }

            return string.Join("/", names.ToArray());
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
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string SanitizeFileName(string value)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            StringBuilder builder = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                builder.Append(Array.IndexOf(invalid, c) >= 0 || char.IsWhiteSpace(c) ? '_' : c);
            }

            return builder.ToString();
        }

        private static string SafeLabel(string label)
        {
            return string.IsNullOrWhiteSpace(label) ? "unnamed" : label;
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}
