using UnityEngine;

namespace AudioFeatureLab
{
    /// <summary>
    /// Low-passes a source when an AudioOcclusion collider stands between it and the listener.
    /// Walls, doors, and terrain belong on that layer. Players, particles, and pickups do not.
    /// <see cref="occlusion"/> is 0 in the open and 1 when the path is blocked.
    /// </summary>
    [AddComponentMenu("Audio Feature Lab/Occlusion Low Pass")]
    [RequireComponent(typeof(AudioLowPassFilter))]
    public class OcclusionLowPass : MonoBehaviour
    {
        public const string LayerName = "AudioOcclusion";

        public AudioSource source;
        public Transform listener;
        public LayerMask obstacles;
        public float openCutoff = 18000f;
        public float blockedCutoff = 650f;
        public float smoothingSeconds = 0.08f;
        [Range(0f, 1f)] public float volumeDip = 0.35f;

        [Range(0f, 1f)] public float occlusion;
        public float cutoffHz;

        AudioLowPassFilter filter;
        float baseVolume = 1f;
        bool captured;

        void Reset()
        {
            obstacles = Mask();
        }

        void Awake()
        {
            if (obstacles.value == 0)
                obstacles = Mask();
            filter = GetComponent<AudioLowPassFilter>();
            if (source == null)
                source = GetComponent<AudioSource>();
            if (source != null && !captured)
            {
                baseVolume = source.volume;
                captured = true;
            }
        }

        void Update()
        {
            if (listener == null && Camera.main != null)
            {
                AudioListener audioListener = FindAnyObjectByType<AudioListener>();
                if (audioListener != null)
                    listener = audioListener.transform;
            }

            float target = 0f;
            if (listener != null && source != null)
            {
                Vector3 from = listener.position;
                Vector3 to = source.transform.position - from;
                float distance = to.magnitude;
                if (distance > 0.05f && Physics.Raycast(from, to / distance, out RaycastHit hit, distance - 0.05f, obstacles, QueryTriggerInteraction.Ignore))
                {
                    if (hit.transform != source.transform && !hit.transform.IsChildOf(source.transform))
                        target = 1f;
                }
            }

            occlusion = AudioUnits.Ballistic(occlusion, target, smoothingSeconds, smoothingSeconds, Time.deltaTime);
            cutoffHz = Mathf.Lerp(openCutoff, blockedCutoff, occlusion);
            if (filter != null)
                filter.cutoffFrequency = cutoffHz;
            if (source != null)
                source.volume = baseVolume * Mathf.Lerp(1f, 1f - volumeDip, occlusion);
        }

        public static LayerMask Mask()
        {
            return LayerMask.GetMask(LayerName);
        }
    }
}
