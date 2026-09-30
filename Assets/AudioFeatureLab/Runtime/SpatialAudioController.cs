using UnityEngine;

namespace AudioFeatureLab
{
    public enum SpatialMode
    {
        TwoD = 0,
        ThreeD = 1,
        Hybrid = 2
    }

    /// <summary>
    /// Pushes the main spatial AudioSource settings from public fields, with keys to compare 2D, 3D, and hybrid.
    /// </summary>
    [AddComponentMenu("Audio Feature Lab/Spatial Audio Controller")]
    public class SpatialAudioController : MonoBehaviour
    {
        public AudioSource source;
        public bool keyboardControl = true;
        public SpatialMode mode = SpatialMode.Hybrid;
        public KeyCode modeKey = KeyCode.B;
        public KeyCode rolloffKey = KeyCode.R;
        public KeyCode spreadKey = KeyCode.V;

        [Range(0f, 1f)] public float spatialBlend = 0.5f;
        public float dopplerLevel = 0.4f;
        [Range(0f, 360f)] public float spreadDegrees = 40f;
        public float minDistance = 1.5f;
        public float maxDistance = 28f;
        public AudioRolloffMode rolloff = AudioRolloffMode.Logarithmic;
        [Range(-1f, 1f)] public float panStereo;
        [Range(0, 256)] public int priority = 128;
        public bool spatialize;

        void Start()
        {
            ApplyMode(mode);
        }

        void Update()
        {
            if (keyboardControl && source != null)
            {
                if (Input.GetKeyDown(modeKey))
                {
                    mode = (SpatialMode)(((int)mode + 1) % 3);
                    ApplyMode(mode);
                }

                if (Input.GetKeyDown(rolloffKey))
                    rolloff = (AudioRolloffMode)(((int)rolloff + 1) % 3);
                if (Input.GetKeyDown(spreadKey))
                    spreadDegrees = spreadDegrees < 90f ? 180f : 0f;
            }

            Push();
        }

        public void ApplyMode(SpatialMode value)
        {
            mode = value;
            switch (value)
            {
                case SpatialMode.TwoD:
                    spatialBlend = 0f;
                    dopplerLevel = 0f;
                    spreadDegrees = 0f;
                    panStereo = 0f;
                    spatialize = false;
                    break;
                case SpatialMode.ThreeD:
                    spatialBlend = 1f;
                    dopplerLevel = 1f;
                    spreadDegrees = 0f;
                    panStereo = 0f;
                    rolloff = AudioRolloffMode.Logarithmic;
                    spatialize = false;
                    break;
                default:
                    spatialBlend = 0.45f;
                    dopplerLevel = 0.35f;
                    spreadDegrees = 70f;
                    rolloff = AudioRolloffMode.Linear;
                    spatialize = false;
                    break;
            }
        }

        public void Push()
        {
            if (source == null)
                return;
            source.spatialBlend = spatialBlend;
            source.dopplerLevel = dopplerLevel;
            source.spread = spreadDegrees;
            source.minDistance = Mathf.Max(0.01f, minDistance);
            source.maxDistance = Mathf.Max(source.minDistance + 0.1f, maxDistance);
            source.rolloffMode = rolloff;
            source.panStereo = panStereo;
            source.priority = priority;
            source.spatialize = spatialize;
        }
    }
}
