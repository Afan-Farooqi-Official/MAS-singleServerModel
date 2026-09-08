using System;

namespace KfcQueueSim
{
    /// <summary>
    /// Handles random variate generation using Inverse-Transform Sampling.
    /// Reproducible via fixed seed.
    /// </summary>
    public class RandomGen
    {
        private readonly Random _rng;

        public RandomGen(int seed)
        {
            _rng = new Random(seed);
        }

        public RandomGen()
        {
            _rng = new Random(42);
        }

        /// <summary>
        /// Generates a standard uniform variate U in (0, 1).
        /// </summary>
        public double NextUniform()
        {
            double u = _rng.NextDouble();
            while (u <= 0.0 || u >= 1.0)
            {
                u = _rng.NextDouble();
            }
            return u;
        }

        /// <summary>
        /// Generates an Exponential variate with specified mean (E[X] = 1/lambda)
        /// using Inverse-Transform Sampling:
        /// CDF: F(x) = 1 - exp(-x / mean) = U
        /// Inverse: X = -mean * ln(1 - U)
        /// </summary>
        public double NextExponential(double mean)
        {
            if (mean <= 0) return 0.0;
            double u = NextUniform();
            return -mean * Math.Log(u);
        }
    }
}
