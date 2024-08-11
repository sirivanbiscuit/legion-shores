using System;

namespace SeedTools
{
    public class Seed
    {
        public const int SEED_LOW = 0;
        public const int SEED_HIGH = 999999999;

        private readonly int _val;
        private readonly System.Random _rng;

        private static readonly System.Random defRng = new();

        /// <summary>
        /// Creates a RNG with the given seed
        /// </summary>
        public Seed(int val)
        {
            if (val < SEED_LOW || val > SEED_HIGH)
                throw new ArgumentOutOfRangeException("Not a valid seed");

            _val = val;
            _rng = new(val);
        }

        /// <summary>
        /// Creates a RNG with a random seed
        /// </summary>
        public Seed() : this(defRng.Next(SEED_LOW, SEED_HIGH + 1)) { }

        /// <summary>
        /// Get the seed value
        /// </summary>
        public int Get() => _val;

        /// <summary>
        /// Generates a random integer between the two bounds,
        /// inclusive of both bounds: [low, high]
        /// </summary>
        public int RangeRoll(int low, int high) => _rng.Next(low, high+1);

        /// <summary>
        /// Generates a random integer between 0 and the upper bound,
        /// inclusive of both: [0, high]
        /// </summary>
        public int RangeRoll(int high) => RangeRoll(0, high);

        /// <summary>
        /// Returns true with the given probability
        /// </summary>
        public bool PerRoll(double chance) => PerRaw() < chance;

        /// <summary>
        /// Generates a random fraction within bounds [0,1)
        /// </summary>
        public double PerRaw() => _rng.NextDouble();

    }
}

