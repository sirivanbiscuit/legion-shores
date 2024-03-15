using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace WorldBuilderTools
{
    class WorldBuilderTools { }

    /// <summary>
    /// Procedural presets for quickly generating a terrain map.                                
    /// </summary>
    public enum QuickPreset
    {
        NOISE, CONTINENTS
    }

    /// <summary>
    /// Extenstion class for getting a buldier from a QuickPreset.
    /// </summary>
    public static class QuickPresetAdapter
    {
        public static BuilderPreset Build(this QuickPreset preset)
        {
            switch (preset)
            {
                case QuickPreset.NOISE:
                    return (builder) =>
                    {
                        builder.PreGenNoise(0.4d, false);
                        return;
                    };
                case QuickPreset.CONTINENTS:
                    return (builder) =>
                    {
                        builder.PreGenContinents(
                            (int)Math.Pow(builder.Size() / 25d, 1.5));
                        builder.GenLandProcedure(
                            GenProcedure.DRY, 75 + builder.Size() / 8, 0.1d);
                        builder.GenLandProcedure(
                            GenProcedure.ERODE, (75 + builder.Size() / 8) / 5, 0.1d);
                    };
            }
            return null;
        }
    }

    /// <summary>
    /// Utilitiy enum for various forms of terrain generation.
    /// </summary>
    public enum LandCondition
    {
        WATERLOCKED, // completed surrounded by water
        LANDLOCKED, // completed surrounded by land
        NEAR_LAND, // at least one nearby land tile 
        NEAR_WATER, // at least one nearby water tile
        PENINSULA, // at least 6 water tiles adjacent to a land tile
        GULF // at least 6 land tiles adjacent to a water tile
    }

    /// <summary>
    /// Utility enum for various forms of terrain generation.
    /// </summary>
    public enum GenProcedure
    {
        DRY, // lower sea level to create more land
        ERODE // raise sea level to create more water
    }

    /// <summary>
    /// For creating more advanced presets to run map build.
    /// </summary>
    public delegate void BuilderPreset(WorldBuilder builder);

    /// <summary>
    /// Class that for converting map data to/from JSON data.
    /// </summary>
    public class TerrainEntryMap
    {
        public int MapLength;
        public int[] TerrainEntries;

        public TerrainEntryMap(int[,] map, int mapLength)
        {
            MapLength = mapLength;
            TerrainEntries = new int[mapLength * mapLength];
            for (int x = 0; x < mapLength; x++)
                for (int y = 0; y < mapLength; y++)
                    TerrainEntries[GridTools.GridIdFromXY(x, y, mapLength)] = map[x, y];
        }

        public TerrainEntryMap() { }
    }

    /// <summary>
    /// Misc tools for exploring the terrain grid.
    /// </summary>
    public static class GridTools
    {
        public static int XFromGridId(int gridId, int gridLength)
        {
            return gridId % gridLength;
        }

        public static int YFromGridId(int gridId, int gridLength)
        {
            return (gridId - (gridId % gridLength)) / gridLength;
        }

        public static int GridIdFromXY(int x, int y, int gridLength)
        {
            return x + (gridLength * y);
        }

        public static int[] GetNeighbourTiles(int[,] map, int x, int y)
        {
            ArrayList nearbyTiles = new ArrayList();
            for (int placeX = x - 1; placeX <= x + 1; placeX++)
                for (int placeY = y - 1; placeY <= y + 1; placeY++)
                    try
                    {
                        if (!(placeX == x && placeY == y))
                            nearbyTiles.Add(map[placeX, placeY]);
                    }
                    catch (IndexOutOfRangeException) { }

            // return a simpler array
            int[] nT = new int[nearbyTiles.Count];
            for (int i = 0; i < nearbyTiles.Count; i++)
                nT[i] = (int)nearbyTiles[i];
            return nT;

        }
    }

    /// <summary>
    /// Builder for dynamic terrain maps.
    /// </summary>
    public class WorldBuilder
    {
        /* 
         * IMPORTANT NOTE
         * 
         * The WorldBuilder does NOT draw on any tilemap nor does it know of any tile or 
         * map objects. Additionally it does not extend Mono, so it should ideally be 
         * used for merely calculating an abstract world map and printing bare bones 
         * terrain data to a target JSON file.
         * 
         * The TerrainPlacer class has tools for actually drawing a map onto the game 
         * screen. Testing of map generation should ideally be done in the context menu 
         * of a TerrainPlacer game object.
         * 
         * All builder functions create a 2D integer array to represent terrain in an 
         * abstract form. The following integers represent the following biome/terrain 
         * tiles:
         * - 0: ocean
         * - 1: plains
         * - 2: forest
         * - 3: mountain
         */

        public const int OCEAN = 0;
        public const int PLAINS = 1;
        public const int FOREST = 2;
        public const int MOUNTAIN = 3;
        public const int DESERT = 4;

        // This should be equal to the number of terrain types above:
        public const int NUM_TYPES = 5;

        int[,,] list = { { { 1} } };

        private static readonly System.Random rng = new();
        private int RandomTile() { return rng.Next(NUM_TYPES); }
        private int RandomLandTile() { return rng.Next(1, NUM_TYPES); }

        // Try to make sure _length==_buildmap.Length at ALL times.
        private int _length;
        private int[,] _buildMap;

        public WorldBuilder(BuilderPreset preset, int length)
        {
            _length = length;
            _buildMap = new int[_length, _length];
            preset(this);
        }

        public int Size() { return _length; }

        public void SaveToJSON(string path)
        {
            File.WriteAllText(
                path, JsonUtility.ToJson(new TerrainEntryMap(_buildMap, _length))
            );
        }

        public static bool IsMapCondition(
            int[] neighbourTiles, int targetType, LandCondition condition)
        {
            switch (condition)
            {
                // completed surrounded by water
                case LandCondition.WATERLOCKED:
                    foreach (int t in neighbourTiles) if (t != OCEAN) return false;
                    return true;

                // completed surrounded by land
                case LandCondition.LANDLOCKED:
                    foreach (int t in neighbourTiles) if (t == OCEAN) return false;
                    return true;

                // at least one nearby water tile
                case LandCondition.NEAR_WATER:
                    return !IsMapCondition(
                        neighbourTiles, targetType, LandCondition.LANDLOCKED);

                // at least one nearby land tile
                case LandCondition.NEAR_LAND:
                    return !IsMapCondition(
                        neighbourTiles, targetType, LandCondition.WATERLOCKED);

                // at least 6 water tiles adjacent to a land tile
                case LandCondition.PENINSULA:
                    int adjWater = 0;
                    foreach (int t in neighbourTiles)
                        adjWater += (t == OCEAN) ? 1 : 0;
                    return targetType != OCEAN && adjWater >= 6;

                // at least 6 land tiles adjacent to a water tile
                case LandCondition.GULF:
                    int adjLand = 0;
                    foreach (int t in neighbourTiles)
                        adjLand += (t != OCEAN) ? 1 : 0;
                    return targetType == OCEAN && adjLand >= 6;
            }
            return false;
        }

        /// <summary>
        /// Fills the map will an empty ocean.
        /// </summary>
        public void PreGenOcean()
        {
            for (int x = 0; x < _length; x++)
                for (int y = 0; y < _length; y++) { _buildMap[x, y] = OCEAN; }
        }

        /// <summary>
        /// Fills the map will an empty ocean and places a given number of "root" plains 
        /// tiles randomly spread around it. Similar to a noise map, except the exact 
        /// amount of tiles is given rather than the percentage of total tiles.
        /// </summary>
        public void PreGenContinents(int roots)
        {
            PreGenOcean();
            for (int i = 0; i < roots; i++)
                _buildMap[rng.Next(_length), rng.Next(_length)] = PLAINS;
        }

        /// <summary>
        /// Fills the map will an empty ocean, then fills a given percentage
        /// of the map area will dry land tiles. 
        /// </summary>
        public void PreGenNoise(double noise, bool randTiles)
        {
            PreGenOcean();
            for (int x = 0; x < _length; x++)
                for (int y = 0; y < _length; y++)
                    if (rng.NextDouble() < noise)
                        _buildMap[x, y] = randTiles ? RandomLandTile() : PLAINS;
        }

        public void GenLandProcedure(GenProcedure procedure, int cycles, double power)
        {
            int[,] refMap = (int[,])_buildMap.Clone();
            for (int x = 0; x < _length; x++)
                for (int y = 0; y < _length; y++)
                    switch (procedure)
                    {
                        case GenProcedure.DRY:
                            if (refMap[x, y] != OCEAN) break;
                            if (IsMapCondition(
                                GridTools.GetNeighbourTiles(refMap, x, y),
                                OCEAN, LandCondition.NEAR_LAND))
                            {
                                if (rng.NextDouble() < power) _buildMap[x, y] = PLAINS;
                            }
                            break;
                        case GenProcedure.ERODE:
                            if (refMap[x, y] != PLAINS) break;
                            if (IsMapCondition(
                                GridTools.GetNeighbourTiles(refMap, x, y),
                                OCEAN, LandCondition.NEAR_WATER))
                            {
                                power *= 0.5d;
                                if (rng.NextDouble() < power) _buildMap[x, y] = OCEAN;
                            }
                            break;
                    }

            // next cycle
            if (cycles > 1) GenLandProcedure(procedure, cycles - 1, power);
        }
    }
}
