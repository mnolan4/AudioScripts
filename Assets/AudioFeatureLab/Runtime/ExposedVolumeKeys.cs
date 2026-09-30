using UnityEngine;
using UnityEngine.Audio;

namespace AudioFeatureLab
{
    /// <summary>
    /// Nudges exposed mixer parameters. SetFloat takes a parameter away from snapshot control
    /// until ClearFloat gives it back. Backspace clears every override.
    /// </summary>
    [AddComponentMenu("Audio Feature Lab/Exposed Volume Keys")]
    public class ExposedVolumeKeys : MonoBehaviour
    {
        public AudioMixer mixer;
        public string[] parameters =
        {
            "MasterVolume",
            "MusicVolume",
            "AmbienceVolume",
            "SfxVolume",
            "VoiceVolume"
        };

        public KeyCode[] selectKeys =
        {
            KeyCode.Z,
            KeyCode.X,
            KeyCode.C,
            KeyCode.V,
            KeyCode.B
        };

        public KeyCode downKey = KeyCode.DownArrow;
        public KeyCode upKey = KeyCode.UpArrow;
        public KeyCode clearKey = KeyCode.Backspace;
        public float stepDecibels = 2f;

        public int selected;
        public float decibels;
        public bool overrideActive;
        public string selectedName = string.Empty;

        void Update()
        {
            if (mixer == null || parameters == null || parameters.Length == 0)
                return;
            selected = Mathf.Clamp(selected, 0, parameters.Length - 1);
            selectedName = parameters[selected];
            if (selectKeys != null)
            {
                int count = Mathf.Min(selectKeys.Length, parameters.Length);
                for (int i = 0; i < count; i++)
                {
                    if (Input.GetKeyDown(selectKeys[i]))
                        selected = i;
                }
            }

            if (Input.GetKeyDown(upKey))
                Nudge(stepDecibels);
            if (Input.GetKeyDown(downKey))
                Nudge(-stepDecibels);
            if (Input.GetKeyDown(clearKey))
                ClearAll();

            if (mixer.GetFloat(selectedName, out float current))
                decibels = current;
        }

        void Nudge(float delta)
        {
            float current = 0f;
            mixer.GetFloat(selectedName, out current);
            decibels = Mathf.Clamp(current + delta, -80f, 20f);
            mixer.SetFloat(selectedName, decibels);
            overrideActive = true;
        }

        public void ClearAll()
        {
            if (parameters == null)
                return;
            for (int i = 0; i < parameters.Length; i++)
                mixer.ClearFloat(parameters[i]);
            overrideActive = false;
        }
    }
}
