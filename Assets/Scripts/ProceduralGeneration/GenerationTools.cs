using SeedTools;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using PoliticalEntities;
using Vector2 = UnityEngine.Vector2;
using NG = NameGenerators;

namespace GenerationTools
{
    /// <summary>
    /// A utility class for navigating the map in its constructions.
    /// You shouldn't have to use this unless you are making a new
    /// procedural map generator. It contains unique IDs for all valid
    /// tile types and 2D-array navigation methods.
    /// </summary>
    public static class MapUtil
    {
        // Elevation-based IDs
        public const byte OCEAN = 0;
        public const byte SWAMP = 1;
        public const byte WETLANDS = 2;
        public const byte PLAINS = 3;
        public const byte MOUNTAINS = 4;

        // Non-elevation IDs
        public const byte FOREST = 5;
        public const byte DESERT = 6;
        public const byte SHALLOW = 7;
        public const byte DRYFOREST = 8;
        public const byte CLOUD = 9;
        public const byte UPPERCLOUD = 10;

        // Utility IDs (don't need textures/tiles)
        public const byte RIVER = 11;
        public const byte NULL = 12;

        public static Tile[] GetTileSet(Tile cloud, Tile upperCloud,
            Tile ocean, Tile swamp, Tile wetlands, Tile shallow,
            Tile plains, Tile mountains, Tile forest,
            Tile desert, Tile dryForest)
        {
            Tile[] set = new Tile[11];
            set[CLOUD] = cloud;
            set[UPPERCLOUD] = upperCloud;
            set[OCEAN] = ocean;
            set[SWAMP] = swamp;
            set[WETLANDS] = wetlands;
            set[PLAINS] = plains;
            set[MOUNTAINS] = mountains;
            set[FOREST] = forest;
            set[DESERT] = desert;
            set[SHALLOW] = shallow;
            set[DRYFOREST] = dryForest;
            return set;
        }

        public static bool IsAquatic(byte t)
            => t == OCEAN || t == SHALLOW || t == SWAMP || t == RIVER;
        public static bool IsUncrossable(byte t)
            => t == MOUNTAINS || t == CLOUD || t == NULL;
        public static bool IsArid(byte t)
            => t == DESERT || t == DRYFOREST;
        public static bool IsForest(byte t)
            => t == FOREST || t == DRYFOREST;
        public static bool IsWalkable(byte t)
            => !IsUncrossable(t) && !IsAquatic(t);
        public static bool IsWalkableArid(byte t)
            => IsWalkable(t) && IsArid(t);
        public static bool IsWalkableNonArid(byte t)
            => IsWalkable(t) && !IsArid(t);

        public static byte[,] DeepCopy(byte[,] src)
        {
            // non-square source map error
            if (src.GetLength(0) != src.GetLength(1))
                throw new ArgumentException("MU01: Source map is not square");
            // deep copy and return
            int len = src.GetLength(0);
            byte[,] ret = new byte[len, len];
            for (int x = 0; x < len; x++)
                for (int y = 0; y < len; y++)
                    ret[x, y] = src[x, y];
            return ret;
        }
        public static int[,] DeepCopy(int[,] src)
        {
            // non-square source map error
            if (src.GetLength(0) != src.GetLength(1))
                throw new ArgumentException("MU08: Source map is not square");
            // deep copy and return
            int len = src.GetLength(0);
            int[,] ret = new int[len, len];
            for (int x = 0; x < len; x++)
                for (int y = 0; y < len; y++)
                    ret[x, y] = src[x, y];
            return ret;
        }

        public static Vector2Int GetDiags(Vector2Int get, bool lower)
        {
            if (get.x == 0 && get.y == 0) return new(0, 0);
            else if (get.x == 0) return new((lower ? -1 : 1), get.y);
            else if (get.y == 0) return new(get.x, (lower ? -1 : 1));
            else return lower ? new(0, get.y) : new(get.x, 0);
        }

        public static Vector2 UnitVecFromDir(double dir)
        {
            if (dir < 0 || dir >= 1)
                throw new ArgumentException("MU03: Direction not in [0,1)");
            return new((float)Math.Cos(dir * 2 * Math.PI),
                (float)Math.Sin(dir * 2 * Math.PI));
        }

        public static byte[] GetNeighbourTiles(
            byte[,] map, int x, int y,
            bool includeCorners, bool includeSelf)
        {
            int retSize = 4;
            if (includeCorners) retSize += 4;
            if (includeSelf) retSize++;
            byte[] neighs = new byte[retSize];
            try
            {
                int pt = 0;
                for (int i = -1; i <= 1; i++)
                    for (int j = -1; j <= 1; j++)
                        if ((i == 0 || j == 0 || includeCorners)
                            && (i != 0 || j != 0 || includeSelf))
                        {
                            neighs[pt] = map[x + i, y + j];
                            pt++;
                        }
            }
            catch (IndexOutOfRangeException)
            {
                throw new ArgumentException("MU02: Invalid coordinates");
            }
            return neighs;
        }

        public static int FindNearbyTiles(
            byte[,] map, int x, int y, byte findLow, byte findHigh)
        {
            byte[] nearby = GetNeighbourTiles(map, x, y, true, false);
            int found = 0;
            foreach (byte tile in nearby)
                if (tile >= findLow && tile <= findHigh) found++;
            return found;
        }

        public static bool HasNearbyTiles(
            byte[,] map, int x, int y,
            byte targetLow, byte targetHigh, int required)
        {
            return
                FindNearbyTiles(map, x, y, targetLow, targetHigh)
                >= required;
        }

        public static int FindNearbyTiles(
            byte[,] map, int x, int y, byte find)
            => FindNearbyTiles(map, x, y, find, find);

        public static bool HasNearbyTiles(
            byte[,] map, int x, int y, byte target, int required)
            => HasNearbyTiles(map, x, y, target, target, required);

