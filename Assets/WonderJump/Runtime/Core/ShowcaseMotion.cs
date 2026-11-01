using UnityEngine;

namespace WonderJump.Core
{
    /// <summary>Gentle spin + bob for the Phase 0 asset showcase (and later for idle collectibles).</summary>
    public sealed class ShowcaseMotion : MonoBehaviour
    {
        public float spinDegreesPerSecond = 90f;
        public float bobAmplitude = 0.1f;
        public float bobFrequency = 1.2f;

        private Vector3 origin;
        private float phase;

        private void Start()
        {
            origin = transform.localPosition;
            phase = origin.x; // de-synchronise neighbours
        }

        private void Update()
        {
            float t = Time.time;
            transform.localRotation = Quaternion.Euler(0f, t * spinDegreesPerSecond, 0f);
            transform.localPosition = origin + new Vector3(0f, Mathf.Sin((t + phase) * bobFrequency * Mathf.PI * 2f) * bobAmplitude, 0f);
        }
    }
}
