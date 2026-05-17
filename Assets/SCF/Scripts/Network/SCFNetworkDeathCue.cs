using UnityEngine;

namespace SCF.Gameplay
{
    public sealed class SCFNetworkDeathCue : MonoBehaviour
    {
        private Vector3 originalLocalPosition;
        private Quaternion originalLocalRotation;
        private Quaternion targetLocalRotation;
        private Vector3 targetLocalPosition;
        private float startedAt;
        private float duration = 0.45f;
        private bool captured;
        private bool playing;

        public void Play(float seconds)
        {
            CaptureOriginalPose();
            duration = Mathf.Max(0.05f, seconds);
            startedAt = Time.unscaledTime;
            targetLocalRotation = originalLocalRotation * Quaternion.Euler(74f, -8f, -28f);
            targetLocalPosition = originalLocalPosition + Vector3.down * 0.25f;
            playing = true;
            enabled = true;
        }

        public void ResetPose()
        {
            if (!captured)
            {
                return;
            }

            transform.localPosition = originalLocalPosition;
            transform.localRotation = originalLocalRotation;
            playing = false;
            enabled = false;
        }

        private void Awake()
        {
            CaptureOriginalPose();
            enabled = false;
        }

        private void Update()
        {
            if (!playing)
            {
                return;
            }

            float t = Mathf.Clamp01((Time.unscaledTime - startedAt) / duration);
            t = t * t * (3f - 2f * t);
            transform.localRotation = Quaternion.Slerp(originalLocalRotation, targetLocalRotation, t);
            transform.localPosition = Vector3.Lerp(originalLocalPosition, targetLocalPosition, t);
            if (t >= 1f)
            {
                playing = false;
            }
        }

        private void CaptureOriginalPose()
        {
            if (captured)
            {
                return;
            }

            originalLocalPosition = transform.localPosition;
            originalLocalRotation = transform.localRotation;
            captured = true;
        }
    }
}
