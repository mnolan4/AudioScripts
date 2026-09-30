using System;

namespace AudioFeatureLab
{
    /// <summary>
    /// Radix-2 discrete Fourier transform for analysis scripts.
    /// The input length must be a power of two.
    /// </summary>
    public static class SimpleFft
    {
        public static bool IsPowerOfTwo(int value)
        {
            return value >= 2 && (value & (value - 1)) == 0;
        }

        public static int ClampPowerOfTwo(int value, int min, int max)
        {
            value = Math.Max(min, Math.Min(max, value));
            int power = min;
            while ((power << 1) <= value && (power << 1) <= max)
                power <<= 1;
            return power;
        }

        public static void ApplyHann(float[] samples)
        {
            int n = samples.Length;
            if (n <= 1)
                return;
            float scale = (float)(2.0 * Math.PI / (n - 1));
            for (int i = 0; i < n; i++)
            {
                float window = 0.5f - 0.5f * (float)Math.Cos(scale * i);
                samples[i] *= window;
            }
        }

        /// <summary>
        /// In-place complex FFT. A negative exponent matches the usual analysis sign.
        /// </summary>
        public static void Forward(float[] real, float[] imag)
        {
            int n = real.Length;
            if (imag.Length != n || !IsPowerOfTwo(n))
                throw new ArgumentException("FFT size must be a power of two and both buffers must match.");

            int j = 0;
            for (int i = 0; i < n; i++)
            {
                if (i < j)
                {
                    Swap(real, i, j);
                    Swap(imag, i, j);
                }

                int half = n >> 1;
                while (half >= 1 && j >= half)
                {
                    j -= half;
                    half >>= 1;
                }

                j += half;
            }

            for (int size = 2; size <= n; size <<= 1)
            {
                int halfSize = size >> 1;
                double theta = -2.0 * Math.PI / size;
                double stepReal = Math.Cos(theta);
                double stepImag = Math.Sin(theta);
                for (int start = 0; start < n; start += size)
                {
                    double twiddleReal = 1.0;
                    double twiddleImag = 0.0;
                    for (int k = 0; k < halfSize; k++)
                    {
                        int even = start + k;
                        int odd = even + halfSize;
                        double oddReal = twiddleReal * real[odd] - twiddleImag * imag[odd];
                        double oddImag = twiddleReal * imag[odd] + twiddleImag * real[odd];
                        double evenReal = real[even];
                        double evenImag = imag[even];
                        real[even] = (float)(evenReal + oddReal);
                        imag[even] = (float)(evenImag + oddImag);
                        real[odd] = (float)(evenReal - oddReal);
                        imag[odd] = (float)(evenImag - oddImag);
                        double nextReal = twiddleReal * stepReal - twiddleImag * stepImag;
                        twiddleImag = twiddleReal * stepImag + twiddleImag * stepReal;
                        twiddleReal = nextReal;
                    }
                }
            }
        }

        /// <summary>
        /// Writes a one-sided magnitude spectrum. magnitudes.Length must be timeDomain.Length / 2.
        /// </summary>
        public static void MagnitudeSpectrum(float[] timeDomain, float[] magnitudes, bool hannWindow)
        {
            int n = timeDomain.Length;
            if (magnitudes.Length != n / 2)
                throw new ArgumentException("Magnitude buffer must be half the FFT size.");

            float[] real = new float[n];
            float[] imag = new float[n];
            Array.Copy(timeDomain, real, n);
            if (hannWindow)
                ApplyHann(real);
            Forward(real, imag);

            float norm = 2f / n;
            magnitudes[0] = Math.Abs(real[0]) / n;
            for (int i = 1; i < magnitudes.Length; i++)
                magnitudes[i] = (float)Math.Sqrt(real[i] * real[i] + imag[i] * imag[i]) * norm;
        }

        public static int PeakBin(float[] magnitudes, int ignoreBelow = 1)
        {
            int peak = ignoreBelow;
            float best = float.MinValue;
            for (int i = ignoreBelow; i < magnitudes.Length; i++)
            {
                if (magnitudes[i] > best)
                {
                    best = magnitudes[i];
                    peak = i;
                }
            }

            return peak;
        }

        static void Swap(float[] buffer, int a, int b)
        {
            float value = buffer[a];
            buffer[a] = buffer[b];
            buffer[b] = value;
        }
    }
}