        public static int CoordToId(int x, int y, int mapLen)
            => x * mapLen + y;

        public static Vector2Int IdToCoord(int id, int mapLen)
            => new(id / mapLen, id % mapLen);

        public static bool Near(string[,,] map,
            int x, int y, int l, string lookFor)
        {
            try
            {
                for (int i = -1; i <= 1; i++)
                    for (int j = -1; j <= 1; j++)
                    {
                        if (!(i == 0 && j == 0)
                            && map[x + i, y + j, l] == lookFor)
                            return true;
                    }
            }
            catch (IndexOutOfRangeException)
            { throw new ArgumentException("MU04: Invalid coordinates"); }
            return false;
        }

        public static bool Near(int[,] map,
            int x, int y, int lookFor, bool inclCorners)
        {
            try
            {
                for (int i = -1; i <= 1; i++)
                    for (int j = -1; j <= 1; j++)
                    {
                        if (!(i == 0 && j == 0)
                            && (inclCorners || i == 0 || j == 0)
                            && map[x + i, y + j] == lookFor)
                            return true;
                    }
            }
            catch (IndexOutOfRangeException)
            { throw new ArgumentException("MU05: Invalid coordinates"); }
            return false;
        }

        /*
         * Returns the most-common nearby string that occurs at least
         * (surroundReq) number of times. Includes both edges and corners.
         */
        public static string GetSurrounding(string[,,] map,
            int x, int y, int layer, int surroundReq)
        {
            try
            {
                Dictionary<string, int> near = new();
                for (int i = -1; i <= 1; i++)
                    for (int j = -1; j <= 1; j++)
                    {
                        if (!(i == 0 && j == 0))
                        {
                            string lookAt = map[x + i, y + j, layer];
                            if (!near.ContainsKey(lookAt)) near[lookAt] = 1;
                            else near[lookAt]++;
                        }
                    }
                string max = null;
                int maxAmount = 0;
                foreach (string key in near.Keys)
                {
                    int look = near[key];
                    if (look >= surroundReq
                        && look > maxAmount)
                    {
                        max = key;
                        maxAmount = look;
                    }
                }
                return max;
            }
            catch (IndexOutOfRangeException)
            { throw new ArgumentException("MU06: Invalid coordinates"); }
        }

        /*
         * Returns the most-common nearby integer that occurs at least
         * (surroundReq) number of times. Includes both edges and corners.
         */
        public static int GetSurrounding(int[,] map,
            int x, int y, int surroundReq, int blacklist = -1)
        {
            try
            {
                Dictionary<int, int> near = new();
                for (int i = -1; i <= 1; i++)
                    for (int j = -1; j <= 1; j++)
                    {
                        if (!(i == 0 && j == 0))
                        {
                            int look = map[x + i, y + j];
                            if (look == blacklist) continue;
                            if (!near.ContainsKey(look)) near[look] = 1;
                            else near[look]++;
                        }
                    }
                int max = -1;
                int maxAmount = 0;
                foreach (int key in near.Keys)
                {
                    int look = near[key];
                    if (look >= surroundReq
                        && look > maxAmount)
                    {
                        max = key;
                        maxAmount = look;
                    }
                }
                return max;
            }
            catch (IndexOutOfRangeException)
            { throw new ArgumentException("MU06: Invalid coordinates"); }
        }

        public static int FindNear(int[,] map, int x, int y, int lookFor)
        {
            try
            {
                int count = 0;
                for (int i = -1; i <= 1; i++)
                    for (int j = -1; j <= 1;
                        j += (i == 0 && j == -1) ? 2 : 1)
                    {
                        if (map[x, y] == lookFor) count++;
                    }
                return count;
            }
            catch (IndexOutOfRangeException)
            { throw new ArgumentException("MU07: Invalid coordinates"); }
        }
    }

    /// <summary>
    /// An advanced construction object for making the base terrain of a 
    /// world map. To create a map you may use Export(...) in which you
    /// must set up your own order of procedural operations using a custom
    /// Construct, for example:
    /// <code>... Export(100, (cstr)=>cstr.func1().func2(). ...);</code>
    /// </summary>
    public class MapConstructor
    {
        public delegate void Construct(MapConstructor constructor);

        public static byte[,] Export(int length, Construct construct)
        {
            MapConstructor c = new(length);
            construct(c);
            return c._map;
        }

        public const int MIN_SIZE = 256;
        public const int MAX_SIZE = 512;

        private readonly int _size;
        private readonly byte[,] _map;

        private Seed _seed = new();

        private MapConstructor(int size)
        {
            if (size < MIN_SIZE || size > MAX_SIZE)
                throw new ArgumentException("MC01: Invalid Size");

            _size = size;
            _map = new byte[size, size];
        }

        /*
         * Sets the map generator seed to the given value.
         */
        public MapConstructor ApplySeed(int seedValue)
        {
            _seed = new(seedValue);
            return this;
        }

        /*
         * Draws a cloud border wall around the map.
         */
        public MapConstructor SetCloudBorder(int thickness)
        {
            int t = thickness;
            for (int x = 0; x < _size; x++)
                for (int y = 0; y < _size; y++)
                {
                    _map[x, y] = MapUtil.CLOUD;
                    if (x >= t && x < _size - t && y == t - 1)
                        y = _size - t - 1;
                }
            return this;
        }

        /*
         * Fills the 'out of bounds' regions on the map such that
         * the map contained a square-shaped region of filled tiles.
         * The playable 
         */
        public MapConstructor SetBorderRealm()
        {
            return null;
        }

        /*
         * Fills the entire map with the given terrain.
         */
        public MapConstructor SimpleFill(byte fill)
        {
            for (int x = 0; x < _size; x++)
                for (int y = 0; y < _size; y++)
                    _map[x, y] = fill;
            return this;
        }

