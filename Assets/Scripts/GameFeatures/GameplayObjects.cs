using ResourceDecks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameplayObjects
{
    /*
     * North: High Y (isometric top-left)
     * East: High X (isometric top-right)
     * South: Low Y (isometric bottom-right)
     * West: Low X (isometric bottom-left)
     */
    public enum UnitDir
    { NORTH, EAST, SOUTH, WEST }

    [Serializable]
    public abstract class MapUnit<T, C>
    {
        public readonly T Type;
        public readonly C Value;

        private int _counters = 0;

        public MapUnit(T t, C c) { Type = t; Value = c; }

        public UnitDir Direction = UnitDir.EAST;

        public void PlaceCounter() => _counters++;
        public int Counters() => _counters;
    }

    public enum LandUnitType
    { INFANTRY, CAVALRY, ARCHERY, ARTILLERY }
    public enum LandUnitClass
    { LIGHT, MEDIUM, HEAVY }

    public class LandUnit : MapUnit<LandUnitType, LandUnitClass>
    {
        public LandUnit(TroopCard origin)
            : base(GetType(origin), GetClass(origin)) { }

        private static LandUnitType GetType(TroopCard card) => card switch
        {
            // Infantry
            TroopCard.INFANTRY_LIGHT
            or TroopCard.INFANTRY_MEDIUM
            or TroopCard.INFANTRY_HEAVY => LandUnitType.INFANTRY,
            // Cavalry
            TroopCard.CAVALRY_LIGHT
            or TroopCard.CAVALRY_MEDIUM
            or TroopCard.CAVALRY_HEAVY => LandUnitType.CAVALRY,
            // Archery
            TroopCard.ARCHERY_LIGHT
            or TroopCard.ARCHERY_MEDIUM
            or TroopCard.ARCHERY_HEAVY => LandUnitType.ARCHERY,
            // Artillery
            TroopCard.ARTILLERY_LIGHT
            or TroopCard.ARTILLERY_MEDIUM
            or TroopCard.ARTILLERY_HEAVY => LandUnitType.ARTILLERY,
            // Null
            _ => throw new Exception("GO01: Null Type"),
        };

        private static LandUnitClass GetClass(TroopCard card) => card switch
        {
            // Light
            TroopCard.INFANTRY_LIGHT
            or TroopCard.CAVALRY_LIGHT
            or TroopCard.ARCHERY_LIGHT
            or TroopCard.ARTILLERY_LIGHT => LandUnitClass.LIGHT,
            // Medium
            TroopCard.INFANTRY_MEDIUM
            or TroopCard.CAVALRY_MEDIUM
            or TroopCard.ARCHERY_MEDIUM
            or TroopCard.ARTILLERY_MEDIUM => LandUnitClass.MEDIUM,
            // Heavy
            TroopCard.INFANTRY_HEAVY
            or TroopCard.CAVALRY_HEAVY
            or TroopCard.ARCHERY_HEAVY
            or TroopCard.ARTILLERY_HEAVY => LandUnitClass.HEAVY,
            // Null
            _ => throw new Exception("GO02: Null Class"),
        };
    }

    public enum NavalUnitType
    { TRANSPORT, FRIGATE }
    public enum NavalUnitClass
    { LIGHT, HEAVY }

    public class NavalUnit : MapUnit<NavalUnitType, NavalUnitClass>
    {
        public NavalUnit(ShipCard origin)
            : base(GetType(origin), GetClass(origin)) { }

        private static NavalUnitType GetType(ShipCard card) => card switch
        {
            // Transport
            ShipCard.TRANSPORT_LIGHT
            or ShipCard.TRANSPORT_HEAVY => NavalUnitType.TRANSPORT,
            // Frigate
            ShipCard.FRIGATE_LIGHT
            or ShipCard.FRIGATE_HEAVY => NavalUnitType.FRIGATE,
            // Null
            _ => throw new Exception("GO03: Null Type"),
        };

        private static NavalUnitClass GetClass(ShipCard card) => card switch
        {
            // Light
            ShipCard.TRANSPORT_LIGHT
            or ShipCard.FRIGATE_LIGHT => NavalUnitClass.LIGHT,
            // Heavy
            ShipCard.TRANSPORT_HEAVY
            or ShipCard.FRIGATE_HEAVY => NavalUnitClass.HEAVY,
            // Null
            _ => throw new Exception("GO04: Null Class"),
        };
    }

    public enum NullUnitType
    { JAZZ }

    public enum NullUnitClass
    { STANDARD }

    public class NullUnit : MapUnit<NullUnitType, NullUnitClass>
    {
        public NullUnit(TroopCard origin)
            : base(GetType(origin), GetClass(origin)) { }

        private static NullUnitType GetType(TroopCard card) => card switch
        {
            // Jazz
            TroopCard.JAZZ => NullUnitType.JAZZ,
            // Null
            _ => throw new Exception("GO05: Null Type"),
        };

        private static NullUnitClass GetClass(TroopCard card) => card switch
        {
            // All
            TroopCard.JAZZ => NullUnitClass.STANDARD,
            // Null
            _ => throw new Exception("GO06: Null Class"),
        };
    }

}
