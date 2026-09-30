using UnityEngine;
using UnityEngine.Audio;

namespace AudioFeatureLab
{
    /// <summary>
    /// Assigns an AudioSource to a mixer group at runtime. The group is the bus the source feeds.
    /// </summary>
    [AddComponentMenu("Audio Feature Lab/Mixer Group Output")]
    public class MixerGroupOutput : MonoBehaviour
    {
        public AudioSource source;
        public AudioMixerGroup group;

        void Awake()
        {
            Apply();
        }

        public void Apply()
        {
            if (source == null)
                source = GetComponent<AudioSource>();
            if (source != null)
                source.outputAudioMixerGroup = group;
        }
    }
}
