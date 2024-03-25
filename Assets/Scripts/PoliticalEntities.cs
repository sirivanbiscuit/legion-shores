using JetBrains.Annotations;
using SeedTools;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using WP = GenerationTools.WorldPopulator;

namespace PoliticalEntities
{
    public enum PolType
    {
        // Should be only ONE digit
        ETHNIC = 0,
        REALM = 1,
        REGION = 2,
        ENTITY = 3
    }

    public enum RegS
    {
        TINY = 16,
        SMALL = 25,
        NORMAL = 36,
        LARGE = 49,
        HUGE = 64
    }

    public enum RealmType
    {
        NULL = 0,
        PLAYER = 1,
        BARON = 2,
        LORD = 3
    }

    public abstract class AbstractPol
    {
        protected string _name;

        public AbstractPol(string name) => _name = name;

        public string GetName() => _name;
        public void SetName(string name) => _name = name;

        public abstract PolType PolType();
    }

    public class Ethnic : AbstractPol
    {
        // should be immutable after world gen
        private readonly List<Region> _regions = new();
        public List<Region> Regions() => _regions;

        // never should be immutable
        private readonly List<Realm> _realms = new();
        public List<Realm> Realms() => _realms;

        // should be immutable after world gen
        // but matters less bc this is just for efficiency
        public Vector2Int minVec, maxVec;

        public readonly bool IsWild;

        public const string Wild = "Wilds";

        public override PolType PolType()
            => PoliticalEntities.PolType.ETHNIC;

        public Ethnic(string name) : base(name)
        { IsWild = name == Wild; }
    }

    public class Realm : AbstractPol
    {
        // never should be immutable
        private readonly List<Region> _regions = new();
        public List<Region> Regions() => _regions;

        public RealmType Type = RealmType.NULL;

        public override PolType PolType()
            => PoliticalEntities.PolType.REALM;

        public Realm(string name) : base(name) { }

        public bool IsPlayable() => Type == RealmType.PLAYER;
    }

    public class Region : AbstractPol
    {
        // should be immutable after world gen
        private readonly List<Region> _near = new();
        public List<Region> Nearby() => _near;

        public override PolType PolType()
            => PoliticalEntities.PolType.REGION;

        public Region(string name) : base(name) { }
    }

    public class Entity : AbstractPol
    {
        public override PolType PolType()
            => PoliticalEntities.PolType.ENTITY;

        public Entity(string name) : base(name) { }
    }

    /// <summary>
    /// A package for storing world map data and navigating ethnic/region
    /// classes represented by the map. WorldPopulator creates one of these.
    /// </summary>
    public class World
    {
        private readonly byte[,] _terr;
        private readonly string[,,] _pol;
        private readonly List<Ethnic> _ethnics = new();
        private readonly Dictionary<string, AbstractPol> _info;

        private readonly Seed _seed;

        private readonly List<Entity> _entities = new();

        // tracks current ID for new placeable realms
        private int _rea = 0;

        public byte[,] GetTerr() => _terr;
        public string[,,] GetPol() => _pol;

