using UnityEngine;
using UnityEngine.Audio;

namespace AudioFeatureLab
{
    /// <summary>
    /// Crossfades two Audio Mixer snapshots. Keys 1 through 9 set the blend:
    /// 1 is snapshot A, 9 is snapshot B.
    /// The public <see cref="blend"/> value is applied with TransitionToSnapshots.
    /// </summary>
    [AddComponentMenu("Audio Feature Lab/Snapshot Crossfader")]
    public class SnapshotCrossfader : MonoBehaviour
    {
        public AudioMixer mixer;
        public AudioMixerSnapshot snapshotA;
        public AudioMixerSnapshot snapshotB;
        public AudioMixerSnapshot[] catalog;
        public bool keyboardControl = true;
        public float transitionSeconds = 0.85f;
        public KeyCode cycleAKey = KeyCode.Y;
        public KeyCode cycleBKey = KeyCode.U;

        [Range(0f, 1f)] public float blend;
        public float weightA;
        public float weightB;

        float targetBlend;
        float appliedBlend = -1f;

        void Start()
        {
            targetBlend = blend;
            ApplyIfNeeded(force: true);
        }

        void Update()
        {
            if (keyboardControl && BlendKeyboard.TryReadBlend(out float keyed))
                targetBlend = keyed;
            if (keyboardControl && catalog != null && catalog.Length > 1)
            {
                if (Input.GetKeyDown(cycleAKey))
                    snapshotA = Next(snapshotA, snapshotB);
                if (Input.GetKeyDown(cycleBKey))
                    snapshotB = Next(snapshotB, snapshotA);
            }

            float speed = transitionSeconds <= 0.0001f ? 1000f : 1f / transitionSeconds;
            blend = Mathf.MoveTowards(blend, targetBlend, speed * Time.unscaledDeltaTime);
            ApplyIfNeeded(force: false);
        }

        public void SetBlend(float value, bool immediate)
        {
            targetBlend = Mathf.Clamp01(value);
            if (immediate)
            {
                blend = targetBlend;
                ApplyIfNeeded(force: true);
            }
        }

        AudioMixerSnapshot Next(AudioMixerSnapshot current, AudioMixerSnapshot other)
        {
            int index = IndexOf(current);
            for (int step = 1; step <= catalog.Length; step++)
            {
                AudioMixerSnapshot candidate = catalog[(index + step) % catalog.Length];
                if (candidate != null && candidate != other)
                    return candidate;
            }

            return current;
        }

        int IndexOf(AudioMixerSnapshot snapshot)
        {
            if (catalog == null)
                return 0;
            for (int i = 0; i < catalog.Length; i++)
            {
                if (catalog[i] == snapshot)
                    return i;
            }

            return 0;
        }

        void ApplyIfNeeded(bool force)
        {
            weightA = 1f - blend;
            weightB = blend;
            if (mixer == null || snapshotA == null || snapshotB == null)
                return;
            if (!force && Mathf.Abs(appliedBlend - blend) < 0.0005f)
                return;

            mixer.TransitionToSnapshots(
                new[] { snapshotA, snapshotB },
                new[] { weightA, weightB },
                0f);
            appliedBlend = blend;
        }
    }
}
