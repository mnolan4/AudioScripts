using UnityEngine;

namespace AudioFeatureLab
{
    /// <summary>
    /// Draws the AudioSource min and max distance on the ground so rolloff is visible while you walk.
    /// </summary>
    [AddComponentMenu("Audio Feature Lab/Distance Rings")]
    public class DistanceRing : MonoBehaviour
    {
        public AudioSource source;
        public int segments = 64;
        public float yOffset = 0.05f;

        LineRenderer minRing;
        LineRenderer maxRing;

        void Start()
        {
            if (source == null)
                source = GetComponent<AudioSource>();
            minRing = CreateRing("Min Distance", new Color(0.35f, 0.95f, 0.55f, 0.9f));
            maxRing = CreateRing("Max Distance", new Color(0.95f, 0.45f, 0.25f, 0.55f));
        }

        void LateUpdate()
        {
            if (source == null)
                return;
            Draw(minRing, source.minDistance);
            bool showMax = source.maxDistance < 80f;
            maxRing.enabled = showMax;
            if (showMax)
                Draw(maxRing, source.maxDistance);
        }

        LineRenderer CreateRing(string label, Color color)
        {
            var child = new GameObject(label);
            child.transform.SetParent(transform, false);
            var line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.widthMultiplier = 0.04f;
            line.positionCount = Mathf.Max(8, segments);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                line.material = new Material(shader);
                line.startColor = color;
                line.endColor = color;
            }

            return line;
        }

        void Draw(LineRenderer line, float radius)
        {
            int count = line.positionCount;
            Vector3 center = transform.position;
            center.y = yOffset;
            for (int i = 0; i < count; i++)
            {
                float angle = (Mathf.PI * 2f * i) / count;
                line.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
        }
    }
}
