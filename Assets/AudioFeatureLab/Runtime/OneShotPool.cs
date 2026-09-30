using UnityEngine;

namespace AudioFeatureLab
{
    /// <summary>
    /// Small pool of AudioSources for overlapping one-shots, in 2D or at a world position.
    /// </summary>
    [AddComponentMenu("Audio Feature Lab/One Shot Pool")]
    public class OneShotPool : MonoBehaviour
    {
        public AudioClip clip;
        public int voices = 6;
        [Range(0f, 1f)] public float volume = 0.85f;
        [Range(0f, 1f)] public float spatialBlend = 1f;
        public float minDistance = 1.2f;
        public float maxDistance = 18f;
        public Vector2 pitchRange = new Vector2(0.94f, 1.08f);
        public KeyCode trigger = KeyCode.Space;
        public Transform follow;
        public bool parentVoicesToPool = true;

        AudioSource[] pool;
        int cursor;

        void Awake()
        {
            voices = Mathf.Max(1, voices);
            pool = new AudioSource[voices];
            for (int i = 0; i < voices; i++)
            {
                var voice = new GameObject("One Shot " + (i + 1));
                if (parentVoicesToPool)
                    voice.transform.SetParent(transform, false);
                AudioSource source = voice.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = spatialBlend;
                source.minDistance = minDistance;
                source.maxDistance = maxDistance;
                source.dopplerLevel = 0f;
                pool[i] = source;
            }
        }

        void Update()
        {
            if (trigger != KeyCode.None && Input.GetKeyDown(trigger))
            {
                Vector3 position = follow != null ? follow.position : transform.position;
                PlayAt(position);
            }
        }

        public void PlayAt(Vector3 position)
        {
            if (clip == null || pool == null || pool.Length == 0)
                return;
            AudioSource source = pool[cursor];
            cursor = (cursor + 1) % pool.Length;
            source.transform.position = position;
            source.spatialBlend = spatialBlend;
            source.pitch = Random.Range(pitchRange.x, pitchRange.y);
            source.PlayOneShot(clip, volume);
        }
    }
}
