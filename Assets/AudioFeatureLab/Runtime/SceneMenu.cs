using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AudioFeatureLab
{
    [AddComponentMenu("Audio Feature Lab/Scene Menu")]
    public class SceneMenu : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            public string label;
            public string sceneName;
            public KeyCode key;
        }

        public string headline = "Audio Feature Lab";
        [TextArea(4, 12)] public string summary = string.Empty;
        public Entry[] entries = Array.Empty<Entry>();

        void Update()
        {
            if (entries == null)
                return;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null && Input.GetKeyDown(entries[i].key))
                    SceneManager.LoadScene(entries[i].sceneName);
            }
        }

        void OnGUI()
        {
            float panelWidth = Mathf.Min(1100f, Screen.width - 64f);
            var panel = new Rect(32f, 28f, panelWidth, Screen.height - 56f);
            LabGui.Fill(panel);
            GUILayout.BeginArea(new Rect(panel.x + 28f, panel.y + 24f, panel.width - 56f, panel.height - 48f));
            GUILayout.Label(headline, LabGui.Title);
            GUILayout.Space(16f);
            GUILayout.Label(summary, LabGui.Body);
            GUILayout.Space(22f);
            if (entries != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    Entry entry = entries[i];
                    if (entry == null)
                        continue;
                    if (GUILayout.Button(entry.label, LabGui.Button, GUILayout.Height(64f)))
                        SceneManager.LoadScene(entry.sceneName);
                    GUILayout.Space(8f);
                }
            }

            GUILayout.EndArea();
        }
    }
}
