using UnityEngine;

namespace AudioFeatureLab
{
    /// <summary>
    /// One shared DSP start time so stems and crossfades begin on the same sample.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [AddComponentMenu("Audio Feature Lab/Shared Transport")]
    public class SharedTransport : MonoBehaviour
    {
        public double leadInSeconds = 0.2;
        public double StartDspTime { get; private set; }
        public bool scheduled;

        void Awake()
        {
            StartDspTime = AudioSettings.dspTime + leadInSeconds;
            scheduled = true;
        }
    }
}