        public World(byte[,] terr, string[,,] pol, int seedValue,
            Dictionary<string, AbstractPol> info)
        {
            if (!(pol.GetLength(0) == pol.GetLength(1)
                && pol.GetLength(2) == WP.INFO_LAYERS))
                throw new ArgumentException("W01: Bad Map Size");
            _terr = terr; _pol = pol; _info = info;
            _seed = new(seedValue);
            int size = pol.GetLength(0);
            string b = WP.BaseId(WP.LEN_ETH_ID);
            // build eth/reg/rea class structures
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    if (pol[x, y, WP.LAYER_ETH] != b)
                    {
                        Ethnic e = (Ethnic)WorldTools.GetRef(
                            PolType.ETHNIC, pol[x, y, WP.LAYER_ETH], info);
                        Region rg = (Region)WorldTools.GetRef(
                            PolType.REGION, pol[x, y, WP.LAYER_REG], info);
                        Realm rl = (Realm)WorldTools.GetRef(
                            PolType.REALM, pol[x, y, WP.LAYER_REA], info);
                        if (e == null || rg == null)
                            throw new ArgumentException("W03: Bad Refs");
                        if (!_ethnics.Contains(e))
                        {
                            _ethnics.Add(e);
                            e.minVec = new(x, y); e.maxVec = new(x, y);
                        }
                        else
                        {
                            Vector2Int l = e.minVec, h = e.maxVec;
                            if (x < l.x) e.minVec.x = x;
                            else if (x > h.x) e.maxVec.x = x;
                            if (y < l.y) e.minVec.y = y;
                            else if (y > h.y) e.maxVec.y = y;
                        }
                        if (!e.Regions().Contains(rg)) e.Regions().Add(rg);
                        if (rl == null) continue;
                        if (!e.Realms().Contains(rl)) e.Realms().Add(rl);
                        if (!rl.Regions().Contains(rg)) rl.Regions().Add(rg);
                    }
            // find region adjacencies
            b = WP.BaseId(WP.LEN_REG_ID);
            for (int x = 1; x < size - 1; x++)
                for (int y = 1; y < size - 1; y++)
                {
                    Region at = (Region)WorldTools.GetRef(PolType.REGION,
                                pol[x, y, WP.LAYER_REG], info);
                    for (int i = -1; i <= 1; i++)
                        for (int j = -1; j <= 1; j++)
                            if (!(i == 0 || j == 0))
                            {
                                string n = pol[x + i, y + j, WP.LAYER_REG];
                                Region near = (Region)WorldTools.GetRef(
                                    PolType.REGION, n, info);
                                if (n != b && near != at
                                    && !at.Nearby().Contains(near))
                                {
                                    at.Nearby().Add(near);
                                    near.Nearby().Add(at);
                                }
                            }
                }
        }

        public Ethnic GetEthnic(int x, int y)
            => (Ethnic)WorldTools.GetRef(PolType.ETHNIC,
                _pol[x, y, WP.LAYER_ETH], _info);
        public Region GetRegion(int x, int y)
            => (Region)WorldTools.GetRef(PolType.REGION,
                _pol[x, y, WP.LAYER_REG], _info);
        public Realm GetRealm(int x, int y)
            => (Realm)WorldTools.GetRef(PolType.REALM,
                _pol[x, y, WP.LAYER_REA], _info);
        public Entity GetEntity(int x, int y)
            => (Entity)WorldTools.GetRef(PolType.ENTITY,
                _pol[x, y, WP.LAYER_ENT], _info);

        public Ethnic GetEthnic(Realm realm)
        {
            foreach (Ethnic e in _ethnics)
                if (e.Realms().Contains(realm)) return e;
            return null;
        }

        public Ethnic GetEthnic(Region region)
        {
            foreach (Ethnic e in _ethnics)
                if (e.Regions().Contains(region)) return e;
            return null;
        }

        public int EthnicsCount() => _ethnics.Count;
        public int EntitiesCount() => _entities.Count;

        public int RegionsCount()
        {
            int num = 0;
            foreach (Ethnic e in _ethnics) num += e.Regions().Count;
            return num;
        }

        public int RealmsCount()
        {
            int num = 0;
            foreach (Ethnic e in _ethnics) num += e.Realms().Count;
            return num;
        }

