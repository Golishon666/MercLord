using System;
using MercLord.Game.Configs;
using MercLord.Global.Cells;

namespace MercLord.Global.Generation
{
    internal static class SphericalWorldNoise
    {
        public static float Noise(
            WorldSpherePoint point,
            int seed,
            int salt,
            int octaves,
            float baseFrequency,
            WorldNoiseSettings settings = null)
        {
            settings ??= WorldNoiseSettings.Default;
            var warpFrequency = settings.DomainWarpFrequency;
            var warpStrength = settings.DomainWarpStrength;
            var warpX = ValueNoise3D(point.X * warpFrequency + 13.1, point.Y * warpFrequency - 7.3, point.Z * warpFrequency + 5.9, seed, salt + 311);
            var warpY = ValueNoise3D(point.X * warpFrequency - 3.7, point.Y * warpFrequency + 17.1, point.Z * warpFrequency - 11.4, seed, salt + 719);
            var warpZ = ValueNoise3D(point.X * warpFrequency + 23.8, point.Y * warpFrequency + 2.6, point.Z * warpFrequency - 19.2, seed, salt + 1103);
            var x = point.X + (warpX - 0.5) * warpStrength;
            var y = point.Y + (warpY - 0.5) * warpStrength;
            var z = point.Z + (warpZ - 0.5) * warpStrength;
            var amplitude = 1.0;
            var totalAmplitude = 0.0;
            var value = 0.0;
            var frequency = (double)baseFrequency;
            for (var octave = 0; octave < octaves; octave++)
            {
                value += ValueNoise3D(x * frequency, y * frequency, z * frequency, seed, salt + octave * 193) * amplitude;
                totalAmplitude += amplitude;
                amplitude *= settings.OctavePersistence;
                frequency *= settings.OctaveLacunarity;
            }

            return Clamp01((float)(value / totalAmplitude));
        }

        public static float Hash01(int seed, int a, int b)
        {
            unchecked
            {
                var hash = seed;
                hash = hash * 397 ^ a;
                hash = hash * 397 ^ b;
                hash ^= hash << 13;
                hash ^= hash >> 17;
                hash ^= hash << 5;
                return (hash & 0x7fffffff) / (float)int.MaxValue;
            }
        }

        private static double ValueNoise3D(double x, double y, double z, int seed, int salt)
        {
            var x0 = FastFloor(x);
            var y0 = FastFloor(y);
            var z0 = FastFloor(z);
            var tx = Smooth(x - x0);
            var ty = Smooth(y - y0);
            var tz = Smooth(z - z0);

            var x00 = Lerp(Hash01(seed, salt, x0, y0, z0), Hash01(seed, salt, x0 + 1, y0, z0), tx);
            var x10 = Lerp(Hash01(seed, salt, x0, y0 + 1, z0), Hash01(seed, salt, x0 + 1, y0 + 1, z0), tx);
            var x01 = Lerp(Hash01(seed, salt, x0, y0, z0 + 1), Hash01(seed, salt, x0 + 1, y0, z0 + 1), tx);
            var x11 = Lerp(Hash01(seed, salt, x0, y0 + 1, z0 + 1), Hash01(seed, salt, x0 + 1, y0 + 1, z0 + 1), tx);
            var y0Value = Lerp(x00, x10, ty);
            var y1Value = Lerp(x01, x11, ty);
            return Lerp(y0Value, y1Value, tz);
        }

        private static int FastFloor(double value)
        {
            var integer = (int)value;
            return value < integer ? integer - 1 : integer;
        }

        private static double Smooth(double value)
        {
            return value * value * value * (value * (value * 6.0 - 15.0) + 10.0);
        }

        private static double Lerp(double from, double to, double t)
        {
            return from + (to - from) * t;
        }

        private static float Hash01(int seed, int salt, int x, int y, int z)
        {
            unchecked
            {
                var hash = seed;
                hash = hash * 397 ^ salt;
                hash = hash * 397 ^ x;
                hash = hash * 397 ^ y;
                hash = hash * 397 ^ z;
                hash ^= hash << 13;
                hash ^= hash >> 17;
                hash ^= hash << 5;
                return (hash & 0x7fffffff) / (float)int.MaxValue;
            }
        }

        private static float Clamp01(float value)
        {
            if (value <= 0f)
            {
                return 0f;
            }

            return value >= 1f ? 1f : value;
        }
    }
}
