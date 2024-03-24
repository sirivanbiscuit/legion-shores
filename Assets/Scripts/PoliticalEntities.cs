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
    }

    public class Ethnic : AbstractPol
    {
        private readonly List<Region> _regions = new();
        public List<Region> Regions() => _regions;

        private readonly List<Realm> _realms = new();
        public List<Realm> Realms() => _realms;

        public Ethnic(string name) : base(name) { }
    }

    public class Realm : AbstractPol
    {
        private readonly List<Region> _regions = new();
        public List<Region> Regions() => _regions;

        public RealmType Type=RealmType.NULL;

        public Realm(string name) : base(name) { }

        public bool IsPlayable() => Type == RealmType.PLAYER;
    }

    public class Region : AbstractPol
    {
        private readonly List<Region> _near = new();
        public List<Region> Nearby() => _near;

        public Region(string name) : base(name) { }
    }

    public class Entity : AbstractPol
    {
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

        private readonly List<Entity> _entities = new();

        public byte[,] GetTerr() => _terr;
        public string[,,] GetPol() => _pol;

        public World(byte[,] terr, string[,,] pol,
            Dictionary<string, AbstractPol> info)
        {
            if (!(pol.GetLength(0) == pol.GetLength(1)
                && pol.GetLength(2) == WP.INFO_LAYERS))
                throw new ArgumentException("W01: Bad Map Size");
            _terr = terr; _pol = pol; _info = info;
            int size = pol.GetLength(0);
            string b = WP.BaseId(WP.LEN_ETH_ID);
            // build eth/reg/rea class structures
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    if (pol[x, y, WP.LAYER_ETH] != b)
                    {
                        Ethnic e = (Ethnic)WorldTools.GetNameRef(
                            PolType.ETHNIC, pol[x, y, WP.LAYER_ETH], info);
                        Region rg = (Region)WorldTools.GetNameRef(
                            PolType.REGION, pol[x, y, WP.LAYER_REG], info);
                        Realm rl = (Realm)WorldTools.GetNameRef(
                            PolType.REALM, pol[x, y, WP.LAYER_REA], info);
                        if (e == null || rg == null)
                            throw new ArgumentException("W03: Bad Refs");
                        if (!_ethnics.Contains(e)) _ethnics.Add(e);
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
                    Region at = (Region)WorldTools.GetNameRef(PolType.REGION,
                                pol[x, y, WP.LAYER_REG], info);
                    for (int i = -1; i <= 1; i++)
                        for (int j = -1; j <= 1; j++)
                            if (!(i == 0 || j == 0))
                            {
                                string n = pol[x + i, y + j, WP.LAYER_REG];
                                Region near = (Region)WorldTools.GetNameRef(
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
            => (Ethnic)_info[_pol[x, y, WP.LAYER_ETH]];

        public Region GetRegion(int x, int y)
            => (Region)_info[_pol[x, y, WP.LAYER_REG]];

        public Realm GetRealm(int x, int y)
            => (Realm)_info[_pol[x, y, WP.LAYER_REA]];

        public Entity GetEntity(int x, int y)
            => (Entity)_info[_pol[x, y, WP.LAYER_ENT]];

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
            AbstractPol ap;
            switch (type)
            {
                case PolType.ETHNIC: ap = new Ethnic(fullName); break;
                case PolType.REGION: ap = new Region(fullName); break;
                case PolType.REALM: ap = new Realm(fullName); break;
                case PolType.ENTITY: ap = new Entity(fullName); break;
                default: throw new ArgumentException("WT01: Bad PolType");
            }
            info.Add(((int)type) + ":" + enc, ap);
        }

        public static AbstractPol GetNameRef(PolType type, string enc,
            Dictionary<string, AbstractPol> info)
        {
            try { return info[((int)type) + ":" + enc]; }
            catch (KeyNotFoundException) { return null; }
        }
    }
}