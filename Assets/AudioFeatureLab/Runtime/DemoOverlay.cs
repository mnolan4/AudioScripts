using UnityEngine;

namespace AudioFeatureLab
{
    [AddComponentMenu("Audio Feature Lab/Demo Overlay")]
    public class DemoOverlay : MonoBehaviour
    {
        public string title = "Audio Lab";
        [TextArea(8, 24)] public string body = string.Empty;
        public Vector2 scroll;
        public float width = 720f;

        void OnGUI()
        {
            float panelWidth = Mathf.Clamp(Mathf.Max(width, Screen.width * 0.46f), 640f, Screen.width * 0.58f);
            var panel = new Rect(16f, 16f, panelWidth, Screen.height - 32f);
            LabGui.Fill(panel);
            GUILayout.BeginArea(new Rect(panel.x + 18f, panel.y + 16f, panel.width - 36f, panel.height - 32f));
            scroll = GUILayout.BeginScrollView(scroll, GUIStyle.none);
            GUILayout.Label(title, LabGui.Title);
            GUILayout.Space(10f);
            GUILayout.Label(body, LabGui.Body);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
