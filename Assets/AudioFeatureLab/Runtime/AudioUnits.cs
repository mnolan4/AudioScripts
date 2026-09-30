using UnityEngine;

namespace AudioFeatureLab
{
    public enum CrossfadeLaw
    {
        EqualPower = 0,
        Linear = 1
    }

    /// <summary>
    /// Conversions and gain laws shared by the audio lab scripts.
    /// </summary>
    public static class AudioUnits
    {
        public const float SilenceDecibels = -80f;

        public static float LinearToDecibels(float linear)
        {
            if (linear <= 0.000001f)
                return SilenceDecibels;
            return Mathf.Clamp(20f * Mathf.Log10(linear), SilenceDecibels, 20f);
        }

        public static float DecibelsToLinear(float decibels)
        {
            if (decibels <= SilenceDecibels)
                return 0f;
            return Mathf.Pow(10f, decibels / 20f);
        }

        public static void CrossfadeGains(float blend, CrossfadeLaw law, out float gainA, out float gainB)
        {
            blend = Mathf.Clamp01(blend);
            if (law == CrossfadeLaw.Linear)
            {
                gainA = 1f - blend;
                gainB = blend;
                return;
            }

            float angle = blend * Mathf.PI * 0.5f;
            gainA = Mathf.Cos(angle);
            gainB = Mathf.Sin(angle);
        }

        /// <summary>
        /// One-pole smoother. Attack and release times are time constants in seconds.
        /// </summary>
        public static float Ballistic(float current, float target, float attackSeconds, float releaseSeconds, float deltaTime)
        {
            float seconds = target > current ? attackSeconds : releaseSeconds;
            if (seconds <= 0.0001f)
                return target;
            float coefficient = 1f - Mathf.Exp(-deltaTime / seconds);
            return Mathf.Lerp(current, target, coefficient);
        }
    }
}
