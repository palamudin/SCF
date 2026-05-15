using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SCF.Gameplay
{
    [Serializable]
    public sealed class SCFCharacterCandidate
    {
        [SerializeField] private string displayName;
        [SerializeField] private GameObject prefab;
        [SerializeField] private Vector3 localPosition;
        [SerializeField] private Vector3 localEulerAngles;
        [SerializeField] private Vector3 localScale = Vector3.one;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) && prefab != null ? prefab.name : displayName;
        public GameObject Prefab => prefab;
        public Vector3 LocalPosition => localPosition;
        public Vector3 LocalEulerAngles => localEulerAngles;
        public Vector3 LocalScale => localScale == Vector3.zero ? Vector3.one : localScale;

        public SCFCharacterCandidate()
        {
            localScale = Vector3.one;
        }

        public SCFCharacterCandidate(string displayName, GameObject prefab)
        {
            this.displayName = displayName;
            this.prefab = prefab;
            localScale = Vector3.one;
        }

        public SCFCharacterCandidate(string displayName, GameObject prefab, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
        {
            this.displayName = displayName;
            this.prefab = prefab;
            this.localPosition = localPosition;
            this.localEulerAngles = localEulerAngles;
            this.localScale = localScale == Vector3.zero ? Vector3.one : localScale;
        }
    }

    [DisallowMultipleComponent]
    public sealed class SCFCharacterVisualSlot : MonoBehaviour
    {
        [SerializeField] private string visualRootName = "SCF_CharacterVisual";
        [SerializeField] private string visualFitOffsetName = "SCF_VisualFitOffset";
        [SerializeField] private RuntimeAnimatorController animatorController;
        [SerializeField] private SCFMotionDatabase genericMotionDatabase;
        [SerializeField] private SCFMotionDatabase humanoidMotionDatabase;
        [SerializeField, Min(0.1f)] private float targetVisualHeight = 1.82f;
        [SerializeField] private bool fitVisualToController;
        [SerializeField] private bool disablePrefabBehaviours = true;
        [SerializeField] private bool hideCapsuleRenderers = true;
        [SerializeField] private Animator activeAnimator;
        [SerializeField] private GameObject activeVisual;
        [SerializeField] private string activeCharacterName;
        [SerializeField] private Vector3 activeVisualLocalPosition;
        [SerializeField] private Vector3 activeVisualLocalEulerAngles;
        [SerializeField] private Vector3 activeVisualLocalScale = Vector3.one;

        public Animator ActiveAnimator => activeAnimator;
        public GameObject ActiveVisual => activeVisual;
        public string ActiveCharacterName => activeCharacterName;

        private Transform visualRoot;
        private Transform visualFitOffset;

        private void Awake()
        {
            EnsureVisualRoot();
            if (activeVisual == null)
            {
                activeVisual = ResolveExistingActiveVisual();
            }

            if (activeVisual != null)
            {
                EnsureVisualParentedToFitOffset(activeVisual.transform);
            }

            NormalizeActiveVisual();

            if (activeAnimator == null)
            {
                activeAnimator = GetComponentInChildren<Animator>(true);
            }

            if (activeAnimator != null)
            {
                ConfigureAnimator(activeAnimator);
            }
        }

        public void Configure(RuntimeAnimatorController controller)
        {
            animatorController = controller;
            if (activeAnimator != null && animatorController != null)
            {
                activeAnimator.runtimeAnimatorController = animatorController;
            }

            NormalizeActiveVisual();
            RebindMovementStack();
        }

        public void Configure(RuntimeAnimatorController controller, SCFMotionDatabase genericDatabase, SCFMotionDatabase humanoidDatabase)
        {
            genericMotionDatabase = genericDatabase;
            humanoidMotionDatabase = humanoidDatabase;
            Configure(controller);
        }

        public void ApplyCandidate(SCFCharacterCandidate candidate)
        {
            if (candidate == null || candidate.Prefab == null)
            {
                return;
            }

            ApplyCharacter(candidate.Prefab, candidate.DisplayName, candidate.LocalPosition, candidate.LocalEulerAngles, candidate.LocalScale);
        }

        public void ApplyCharacter(GameObject prefab, string displayName = null)
        {
            ApplyCharacter(prefab, displayName, Vector3.zero, Vector3.zero, Vector3.one);
        }

        public void ClearCharacter()
        {
            EnsureVisualRoot();
            ClearVisualRoot();
            activeVisual = null;
            activeAnimator = null;
            activeCharacterName = string.Empty;
            activeVisualLocalPosition = Vector3.zero;
            activeVisualLocalEulerAngles = Vector3.zero;
            activeVisualLocalScale = Vector3.one;
            ResetVisualSocketTransforms();
            SetPrototypeCapsulesVisible(!hideCapsuleRenderers);
            RebindMovementStack();
        }

        public void ApplyCharacter(GameObject prefab, string displayName, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
        {
            if (prefab == null)
            {
                return;
            }

            EnsureVisualRoot();
            ClearVisualRoot();

            GameObject visualSource = ResolveVisualSource(prefab);
            activeVisual = Instantiate(visualSource, visualFitOffset);
            activeVisual.name = "SCF_Selected_" + prefab.name;
            activeVisualLocalPosition = localPosition;
            activeVisualLocalEulerAngles = localEulerAngles;
            activeVisualLocalScale = MultiplyScale(SanitizeScale(activeVisual.transform.localScale), SanitizeScale(localScale));
            ResetSelectedVisualTransform(activeVisual.transform, activeVisualLocalPosition, activeVisualLocalEulerAngles, activeVisualLocalScale);
            activeCharacterName = string.IsNullOrWhiteSpace(displayName) ? prefab.name : displayName;

#if UNITY_EDITOR
            RemoveMissingScripts(activeVisual);
#endif
            StripUnwantedVisualPieces(activeVisual);
            StripGameplayFromVisual(activeVisual);
            activeAnimator = ResolveAnimator(activeVisual);
            ConfigureAnimator(activeAnimator);

            ResetVisualSocketTransforms();

            SetPrototypeCapsulesVisible(!hideCapsuleRenderers);
            RebindMovementStack();
        }

        private void NormalizeActiveVisual()
        {
            if (activeVisual == null)
            {
                return;
            }

            EnsureVisualRoot();
            EnsureVisualParentedToFitOffset(activeVisual.transform);
            CaptureActiveVisualTransformIfUnset();

#if UNITY_EDITOR
            RemoveMissingScripts(activeVisual);
#endif
            StripUnwantedVisualPieces(activeVisual);
            ResetVisualSocketTransforms();
        }

        private GameObject ResolveVisualSource(GameObject prefab)
        {
            Animator[] animators = prefab.GetComponentsInChildren<Animator>(true);
            Animator bestAnimator = null;
            int bestScore = int.MinValue;
            for (int i = 0; i < animators.Length; i++)
            {
                Animator candidate = animators[i];
                if (candidate == null)
                {
                    continue;
                }

                int rendererCount = candidate.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length
                                    + candidate.GetComponentsInChildren<MeshRenderer>(true).Length;
                if (rendererCount == 0)
                {
                    continue;
                }

                int depth = ResolveDepth(prefab.transform, candidate.transform);
                int score = rendererCount * 100 + depth * 25;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestAnimator = candidate;
                }
            }

            if (bestAnimator != null)
            {
                return bestAnimator.gameObject;
            }

            return prefab;
        }

        private static int ResolveDepth(Transform root, Transform child)
        {
            int depth = 0;
            Transform current = child;
            while (current != null && current != root)
            {
                depth++;
                current = current.parent;
            }

            return depth;
        }

        private void EnsureVisualRoot()
        {
            if (visualRoot != null)
            {
                ResetVisualRootTransform();
                EnsureVisualFitOffset();
                return;
            }

            Transform existing = transform.Find(visualRootName);
            if (existing != null)
            {
                visualRoot = existing;
                ResetVisualRootTransform();
                EnsureVisualFitOffset();
                return;
            }

            GameObject root = new GameObject(visualRootName);
            root.transform.SetParent(transform, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            visualRoot = root.transform;
            EnsureVisualFitOffset();
        }

        private void ResetVisualRootTransform()
        {
            if (visualRoot == null)
            {
                return;
            }

            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one;
        }

        private void EnsureVisualFitOffset()
        {
            if (visualRoot == null)
            {
                return;
            }

            if (visualFitOffset != null && visualFitOffset.parent == visualRoot)
            {
                ResetVisualFitOffsetTransform();
                return;
            }

            Transform existing = visualRoot.Find(visualFitOffsetName);
            if (existing != null)
            {
                visualFitOffset = existing;
                ResetVisualFitOffsetTransform();
                return;
            }

            GameObject offset = new GameObject(visualFitOffsetName);
            offset.transform.SetParent(visualRoot, false);
            visualFitOffset = offset.transform;
            ResetVisualFitOffsetTransform();
        }

        private void ResetVisualFitOffsetTransform()
        {
            if (visualFitOffset == null)
            {
                return;
            }

            visualFitOffset.localPosition = Vector3.zero;
            visualFitOffset.localRotation = Quaternion.identity;
            visualFitOffset.localScale = Vector3.one;
        }

        private void ResetVisualSocketTransforms()
        {
            ResetVisualRootTransform();
            ResetVisualFitOffsetTransform();
            if (activeVisual != null)
            {
                CaptureActiveVisualTransformIfUnset();
                ResetSelectedVisualTransform(activeVisual.transform, activeVisualLocalPosition, activeVisualLocalEulerAngles, activeVisualLocalScale);
            }
        }

        private void CaptureActiveVisualTransformIfUnset()
        {
            if (activeVisual == null || activeVisualLocalScale != Vector3.zero)
            {
                return;
            }

            Transform visualTransform = activeVisual.transform;
            activeVisualLocalPosition = visualTransform.localPosition;
            activeVisualLocalEulerAngles = visualTransform.localEulerAngles;
            activeVisualLocalScale = SanitizeScale(visualTransform.localScale);
        }

        private static void ResetSelectedVisualTransform(Transform visual, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
        {
            if (visual == null)
            {
                return;
            }

            visual.localPosition = localPosition;
            visual.localRotation = Quaternion.Euler(localEulerAngles);
            visual.localScale = localScale == Vector3.zero ? Vector3.one : localScale;
        }

        private static Vector3 MultiplyScale(Vector3 left, Vector3 right)
        {
            return new Vector3(left.x * right.x, left.y * right.y, left.z * right.z);
        }

        private static Vector3 SanitizeScale(Vector3 scale)
        {
            return new Vector3(SanitizeScaleAxis(scale.x), SanitizeScaleAxis(scale.y), SanitizeScaleAxis(scale.z));
        }

        private static float SanitizeScaleAxis(float axis)
        {
            return Mathf.Approximately(axis, 0f) ? 1f : axis;
        }

        private GameObject ResolveExistingActiveVisual()
        {
            EnsureVisualFitOffset();
            if (visualFitOffset != null && visualFitOffset.childCount > 0)
            {
                return visualFitOffset.GetChild(0).gameObject;
            }

            if (visualRoot == null)
            {
                return null;
            }

            for (int i = 0; i < visualRoot.childCount; i++)
            {
                Transform child = visualRoot.GetChild(i);
                if (child != visualFitOffset)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private void EnsureVisualParentedToFitOffset(Transform visual)
        {
            if (visual == null)
            {
                return;
            }

            EnsureVisualFitOffset();
            if (visualFitOffset != null && visual.parent != visualFitOffset)
            {
                visual.SetParent(visualFitOffset, false);
            }
        }

        private void ClearVisualRoot()
        {
            EnsureVisualFitOffset();
            for (int i = visualRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = visualRoot.GetChild(i);
                if (child != visualFitOffset)
                {
                    DestroyVisualPiece(child.gameObject);
                }
            }

            if (visualFitOffset == null)
            {
                EnsureVisualFitOffset();
            }

            if (visualFitOffset == null)
            {
                return;
            }

            for (int i = visualFitOffset.childCount - 1; i >= 0; i--)
            {
                DestroyVisualPiece(visualFitOffset.GetChild(i).gameObject);
            }

            ResetVisualRootTransform();
            ResetVisualFitOffsetTransform();
        }

        private void StripGameplayFromVisual(GameObject visual)
        {
            Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            Rigidbody[] rigidbodies = visual.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                rigidbodies[i].isKinematic = true;
                rigidbodies[i].detectCollisions = false;
            }

            if (!disablePrefabBehaviours)
            {
                return;
            }

            Behaviour[] behaviours = visual.GetComponentsInChildren<Behaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                Behaviour behaviour = behaviours[i];
                if (behaviour == null || behaviour is Animator)
                {
                    continue;
                }

                behaviour.enabled = false;
            }
        }

        private void StripUnwantedVisualPieces(GameObject visual)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bodyBounds;
            if (!TryResolveBodyBounds(renderers, out bodyBounds))
            {
                return;
            }

            float bodyHeight = Mathf.Max(0.1f, bodyBounds.size.y);
            for (int i = renderers.Length - 1; i >= 0; i--)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || renderer is SkinnedMeshRenderer || ShouldKeepLowAccessory(renderer.name))
                {
                    continue;
                }

                string lowerName = renderer.name.ToLowerInvariant();
                bool namedBoard = lowerName.Contains("skate") || lowerName.Contains("board");
                Bounds bounds = renderer.bounds;
                bool belowAnkle = bounds.center.y < bodyBounds.min.y + bodyHeight * 0.2f;
                bool flatWidePiece = bounds.size.y < bodyHeight * 0.08f
                                     && Mathf.Max(bounds.size.x, bounds.size.z) > bodyHeight * 0.22f;

                if (namedBoard || (belowAnkle && flatWidePiece))
                {
                    DestroyVisualPiece(renderer.gameObject);
                }
            }
        }

        private static bool TryResolveBodyBounds(Renderer[] renderers, out Bounds bodyBounds)
        {
            bool initialized = false;
            bodyBounds = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !(renderer is SkinnedMeshRenderer))
                {
                    continue;
                }

                if (!initialized)
                {
                    bodyBounds = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    bodyBounds.Encapsulate(renderer.bounds);
                }
            }

            if (initialized)
            {
                return true;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (!initialized)
                {
                    bodyBounds = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    bodyBounds.Encapsulate(renderer.bounds);
                }
            }

            return initialized;
        }

        private static bool ShouldKeepLowAccessory(string rendererName)
        {
            if (string.IsNullOrWhiteSpace(rendererName))
            {
                return false;
            }

            string lowerName = rendererName.ToLowerInvariant();
            return lowerName.Contains("shoe")
                   || lowerName.Contains("sneaker")
                   || lowerName.Contains("sock")
                   || lowerName.Contains("foot")
                   || lowerName.Contains("toe");
        }

        private static void DestroyVisualPiece(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private Animator ResolveAnimator(GameObject visual)
        {
            Animator animator = visual.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                return animator;
            }

            return visual.AddComponent<Animator>();
        }

        private void ConfigureAnimator(Animator animator)
        {
            if (animator == null)
            {
                return;
            }

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            EnsureAnimationEventSink(gameObject);
            EnsureAnimationEventSink(visualRoot != null ? visualRoot.gameObject : null);
            EnsureAnimationEventSink(visualFitOffset != null ? visualFitOffset.gameObject : null);
            EnsureAnimationEventSink(activeVisual);
            EnsureAnimationEventSink(animator.gameObject);

            if (animatorController != null)
            {
                animator.runtimeAnimatorController = animatorController;
            }

            animator.Rebind();
            animator.Update(0f);
        }

        private static void EnsureAnimationEventSink(GameObject target)
        {
            if (target != null && target.GetComponent<SCFAnimationEventSink>() == null)
            {
                target.AddComponent<SCFAnimationEventSink>();
            }
        }