        /// <summary>
        /// Attempts to assign the given Region to the given Realm. 
        /// <para/>
        /// If the Realm isn't on the map yet (and thus has no key ref),
        /// such will be taken care of.
        /// </summary>
        public void AssignRegion(Realm target, Region src)
        {
            // verify object presence
            if (target == null || src == null)
                throw new ArgumentException("W02: Null Reg/Rea");
            // verify key presence
            string key = WorldTools.FindKey(target, _info);
            string bR = WP.BaseId(WP.LEN_REG_ID);
            if (key != null)
            {
                // set up the object references
                Ethnic eth = GetEthnic(src);
                target.Regions().Add(src);
                eth.Realms().Add(target);
                // set realm IDs on the map
                Vector2Int l = eth.minVec, h = eth.maxVec;
                for (int x = l.x; x <= h.x; x++)
                    for (int y = l.y; y <= h.y; y++)
                    {
                        if (_pol[x, y, WP.LAYER_REG] != bR
                            && GetRegion(x, y) == src)
                        {
                            _pol[x, y, WP.LAYER_REA] = key;
                        }
                    }
            }
            // failed to find key: make one!
            else
            {
                /* It is more likely that I get hit by a bus on my next walk
                 * to the cafe I wrote this in, than it is for this bug to
                 * occur ever. HOWEVER since it completely obliterates the 
                 * game upon said occurence, W03 catches it anyway.
                 */
                int lR = WP.LEN_REG_ID;
                if (_rea >= Math.Pow(WP.ID.Length, lR))
                    throw new Exception("W03: Huh?");
                // probably safe past this line
                WorldTools.CreateRef(target, WP.IntToId(_rea++, lR), _info);
                AssignRegion(target, src);
            }
        }

        /// <summary>
        /// Fills all Ethnics with up to a given number of default
        /// playable starting Realms. 
        /// <para/>
        /// The default start consists of a playable crownland region
        /// surrounded by up to 4 lordships (if there isn't enough space
        /// for all 4, then less will spawn - start locations are NOT
        /// necessarily intended to be balanced).
        /// </summary>
        public void SpawnWorldRealms(int maxRealmsPerEth)
        {
            foreach (Ethnic eth in _ethnics)
            {
                if (eth.IsWild) continue;
                int placedPl = 0, placedLr = 0;
                List<Region> opts = eth.Regions();
                while (placedPl < maxRealmsPerEth && opts.Count > 0)
                {
                    Realm rea = new("Player " + placedPl++);
                    Region reg = opts[_seed.RangeRoll(0, opts.Count - 1)];
                    AssignRegion(rea, reg);
                    opts.Remove(reg);
                    int lords = 0;
                    foreach (Region near in reg.Nearby())
                    {
                        if (opts.Contains(near) && lords++ <= 3)
                        {
                            Realm lord = new("Lord " + placedLr++);
                            AssignRegion(lord, near);
                            foreach (Region nn in near.Nearby())
                            { if (opts.Contains(nn)) opts.Remove(nn); }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Utility class for world creation/referencing
    /// </summary>
    public static class WorldTools
    {
        public static void CreateRef(PolType type,
            string fullName, string enc,
            Dictionary<string, AbstractPol> info)
        {
            CreateRef(type switch
            {
                PolType.ETHNIC => new Ethnic(fullName),
                PolType.REGION => new Region(fullName),
                PolType.REALM => new Realm(fullName),
                PolType.ENTITY => new Entity(fullName),
                _ => throw new ArgumentException("WT01: Bad PolType"),
            },
            enc, info);
        }

        public static void CreateRef(AbstractPol pol, string enc,
            Dictionary<string, AbstractPol> info)
            => info[Bind(pol.PolType(), enc)] = pol;


        public static AbstractPol GetRef(PolType type, string enc,
            Dictionary<string, AbstractPol> info)
        {
            try { return info[Bind(type, enc)]; }
            catch (KeyNotFoundException) { return null; }
        }

        public static string FindKey(AbstractPol pol,
            Dictionary<string, AbstractPol> info)
        {
            foreach (string s in info.Keys)
                if (info[s] == pol) return Unbind(s);
            return null;
        }

        private static string Bind(PolType type, string enc)
            => ((int)type) + ":" + enc;

        private static string Unbind(string referenceKey)
            => referenceKey[2..];
    }
}