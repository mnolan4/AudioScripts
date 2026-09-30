using UnityEngine;

namespace AudioFeatureLab
{
    /// <summary>
    /// Copies the listener mix on the audio thread so other scripts can meter and analyze the output.
    /// Place this on the same GameObject as the AudioListener.
    /// </summary>
    [AddComponentMenu("Audio Feature Lab/Listener Spectrum Tap")]
    [RequireComponent(typeof(AudioListener))]
    public class ListenerSpectrumTap : MonoBehaviour
    {
        public int capacity = 8192;

        [Range(0f, 1f)] public float peak;
        public float peakDecibels = AudioUnits.SilenceDecibels;
        [Range(0f, 1f)] public float rms;
        public float releaseSeconds = 0.25f;

        float[] ring;
        int write;
        int count;
        float threadPeak;
        float threadMeanSquare;
        int threadFrames;
        readonly object gate = new object();

        void OnEnable()
        {
            ring = new float[Mathf.Max(1024, capacity)];
            write = 0;
            count = 0;
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (ring == null || channels <= 0)
                return;
            int frames = data.Length / channels;
            float localPeak = 0f;
            double square = 0.0;
            lock (gate)
            {
                for (int i = 0; i < frames; i++)
                {
                    float sample = 0f;
                    int offset = i * channels;
                    for (int channel = 0; channel < channels; channel++)
                        sample += data[offset + channel];
                    sample /= channels;
                    ring[write] = sample;
                    write++;
                    if (write >= ring.Length)
                        write = 0;
                    if (count < ring.Length)
                        count++;
                    float absolute = sample < 0f ? -sample : sample;
                    if (absolute > localPeak)
                        localPeak = absolute;
                    square += sample * sample;
                }

                if (localPeak > threadPeak)
                    threadPeak = localPeak;
                threadMeanSquare += (float)square;
                threadFrames += frames;
            }
        }

        void Update()
        {
            float instantPeak;
            float instantRms;
            lock (gate)
            {
                instantPeak = threadPeak;
                instantRms = threadFrames > 0 ? Mathf.Sqrt(threadMeanSquare / threadFrames) : 0f;
                threadPeak = 0f;
                threadMeanSquare = 0f;
                threadFrames = 0;
            }

            float dt = Time.unscaledDeltaTime;
            peak = AudioUnits.Ballistic(peak, instantPeak, 0.01f, releaseSeconds, dt);
            rms = AudioUnits.Ballistic(rms, instantRms, 0.01f, releaseSeconds, dt);
            peakDecibels = AudioUnits.LinearToDecibels(peak);
        }

        public int CopyLatest(float[] destination)
        {
            if (destination == null || ring == null)
                return 0;
            lock (gate)
            {
                int frames = Mathf.Min(destination.Length, count);
                int start = write - frames;
                if (start < 0)
                    start += ring.Length;
                for (int i = 0; i < frames; i++)
                {
                    destination[i] = ring[start];
                    start++;
                    if (start >= ring.Length)
                        start = 0;
                }

                return frames;
            }
        }
    }
}