        /*
         * (Considers Elevation)
         * 
         * Scribbles around the map, creating intricate land formations
         * within a given range. These formations are called "floods".
         * Floods in the same cycle will not overlap with each other. 
         * Floods in different cycles will overlap, increasing elevation.
         * 
         * Floods may be set to decay over each flood and/or cycle. If 
         * floods are set to decay each flood, but not each cycle, then the
         * flood size will reset each cycle. Negative decay rates will lead
         * to floods increasing over time instead (there is no error checking
         * for negative decay so be careful).
         * 
         * NOTE that floodOriginBuffer is added in ADDITION to the buffer
         * already created by floodSpreadBuffer. If you don't desire
         * different bounds for flood origin points and floods in general,
         * then leave floodOriginBuffer at 0.
         */
        public MapConstructor OverlayFlooderFill(
            int cycles, int floodsPerCycle, int floodSize,
            int floodOriginBuffer, int floodSpreadBuffer,
            int floodSizeDecay, bool decayEachFlood, bool decayEachCycle,
            bool overlayPrevious, double floodOriginVariation)
        {
            // error - flood size too large
            int targRegLen = _size - 2 * (1 + floodSpreadBuffer);
            if (floodsPerCycle * floodSize > targRegLen * targRegLen)
                throw new ArgumentException("MC02: Spread Buffer too high");

            // error - variation not in [0,1] (could lead to infinite loop)
            if (floodOriginVariation > 1 || floodOriginVariation < 0)
                throw new ArgumentException("MC03: Invalid Flood Variation");

            // increase markBuffer if there are ever >20 terrain types
            if (!overlayPrevious) SimpleFill(MapUtil.OCEAN);
            int maxHeight = MapUtil.MOUNTAINS;
            int fB = floodOriginBuffer, fS = floodSpreadBuffer;
            int fSi = floodSize;
            double fV = floodOriginVariation;
            byte markBuffer = 20;
            int l = fS, h = _size - 1 - fS;
            int m = (_size / 2) - fB - fS;
            for (int c = 0; c < cycles; c++)
            {
                Vector2Int last = new(_size / 2, _size / 2);
                for (int f = 0; f < floodsPerCycle; f++)
                {
                    int dir = 0;
                    int iter = 0;
                    Vector2Int org;
                    do
                    {
                        org = new(
                            _seed.RangeRoll(l + fB, h - fB),
                            _seed.RangeRoll(l + fB, h - fB));
                    } while (Math.Abs(org.x - last.x) < m * fV ||
                             Math.Abs(org.y - last.y) < m * fV);
                    last = new(org.x, org.y);
                    //_map[org.x, org.y] = MapUtil.CLOUD; // for testing
                    while (iter < fSi)
                    {
                        double rng = _seed.PerRaw();
                        if (_map[org.x, org.y] < maxHeight)
                        {
                            _map[org.x, org.y] += (byte)(markBuffer + 1);
                            iter++;
                        }
                        if (rng < 0.25)
                        {
                            if (org.x > l && dir != 0) { org.x--; dir = 0; }
                        }
                        else if (rng < 0.5)
                        {
                            if (org.x < h && dir != 1) { org.x++; dir = 1; }
                        }
                        else if (rng < 0.75)
                        {
                            if (org.y > l && dir != 2) { org.y--; dir = 2; }
                        }
                        else
                        {
                            if (org.y < h && dir != 3) { org.y++; dir = 3; }
                        }
                    }
                    // decay for next flood
                    if (decayEachFlood) fSi -= floodSizeDecay;
                }
                for (int x = l; x <= h; x++)
                    for (int y = l; y <= h; y++)
                        if (_map[x, y] > markBuffer)
                            _map[x, y] -= markBuffer;
                // decay/reset for next cycle
                if (decayEachCycle) fSi -= floodSizeDecay;
                else fSi = floodSize;
            }
            return this;
        }

        /*
         * (Considers Elevation)
         * 
         * Errodes low elevation tiles into ocean and raises higher
         * elevation tiles towards dry land. 
         */
        public MapConstructor SettleFluids(
            double fluidStrength, double landStrength,
            int fluidDryBuffer, int landSinkBuffer,
            int cycles, int settleAreaBuffer)
        {
            // strength values must be probabilities
            if (fluidStrength < 0 || landStrength < 0
                || fluidStrength > 1 || landStrength > 1)
                throw new ArgumentException("MC04: Invalid strength values");

            int l = 1 + settleAreaBuffer;
            int h = _size - 2 - settleAreaBuffer;
            for (int c = 0; c < cycles; c++)
            {
                byte[,] rfr = MapUtil.DeepCopy(_map);
                for (int x = l; x <= h; x++)
                    for (int y = l; y <= h; y++)
                    {
                        // swamp procedures
                        if (rfr[x, y] == MapUtil.SWAMP)
                        {
                            if (MapUtil.HasNearbyTiles(rfr, x, y,
                                MapUtil.OCEAN, 1))
                            {
                                if (_seed.PerRoll(fluidStrength))
                                    _map[x, y] = MapUtil.OCEAN;
                            }
                            else if (MapUtil.HasNearbyTiles(rfr, x, y,
                                     MapUtil.WETLANDS, MapUtil.MOUNTAINS,
                                    fluidDryBuffer))
                            {
                                if (_seed.PerRoll(landStrength))
                                    _map[x, y] = MapUtil.WETLANDS;
                            }
                        }
                        // wetland procedures
                        else if (rfr[x, y] == MapUtil.WETLANDS)
                        {
                            if (MapUtil.HasNearbyTiles(rfr, x, y,
                                    MapUtil.PLAINS, MapUtil.MOUNTAINS, 1))
                            {
                                if (_seed.PerRoll(landStrength))
                                    _map[x, y] = MapUtil.PLAINS;
                            }
                            else if (MapUtil.HasNearbyTiles(rfr, x, y,
                                     MapUtil.OCEAN, MapUtil.SWAMP,
                                    landSinkBuffer))
                            {
                                if (_seed.PerRoll(fluidStrength))
                                    _map[x, y] = MapUtil.SWAMP;
                            }
                        }
                    }
            }
            return this;
        }

