using System;
using System.Collections.Generic;
using UnityEngine;

namespace AudioFeatureLab
{
    /// <summary>
    /// One definable analysis range. <see cref="level"/> is the public 0-1 output for this range.
    /// </summary>
    [Serializable]
    public class FrequencyBand
    {
        public string name = "Band";
        public float minHz = 20f;
        public float maxHz = 20000f;
        [Range(0.25f, 40f)] public float boost = 1f;

        [Range(0f, 1f)] public float level;
        public float energy;
    }

    public static class SpectrumBands
    {
        public static List<FrequencyBand> CreateDefaults()
        {
            return new List<FrequencyBand>
            {
                Band("Sub", 20f, 60f, 2.4f),
                Band("Bass", 60f, 250f, 1.8f),
                Band("Low Mid", 250f, 500f, 1.6f),
                Band("Mid", 500f, 2000f, 1.5f),
                Band("High Mid", 2000f, 6000f, 1.7f),
                Band("High", 6000f, 12000f, 2.2f),
                Band("Air", 12000f, 20000f, 3f)
            };
        }

        public static float BinFrequency(int bin, int binCount, int sampleRate)
        {
            if (binCount <= 1)
                return 0f;
            return bin * (sampleRate * 0.5f) / binCount;
        }

        public static float BandEnergy(float[] magnitudes, int sampleRate, float minHz, float maxHz)
        {
            if (magnitudes == null || magnitudes.Length == 0)
                return 0f;
            if (maxHz < minHz)
            {
                float swap = minHz;
                minHz = maxHz;
                maxHz = swap;
            }

            double sum = 0.0;
            int count = 0;
            for (int i = 0; i < magnitudes.Length; i++)
            {
                float hz = BinFrequency(i, magnitudes.Length, sampleRate);
                if (hz < minHz || hz >= maxHz)
                    continue;
                float magnitude = magnitudes[i];
                sum += magnitude * magnitude;
                count++;
            }

            if (count == 0)
                return 0f;
            return (float)Math.Sqrt(sum);
        }

        /// <summary>
        /// Maps a linear magnitude into a stable 0-1 level. Full-scale narrow tones land high; silence stays near 0.
        /// </summary>
        public static float NormalizeEnergy(float energy, float boost)
        {
            float shaped = Mathf.Log10(1f + Mathf.Max(0f, energy) * 18f * Mathf.Max(0.01f, boost));
            return Mathf.Clamp01(shaped / 1.35f);
        }

        static FrequencyBand Band(string name, float minHz, float maxHz, float boost)
        {
            return new FrequencyBand
            {
                name = name,
                minHz = minHz,
                maxHz = maxHz,
                boost = boost
            };
        }
    }
}