#if UNITY_EDITOR
        private static void RemoveMissingScripts(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(children[i].gameObject);
            }
        }
#endif

        private void FitVisualToCapsule(Transform visual)
        {
            EnsureVisualParentedToFitOffset(visual);
            ResetVisualSocketTransforms();
        }

        private float ResolveTargetHeight()
        {
            CharacterController controller = GetComponent<CharacterController>();
            if (controller != null)
            {
                return Mathf.Max(0.1f, controller.height * Mathf.Abs(transform.lossyScale.y) * 0.98f);
            }

            return targetVisualHeight;
        }

        private float ResolveTargetFootY()
        {
            CharacterController controller = GetComponent<CharacterController>();
            if (controller != null && controller.enabled)
            {
                return controller.bounds.min.y;
            }

            return transform.position.y;
        }

        private void SetPrototypeCapsulesVisible(bool visible)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || renderer is SkinnedMeshRenderer || renderer.transform.IsChildOf(visualRoot))
                {
                    continue;
                }

                if (renderer.name.IndexOf("prototype", StringComparison.OrdinalIgnoreCase) >= 0
                    || renderer.name.IndexOf("capsule", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    renderer.enabled = visible;
                }
            }
        }

        private void RebindMovementStack()
        {
            IsometricPlayerInput input = GetComponent<IsometricPlayerInput>();
            IsometricCharacterMotor motor = GetComponent<IsometricCharacterMotor>();
            Camera camera = Camera.main;
            Transform facingRoot = activeAnimator != null ? activeAnimator.transform : transform;

            if (motor != null)
            {
                motor.Configure(input, camera, facingRoot);
            }

            MovementAnimatorBridge bridge = GetComponent<MovementAnimatorBridge>();
            if (bridge != null)
            {
                bridge.Configure(activeAnimator);
            }

            MotionMatchingSignalHub signalHub = GetComponent<MotionMatchingSignalHub>();
            if (signalHub != null)
            {
                signalHub.Configure(activeAnimator);
            }

            SCFMotionSelector motionSelector = GetComponent<SCFMotionSelector>();
            if (motionSelector != null)
            {
                SCFMotionDatabase selectedDatabase = ResolveMotionDatabase(activeAnimator);
                if (selectedDatabase != null)
                {
                    motionSelector.Configure(activeAnimator, selectedDatabase);
                }
                else
                {
                    motionSelector.Configure(activeAnimator);
                }
            }
        }

        private SCFMotionDatabase ResolveMotionDatabase(Animator animator)
        {
            if (genericMotionDatabase != null
                && string.Equals(activeCharacterName, "Parkour Frank", StringComparison.OrdinalIgnoreCase))
            {
                return genericMotionDatabase;
            }

            if (animator != null
                && animator.avatar != null
                && animator.avatar.isHuman
                && humanoidMotionDatabase != null)
            {
                return humanoidMotionDatabase;
            }

            if (genericMotionDatabase != null)
            {
                return genericMotionDatabase;
            }

            return humanoidMotionDatabase;
        }
    }
}
