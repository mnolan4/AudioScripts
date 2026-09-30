using UnityEngine;

namespace AudioFeatureLab
{
    /// <summary>
    /// A single bar driven by a microphone peak or a source output peak.
    /// </summary>
    [AddComponentMenu("Audio Feature Lab/Level Bar")]
    public class LevelBar : MonoBehaviour
    {
        public MicrophonePeakMeter microphone;
        public OutputLevelMeter output;
        public ListenerSpectrumTap listenerTap;
        public float maxHeight = 3.2f;

        void LateUpdate()
        {
            float level = 0f;
            if (microphone != null)
                level = microphone.peak;
            else if (listenerTap != null)
                level = listenerTap.peak;
            else if (output != null)
                level = output.peak;
            float height = 0.08f + Mathf.Clamp01(level) * maxHeight;
            Vector3 scale = transform.localScale;
            scale.y = height;
            transform.localScale = scale;
        }
    }
}
