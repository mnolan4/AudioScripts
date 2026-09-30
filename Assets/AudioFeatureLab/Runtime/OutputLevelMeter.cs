using UnityEngine;

namespace AudioFeatureLab
{
    /// <summary>
    /// Peak and RMS of one playing AudioSource, taken from GetOutputData (after that source's volume).
    /// </summary>
    [AddComponentMenu("Audio Feature Lab/Output Level Meter")]
    public class OutputLevelMeter : MonoBehaviour
    {
        public AudioSource source;
        public int window = 1024;
        public float releaseSeconds = 0.25f;

        [Range(0f, 1f)] public float peak;
        public float peakDecibels = AudioUnits.SilenceDecibels;
        [Range(0f, 1f)] public float rms;

        float[] buffer;

        void Update()
        {
            if (buffer == null || buffer.Length != window)
                buffer = new float[Mathf.Max(64, window)];
            float instantPeak = 0f;
            float instantRms = 0f;
            if (source != null && source.isPlaying)
            {
                source.GetOutputData(buffer, 0);
                float sum = 0f;
                for (int i = 0; i < buffer.Length; i++)
                {
                    float sample = buffer[i];
                    float absolute = Mathf.Abs(sample);
                    if (absolute > instantPeak)
                        instantPeak = absolute;
                    sum += sample * sample;
                }

                instantRms = Mathf.Sqrt(sum / buffer.Length);
            }

            float dt = Time.unscaledDeltaTime;
            peak = AudioUnits.Ballistic(peak, instantPeak, 0.01f, releaseSeconds, dt);
            rms = AudioUnits.Ballistic(rms, instantRms, 0.01f, releaseSeconds, dt);
            peakDecibels = AudioUnits.LinearToDecibels(peak);
        }
    }
}
