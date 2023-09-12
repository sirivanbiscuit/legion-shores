using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MapSeedTools
{
    public class Seed
    {
        public const int SEED_LOW = 0;
        public const int SEED_HIGH = 999999999;

        private readonly int _val;
        private readonly System.Random _rng;

        public Seed(int val)
        {
            if (val < SEED_LOW || val > SEED_HIGH)
                throw new ArgumentOutOfRangeException();

            _val = val;
            _rng = new(val);
        }

        public int Get() => _val;

        public int RangeRoll(int low, int high) => _rng.Next(low, high);

        public int RangeRoll(int high) => RangeRoll(0, high);

        public bool PerRoll(double chance) => PerRaw() < chance;

        public double PerRaw() => _rng.NextDouble();
    }
}
