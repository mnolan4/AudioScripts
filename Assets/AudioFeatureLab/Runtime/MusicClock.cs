using UnityEngine;

namespace AudioFeatureLab
{
    /// <summary>
    /// Musical clock locked to AudioSettings.dspTime.
    /// <see cref="beat"/> counts quarter notes from the scheduled downbeat. <see cref="pulse"/> peaks on each beat.
    /// </summary>
    [AddComponentMenu("Audio Feature Lab/Music Clock")]
    public class MusicClock : MonoBehaviour
    {
        public SharedTransport transport;
        public float bpm = 120f;
        public int beatsPerBar = 4;
        public double startDspTime;
        public bool running = true;

        public float beat;
        public int bar;
        public int beatInBar;
        public float pulse;

        void Start()
        {
            if (transport != null)
                startDspTime = transport.StartDspTime;
            else if (startDspTime <= 0.0)
                startDspTime = AudioSettings.dspTime;
        }

        void Update()
        {
            if (!running || bpm <= 1f)
                return;
            double secondsPerBeat = 60.0 / bpm;
            double elapsed = AudioSettings.dspTime - startDspTime;
            if (elapsed < 0.0)
                elapsed = 0.0;
            double beats = elapsed / secondsPerBeat;
            beat = (float)beats;
            int whole = Mathf.FloorToInt(beat);
            beatInBar = beatsPerBar > 0 ? whole % beatsPerBar : whole;
            bar = beatsPerBar > 0 ? whole / beatsPerBar : 0;
            float fraction = beat - whole;
            pulse = fraction < 0.12f ? 1f - (fraction / 0.12f) : 0f;
        }
    }
}
