using System.Collections;
using UnityEngine;

namespace AudioFeatureLab
{
    /// <summary>
    /// Fades a single AudioSource in or out without touching unrelated sources.
    /// </summary>
    [AddComponentMenu("Audio Feature Lab/Audio Fader")]
    public class AudioFader : MonoBehaviour
    {
        public AudioSource source;
        public float seconds = 1.25f;
        public bool unscaledTime = true;
        public KeyCode fadeOutKey = KeyCode.None;
        public KeyCode fadeInKey = KeyCode.None;

        [Range(0f, 1f)] public float currentGain = 1f;

        float baseVolume = 1f;
        Coroutine running;

        void Awake()
        {
            if (source == null)
                source = GetComponent<AudioSource>();
            if (source != null)
                baseVolume = source.volume;
        }

        void Update()
        {
            if (fadeOutKey != KeyCode.None && Input.GetKeyDown(fadeOutKey))
                FadeTo(0f);
            if (fadeInKey != KeyCode.None && Input.GetKeyDown(fadeInKey))
                FadeTo(1f);
            if (source != null && running == null)
                source.volume = baseVolume * currentGain;
        }

        public void FadeTo(float gain)
        {
            if (running != null)
                StopCoroutine(running);
            running = StartCoroutine(FadeRoutine(Mathf.Clamp01(gain)));
        }

        IEnumerator FadeRoutine(float target)
        {
            float start = currentGain;
            float duration = Mathf.Max(0.01f, seconds);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                currentGain = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
                if (source != null)
                    source.volume = baseVolume * currentGain;
                yield return null;
            }

            currentGain = target;
            if (source != null)
                source.volume = baseVolume * currentGain;
            running = null;
        }
    }
}