        /*
         * (Considers Elevation)
         * 
         * Randomly fills wetlands tiles with plains tiles. The rate at 
         * which this happens can be set. 
         */
        public MapConstructor DeteriorateWetlands(
            double deterPower, bool innerOnly,
            int cycles, int deterAreaBuffer)
        {
            if (deterPower < 0 || deterPower > 1)
                throw new ArgumentException("MC05: Invalid power values");

            int l = 1 + deterAreaBuffer;
            int h = _size - 2 - deterAreaBuffer;
            for (int c = 0; c < cycles; c++)
            {
                byte[,] rfr = MapUtil.DeepCopy(_map);
                for (int x = l; x <= h; x++)
                    for (int y = l; y <= h; y++)
                    {
                        if (rfr[x, y] == MapUtil.WETLANDS)
                            if (_seed.PerRoll(deterPower))
                                if (MapUtil.HasNearbyTiles(
                                    rfr, x, y,
                                    MapUtil.WETLANDS, MapUtil.MOUNTAINS, 8)
                                    || !innerOnly)
                                    _map[x, y] = MapUtil.PLAINS;
                    }
            }
            return this;
        }

        /*
         * (Considers Elevation)
         * 
         * Cleans up tiles that are completely surrounded by others.
         */
        public MapConstructor SimpleLandCleanup(
            bool pickyClean, int cleanAreaBuffer)
        {
            int l = 1 + cleanAreaBuffer;
            int h = _size - 2 - cleanAreaBuffer;
            int find = pickyClean ? 8 : 6;
            byte[,] rfr = MapUtil.DeepCopy(_map);
            for (int x = l; x <= h; x++)
                for (int y = l; y <= h; y++)
                {
                    switch (rfr[x, y])
                    {
                        // Swamps cleaned into oceans
                        case MapUtil.SWAMP:
                            if (MapUtil.HasNearbyTiles(
                                rfr, x, y,
                                MapUtil.OCEAN, find))
                                _map[x, y] = MapUtil.OCEAN;
                            break;
                        // Shallows cleaned into oceans
                        case MapUtil.SHALLOW:
                            if (MapUtil.HasNearbyTiles(
                                rfr, x, y,
                                MapUtil.OCEAN, find))
                                _map[x, y] = MapUtil.OCEAN;
                            break;
                        // Oceans cleaned into swamps/shallows
                        case MapUtil.OCEAN:
                            if (MapUtil.HasNearbyTiles(
                                rfr, x, y,
                                MapUtil.SWAMP, MapUtil.MOUNTAINS, find))
                                _map[x, y] = MapUtil.SWAMP;
                            else if (MapUtil.HasNearbyTiles(
                                rfr, x, y,
                                MapUtil.SHALLOW, find))
                                _map[x, y] = MapUtil.SHALLOW;
                            break;
                        // Plains cleaned into deserts
                        case MapUtil.PLAINS:
                            if (MapUtil.HasNearbyTiles(
                                rfr, x, y,
                                MapUtil.DESERT, find))
                                _map[x, y] = MapUtil.DESERT;
                            break;
                        // Deserts cleaned into plains
                        case MapUtil.DESERT:
                            if (MapUtil.HasNearbyTiles(
                                rfr, x, y,
                                MapUtil.PLAINS, find))
                                _map[x, y] = MapUtil.PLAINS;
                            break;
                        // Wetlands cleaned into plains
                        case MapUtil.WETLANDS:
                            if (MapUtil.HasNearbyTiles(
                                rfr, x, y,
                                MapUtil.PLAINS, MapUtil.MOUNTAINS, find))
                                _map[x, y] = MapUtil.PLAINS;
                            break;
                    }
                }
            return this;
        }

