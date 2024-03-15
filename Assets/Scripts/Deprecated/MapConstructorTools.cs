using SeedTools;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MapConstructorTools
{
    /// <summary>
    /// Advanced class for building nice-looking world maps.
    /// </summary>
    public class MapConstructor
    {
        public const int ELEV_LAYER = 0; // Used by E-methods
        public const int TERR_LAYER = 1; // Used by T-methods

        private const int ELEV_LOW_BOUND = -3;
        private const int ELEV_LOW_APPROACH = -2;
        private const int ELEV_MID = 0;
        private const int ELEV_HIGH_APPROACH = 2;
        private const int ELEV_HIGH_BOUND = 3;

        public const int WATER = 0;
        public const int PLAINS = 1;
        public const int FOREST = 2;
        public const int MOUNTAIN = 3;
        public const int DESERT = 4;
        public const int ARID = 5;

        public delegate void Construct(MapConstructor constructor);

        private readonly int _s;
        private readonly int[,,] _map;
        private readonly Seed _seed;

        private MapConstructor(int size, Seed seed)
        {
            _s = size;
            _map = new int[size, size, 2];
            _seed = seed;
        }

        public static int[,] ExportTerr(int size, Seed seed, Construct construct)
            => Export(size, TERR_LAYER, seed, construct);

        public static int[,] ExportTopo(int size, Seed seed, Construct construct)
            => Export(size, ELEV_LAYER, seed, construct);

        private static int[,] Export(int size, int layer,
            Seed seed, Construct construct)
        {
            MapConstructor mc = new(size, seed);
            construct(mc);
            int[,] exportMap = new int[size, size];
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    exportMap[x, y] = mc._map[x, y, layer];

            return exportMap;
        }

        private static bool IsPercent(double val) => val >= 0 && val < 1;

        /// <summary>
        /// Forcibly sets all tiles in the map to a random elevation.
        /// </summary>
        public MapConstructor EBuildNoiseMap()
        {
            for (int x = 0; x < _s; x++)
                for (int y = 0; y < _s; y++)
                    _map[x, y, ELEV_LAYER] =
                        _seed.RangeRoll(ELEV_LOW_APPROACH, ELEV_HIGH_APPROACH + 1);

            return this;
        }

        /// <summary>
        /// Shatters the map into a variety of various continental plates. Plates may
        /// shift, creating mountains or valleys. Plates can also become oceanic,
        /// completely filling themselves with water.
        /// </summary>
        public MapConstructor ETectonicsProcedure(
            int plates, int borderThickness, double oceanRatio)
        {
            if (!(plates > 0 && borderThickness > 0 && IsPercent(oceanRatio)))
                throw new ArgumentException();

            int[,] elevMask = MapPlateFlooder.Create(
                _s, plates, borderThickness, oceanRatio,
                ELEV_LOW_BOUND, ELEV_LOW_APPROACH, ELEV_MID,
                ELEV_HIGH_APPROACH, ELEV_HIGH_BOUND, _seed);

            for (int x = 0; x < _s; x++)
                for (int y = 0; y < _s; y++)
                    _map[x, y, ELEV_LAYER] += elevMask[x, y];

            return this;
        }

        /// <summary>
        /// Sets each tile to the average elevation of all those which surround it.
        /// This will lead to a smoother map with less random valleys or plateaus
        /// everywhere (but still some). Extreme elevations are resistant to this 
        /// smoothing.
        /// </summary>
        public MapConstructor ESmoothingProcedure()
        {
            int targ, avg;
            int[] nearbys;
            double vars, sum;
            int[,,] refMap = (int[,,])_map.Clone();
            for (int x = 0; x < _s; x++)
                for (int y = 0; y < _s; y++)
                {
                    targ = refMap[x, y, ELEV_LAYER];
                    nearbys = refMap.GetNeighbours(ELEV_LAYER, x, y);
                    vars = 0;
                    sum = 0;
                    foreach (int i in nearbys)
                    {
                        vars++; sum += i;
                    }
                    avg = (int)Math.Round(sum / vars);
                    _map[x, y, ELEV_LAYER] =
                        targ >= ELEV_HIGH_APPROACH || targ <= ELEV_LOW_APPROACH
                        ? (int)(targ * 0.5d + avg * 0.5d) : avg;
                }

            return this;
        }

        /// <summary>
        /// Fills the terrain map using data from the elevation map. All elevations at
        /// or below sea level will fill with water and all at or above mountain level   
        /// will become mountain tiles.
        /// </summary>
        public MapConstructor TTileSettingProcedure(
            int seaLevel, int mountLevel)
        {
            if (!(seaLevel >= ELEV_LOW_BOUND && seaLevel <= ELEV_HIGH_BOUND))
                throw new ArgumentException();

            for (int x = 0; x < _s; x++)
                for (int y = 0; y < _s; y++)
                {
                    if (_map[x, y, ELEV_LAYER] <= seaLevel)
                        _map[x, y, TERR_LAYER] = WATER;
                    else if (_map[x, y, ELEV_LAYER] >= mountLevel)
                        _map[x, y, TERR_LAYER] = MOUNTAIN;
                    else
                        _map[x, y, TERR_LAYER] = PLAINS;
                }

            return this;
        }

        public MapConstructor TVegetationProcedure(
            double weatherDirection, double power)
        {
            if (!(IsPercent(weatherDirection) && IsPercent(power)))
                throw new ArgumentException();

            return this;
        }

        /// <summary>
        /// Dries a given proportion of water tiles into bare land tiles, errodes a
        /// given proportion of land tiles into water tiles, and repeats the process a 
        /// given number of times.
        /// </summary>
        public MapConstructor TLandSculptProcedure(
            int cycles, double dryPower, double errosionPower)
        {
            if (!(cycles >= 0 && IsPercent(dryPower) && IsPercent(errosionPower)))
                throw new ArgumentException();

            for (int cyc = 0; cyc < cycles; cyc++)
                for (int x = 0; x < _s; x++)
                    for (int y = 0; y < _s; y++)
                    {
                        if (_map[x, y, TERR_LAYER] == WATER)
                        {
                            if (!_seed.PerRoll(dryPower)) continue;
                            if (_map.HasFeature(x, y, LandFeatures.Feature.NEAR_LAND))
                                _map[x, y, TERR_LAYER] = PLAINS;
                        }
                        else
                        {
                            if (!_seed.PerRoll(errosionPower)) continue;
                            if (_map.HasFeature(x, y, LandFeatures.Feature.NEAR_WATER))
                                _map[x, y, TERR_LAYER] = WATER;
                        }
                    }

            return this;
        }

        public MapConstructor TRiverGenProcedure(
            double spawnRate)
        {
            if (!IsPercent(spawnRate)) throw new ArgumentException();

            return this;
        }

        /// <summary>
        /// Forces all tiles to be within the valid bounds of elevation.
        /// </summary>
        public MapConstructor EForceBoundsProcedure()
        {
            for (int x = 0; x < _s; x++)
                for (int y = 0; y < _s; y++)
                {
                    if (_map[x, y, ELEV_LAYER] < ELEV_LOW_BOUND)
                        _map[x, y, ELEV_LAYER] = ELEV_LOW_BOUND;
                    else if (_map[x, y, ELEV_LAYER] > ELEV_HIGH_BOUND)
                        _map[x, y, ELEV_LAYER] = ELEV_HIGH_BOUND;
                }

            return this;
        }

        /// <summary>
        /// Forces all elevations near the elevation bounds to be pushed directly to the
        /// bounds. This procedure only affects elevations witin valid bounds.
        /// </summary>
        public MapConstructor EPullElevationGaps()
        {
            for (int x = 0; x < _s; x++)
                for (int y = 0; y < _s; y++)
                {
                    if (_map[x, y, ELEV_LAYER] <= ELEV_LOW_APPROACH)
                        _map[x, y, ELEV_LAYER] = ELEV_LOW_BOUND;
                    else if (_map[x, y, ELEV_LAYER] >= ELEV_HIGH_APPROACH)
                        _map[x, y, ELEV_LAYER] = ELEV_HIGH_BOUND;
                }

            return this;
        }
    }

    /// <summary>
    /// Extension class containing procedures for building an elevation map with 
    /// distinct mountain ranges and valleys. The constructed map should be placed over 
    /// top of a pre-existing map like a mask, it does NOT have any other geographical 
    /// features.
    /// </summary>
    public static class MapPlateFlooder
    {
        private const int MAX_FLOOD_SEEDS = 64;

        private const double EDGE_SPREAD_POW = 0.8;
        private const double CORN_SPREAD_POW = 0.4;

        private const char NULL_ROOT = '\0';
        private const string ALL_ROOTS = // there are 64 available plates
            "abcdefghijklmnop" +
            "qrstuvwxyzABCDEF" +
            "GHIJKLMNOPQRSTUV" +
            "WXYZ1234567890<>";

        public static int[,] Create(
            int size, int roots, int plateShift, double oceanRatio,
            int low, int midLow, int mid, int midHigh, int high, Seed seed)
        {
            if (roots > MAX_FLOOD_SEEDS || roots < 0)
                throw new ArgumentOutOfRangeException();

            char[,] map = new char[size, size];
            for (int i = 0; i < roots; i++)
                map[seed.RangeRoll(size), seed.RangeRoll(size)] = ALL_ROOTS[i];

            char[,] refMap;
            bool hasNulls = true;
            while (hasNulls)
            {
                hasNulls = false;
                refMap = (char[,])map.Clone();
                for (int x = 0; x < size; x++)
                    for (int y = 0; y < size; y++)
                        if (refMap[x, y] != NULL_ROOT)
                            map.RootSpread(refMap[x, y], x, y, seed);
                        else hasNulls = true;
            }

            return map.ShiftAndSet(plateShift, roots, oceanRatio,
                low, midLow, mid, midHigh, high, seed);
        }

        private static void RootSpread(this char[,] grid,
            char root, int x, int y, Seed seed)
        {
            for (int xOS = -1; xOS <= 1; xOS++)
                for (int yOS = -1; yOS <= 1; yOS++)
                    try
                    {
                        if (grid[x + xOS, y + yOS] == NULL_ROOT)
                            if (
                            (Math.Abs(xOS) != Math.Abs(yOS)
                            && seed.PerRoll(EDGE_SPREAD_POW)) ||
                            (Math.Abs(xOS) == Math.Abs(yOS) &&
                            seed.PerRoll(CORN_SPREAD_POW))
                            )
                                grid[x + xOS, y + yOS] = root;
                    }
                    catch (IndexOutOfRangeException) { }
        }

        private static int[,] ShiftAndSet(this char[,] rootGrid,
            int maxShift, int maxRoots, double oceanRatio,
            int lowSet, int midLowSet, int midSet, int midHighSet, int highSet,
            Seed seed)
        {
            int size = rootGrid.GetLength(0);
            int[,] plateGrid = new int[size, size].Fill(lowSet);

            int cycs = maxRoots * 2; // one cycle per plate
            for (int root = 0; root < cycs; root++)
            {
                int sX = seed.RangeRoll(maxShift + 1);
                int sY = seed.RangeRoll(maxShift + 1);
                for (int x = 0; x < size; x++)
                    for (int y = 0; y < size; y++)
                        try
                        {
                            if (rootGrid[x, y] == ALL_ROOTS[root % maxRoots]
                                && root >= (int)(maxRoots * oceanRatio))
                            {
                                /*
                                 * This procedure will be CANCELLED for ocean
                                 * tiles during the first round.
                                 */
                                plateGrid.ForcePlateElev(x + sX, y + sY,
                                    lowSet, midLowSet,
                                    midSet,
                                    midHighSet, highSet);
                            }
                        }
                        catch (IndexOutOfRangeException) { }
            }

            return plateGrid;
        }

        private static void ForcePlateElev(this int[,] grid,
            int x, int y, int low, int midLow, int mid, int midHigh, int high)
        {
            int origin = grid[x, y];
            if (origin == low) grid[x, y] = midLow;
            else if (origin == midLow) grid[x, y] = mid;
            else if (origin == mid) grid[x, y] = midHigh;
            else if (origin == midHigh) grid[x, y] = high;
        }
    }

    /// <summary>
    /// Extension class with miscellaneous tools for exploring a grid map.
    /// </summary>
    public static class GridTools
    {
        private static readonly Vector2Int[] VECTS = {
            new(-1, -1), new(-1, 1), new(1, -1), new(1, 1),
            new(0, -1), new(0, 1), new(-1, 0), new(1, 0)
        };

        public static int[] GetNeighbours(this int[,,] map, int layer, int x, int y)
        {
            List<int> corners = new();
            for (int i = 0; i < VECTS.Length; i++)
                try { corners.Add(map[x + VECTS[i].x, y + VECTS[i].y, layer]); }
                catch (IndexOutOfRangeException) { }

            return corners.ToArray();
        }

        public static int[,] Fill(this int[,] grid, int filler)
        {
            for (int x = 0; x < grid.GetLength(0); x++)
                for (int y = 0; y < grid.GetLength(1); y++)
                    grid[x, y] = filler;

            return grid;
        }
    }

    /// <summary>
    /// Extension class for finding information about the features of a map around
    /// targeted grid tiles.
    /// </summary>
    public static class LandFeatures
    {
        public enum Feature
        {
            WATERLOCKED, // completed surrounded by water
            LANDLOCKED, // completed surrounded by land
            NEAR_LAND, // at least one nearby land tile 
            NEAR_WATER, // at least one nearby water tile
            PENINSULA, // at least 6 water tiles adjacent to a land tile
            GULF // at least 6 land tiles adjacent to a water tile
        }

        public static bool HasFeature(this int[,,] map,
            int x, int y, Feature feature)
        {
            int[] neighbourTiles = map.GetNeighbours(MapConstructor.TERR_LAYER, x, y);
            int targType = map[x, y, MapConstructor.TERR_LAYER];

            switch (feature)
            {
                case Feature.WATERLOCKED:
                    foreach (int t in neighbourTiles)
                        if (t != MapConstructor.WATER) return false;
                    return true;

                case Feature.LANDLOCKED:
                    foreach (int t in neighbourTiles)
                        if (t == MapConstructor.WATER) return false;
                    return true;

                case Feature.NEAR_WATER:
                    return !HasFeature(map, x, y, Feature.LANDLOCKED);

                case Feature.NEAR_LAND:
                    return !HasFeature(map, x, y, Feature.WATERLOCKED);

                case Feature.PENINSULA:
                    int adjWater = 0;
                    foreach (int t in neighbourTiles)
                        if (t == MapConstructor.WATER) adjWater++;
                    return targType != MapConstructor.WATER && adjWater >= 6;

                case Feature.GULF:
                    int adjLand = 0;
                    foreach (int t in neighbourTiles)
                        if (t != MapConstructor.WATER) adjLand++;
                    return targType == MapConstructor.WATER && adjLand >= 6;
            }
            return false;
        }
    }
}
