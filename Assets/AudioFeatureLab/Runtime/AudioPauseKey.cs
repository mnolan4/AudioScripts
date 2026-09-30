using UnityEngine;

namespace AudioFeatureLab
{
    /// <summary>
    /// Pauses every voice through AudioListener.pause, and trims AudioListener.volume.
    /// </summary>
    [AddComponentMenu("Audio Feature Lab/Audio Pause Key")]
    public class AudioPauseKey : MonoBehaviour
    {
        public KeyCode pauseKey = KeyCode.P;
        public KeyCode volumeDownKey = KeyCode.LeftBracket;
        public KeyCode volumeUpKey = KeyCode.RightBracket;
        public bool paused;
        [Range(0f, 1f)] public float listenerVolume = 1f;

        void Update()
        {
            if (Input.GetKeyDown(pauseKey))
            {
                paused = !paused;
                AudioListener.pause = paused;
            }

            if (Input.GetKeyDown(volumeDownKey))
                listenerVolume = Mathf.Clamp01(listenerVolume - 0.1f);
            if (Input.GetKeyDown(volumeUpKey))
                listenerVolume = Mathf.Clamp01(listenerVolume + 0.1f);
            AudioListener.volume = listenerVolume;
        }

        void OnDisable()
        {
            AudioListener.pause = false;
            AudioListener.volume = 1f;
        }
    }
}