        /*
         * (Considers Elevation)
         * 
         * Builds intricate rivers by tearing canyons through the map
         * and filling them with swamps. Rivers will originate at random 
         * mountain tiles and flow until they hit ocean/swamp tiles. If you
         * want rivers to originate anywhere, make naturalPref < 1.
         * 
         * NOTE these swamp tiles will probably become shallows later on
         * in generation (probably, depending on preference).
         * 
         * NOTE that riverStartBuffer is added in ADDITION to the buffer
         * already created by riverFlowBuffer. If you don't desire
         * different bounds for river origin points and rivers in general,
         * then leave riverStartBuffer at 0.
         * 
         * ALSO there is no error-checker for buffers (TODO or something idk)
         */
        public MapConstructor GenerateRivers(
            int riverAmount, int riverWidth, bool minors,
            double riverStraightness, double riverDepth, double naturalPref,
            int riverStartBuffer, int riverFlowBuffer)
        {
            if (riverStraightness < 0 || riverStraightness > 1)
                throw new ArgumentException("MC06: Invalid straightness");

            if (riverDepth < 0 || riverDepth > 1)
                throw new ArgumentException("MC13: Invalid integrity");

            if (naturalPref < 0 || naturalPref > 1)
                throw new ArgumentException("MC15: Invalid preference");

            int l = 1 + riverFlowBuffer;
            int h = _size - 2 - riverFlowBuffer;
            int sB = riverStartBuffer;
            int rW = riverWidth, rA = riverAmount;
            int maxAts = _size * _size;
            for (int r = 0; r < rA; r++)
            {
                // switch to minors if applicable
                if (rW == riverWidth && r >= rA / 2 && minors) rW /= 2;
                // find origin mountain
                Vector2Int org;
                byte[,] rfr = MapUtil.DeepCopy(_map);
                int orgAts = 0;
                bool mountSpawn = _seed.PerRoll(naturalPref);
                do
                {
                    if (orgAts >= maxAts)
                        throw new Exception("MC12: Failed river origins");
                    org = new(_seed.RangeRoll(l + sB, h - sB),
                        _seed.RangeRoll(l + sB, h - sB));
                    orgAts++;
                } while ((mountSpawn &&
                            !(rfr[org.x, org.y] == MapUtil.MOUNTAINS
                            && MapUtil.HasNearbyTiles(rfr, org.x, org.y,
                            MapUtil.MOUNTAINS, 4)))
                        || !mountSpawn &&
                            !(rfr[org.x, org.y] == MapUtil.PLAINS));
                // spread until hit water
                Vector2Int dir;
                do dir = new(_seed.RangeRoll(-1, 1), _seed.RangeRoll(-1, 1));
                while (dir.x == 0 && dir.y == 0);
                Vector2Int newDir, ogDir = new(dir.x, dir.y);
                bool foundWater = false;
                int watAts = 0;
                while (!foundWater)
                {
                    if (watAts >= maxAts)
                        throw new Exception("MC14: Failed river spread");
                    if (_seed.PerRoll(1 - riverStraightness))
                    {
                        newDir = MapUtil.GetDiags(dir, _seed.PerRoll(0.5));
                        if (newDir.x == ogDir.x || newDir.y == ogDir.y)
                            dir = new(newDir.x, newDir.y);
                    }
                    org.x += dir.x; org.y += dir.y;
                    try
                    {
                        if (rfr[org.x, org.y] == MapUtil.OCEAN
                            || rfr[org.x, org.y] == MapUtil.RIVER)
                        {
                            foundWater = true;
                        }
                        else
                        {
                            watAts++;
                            for (int i = -rW; i <= rW; i++)
                                for (int j = -rW; j <= rW; j++)
                                {
                                    // dont consider far corners
                                    if (Math.Abs(i) + Math.Abs(j) > rW)
                                        continue;
                                    // flood river patch
                                    byte check = _map[org.x + i, org.y + j];
                                    if (check != MapUtil.RIVER
                                        && (check != MapUtil.MOUNTAINS
                                            || (i == 0 && j == 0)))
                                    {
                                        if (!(i == 0 && j == 0)
                                            && MapUtil.HasNearbyTiles(
                                            rfr, org.x + i, org.y + j,
                                            MapUtil.MOUNTAINS, 1))
                                            continue;
                                        double rI = (i == 0 && j == 0)
                                            ? 1 : riverDepth;
                                        if (_seed.PerRoll(rI))
                                        {
                                            _map[org.x + i, org.y + j] =
                                                MapUtil.RIVER;
                                        }
                                        else
                                        {
                                            if (check == MapUtil.PLAINS)
                                                _map[org.x + i, org.y + j] =
                                                    MapUtil.WETLANDS;
                                        }
                                    }
                                }
                        }
                    }
                    catch (Exception) { foundWater = true; }
                }
            }
            for (int x = l; x <= h; x++)
                for (int y = l; y <= h; y++)
                    if (_map[x, y] == MapUtil.RIVER)
                        _map[x, y] = MapUtil.SWAMP;

            return this;
        }

        /*
         * BREAKS ELEVATION ID - use only after elevation-based algs done.
         * 
         * Grows forests of a set size. The density of a forest will force
         * trees to be closer together and forest borders to be tighter.
         */
        public MapConstructor GrowForests(
            int forestSize, int forestAmount, int forestAreaBuffer,
            bool fastGen, double forestDensity)
        {
            if (forestDensity < 0 || forestDensity > 1)
                throw new ArgumentException("MC07: Invalid density");

            int l = 1 + forestAreaBuffer;
            int h = _size - 2 - forestAreaBuffer;
            int maxAts = _size * (fastGen ? 1 : _size);
            int maxGrowAts = (_size * _size) * (fastGen ? 1 : _size);
            for (int f = 0; f < forestAmount; f++)
            {
                Vector2Int org;
                int orgAts = 0;
                do
                {
                    if (orgAts >= maxAts)
                        throw new Exception("MC08: Forest Gen failed");
                    org = new(_seed.RangeRoll(l, h),
                        _seed.RangeRoll(l, h));
                    orgAts++;
                } while (_map[org.x, org.y] != MapUtil.PLAINS);
                int planted = 1, dir = 0, growAts = 0;
                while (planted < forestSize)
                {
                    if (growAts >= maxGrowAts)
                        throw new Exception("MC09: Forest Gen failed");
                    double rng = _seed.PerRaw();
                    if (_map[org.x, org.y] == MapUtil.PLAINS
                        && _seed.PerRoll(forestDensity))
                    {
                        _map[org.x, org.y] = MapUtil.FOREST;
                        planted++;
                    }
                    if (rng < 0.25)
                    {
                        if (org.x > l && dir != 0) { org.x--; dir = 0; }
                    }
                    else if (rng < 0.5)
                    {
                        if (org.x < h && dir != 1) { org.x++; dir = 1; }
                    }
                    else if (rng < 0.75)
                    {
                        if (org.y > l && dir != 2) { org.y--; dir = 2; }
                    }
                    else
                    {
                        if (org.y < h && dir != 3) { org.y++; dir = 3; }
                    }
                    growAts++;
                }
            }
            return this;
        }

