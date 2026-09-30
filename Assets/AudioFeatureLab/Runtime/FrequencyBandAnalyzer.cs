using System;
using System.Collections.Generic;
using UnityEngine;

namespace AudioFeatureLab
{
    public enum AnalysisInput
    {
        AudioSource = 0,
        Microphone = 1,
        ListenerOutput = 2
    }

    /// <summary>
    /// FFT analysis with inspector-defined frequency ranges.
    /// Each range writes <see cref="FrequencyBand.level"/> every frame.
    /// </summary>
    [AddComponentMenu("Audio Feature Lab/Frequency Band Analyzer")]
    public class FrequencyBandAnalyzer : MonoBehaviour
    {
        public AnalysisInput input = AnalysisInput.AudioSource;
        public AudioSource source;
        public MicrophoneCapture microphone;
        public ListenerSpectrumTap listenerTap;
        public bool useManualFft;
        public FFTWindow unityWindow = FFTWindow.BlackmanHarris;
        [Range(64, 8192)] public int fftSize = 2048;
        public float smoothingSeconds = 0.08f;
        public bool keyboardToggle;
        public KeyCode toggleInputKey = KeyCode.H;
        public List<FrequencyBand> bands = new List<FrequencyBand>();
        public float[] levels = Array.Empty<float>();
        public int sampleRate;

        float[] time;
        float[] magnitudes;
        float[] unitySpectrum;

        void Awake()
        {
            if (bands == null || bands.Count == 0)
                bands = SpectrumBands.CreateDefaults();
        }

        void OnValidate()
        {
            fftSize = SimpleFft.ClampPowerOfTwo(fftSize, 64, 8192);
        }

        void Update()
        {
            if (keyboardToggle && Input.GetKeyDown(toggleInputKey))
            {
                input = input == AnalysisInput.Microphone
                    ? AnalysisInput.AudioSource
                    : AnalysisInput.Microphone;
            }

            fftSize = SimpleFft.ClampPowerOfTwo(fftSize, 64, 8192);
            if (bands == null)
                return;
            if (levels == null || levels.Length != bands.Count)
                levels = new float[bands.Count];

            sampleRate = ResolveSampleRate();
            EnsureBuffers();
            bool heard = FillMagnitudes();
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < bands.Count; i++)
            {
                FrequencyBand band = bands[i];
                float energy = heard
                    ? SpectrumBands.BandEnergy(magnitudes, sampleRate, band.minHz, band.maxHz)
                    : 0f;
                float target = SpectrumBands.NormalizeEnergy(energy, band.boost);
                band.energy = energy;
                band.level = AudioUnits.Ballistic(band.level, target, smoothingSeconds, smoothingSeconds * 1.4f, dt);
                levels[i] = band.level;
            }
        }

        public float GetLevel(string bandName)
        {
            if (bands == null || string.IsNullOrEmpty(bandName))
                return 0f;
            for (int i = 0; i < bands.Count; i++)
            {
                if (string.Equals(bands[i].name, bandName, StringComparison.OrdinalIgnoreCase))
                    return bands[i].level;
            }

            return 0f;
        }

        int ResolveSampleRate()
        {
            if (input == AnalysisInput.Microphone && microphone != null && microphone.sampleRate > 0)
                return microphone.sampleRate;
            if (input == AnalysisInput.AudioSource && useManualFft && source != null && source.clip != null)
                return source.clip.frequency;
            return AudioSettings.outputSampleRate;
        }

        void EnsureBuffers()
        {
            if (time == null || time.Length != fftSize)
                time = new float[fftSize];
            if (magnitudes == null || magnitudes.Length != fftSize / 2)
                magnitudes = new float[fftSize / 2];
        }

        bool FillMagnitudes()
        {
            if (input == AnalysisInput.ListenerOutput)
                return FillFromTap();
            if (input == AnalysisInput.Microphone)
                return FillFromMicrophone();
            if (useManualFft)
                return FillFromClip();
            return FillFromUnitySpectrum();
        }

        bool FillFromUnitySpectrum()
        {
            if (source == null || !source.isPlaying)
                return false;
            if (unitySpectrum == null || unitySpectrum.Length != fftSize / 2)
                unitySpectrum = new float[fftSize / 2];
            source.GetSpectrumData(unitySpectrum, 0, unityWindow);
            Array.Copy(unitySpectrum, magnitudes, unitySpectrum.Length);
            return true;
        }

        bool FillFromMicrophone()
        {
            if (microphone == null || !microphone.isRunning)
                return false;
            return microphone.ReadLatest(time) == time.Length && ComputeManual();
        }

        bool FillFromTap()
        {
            if (listenerTap == null)
                return false;
            return listenerTap.CopyLatest(time) == time.Length && ComputeManual();
        }

        bool FillFromClip()
        {
            if (source == null || source.clip == null || !source.isPlaying)
                return false;
            AudioClip clip = source.clip;
            int channels = Mathf.Max(1, clip.channels);
            int frames = fftSize;
            int start = source.timeSamples - frames;
            float[] interleaved = new float[frames * channels];
            if (start >= 0)
            {
                clip.GetData(interleaved, start);
            }
            else
            {
                int tail = -start;
                int head = frames - tail;
                float[] tailBuffer = new float[tail * channels];
                float[] headBuffer = new float[Mathf.Max(1, head) * channels];
                clip.GetData(tailBuffer, clip.samples - tail);
                if (head > 0)
                    clip.GetData(headBuffer, 0);
                int tailSamples = tailBuffer.Length;
                for (int i = 0; i < tailSamples; i++)
                    interleaved[i] = tailBuffer[i];
                for (int i = 0; i < head * channels; i++)
                    interleaved[tailSamples + i] = headBuffer[i];
            }

            for (int i = 0; i < frames; i++)
            {
                float sum = 0f;
                int offset = i * channels;
                for (int channel = 0; channel < channels; channel++)
                    sum += interleaved[offset + channel];
                time[i] = sum / channels;
            }

            return ComputeManual();
        }

        bool ComputeManual()
        {
            SimpleFft.MagnitudeSpectrum(time, magnitudes, true);
            return true;
        }
    }
}
