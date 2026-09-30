using System;
using UnityEngine;

namespace AudioFeatureLab
{
    [Serializable]
    public class StemLayer
    {
        public string name = "Stem";
        public AudioSource source;
        public AudioClip clip;
        [Range(0f, 1f)] public float gain = 0.7f;
        public bool audible = true;
        public KeyCode toggleKey = KeyCode.None;
    }

    /// <summary>
    /// Starts looping stems with PlayScheduled so they stay aligned to the DSP clock.
    /// </summary>
    [AddComponentMenu("Audio Feature Lab/Synced Stem Player")]
    public class SyncedStemPlayer : MonoBehaviour
    {
        public StemLayer[] stems = Array.Empty<StemLayer>();
        public SharedTransport transport;
        public bool playOnStart = true;
        public double scheduledStartDsp;

        void Start()
        {
            if (playOnStart)
                PlaySynced();
        }

        void Update()
        {
            if (stems == null)
                return;
            for (int i = 0; i < stems.Length; i++)
            {
                StemLayer stem = stems[i];
                if (stem == null || stem.source == null)
                    continue;
                if (stem.toggleKey != KeyCode.None && Input.GetKeyDown(stem.toggleKey))
                    stem.audible = !stem.audible;
                stem.source.volume = stem.audible ? stem.gain : 0f;
            }
        }

        public void PlaySynced()
        {
            scheduledStartDsp = transport != null
                ? transport.StartDspTime
                : AudioSettings.dspTime + 0.2;
            if (stems == null)
                return;
            for (int i = 0; i < stems.Length; i++)
            {
                StemLayer stem = stems[i];
                if (stem == null || stem.source == null)
                    continue;
                if (stem.clip != null)
                    stem.source.clip = stem.clip;
                stem.source.loop = true;
                stem.source.PlayScheduled(scheduledStartDsp);
            }
        }
    }
}
