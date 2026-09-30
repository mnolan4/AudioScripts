using UnityEngine;

namespace AudioFeatureLab
{
    /// <summary>
    /// Shared overlay type: large size and a near-white fill on a dark panel.
    /// </summary>
    public static class LabGui
    {
        public static readonly Color Text = new Color(0.98f, 0.99f, 1f, 1f);
        public static readonly Color Panel = new Color(0.03f, 0.04f, 0.06f, 0.94f);
        public static readonly Color ButtonFace = new Color(0.14f, 0.16f, 0.2f, 1f);

        static Texture2D panelTexture;
        static Texture2D buttonTexture;
        static GUIStyle titleStyle;
        static GUIStyle bodyStyle;
        static GUIStyle buttonStyle;
        static GUIStyle worldStyle;
        static GUIStyle panelStyle;

        public static GUIStyle Title
        {
            get
            {
                Ensure();
                return titleStyle;
            }
        }

        public static GUIStyle Body
        {
            get
            {
                Ensure();
                return bodyStyle;
            }
        }

        public static GUIStyle Button
        {
            get
            {
                Ensure();
                return buttonStyle;
            }
        }

        public static GUIStyle World
        {
            get
            {
                Ensure();
                return worldStyle;
            }
        }

        public static GUIStyle PanelStyle
        {
            get
            {
                Ensure();
                return panelStyle;
            }
        }

        public static void Fill(Rect rect)
        {
            Ensure();
            Color previous = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(rect, panelTexture);
            GUI.color = previous;
        }

        public static Color BrightFill(Color color)
        {
            float max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            if (max < 0.02f)
                return Text;
            Color lifted = new Color(color.r / max, color.g / max, color.b / max, 1f);
            return Color.Lerp(lifted, Text, 0.28f);
        }

        static void Ensure()
        {
            if (titleStyle != null)
                return;

            panelTexture = MakeTexture(Panel);
            buttonTexture = MakeTexture(ButtonFace);

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 36,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                richText = true,
                padding = new RectOffset(4, 4, 2, 8)
            };
            Paint(titleStyle, Text);

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Normal,
                wordWrap = true,
                richText = true,
                padding = new RectOffset(4, 4, 2, 4)
            };
            Paint(bodyStyle, Text);

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(18, 18, 10, 10),
                wordWrap = true
            };
            buttonStyle.normal.background = buttonTexture;
            buttonStyle.hover.background = buttonTexture;
            buttonStyle.active.background = buttonTexture;
            buttonStyle.focused.background = buttonTexture;
            Paint(buttonStyle, Text);

            worldStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
            Paint(worldStyle, Text);

            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = panelTexture;
        }

        static void Paint(GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
            style.onNormal.textColor = color;
            style.onHover.textColor = color;
            style.onActive.textColor = color;
            style.onFocused.textColor = color;
        }

        static Texture2D MakeTexture(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
