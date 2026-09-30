using UnityEngine;

namespace AudioFeatureLab
{
    /// <summary>
    /// Equal-power or linear crossfade between two AudioSources.
    /// Keys 1 through 9 choose the blend: 1 is source A, 9 is source B, 5 is the center.
    /// </summary>
    [AddComponentMenu("Audio Feature Lab/Audio Source Crossfader")]
    public class AudioSourceCrossfader : MonoBehaviour
    {
        public AudioSource sourceA;
        public AudioSource sourceB;
        public CrossfadeLaw law = CrossfadeLaw.EqualPower;
        public bool keyboardControl = true;
        public bool playOnStart = true;
        public SharedTransport transport;

        [Range(0f, 1f)] public float blend;
        public float transitionSeconds = 0.8f;
        public float masterGain = 1f;

        public float sourceAGain;
        public float sourceBGain;

        public KeyCode masterDownKey = KeyCode.None;
        public KeyCode masterUpKey = KeyCode.None;

        float targetBlend;
        float baseA = 1f;
        float baseB = 1f;

        void Start()
        {
            targetBlend = blend;
            if (sourceA != null)
                baseA = sourceA.volume;
            if (sourceB != null)
                baseB = sourceB.volume;
            if (playOnStart)
                PlayBoth();
            ApplyGains();
        }

        void Update()
        {
            if (keyboardControl && BlendKeyboard.TryReadBlend(out float keyed))
                targetBlend = keyed;

            if (masterDownKey != KeyCode.None && Input.GetKeyDown(masterDownKey))
                masterGain = Mathf.Clamp01(masterGain - 0.1f);
            if (masterUpKey != KeyCode.None && Input.GetKeyDown(masterUpKey))
                masterGain = Mathf.Clamp01(masterGain + 0.1f);

            float speed = transitionSeconds <= 0.0001f ? 1000f : 1f / transitionSeconds;
            blend = Mathf.MoveTowards(blend, targetBlend, speed * Time.unscaledDeltaTime);
            ApplyGains();
        }

        public void SetBlend(float value, bool immediate)
        {
            targetBlend = Mathf.Clamp01(value);
            if (immediate)
            {
                blend = targetBlend;
                ApplyGains();
            }
        }

        public void PlayBoth()
        {
            Play(sourceA);
            Play(sourceB);
        }

        void Play(AudioSource source)
        {
            if (source == null || source.clip == null)
                return;
            source.loop = true;
            if (transport != null)
            {
                source.PlayScheduled(transport.StartDspTime);
                return;
            }

            if (!source.isPlaying)
                source.Play();
        }

        void ApplyGains()
        {
            AudioUnits.CrossfadeGains(blend, law, out sourceAGain, out sourceBGain);
            if (sourceA != null)
                sourceA.volume = baseA * sourceAGain * masterGain;
            if (sourceB != null)
                sourceB.volume = baseB * sourceBGain * masterGain;
        }
    }
}
