using JetBrains.Annotations;
using SeedTools;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using WP = GenerationTools.WorldPopulator;
using NG = NameGenerators;

namespace PoliticalEntities
{
    /*
     * ABSTRACT ENTITIES
     */

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
        TINY = 36,
        SMALL = 49,
        NORMAL = 64,
        LARGE = 81,
        HUGE = 100
    }

    public enum RealmType
    {
        NULL = 0,
        PLAYER = 1,
        BARON = 2,
        LORD = 3
    }

    [Serializable]
    public abstract class AbstractPol
    {
        protected string _name;

        public AbstractPol(string name) => _name = name;

        public string GetName() => _name;
        public void SetName(string name) => _name = name;

        public abstract PolType PolType();
    }

    /*
     * GAME STRUCTURES/MECHANICS
     * - These have an actual presence in the gameplay
     */

    [Serializable]
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
        public int MinX, MaxX, MinY, MaxY;

        public readonly bool IsWild;

        public const string Wild = "Wilds";

        public override PolType PolType()
            => PoliticalEntities.PolType.ETHNIC;

        public Ethnic(string name) : base(name)
        { IsWild = name.Equals(Wild); }

        public int PlayersCount()
        {
            int count = 0;
            foreach (Realm r in _realms)
                if (r.Type == RealmType.PLAYER) count++;
            return count;
        }
    }

    [Serializable]
    public class Realm : AbstractPol
    {
        // never should be immutable
        private readonly List<Region> _regions = new();
        public List<Region> Regions() => _regions;

        // never should be immutable
        private readonly List<Realm> _vassals = new();
        private Realm _overlord = null;

        public RealmType Type = RealmType.NULL;

        public override PolType PolType()
            => PoliticalEntities.PolType.REALM;

        public Realm(string name) : base(name) { }

        public bool IsPlayable() => Type == RealmType.PLAYER;

        public void AddVassal(Realm vassal)
        {
            if (vassal == null)
                throw new ArgumentException("PE02: Could not add vassal");
            vassal._overlord?.RemoveVassal(vassal);
            vassal._overlord = this;
            _vassals.Add(vassal);
        }

        public void RemoveVassal(Realm vassal)
        {
            if (vassal == null || !_vassals.Contains(vassal))
                throw new ArgumentException("PE01: Could not remove vassal");
            vassal._overlord = null;
            _vassals.Remove(vassal);
        }

        public int VassalsCount() => _vassals.Count;
    }

    [Serializable]
    public class Region : AbstractPol
    {
        // should be immutable after world gen
        private readonly List<Region> _near = new();
        public List<Region> Nearby() => _near;

        public override PolType PolType()
            => PoliticalEntities.PolType.REGION;

        public Region(string name) : base(name) { }
    }

    [Serializable]
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
    [Serializable]
    public class World
    {
        private readonly byte[,] _terr;
        private readonly string[,,] _pol;
        private readonly List<Ethnic> _ethnics = new();
        private readonly Dictionary<string, AbstractPol> _info;

        private readonly int _seed;

        private readonly List<Entity> _entities = new();

        // tracks current ID for new placeable realms
        private int _rea = 1;

        public byte[,] GetTerr() => _terr;
        public string[,,] GetPol() => _pol;

        public World(byte[,] terr, string[,,] pol, int seedValue,
            Dictionary<string, AbstractPol> info)
        {
            if (!(pol.GetLength(0) == pol.GetLength(1)
                && pol.GetLength(2) == WP.INFO_LAYERS))
                throw new ArgumentException("W01: Bad Map Size");
            _terr = terr; _pol = pol; _info = info;
            _seed = seedValue;
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
                            e.MinX = x; e.MinY = y;
                            e.MaxX = x; e.MaxY = y;
                        }
                        else
                        {
                            if (x < e.MinX) e.MinX = x;
                            else if (x > e.MaxX) e.MaxX = x;
                            if (y < e.MinY) e.MinY = y;
                            else if (y > e.MaxY) e.MaxY = y;
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
                    if (at == null) continue;
                    for (int i = -1; i <= 1; i++)
                        for (int j = -1; j <= 1; j++)
                            if (!(i == 0 || j == 0))
                            {
                                string n = pol[x + i, y + j, WP.LAYER_REG];
                                Region near = (Region)WorldTools.GetRef(
                                    PolType.REGION, n, info);
                                if (n != b && near != at)
                                {
                                    if (!at.Nearby().Contains(near))
                                        at.Nearby().Add(near);
                                    if (!near.Nearby().Contains(at))
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

        public Realm[] GetRealms()
        {
            Realm[] find = new Realm[RealmsCount()];
            int i = 0;
            foreach (Ethnic e in _ethnics)
                foreach (Realm r in e.Realms()) find[i++] = r;
            return find;
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

        public int PlayerCount()
        {
            int num = 0;
            foreach (Ethnic e in _ethnics)
            {
                foreach (Realm r in e.Realms())
                    if (r.Type == RealmType.PLAYER) num++;
            }
            return num;
        }

        public int Size() => _terr.GetLength(0);
        public int Seed() => _seed;

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
                for (int x = eth.MinX; x <= eth.MaxX; x++)
                    for (int y = eth.MinY; y <= eth.MaxY; y++)
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
            Seed seed = new(_seed);
            foreach (Ethnic eth in _ethnics)
            {
                if (eth.IsWild) continue;
                List<Region> opts = new();
                foreach (Region o in eth.Regions()) opts.Add(o);
                int placedPl = 1;
                while (placedPl <= maxRealmsPerEth && opts.Count > 0)
                {
                    string pN = "House " + NG.NewRealmName(seed);
                    Realm rea = new(pN) { Type = RealmType.PLAYER };
                    placedPl++;
                    Region reg = opts[seed.RangeRoll(0, opts.Count - 1)];
                    AssignRegion(rea, reg);
                    opts.Remove(reg);
                    int lords = 0;
                    List<Region> lordRegs = new();
                    foreach (Region near in reg.Nearby())
                    {
                        if (opts.Contains(near))
                        {
                            opts.Remove(near);
                            if (++lords > 4) continue;
                            string lN = "Lord " + NG.NewRealmName(seed);
                            Realm lord = new(lN) { Type = RealmType.LORD };
                            AssignRegion(lord, near);
                            rea.AddVassal(lord);
                            foreach (Region nn in near.Nearby())
                                if (!lordRegs.Contains(nn)) lordRegs.Add(nn);
                        }
                    }
                    foreach (Region lR in lordRegs)
                        if (opts.Contains(lR)) opts.Remove(lR);
                }
            }
            NG.Purge();
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