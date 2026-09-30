using UnityEngine;

namespace AudioFeatureLab
{
    /// <summary>
    /// Opens a microphone and routes it to an AudioSource for monitoring.
    /// Analysis scripts read the capture buffer, so monitor volume can be zero without losing the signal.
    /// </summary>
    [AddComponentMenu("Audio Feature Lab/Microphone Capture")]
    [RequireComponent(typeof(AudioSource))]
    public class MicrophoneCapture : MonoBehaviour
    {
        public AudioSource source;
        public bool keyboardControl = true;
        public KeyCode toggleKey = KeyCode.M;
        public KeyCode deviceKey = KeyCode.N;
        public KeyCode monitorKey = KeyCode.K;
        [Range(0f, 1f)] public float monitorVolume;
        public int requestedSampleRate = 48000;
        public int clipSeconds = 1;

        public bool isRunning;
        public string activeDevice = string.Empty;
        public int deviceIndex;
        public int sampleRate;
        public int channels = 1;

        void Awake()
        {
            if (source == null)
                source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
        }

        void Update()
        {
            source.volume = monitorVolume;
            if (!keyboardControl)
                return;
            if (Input.GetKeyDown(toggleKey))
            {
                if (isRunning)
                    StopCapture();
                else
                    StartCapture(activeDevice);
            }

            if (Input.GetKeyDown(deviceKey))
                CycleDevice();
            if (Input.GetKeyDown(monitorKey))
                monitorVolume = monitorVolume > 0.05f ? 0f : 0.7f;
        }

        public string[] Devices
        {
            get { return Microphone.devices ?? new string[0]; }
        }

        public void CycleDevice()
        {
            string[] devices = Devices;
            if (devices.Length == 0)
                return;
            deviceIndex = (deviceIndex + 1) % devices.Length;
            string next = devices[deviceIndex];
            if (isRunning)
                StartCapture(next);
            else
                activeDevice = next;
        }

        public void StartCapture(string device)
        {
            StopCapture();
            string[] devices = Devices;
            if (devices.Length == 0)
            {
                activeDevice = string.Empty;
                Debug.LogWarning("No microphone devices are available.");
                return;
            }

            if (string.IsNullOrEmpty(device))
                device = devices[Mathf.Clamp(deviceIndex, 0, devices.Length - 1)];

            source.clip = Microphone.Start(device, true, Mathf.Max(1, clipSeconds), requestedSampleRate);
            activeDevice = device;
            sampleRate = source.clip != null ? source.clip.frequency : requestedSampleRate;
            channels = source.clip != null ? source.clip.channels : 1;
            int guard = 0;
            while (Microphone.GetPosition(device) <= 0 && guard < 1000)
                guard++;
            source.Play();
            isRunning = true;
        }

        public void StopCapture()
        {
            if (!string.IsNullOrEmpty(activeDevice) && Microphone.IsRecording(activeDevice))
                Microphone.End(activeDevice);
            if (source != null)
            {
                source.Stop();
                source.clip = null;
            }

            isRunning = false;
        }

        /// <summary>
        /// Copies the newest mono frames from the microphone ring into destination.
        /// Returns the number of frames written.
        /// </summary>
        public int ReadLatest(float[] destination)
        {
            if (!isRunning || source == null || source.clip == null || destination == null || destination.Length == 0)
                return 0;

            AudioClip clip = source.clip;
            int frames = Mathf.Min(destination.Length, clip.samples);
            int position = Microphone.GetPosition(activeDevice);
            if (position < 0 || frames <= 0)
                return 0;
            int start = position - frames;
            int channelCount = Mathf.Max(1, clip.channels);
            float[] interleaved = new float[frames * channelCount];

            if (start >= 0)
            {
                clip.GetData(interleaved, start);
                Downmix(interleaved, destination, frames, channelCount);
                return frames;
            }

            int tail = -start;
            int head = frames - tail;
            int tailOffset = clip.samples - tail;
            float[] wrapped = new float[frames * channelCount];
            float[] tailBuffer = new float[tail * channelCount];
            float[] headBuffer = new float[head * channelCount];
            clip.GetData(tailBuffer, tailOffset);
            if (head > 0)
                clip.GetData(headBuffer, 0);
            int tailSamples = tail * channelCount;
            for (int i = 0; i < tailSamples; i++)
                wrapped[i] = tailBuffer[i];
            for (int i = 0; i < headBuffer.Length; i++)
                wrapped[tailSamples + i] = headBuffer[i];
            Downmix(wrapped, destination, frames, channelCount);
            return frames;
        }

        void OnDisable()
        {
            StopCapture();
        }

        static void Downmix(float[] interleaved, float[] destination, int frames, int channelCount)
        {
            for (int i = 0; i < frames; i++)
            {
                float sum = 0f;
                int offset = i * channelCount;
                for (int channel = 0; channel < channelCount; channel++)
                    sum += interleaved[offset + channel];
                destination[i] = sum / channelCount;
            }
        }
    }
}
