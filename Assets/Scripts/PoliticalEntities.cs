using System.Collections.Generic;
using UnityEngine;

namespace PoliticalEntities
{
    public enum PolType
    {
        ETHNIC = 0,
        REALM = 1,
        REGION = 2,
        ENTITY = 3
    }

    public abstract class PolEnt
    {
        protected string _name;
        protected readonly List<Vector2Int> _tiles = new();

        protected PolEnt() { }

        public string GetName() => _name;
        public void SetName(string name) => _name = name;
        public List<Vector2Int> Tiles() => _tiles;
    }

    public class Ethnic : PolEnt
    {
        private readonly List<Region> _regions = new();
        public List<Region> Regions() => _regions;
    }

    public class Realm : PolEnt
    {
        private readonly List<Region> _regions = new();
        public List<Region> Regions() => _regions;
    }

    public class Region : PolEnt
    {
    }

    public class World
    {
        private readonly List<Ethnic> _ethnics = new();
        public List<Ethnic> Ethnics() => _ethnics;
    }
}
