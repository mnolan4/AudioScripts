using UnityEngine;

namespace AudioFeatureLab
{
    /// <summary>
    /// Scales a row of bars from FrequencyBand.level.
    /// </summary>
    [AddComponentMenu("Audio Feature Lab/Band Bar Visualizer")]
    public class BandBarVisualizer : MonoBehaviour
    {
        public FrequencyBandAnalyzer analyzer;
        public float barWidth = 0.38f;
        public float barDepth = 0.38f;
        public float maxHeight = 4.2f;
        public float gap = 0.18f;

        Transform[] bars;
        int builtCount = -1;

        void LateUpdate()
        {
            if (analyzer == null || analyzer.bands == null)
                return;
            if (bars == null || builtCount != analyzer.bands.Count)
                Build();
            float origin = -((analyzer.bands.Count - 1) * (barWidth + gap)) * 0.5f;
            for (int i = 0; i < analyzer.bands.Count; i++)
            {
                Transform bar = bars[i];
                float level = Mathf.Clamp01(analyzer.bands[i].level);
                float height = 0.08f + level * maxHeight;
                var scale = bar.localScale;
                scale.x = barWidth;
                scale.y = height;
                scale.z = barDepth;
                bar.localScale = scale;
                bar.localPosition = new Vector3(origin + i * (barWidth + gap), height * 0.5f, 0f);
            }
        }

        void Build()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
            builtCount = analyzer.bands.Count;
            bars = new Transform[builtCount];
            for (int i = 0; i < builtCount; i++)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = analyzer.bands[i].name;
                if (cube.TryGetComponent<Collider>(out Collider collider))
                    Destroy(collider);
                cube.transform.SetParent(transform, false);
                var renderer = cube.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = new Material(Shader.Find("Standard"));
                    renderer.material.color = Color.HSVToRGB((float)i / Mathf.Max(1, builtCount), 0.65f, 0.95f);
                }

                var label = cube.AddComponent<WorldLabel>();
                label.text = analyzer.bands[i].name;
                label.worldOffset = new Vector3(0f, 0.45f, 0f);
                bars[i] = cube.transform;
            }
        }
    }
}
