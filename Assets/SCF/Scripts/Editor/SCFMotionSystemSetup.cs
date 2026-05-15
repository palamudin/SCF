#if UNITY_EDITOR
using System;
using SCF.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SCF.EditorTools
{
    public static class SCFMotionSystemSetup
    {
        [MenuItem("SCF/Motion/Attach Motion System To Player")]
        public static void AttachMotionSystemToPlayer()
        {
            GameObject player = GameObject.Find("SCF_Player");
            if (player == null)
            {
                Debug.LogWarning("SCF motion system setup could not find SCF_Player in the active scene.");
                return;
            }

            SCFMotionDatabase database = SCFMotionDatabaseBaker.BuildBaseDatabase();
            SCFMotionDatabase humanoidDatabase = SCFMotionDatabaseBaker.BuildHumanoidDatabase();
            DisableLegacyMotionComponents(player);
            SCFMotionSelector selector = EnsureComponent<SCFMotionSelector>(player);
            Animator animator = ResolveAnimator(player);
            selector.Configure(animator, ResolveDatabaseForAnimator(animator, database, humanoidDatabase));

            SCFCharacterVisualSlot visualSlot = player.GetComponent<SCFCharacterVisualSlot>();
            if (visualSlot != null)
            {
                visualSlot.Configure(animator != null ? animator.runtimeAnimatorController : null, database, humanoidDatabase);
                EditorUtility.SetDirty(visualSlot);
            }

            MovementAnimatorBridge bridge = player.GetComponent<MovementAnimatorBridge>();
            if (bridge != null)
            {
                bridge.Configure(animator);
                EditorUtility.SetDirty(bridge);
            }

            MotionMatchingSignalHub signalHub = player.GetComponent<MotionMatchingSignalHub>();
            if (signalHub != null)
            {
                signalHub.Configure(animator);
                EditorUtility.SetDirty(signalHub);
            }

            EditorUtility.SetDirty(selector);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = player;
            Debug.Log("SCF owned motion system attached to SCF_Player with " + database.Count + " baked motion clips.");
        }

        private static SCFMotionDatabase ResolveDatabaseForAnimator(Animator animator, SCFMotionDatabase genericDatabase, SCFMotionDatabase humanoidDatabase)
        {
            if (animator != null
                && animator.avatar != null
                && animator.avatar.isHuman
                && humanoidDatabase != null)
            {
                return humanoidDatabase;
            }

            return genericDatabase;
        }

        public static void DisableLegacyMotionComponents(GameObject player)
        {
            if (player == null)
            {
                return;
            }

            MonoBehaviour[] behaviours = player.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || !IsLegacyMotionComponent(behaviour))
                {
                    continue;
                }

                SerializedObject serializedObject = new SerializedObject(behaviour);
                SerializedProperty enabledProperty = serializedObject.FindProperty("m_Enabled");
                if (enabledProperty != null && enabledProperty.boolValue)
                {
                    Undo.RecordObject(behaviour, "Disable legacy motion component");
                    enabledProperty.boolValue = false;
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(behaviour);
                }
            }
        }

        private static bool IsLegacyMotionComponent(MonoBehaviour behaviour)
        {
            Type type = behaviour.GetType();
            string typeName = type.Name;
            string typeNamespace = type.Namespace ?? string.Empty;
            return typeName.StartsWith("SCFMxM", StringComparison.Ordinal)
                || typeNamespace == "MxM"
                || typeNamespace.StartsWith("MxM.", StringComparison.Ordinal);
        }

        private static Animator ResolveAnimator(GameObject player)
        {
            SCFCharacterVisualSlot visualSlot = player.GetComponent<SCFCharacterVisualSlot>();
            if (visualSlot != null && visualSlot.ActiveAnimator != null)
            {
                return visualSlot.ActiveAnimator;
            }

            return player.GetComponentInChildren<Animator>(true);
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            return Undo.AddComponent<T>(gameObject);
        }
    }
}
#endif
