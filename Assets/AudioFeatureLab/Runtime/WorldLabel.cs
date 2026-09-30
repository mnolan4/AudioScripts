using UnityEngine;

namespace AudioFeatureLab
{
    [AddComponentMenu("Audio Feature Lab/World Label")]
    public class WorldLabel : MonoBehaviour
    {
        public string text = "Source";
        public Color color = Color.white;
        public Vector3 worldOffset = new Vector3(0f, 0.8f, 0f);

        void OnGUI()
        {
            Camera camera = Camera.main;
            if (camera == null || string.IsNullOrEmpty(text))
                return;
            Vector3 point = camera.WorldToScreenPoint(transform.position + worldOffset);
            if (point.z < 0f)
                return;

            const float width = 280f;
            const float height = 36f;
            var rect = new Rect(point.x - width * 0.5f, Screen.height - point.y - height * 0.5f, width, height);
            LabGui.Fill(rect);

            GUIStyle style = LabGui.World;
            Color fill = LabGui.BrightFill(color);
            var shadow = new Rect(rect.x + 1.5f, rect.y + 1.5f, rect.width, rect.height);
            style.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
            GUI.Label(shadow, text, style);
            style.normal.textColor = fill;
            GUI.Label(rect, text, style);
        }
    }
}
