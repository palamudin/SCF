using UnityEngine;

namespace SCF.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SCFAnimationEventSink : MonoBehaviour
    {
        [SerializeField] private AudioSource footstepAudioSource;
        [SerializeField] private AudioClip[] soldierFootstepClips;
        [SerializeField] private AudioClip[] frankFootstepClips;
        [SerializeField, Range(0f, 1f)] private float soldierFootstepVolume = 0.52f;
        [SerializeField, Range(0f, 1f)] private float soldierLoadVolumeBoost = 0.35f;
        [SerializeField, Range(0f, 1f)] private float frankFootstepVolume = 0.18f;
        [SerializeField, Range(0f, 1f)] private float frankLoadVolumeBoost;
        [SerializeField, Range(0f, 1f)] private float eventVolumeScale = 1f;
        [SerializeField, Range(0f, 1f)] private float spatialBlend = 0.86f;
        [SerializeField] private Vector2 pitchRange = new Vector2(0.94f, 1.06f);
        [SerializeField, Min(0f)] private float minFootstepInterval = 0.08f;
        [SerializeField] private bool enableProceduralFootstepFallback;
        [SerializeField, Min(0.05f)] private float proceduralWalkStepInterval = 0.52f;
        [SerializeField, Min(0.05f)] private float proceduralRunStepInterval = 0.32f;
        [SerializeField, Min(0f)] private float proceduralMinPlanarSpeed = 0.25f;

        private IsometricCharacterMotor motor;
        private string characterName;
        private float lastFootstepTime = -100f;
        private float proceduralStepTimer;

        public void Configure(
            IsometricCharacterMotor characterMotor,
            string activeCharacterName,
            bool proceduralFallback,
            AudioClip[] soldierClips,
            AudioClip[] frankClips,
            float soldierVolume,
            float soldierLoadBoost,
            float frankVolume,
            float frankLoadBoost)
        {
            motor = characterMotor;
            characterName = activeCharacterName ?? string.Empty;
            enableProceduralFootstepFallback = proceduralFallback;

            if (soldierClips != null && soldierClips.Length > 0)
            {
                soldierFootstepClips = soldierClips;
            }

            if (frankClips != null && frankClips.Length > 0)
            {
                frankFootstepClips = frankClips;
            }

            soldierFootstepVolume = Mathf.Clamp01(soldierVolume);
            soldierLoadVolumeBoost = Mathf.Clamp01(soldierLoadBoost);
            frankFootstepVolume = Mathf.Clamp01(frankVolume);
            frankLoadVolumeBoost = Mathf.Clamp01(frankLoadBoost);
            if (footstepAudioSource != null)
            {
                ConfigureAudioSource(footstepAudioSource);
            }
        }

        private void Update()
        {
            if (!enableProceduralFootstepFallback || motor == null || !IsFrankProfile())
            {
                return;
            }

            if (!motor.IsGrounded
                || motor.IsAirborne
                || motor.IsObstacleTraversing
                || motor.IsWallRunning
                || motor.IsCombatRolling)
            {
                proceduralStepTimer = 0f;
                return;
            }

            float speed = motor.PlanarVelocity.magnitude;
            if (speed < proceduralMinPlanarSpeed)
            {
                proceduralStepTimer = 0f;
                return;
            }

            float interval = motor.WalkHeld ? proceduralWalkStepInterval : proceduralRunStepInterval;
            proceduralStepTimer += Time.deltaTime;
            if (proceduralStepTimer < interval)
            {
                return;
            }

            proceduralStepTimer = 0f;
            PlayFootstep(1f);
        }

        public void OnFootstep()
        {
            PlayFootstep(1f);
        }

        public void OnFootstep(AnimationEvent animationEvent)
        {
            float volumeScale = animationEvent != null && animationEvent.floatParameter > 0f
                ? animationEvent.floatParameter
                : 1f;
            PlayFootstep(volumeScale);
        }

        public void PlayFootstepSound()
        {
            PlayFootstep(1f);
        }

        public void PlayFootstepSound(AnimationEvent animationEvent)
        {
            OnFootstep(animationEvent);
        }

        public void Footstep()
        {
            PlayFootstep(1f);
        }

        public void Footstep(AnimationEvent animationEvent)
        {
            OnFootstep(animationEvent);
        }

        public void PlayFootstep()
        {
            PlayFootstep(1f);
        }

        public void PlayFootstep(AnimationEvent animationEvent)
        {
            OnFootstep(animationEvent);
        }

        public void LeftFootstep()
        {
            PlayFootstep(1f);
        }

        public void LeftFootstep(AnimationEvent animationEvent)
        {
            OnFootstep(animationEvent);
        }

        public void RightFootstep()
        {
            PlayFootstep(1f);
        }

        public void RightFootstep(AnimationEvent animationEvent)
        {
            OnFootstep(animationEvent);
        }

        public void OnLand()
        {
        }

        public void OnLand(AnimationEvent animationEvent)
        {
        }

        public void StartJumping()
        {
        }

        public void StartJumping(AnimationEvent animationEvent)
        {
        }

        public void StopClimbingUp()
        {
        }

        public void EnableController()
        {
        }

        public void DisableController()
        {
        }

        public void OnAttack()
        {
        }

        private void PlayFootstep(float volumeScale)
        {
            if (Time.time - lastFootstepTime < minFootstepInterval)
            {
                return;
            }

            AudioClip[] clips = ResolveFootstepClips();
            if (clips == null || clips.Length == 0)
            {
                return;
            }

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip == null)
            {
                return;
            }

            float volume = ResolveFootstepVolume() * Mathf.Clamp01(volumeScale) * eventVolumeScale;
            if (volume <= 0.001f)
            {
                return;
            }

            AudioSource source = EnsureAudioSource();
            source.pitch = Random.Range(Mathf.Min(pitchRange.x, pitchRange.y), Mathf.Max(pitchRange.x, pitchRange.y));
            source.PlayOneShot(clip, volume);
            lastFootstepTime = Time.time;
        }

        private AudioClip[] ResolveFootstepClips()
        {
            if (IsFrankProfile())
            {
                return frankFootstepClips != null && frankFootstepClips.Length > 0
                    ? frankFootstepClips
                    : soldierFootstepClips;
            }

            return soldierFootstepClips;
        }

        private float ResolveFootstepVolume()
        {
            float load01 = motor != null ? motor.CarriedLoad01 : 0f;
            if (IsFrankProfile())
            {
                return Mathf.Clamp01(frankFootstepVolume + frankLoadVolumeBoost * load01);
            }

            return Mathf.Clamp01(soldierFootstepVolume + soldierLoadVolumeBoost * load01);
        }

        private bool IsFrankProfile()
        {
            if (!string.IsNullOrWhiteSpace(characterName))
            {
                return characterName.IndexOf("frank", System.StringComparison.OrdinalIgnoreCase) >= 0
                       || characterName.IndexOf("parkour", System.StringComparison.OrdinalIgnoreCase) >= 0;
            }

            return motor != null && motor.TraversalProfile == SCFTraversalProfile.Parkour;
        }

        private AudioSource EnsureAudioSource()
        {
            if (footstepAudioSource == null)
            {
                footstepAudioSource = GetComponent<AudioSource>();
            }

            if (footstepAudioSource == null)
            {
                footstepAudioSource = gameObject.AddComponent<AudioSource>();
            }

            ConfigureAudioSource(footstepAudioSource);
            return footstepAudioSource;
        }

        private void ConfigureAudioSource(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.enabled = true;
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = spatialBlend;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1f;
            source.maxDistance = 18f;
        }
    }
}
