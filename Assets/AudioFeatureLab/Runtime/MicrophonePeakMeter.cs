using UnityEngine;

namespace AudioFeatureLab
{
    /// <summary>
    /// Peak and RMS of the live microphone. <see cref="peak"/> is the public linear level (0-1).
    /// </summary>
    [AddComponentMenu("Audio Feature Lab/Microphone Peak Meter")]
    public class MicrophonePeakMeter : MonoBehaviour
    {
        public MicrophoneCapture capture;
        public int window = 1024;
        public float attackSeconds = 0.01f;
        public float releaseSeconds = 0.28f;

        [Range(0f, 1f)] public float peak;
        public float peakDecibels = AudioUnits.SilenceDecibels;
        [Range(0f, 1f)] public float rms;
        public float rmsDecibels = AudioUnits.SilenceDecibels;

        float[] buffer;
        float instantPeak;
        float instantRms;

        void Update()
        {
            if (capture == null)
                capture = GetComponent<MicrophoneCapture>();
            if (buffer == null || buffer.Length != window)
                buffer = new float[Mathf.Max(64, window)];

            instantPeak = 0f;
            instantRms = 0f;
            int read = capture != null ? capture.ReadLatest(buffer) : 0;
            if (read > 0)
            {
                float sum = 0f;
                for (int i = 0; i < read; i++)
                {
                    float sample = buffer[i];
                    float absolute = Mathf.Abs(sample);
                    if (absolute > instantPeak)
                        instantPeak = absolute;
                    sum += sample * sample;
                }

                instantRms = Mathf.Sqrt(sum / read);
            }

            float dt = Time.unscaledDeltaTime;
            peak = AudioUnits.Ballistic(peak, instantPeak, attackSeconds, releaseSeconds, dt);
            rms = AudioUnits.Ballistic(rms, instantRms, attackSeconds, releaseSeconds, dt);
            peakDecibels = AudioUnits.LinearToDecibels(peak);
            rmsDecibels = AudioUnits.LinearToDecibels(rms);
        }
    }
}