        /*
         * BREAKS ELEVATION ID - use only after elevation-based algs done.
         * 
         * Replaces swamp tiles with shallow water. A given percentage
         * of shoreline-swamps will be preserved.
         */
        public MapConstructor BuildShalllows(
            double shorePreservation, int shallowsBuffer)
        {
            if (shorePreservation < 0 || shorePreservation > 1)
                throw new ArgumentException("MC10: Invalid preservation");

            int l = 1 + shallowsBuffer;
            int h = _size - 2 - shallowsBuffer;
            byte[,] rfr = MapUtil.DeepCopy(_map);
            for (int x = l; x <= h; x++)
                for (int y = l; y <= h; y++)
                {
                    if (rfr[x, y] == MapUtil.SWAMP)
                    {
                        if (!(MapUtil.HasNearbyTiles(rfr, x, y,
                            MapUtil.WETLANDS, MapUtil.MOUNTAINS, 1)
                            && _seed.PerRoll(shorePreservation)))
                            _map[x, y] = MapUtil.SHALLOW;
                    }
                }
            return this;
        }

        /*
         * BREAKS ELEVATION ID - use only after elevation-based algs done.
         * 
         * Fills in all shorelines with shallows, overriding ocean tiles.
         */
        public MapConstructor ExpandShallows(
            double shoreAmount, int shallowsBuffer)
        {
            if (shoreAmount < 0 || shoreAmount > 1)
                throw new ArgumentException("MC11: Invalid strength");

            int l = 1 + shallowsBuffer;
            int h = _size - 2 - shallowsBuffer;
            byte[,] rfr = MapUtil.DeepCopy(_map);
            for (int x = l; x <= h; x++)
                for (int y = l; y <= h; y++)
                {
                    if (rfr[x, y] == MapUtil.OCEAN)
                    {
                        if (MapUtil.HasNearbyTiles(rfr, x, y,
                            MapUtil.WETLANDS, MapUtil.MOUNTAINS, 1)
                            && _seed.PerRoll(shoreAmount))
                            _map[x, y] = MapUtil.SHALLOW;
                    }
                }

            return this;
        }

        /*
         * BREAKS ELEVATION ID - use only after elevation-based algs done.
         * 
         * Creates deserts from 'arid' areas, using the given direction
         * as a pseudo-jetstream to elumate such. Direction 0 is equivalent
         * to the vector (1,0) on the map, with all further directions of
         * range [0,1) proceeding counter-clockwise.
         */
        public MapConstructor Desertifacation(
            int desertCycles, int desertZoneBuffer, bool originate,
            double weatherDir, double weatherForce, double spreadVariance)
        {
            if (weatherDir < 0 || weatherDir >= 1)
                throw new ArgumentException("MC16: Invalid direction");

            if (spreadVariance < 0 || spreadVariance > 1)
                throw new ArgumentException("MC17: Invalid variance");

            int l = 1 + desertZoneBuffer;
            int h = _size - 2 - desertZoneBuffer;
            double sV = spreadVariance;
            double wF = weatherForce;
            double wD = weatherDir;
            for (int c = 0; c < desertCycles; c++)
            {
                byte[,] rfr = MapUtil.DeepCopy(_map);
                for (int x = l; x <= h; x++)
                    for (int y = l; y <= h; y++)
                    {
                        if ((originate && rfr[x, y] == MapUtil.MOUNTAINS)
                            || rfr[x, y] == MapUtil.DESERT)
                        {
                            if (originate)
                            {
                                if (rfr[x, y] == MapUtil.MOUNTAINS
                                    && !MapUtil.HasNearbyTiles(rfr, x, y,
                                        MapUtil.MOUNTAINS, 4))
                                    continue;
                            }
                            double dOff = wD
                                + ((_seed.PerRaw() / 2.0) - 0.25) * sV;
                            if (dOff >= 1) dOff -= 1;
                            else if (dOff < 0) dOff += 1;
                            // unit vector of direction of desertifaction
                            Vector2 dir = MapUtil.UnitVecFromDir(dOff);
                            double airPush = _seed.PerRaw();
                            int airStr;
                            if (airPush < wF * 0.1) airStr = 4;
                            else if (airPush < wF * 0.5) airStr = 3;
                            else airStr = 2;
                            Vector2Int spr = new(
                                x + (int)(airStr * dir.x),
                                y + (int)(airStr * dir.y));
                            if (spr.x < l || spr.x > h
                                || spr.y < l || spr.y > h)
                                continue;
                            byte getAt = rfr[spr.x, spr.y];
                            if (getAt == MapUtil.PLAINS)
                                _map[spr.x, spr.y] = MapUtil.DESERT;
                            else if (getAt == MapUtil.WETLANDS)
                                _map[spr.x, spr.y] = MapUtil.PLAINS;
                            else if (getAt == MapUtil.FOREST)
                                _map[spr.x, spr.y] = MapUtil.DRYFOREST;
                        }
                    }
            }
            return this;
        }
    }

    /// <summary>
    /// A seperate construction object for populating a map with various
    /// ethnic regions and territories. The algorithm will require an
    /// input map built via a MapConstructor. Simply get a return World
    /// via the Export(...) function (you will need to reference your 
    /// pre-constructed map here). The constructed World object will need 
    /// to have valid array maps, a completed list of all map Ethnics,
    /// and a ID dictionary which maps all array IDs to objects.
    /// </summary>
    public class WorldPopulator
    {
        // UTILITY CLASS
        private class BBox
        {
            public int lX, lY, hX, hY;

            public BBox(int minX, int minY, int maxX, int maxY)
            { lX = minX; lY = minY; hX = maxX; hY = maxY; }
        }

        /*
         * Get consts from the PolType enum
         * Note: REG and ENT can be generated more efficiently AFTER
         * world export, so their layers will remain empty 
         */
        public const int LAYER_ETH = (int)PolType.ETHNIC;
        public const int LAYER_REA = (int)PolType.REALM;
        public const int LAYER_REG = (int)PolType.REGION;
        public const int LAYER_ENT = (int)PolType.ENTITY;

        // increase if above stuff changes
        public const int INFO_LAYERS = 4;

        public const char DIV = ';';

        public const int LEN_ETH_ID = 2;
        public const int LEN_REA_ID = 3;
        public const int LEN_REG_ID = 3;
        public const int LEN_ENT_ID = 3;

