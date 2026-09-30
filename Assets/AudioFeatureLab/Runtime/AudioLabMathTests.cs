using System;
using UnityEngine;

namespace AudioFeatureLab
{
    public static class AudioLabMathTests
    {
        public static void RunOrThrow()
        {
            AudioUnits.CrossfadeGains(0f, CrossfadeLaw.EqualPower, out float a0, out float b0);
            AudioUnits.CrossfadeGains(1f, CrossfadeLaw.EqualPower, out float a1, out float b1);
            AudioUnits.CrossfadeGains(0.5f, CrossfadeLaw.EqualPower, out float aMid, out float bMid);
            if (Mathf.Abs(a0 - 1f) > 0.001f || b0 > 0.001f || a1 > 0.001f || Mathf.Abs(b1 - 1f) > 0.001f)
                throw new InvalidOperationException("Equal-power endpoints are wrong.");
            if (Mathf.Abs(aMid - bMid) > 0.001f || aMid < 0.6f)
                throw new InvalidOperationException("Equal-power midpoint should be about 0.707 on each side.");

            const int rate = 48000;
            const int size = 4096;
            const float tone = 440f;
            float[] time = new float[size];
            for (int i = 0; i < size; i++)
                time[i] = Mathf.Sin(2f * Mathf.PI * tone * i / rate);
            float[] magnitudes = new float[size / 2];
            SimpleFft.MagnitudeSpectrum(time, magnitudes, true);
            int peak = SimpleFft.PeakBin(magnitudes);
            float peakHz = SpectrumBands.BinFrequency(peak, magnitudes.Length, rate);
            if (Mathf.Abs(peakHz - tone) > (rate * 0.5f / magnitudes.Length) * 1.6f)
                throw new InvalidOperationException("FFT peak was " + peakHz.ToString("0.0") + " Hz, expected near 440.");

            float mid = SpectrumBands.BandEnergy(magnitudes, rate, 300f, 600f);
            float bass = SpectrumBands.BandEnergy(magnitudes, rate, 20f, 80f);
            if (mid < bass * 4f)
                throw new InvalidOperationException("FFT energy did not concentrate in the 440 Hz band.");
            if (SpectrumBands.NormalizeEnergy(mid, 1.5f) < 0.45f)
                throw new InvalidOperationException("Band normalizer was too quiet for a full-scale sine.");
            if (SpectrumBands.NormalizeEnergy(0f, 1f) > 0.001f)
                throw new InvalidOperationException("Silence should normalize to 0.");

            float db = AudioUnits.LinearToDecibels(0.5f);
            if (Mathf.Abs(db - (-6.0206f)) > 0.05f)
                throw new InvalidOperationException("Decibel conversion failed: " + db);

            Debug.Log("Audio feature lab math self-test passed.");
        }
    }
}
