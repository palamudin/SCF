using UnityEngine;

namespace SCF.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SCFAnimationEventSink : MonoBehaviour
    {
        public void OnFootstep()
        {
        }

        public void OnFootstep(AnimationEvent animationEvent)
        {
        }

        public void OnLand()
        {
        }

        public void OnLand(AnimationEvent animationEvent)
        {
        }

        public void StopClimbingUp()
        {
        }
    }
}
