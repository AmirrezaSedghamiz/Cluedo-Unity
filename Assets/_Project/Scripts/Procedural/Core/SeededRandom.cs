using System;
using System.Collections.Generic;

namespace TomorrowsCrime.Procedural.Core
{
    /// <summary>
    /// Deterministic, platform-independent Pseudo-Random Number Generator based on SplitMix64/Xoshiro256.
    /// Provides isolated sub-streams for layout, NPCs, objects, scenarios, and cosmetics
    /// to guarantee that changes in one domain do not contaminate others.
    /// </summary>
    public class SeededRandom
    {
        private ulong _state0;
        private ulong _state1;
        private ulong _state2;
        private ulong _state3;

        public int Seed { get; }

        public SeededRandom(int seed)
        {
            Seed = seed;
            InitializeWithSeed((ulong)seed);
        }

        public SeededRandom(ulong seed64)
        {
            Seed = (int)(seed64 & 0x7FFFFFFF);
            InitializeWithSeed(seed64);
        }

        private void InitializeWithSeed(ulong seed)
        {
            // Use SplitMix64 to initialize state vectors
            ulong smState = seed != 0 ? seed : 0x853c49e6748fea9bUL;
            _state0 = SplitMix64(ref smState);
            _state1 = SplitMix64(ref smState);
            _state2 = SplitMix64(ref smState);
            _state3 = SplitMix64(ref smState);
        }

        private static ulong SplitMix64(ref ulong state)
        {
            ulong z = (state += 0x9e3779b97f4a7c15UL);
            z = (z ^ (z >> 30)) * 0xbf58476d1ce4e5b9UL;
            z = (z ^ (z >> 27)) * 0x94d049bb133111ebUL;
            return z ^ (z >> 31);
        }

        public ulong NextULong()
        {
            // Xoshiro256** implementation
            ulong result = RotateLeft(_state1 * 5, 7) * 9;
            ulong t = _state1 << 17;

            _state2 ^= _state0;
            _state3 ^= _state1;
            _state1 ^= _state2;
            _state0 ^= _state3;

            _state2 ^= t;
            _state3 = RotateLeft(_state3, 45);

            return result;
        }

        private static ulong RotateLeft(ulong x, int k)
        {
            return (x << k) | (x >> (64 - k));
        }

        public double NextDouble()
        {
            return (NextULong() >> 11) * (1.0 / (1UL << 53));
        }

        public float NextFloat(float minInclusive = 0f, float maxInclusive = 1f)
        {
            if (minInclusive >= maxInclusive) return minInclusive;
            return minInclusive + (float)NextDouble() * (maxInclusive - minInclusive);
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (minInclusive >= maxExclusive) return minInclusive;
            uint range = (uint)(maxExclusive - minInclusive);
            ulong randomWord = NextULong();
            uint scaled = (uint)((randomWord & 0xFFFFFFFF) % range);
            return minInclusive + (int)scaled;
        }

        public bool NextBool(float trueProbability = 0.5f)
        {
            return NextDouble() < trueProbability;
        }

        public T NextItem<T>(IReadOnlyList<T> list)
        {
            if (list == null || list.Count == 0) return default;
            int idx = NextInt(0, list.Count);
            return list[idx];
        }

        public void Shuffle<T>(IList<T> list)
        {
            if (list == null || list.Count <= 1) return;
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = NextInt(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// Derives a deterministic isolated sub-stream for a specific domain.
        /// </summary>
        public SeededRandom CreateSubStream(string domainName)
        {
            ulong hash = 14695981039346656037UL; // FNV-1a 64-bit offset basis
            hash ^= (ulong)Seed;
            hash *= 1099511628211UL;

            foreach (char c in domainName)
            {
                hash ^= c;
                hash *= 1099511628211UL;
            }

            return new SeededRandom(hash);
        }
    }

    /// <summary>
    /// Master investigation seed encapsulating domain streams.
    /// </summary>
    public class InvestigationSeed
    {
        public int SeedValue { get; }
        public string SeedCode { get; }
        public DateTime GeneratedAt { get; }

        public SeededRandom LayoutStream { get; }
        public SeededRandom NPCStream { get; }
        public SeededRandom ObjectStream { get; }
        public SeededRandom ScenarioStream { get; }
        public SeededRandom CosmeticStream { get; }

        public InvestigationSeed(int seedValue)
        {
            SeedValue = seedValue;
            SeedCode = $"TC-{Math.Abs(seedValue):X8}";
            GeneratedAt = DateTime.UtcNow;

            var rootRng = new SeededRandom(seedValue);
            LayoutStream = rootRng.CreateSubStream("Layout");
            NPCStream = rootRng.CreateSubStream("NPCs");
            ObjectStream = rootRng.CreateSubStream("Objects");
            ScenarioStream = rootRng.CreateSubStream("Scenario");
            CosmeticStream = rootRng.CreateSubStream("Cosmetics");
        }

        public static InvestigationSeed GenerateRandom()
        {
            int seed = unchecked((int)DateTime.UtcNow.Ticks ^ System.Diagnostics.Process.GetCurrentProcess().Id);
            return new InvestigationSeed(seed);
        }

        public static InvestigationSeed FromCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return GenerateRandom();

            string clean = code.Trim().ToUpper();
            if (clean.StartsWith("TC-")) clean = clean[3..];

            if (int.TryParse(clean, System.Globalization.NumberStyles.HexNumber, null, out int seedVal))
            {
                return new InvestigationSeed(seedVal);
            }

            // Fallback: hash the string
            int hash = 17;
            foreach (char c in code)
            {
                hash = hash * 31 + c;
            }
            return new InvestigationSeed(hash);
        }

        public override string ToString() => SeedCode;
    }
}