        public const string ID
            = "0123456789"
            + "abcdefghijklmnopqrstuvwxyz"
            + "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
            + "?!";

        private readonly int _size;
        private readonly byte[,] _terr;
        private readonly string[,,] _map;

        private int[,] _temp; // starts empty, use if needed (?)

        private readonly Seed _seed;

        private readonly Dictionary<string, AbstractPol> _info = new();

        // probably dont need to export, but helpful for generation
        private readonly Dictionary<string, BBox> _ethBounds = new();

        private WorldPopulator(byte[,] reference, int seedValue)
        {
            if (reference.GetLength(0) != reference.GetLength(1))
                throw new ArgumentException("WP01: Ref map invalid");

            _terr = reference; // NOTE: this is NOT a deep copy!
            _size = reference.GetLength(0);
            _seed = new(seedValue);
            _map = new string[_size, _size, INFO_LAYERS];
            ResetTemp();
        }

        public const int MAX_ETHNICS = 128;
        public const int MAX_REALMS_PER_ETHNIC = 16;

        public static World Export(byte[,] reference, int seedValue,
            int maxEthnics, RegS regionType, double regionRoughness,
            bool forceDesertWilds)
        {
            WorldPopulator wP = new(reference, seedValue);
            wP.FillWithNullStrings();
            wP.BuildEthnics(maxEthnics, forceDesertWilds);
            wP.FormRegions(regionType, regionRoughness);
            return new(wP._terr, wP._map, wP._seed.Get(), wP._info);
        }

        /*
         * Tiles with an ID of completely zeroes (000...) are considered
         * 'null' with no features. For example, a tile with '00' as its
         * ethnic ID means that that tile is not part of any ethnic.
         * 
         * To prevent null-pointers and string length inconsistencies,
         * the map should be completely filled with 'null strings' prior
         * to generation of any further features.
         */
        private void FillWithNullStrings()
        {
            for (int x = 0; x < _size; x++)
                for (int y = 0; y < _size; y++)
                {
                    _map[x, y, LAYER_ETH] = BaseId(LEN_ETH_ID);
                    _map[x, y, LAYER_REA] = BaseId(LEN_REA_ID);
                    _map[x, y, LAYER_REG] = BaseId(LEN_REG_ID);
                    _map[x, y, LAYER_ENT] = BaseId(LEN_ENT_ID);
                }
        }

        /*
         * Divides up to (maxEth) of the LARGEST contiguous biomes into 
         * their own 'Ethnics', or continents inhabited by unique races 
         * and features. There are two valid 'biomes', those being temperate 
         * (forest/wetlands/plains) and arid (deserts/dryforests). Water 
         * tiles are not populated by any Ethnic. Land tiles that do not 
         * get assigned to any Ethnic will be marked as 'Wilds'.
         * 
         * There is an option that forces all arid biomes to be completely
         * inhabited by Wilds. Thus, all Ethnics will be temperate.
         * 
         * You can place up to 64 Ethnics on a single map.
         */
        private void BuildEthnics(int maxEth, bool wildDeserts)
        {
            if (maxEth > MAX_ETHNICS || maxEth < 1)
                throw new ArgumentException("WP03: Invalid ethnics");
            ResetTemp();
            // Divide the entire map by distinguishable landmasses
            // Landmasses must have no crossable land connections to others
            int placedDivId = 1; // 0 will act as a null
            bool availOrg = true;
            Dictionary<int, int> divSizes = new();
            while (availOrg)
            {
                bool foundOrg = false;
                bool aridEth = false;
                int lX = 0, hX = 0;
                int lY = 0, hY = 0;
                for (int x = 0; x < _size && !foundOrg; x++)
                    for (int y = 0; y < _size && !foundOrg; y++)
                    {
                        byte look = _terr[x, y];
                        if (MapUtil.IsWalkable(look)
                            && _temp[x, y] == 0)
                        {
                            aridEth = MapUtil.IsWalkableArid(look);
                            if (wildDeserts && aridEth) continue;
                            foundOrg = true;
                            lX = x - 1; lY = y - 1;
                            hX = x + 1; hY = y + 1;
                            _temp[x, y] = placedDivId;
                            divSizes[placedDivId] = 1;

                        }
                    }
                if (!foundOrg) availOrg = false;
                else
                {
                    bool availSpread = true;
                    int uX = hX, uY = hY;
                    while (availSpread)
                    {
                        hX = uX;
                        hY = uY;
                        availSpread = false;
                        for (int x = lX; x <= hX; x++)
                            for (int y = lY; y <= hY; y++)
                            {
                                if ((aridEth
                                     && MapUtil.IsWalkableArid(_terr[x, y])
                                     || !aridEth
                                     && MapUtil.IsWalkableNonArid(_terr[x, y]))
                                    && _temp[x, y] == 0)
                                {
                                    if (MapUtil.Near(
                                        _temp, x, y, placedDivId, true))
                                    {
                                        availSpread = true;
                                        _temp[x, y] = placedDivId;
                                        divSizes[placedDivId]++;
                                        if (x == lX) lX--;
                                        else if (x == uX) uX++;
                                        if (y == lY) lY--;
                                        else if (y == uY) uY++;
                                    }
                                }
                            }
                    }
                    placedDivId++;
                }
            }
            // Fill the largest (maxEth) landmasses with unique Ethnics
            List<int> largest = new();
            for (int eth = 0; eth < maxEth && divSizes.Count > 0; eth++)
            {
                int lgKey = -1;
                foreach (int key in divSizes.Keys)
                    if (lgKey < 0 || divSizes[key] > divSizes[lgKey])
                        lgKey = key;
                divSizes.Remove(lgKey);
                largest.Add(lgKey);
            }
            for (int i = 1; i <= largest.Count; i++)
            {
                int targI = largest[i - 1];
                string id = IntToId(i, LEN_ETH_ID);
                string name = NG.NewEthnicName(_seed);
                CreateRef(PolType.ETHNIC, name, id);
                bool found = false;
                BBox bb = null;
                for (int x = 0; x < _size; x++)
                    for (int y = 0; y < _size; y++)
                    {
                        if (_temp[x, y] == targI)
                        {
                            if (!found)
                            {
                                _ethBounds[id] = new(x, y, x, y);
                                bb = _ethBounds[id];
                                found = true;
                            }
                            else
                            {
                                if (x < bb.lX) bb.lX = x;
                                else if (x > bb.hX) bb.hX = x;
                                if (y < bb.lY) bb.lY = y;
                                else if (y > bb.hY) bb.hY = y;
                            }
                            _map[x, y, LAYER_ETH] = id;
                        }
                    }
            }
            NG.Purge();
            // Fill remaining land with wilds/nulls
            string nullId = BaseId(LEN_ETH_ID);
            string wildId = IntToId(MAX_ETHNICS, LEN_ETH_ID);
            CreateRef(PolType.ETHNIC, Ethnic.Wild, wildId);
            for (int x = 0; x < _size; x++)
                for (int y = 0; y < _size; y++)
                {
                    if (_map[x, y, LAYER_ETH].Equals(nullId)
                        && MapUtil.IsWalkable(_terr[x, y]))
                        _map[x, y, LAYER_ETH] = wildId;
                }
            _ethBounds[wildId] = new(0, 0, _size - 1, _size - 1);
            // Clean up ethnic boundaries
            string b = BaseId(LEN_ETH_ID);
            int cyc = 4;
            for (int c = 0; c < cyc; c++)
                for (int x = 1; x < _size - 1; x++)
                    for (int y = 1; y < _size - 1; y++)
                    {
                        string sur, get = _map[x, y, LAYER_ETH];
                        if (get == null || get.Equals(b)) continue;
                        sur = MapUtil.GetSurrounding(_map, x, y, LAYER_ETH, 5);
                        if (sur != null && !sur.Equals(b) && !sur.Equals(get))
                            _map[x, y, LAYER_ETH] = sur;
                    }
        }

