using UnityEngine;

namespace SCF.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SCFDeathEcho : MonoBehaviour
    {
        private const float MinimumLifetime = 0.25f;

        private Renderer[] renderers;
        private ParticleSystem evaporationParticles;
        private float dieAt;
        private float fadeDuration = 8f;
        private float disturbedLifetime = 6f;
        private bool disturbed;
        private bool evaporating;

        public static SCFDeathEcho Spawn(GameObject sourceVisual, Transform fallbackRoot, float lifetime, float disturbedSeconds, float fadeSeconds, float deathPoseSeconds)
        {
            Vector3 position = fallbackRoot != null ? fallbackRoot.position : Vector3.zero;
            Quaternion rotation = fallbackRoot != null ? fallbackRoot.rotation : Quaternion.identity;
            if (sourceVisual != null)
            {
                position = sourceVisual.transform.position;
                rotation = sourceVisual.transform.rotation;
            }

            GameObject root = new GameObject("SCF_DeathEcho");
            root.transform.SetPositionAndRotation(position, rotation);

            GameObject visual = null;
            if (sourceVisual != null)
            {
                visual = Instantiate(sourceVisual, root.transform, true);
                visual.name = "SCF_DeathEchoVisual";
                StripLiveComponents(visual);
            }
            else
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visual.name = "SCF_DeathEchoProxy";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = Vector3.up * 0.9f;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
                Collider proxyCollider = visual.GetComponent<Collider>();
                if (proxyCollider != null)
                {
                    Destroy(proxyCollider);
                }
            }

            SCFNetworkDeathCue cue = visual.GetComponent<SCFNetworkDeathCue>();
            if (cue == null)
            {
                cue = visual.AddComponent<SCFNetworkDeathCue>();
            }

            cue.Play(deathPoseSeconds);

            SCFDeathEcho echo = root.AddComponent<SCFDeathEcho>();
            echo.Initialize(visual, lifetime, disturbedSeconds, fadeSeconds);
            return echo;
        }

        public void NotifyWeaponHit()
        {
            Disturb();
        }

        private static void StripLiveComponents(GameObject visual)
        {
            MonoBehaviour[] behaviours = visual.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || behaviour is SCFDeathEcho || behaviour is SCFNetworkDeathCue)
                {
                    continue;
                }

                behaviour.enabled = false;
            }

            Animator[] animators = visual.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                if (animators[i] != null)
                {
                    animators[i].enabled = false;
                }
            }

            Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    Destroy(colliders[i]);
                }
            }

            Rigidbody[] rigidbodies = visual.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                if (rigidbodies[i] != null)
                {
                    Destroy(rigidbodies[i]);
                }
            }
        }

        private void Initialize(GameObject visual, float lifetime, float disturbedSeconds, float fadeSeconds)
        {
            disturbedLifetime = Mathf.Max(MinimumLifetime, disturbedSeconds);
            fadeDuration = Mathf.Max(0.1f, fadeSeconds);
            dieAt = Time.time + Mathf.Max(MinimumLifetime, lifetime);
            renderers = visual != null ? visual.GetComponentsInChildren<Renderer>(true) : System.Array.Empty<Renderer>();

            Bounds bounds = ResolveBounds(visual);
            EnsureHitVolume(bounds);
            CreateEvaporationParticles(bounds);
        }

        private void Update()
        {
            float remaining = dieAt - Time.time;
            if (remaining <= fadeDuration)
            {
                float fade01 = Mathf.Clamp01(1f - remaining / Mathf.Max(0.001f, fadeDuration));
                TickEvaporation(fade01);
            }

            if (remaining > 0f)
            {
                return;
            }

            BeginFinalEvaporation();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || other.transform == transform || other.transform.IsChildOf(transform))
            {
                return;
            }

            if (other.GetComponentInParent<SCFWeaponVisualSlot>() != null
                || other.GetComponentInParent<IsometricCharacterMotor>() != null
                || other.GetComponentInParent<SCFNetworkHitbox>() != null
                || LooksLikeWeapon(other.transform))
            {
                Disturb();
            }
        }

        private static bool LooksLikeWeapon(Transform target)
        {
            if (target == null)
            {
                return false;
            }

            string name = target.name;
            return name.IndexOf("weapon", System.StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("railgun", System.StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("gun", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void Disturb()
        {
            disturbed = true;
            dieAt = Mathf.Min(dieAt, Time.time + disturbedLifetime);
            if (evaporationParticles != null)
            {
                evaporationParticles.Emit(96);
            }
        }

        private Bounds ResolveBounds(GameObject visual)
        {
            bool hasBounds = false;
            Bounds bounds = new Bounds(transform.position + Vector3.up, Vector3.one);
            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null)
                    {
                        continue;
                    }

                    if (!hasBounds)
                    {
                        bounds = renderer.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }
            }

            if (!hasBounds && visual != null)
            {
                bounds = new Bounds(visual.transform.position + Vector3.up * 0.9f, new Vector3(0.8f, 1.8f, 0.8f));
            }

            return bounds;
        }

        private void EnsureHitVolume(Bounds bounds)
        {
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = transform.InverseTransformPoint(bounds.center);
            collider.size = new Vector3(
                Mathf.Max(0.35f, bounds.size.x),
                Mathf.Max(0.65f, bounds.size.y),
                Mathf.Max(0.35f, bounds.size.z));

            Rigidbody body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        private void CreateEvaporationParticles(Bounds bounds)
        {
            GameObject particleObject = new GameObject("SCF_DeathEchoParticles");
            particleObject.transform.SetParent(transform, false);
            particleObject.transform.position = bounds.center;

            evaporationParticles = particleObject.AddComponent<ParticleSystem>();
            evaporationParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = evaporationParticles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 4.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.42f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.018f, 0.075f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.52f, 0.95f, 1f, 0.48f), new Color(1f, 1f, 1f, 0.18f));
            main.maxParticles = 2600;

            ParticleSystem.EmissionModule emission = evaporationParticles.emission;
            emission.enabled = true;
            emission.rateOverTime = 7f;

            ParticleSystem.ShapeModule shape = evaporationParticles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(
                Mathf.Max(0.2f, bounds.size.x),
                Mathf.Max(0.4f, bounds.size.y),
                Mathf.Max(0.2f, bounds.size.z));

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = evaporationParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.4f, 0.95f, 1f), 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.55f, 0.2f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = gradient;

            ParticleSystemRenderer renderer = evaporationParticles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = CreateParticleMaterial();

            evaporationParticles.Play();
        }

        private void TickEvaporation(float fade01)
        {
            if (evaporationParticles != null)
            {
                ParticleSystem.EmissionModule emission = evaporationParticles.emission;
                emission.rateOverTime = Mathf.Lerp(disturbed ? 45f : 15f, 130f, fade01);
            }

            if (renderers == null)
            {
                return;
            }

            if (fade01 < 0.92f)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = false;
                }
            }
        }

        private void BeginFinalEvaporation()
        {
            if (evaporating)
            {
                return;
            }

            evaporating = true;
            if (evaporationParticles != null)
            {
                evaporationParticles.Emit(180);
                evaporationParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            Destroy(gameObject, 5f);
        }

        private static Material CreateParticleMaterial()
        {
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            Material material = new Material(shader != null ? shader : Shader.Find("Unlit/Color"));
            material.color = new Color(0.48f, 0.92f, 1f, 0.55f);
            return material;
        }
    }
}
