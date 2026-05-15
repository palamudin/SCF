using MxM;
using UnityEngine;

namespace SCF.Gameplay
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-35)]
    public sealed class SCFMxMCombatDriver : MonoBehaviour
    {
        [SerializeField] private IsometricCharacterMotor motor;
        [SerializeField] private MxMAnimator mxmAnimator;

        [Header("MxM Events")]
        [SerializeField] private MxMEventDefinition combatRollEvent;
        [SerializeField] private MxMEventDefinition jumpEvent;
        [SerializeField] private MxMEventDefinition landEvent;
        [SerializeField] private MxMEventDefinition wallRunEvent;
        [SerializeField] private string combatRollEventName = "CombatRoll";
        [SerializeField] private string jumpEventName = "Jump";
        [SerializeField] private string landEventName = "Land";
        [SerializeField] private string wallRunEventName = "WallRun";
        [SerializeField] private int eventPriority = 1;
        [SerializeField] private bool exitEventsWithMotion = true;

        [Header("MxM Required Tags")]
        [SerializeField] private bool syncRequiredTags = true;
        [SerializeField] private string jumpingRequiredTag = "Jumping";
        [SerializeField] private string combatRollRequiredTag = "CombatRoll";
        [SerializeField] private string wallRunRequiredTag = "WallRun";

        private int observedCombatRollSequence;
        private int observedJumpSequence;
        private int observedLandSequence;
        private int observedWallRunSequence;
        private ETags lastRequiredTags = ETags.DoNotUse;

        private void Reset()
        {
            motor = GetComponent<IsometricCharacterMotor>();
            mxmAnimator = GetComponent<MxMAnimator>();
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            ResolveReferences();
            if (motor == null || mxmAnimator == null || mxmAnimator.CurrentAnimData == null)
            {
                return;
            }

            SyncTags();
            SyncEvents();
        }

        public void Configure(IsometricCharacterMotor characterMotor, MxMAnimator animator)
        {
            motor = characterMotor;
            mxmAnimator = animator;
        }

        private void SyncEvents()
        {
            if (motor.CombatRollSequence != observedCombatRollSequence)
            {
                observedCombatRollSequence = motor.CombatRollSequence;
                BeginEvent(combatRollEvent, combatRollEventName);
            }

            if (motor.JumpSequence != observedJumpSequence)
            {
                observedJumpSequence = motor.JumpSequence;
                BeginEvent(jumpEvent, jumpEventName);
            }

            if (motor.LandSequence != observedLandSequence)
            {
                observedLandSequence = motor.LandSequence;
                BeginEvent(landEvent, landEventName);
            }

            if (motor.WallRunSequence != observedWallRunSequence)
            {
                observedWallRunSequence = motor.WallRunSequence;
                BeginEvent(wallRunEvent, wallRunEventName);
            }

            // Vault/climb/prone are parked while the core movement set is being tuned.
        }

        private void SyncTags()
        {
            if (!syncRequiredTags)
            {
                return;
            }

            ETags desiredTags = ResolveRequiredTags();
            if (desiredTags == lastRequiredTags)
            {
                return;
            }

            mxmAnimator.SetRequiredTags(desiredTags);
            lastRequiredTags = desiredTags;
        }

        private ETags ResolveRequiredTags()
        {
            switch (motor.MobilityState)
            {
                case CharacterMobilityState.Jumping:
                    return FindTag(jumpingRequiredTag);

                case CharacterMobilityState.CombatRoll:
                    return FindTag(combatRollRequiredTag);

                case CharacterMobilityState.WallRun:
                    return FindTag(wallRunRequiredTag);

                default:
                    return ETags.None;
            }
        }

        private ETags FindTag(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName) || mxmAnimator.CurrentAnimData == null)
            {
                return ETags.None;
            }

            string[] tagNames = mxmAnimator.CurrentAnimData.TagNames;
            if (tagNames == null)
            {
                return ETags.None;
            }

            for (int i = 0; i < tagNames.Length; i++)
            {
                if (tagNames[i] == tagName)
                {
                    return (ETags)(1 << i);
                }
            }

            return ETags.None;
        }

        private void BeginEvent(MxMEventDefinition eventDefinition, string fallbackEventName)
        {
            if (eventDefinition != null)
            {
                mxmAnimator.BeginEvent(eventDefinition);
                return;
            }

            int eventId = FindEventId(fallbackEventName);
            if (eventId >= 0)
            {
                mxmAnimator.BeginEvent(eventId, eventPriority, exitEventsWithMotion);
            }
        }

        private int FindEventId(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName) || mxmAnimator.CurrentAnimData == null)
            {
                return -1;
            }

            string[] eventNames = mxmAnimator.CurrentAnimData.EventNames;
            if (eventNames == null)
            {
                return -1;
            }

            for (int i = 0; i < eventNames.Length; i++)
            {
                if (eventNames[i] == eventName)
                {
                    return i;
                }
            }

            return -1;
        }

        private void ResolveReferences()
        {
            if (motor == null)
            {
                motor = GetComponent<IsometricCharacterMotor>();
            }

            if (mxmAnimator == null)
            {
                mxmAnimator = GetComponent<MxMAnimator>();
            }
        }
    }
}