        /*
         * Breaks down all Ethnics into connected patches of tiles of 
         * (approximately) the given size type. Aquatic tiles will not
         * have regions as they do not have Ethnics.
         */
        private void FormRegions(RegS type, double roughness)
        {
            if (roughness < 0 || roughness >= 1)
                throw new ArgumentException("WP04: Invalid Roughness");
            int rS = (int)type;
            int root = (int)Math.Sqrt(rS);
            double rgh = roughness * 0.5 + 0.5;
            string regBase = BaseId(LEN_REG_ID);
            int rootN = 0;
            int refs = 0; // temp
            // form regions for each ethnic seperately
            foreach (string eth in _ethBounds.Keys)
            {
                // plant 'region roots' in the temp map
                BBox bb = _ethBounds[eth];
                ResetTemp(); // IMPORTANT
                for (int x = bb.lX + (root / 2); x <= bb.hX; x += root)
                    for (int y = bb.lY + (root / 2); y <= bb.hY; y += root)
                    {
                        if (_map[x, y, LAYER_ETH].Equals(eth))
                            _temp[x, y] = ++rootN;
                    }
                // 'spread' from each root until no spreads possible
                bool spread = true;
                while (spread)
                {
                    spread = false;
                    int[,] rfr = MapUtil.DeepCopy(_temp);
                    for (int x = bb.lX; x <= bb.hX; x++)
                        for (int y = bb.lY; y <= bb.hY; y++)
                        {
                            if (_map[x, y, LAYER_ETH].Equals(eth)
                                && rfr[x, y] == 0)
                            {
                                int near = MapUtil.GetSurrounding(
                                    rfr, x, y, 1, 0);
                                if (near > 0)
                                {
                                    spread = true;
                                    if (!_seed.PerRoll(rgh))
                                        _temp[x, y] = near;
                                }
                            }
                        }
                }
                // if there are still tiles left, fill with final reg
                int final = ++rootN;
                for (int x = bb.lX; x <= bb.hX; x++)
                    for (int y = bb.lY; y <= bb.hY; y++)
                    {
                        if (_map[x, y, LAYER_ETH].Equals(eth)
                            && _temp[x, y] == 0) _temp[x, y] = final;
                    }
                // set the temp to reg ID
                for (int x = bb.lX; x <= bb.hX; x++)
                    for (int y = bb.lY; y <= bb.hY; y++)
                    {
                        if (!_map[x, y, LAYER_REG].Equals(regBase)) continue;
                        int reg = _temp[x, y];
                        if (reg == 0) continue;
                        string regId = IntToId(reg, LEN_REG_ID);
                        _map[x, y, LAYER_REG] = regId;
                        if (GetNameRef(PolType.REGION, regId) == null)
                        {
                            CreateRef(PolType.REGION,
                                NG.NewRegionName(_seed), regId);
                            refs++;
                        }
                    }
            }
            NG.Purge();
        }

        private void ResetTemp() => _temp = new int[_size, _size];

        private void CreateRef(PolType type, string fullName, string enc)
            => WorldTools.CreateRef(type, fullName, enc, _info);

        private AbstractPol GetNameRef(PolType type, string enc)
            => WorldTools.GetRef(type, enc, _info);

        public static string IntToId(int i, int idLen)
        {
            string ret = "";
            int n = i;
            int len = ID.Length;
            for (int p = idLen - 1; p >= 0; p--)
            {
                int rm = (int)Math.Pow(len, p);
                ret += ID[n / rm];
                n -= rm * (n / rm);
            }
            return ret;
        }

        public static string BaseId(int idLen)
        {
            string ret = "";
            for (int n = 0; n < idLen; n++) ret += "0";
            return ret;
        }
    }
}