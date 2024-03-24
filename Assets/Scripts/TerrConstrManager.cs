using GenerationTools;
using WP = GenerationTools.WorldPopulator;
using SeedTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using Random = System.Random;
using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class TerrConstrManager : MonoBehaviour
{
    private byte[,] constr;

    public byte[,] Export()
    {
        if (constr == null) throw new Exception("TCM01: Construct is null");
        return constr;
    }

    /*
     * GIANT PROCEDURAL SETTINGS LIST
     * 
     * Set these to something and the world might look decent.
     */

    public Tilemap terrMap;

    public int mapSize;
    public int mapSeed;
    public int borderThickness;

    public Tile cloudTile;

    public int cycles;
    public int floodsPerCycle;
    public int floodSize;
    public int floodOriginBuffer;
    public int floodSpreadBuffer;
    public double floodOriginVariance;
    public int floodDecay;
    public bool decayPerFlood;
    public bool decayPerCycle;

    public Tile oceanTile;
    public Tile swampTile;
    public Tile wetlandsTile;
    public Tile plainsTile;
    public Tile mountainsTile;

    public double fluidSettleStrength;
    public double landSettleStrength;
    public int fluidDryBuffer;
    public int landSinkBuffer;
    public int settleCycles;
    public int settleAreaBuffer;

    public Tile forestTile;

    public int forestSize;
    public int forestAmount;
    public double forestDensity;

    public Tile shallowTile;

    public double shoreSwamps;
    public int rivers;
    public int riverSize;
    public double riverStraightness;
    public double riverDepth;
    public double riverNatPref;

    public Tile desertTile;
    public Tile dryForestTile;

    public int desertCycles;
    public double desertVariance;
    public double weatherDirection;
    public double weatherForce;

    /*
     * GENERATION EXECUTORS
     */

    [ContextMenu("Clear Map")]
    void ClearMap() { terrMap.ClearAllTiles(); }

    [ContextMenu("Build Map (random seed)")]
    void ConstructRandMap()
    {
        Random rng = new();
        mapSeed = rng.Next(Seed.SEED_HIGH);
        ConstructMap();
    }

    [ContextMenu("Build Map (given seed)")]
    void ConstructMap()
    {
        // run terrain algorithm via WorldConstructor
        var watch = Stopwatch.StartNew();
        constr = MapConstructor.Export(mapSize,
            (c) => c
            .ApplySeed(mapSeed)
            .OverlayFlooderFill(cycles, floodsPerCycle, floodSize,
                floodOriginBuffer, floodSpreadBuffer,
                floodDecay, decayPerFlood, decayPerCycle,
                true, floodOriginVariance)
            .DeteriorateWetlands(0.1, true, 1, 0)
            .SettleFluids(fluidSettleStrength, landSettleStrength,
                fluidDryBuffer, landSinkBuffer, settleCycles, settleAreaBuffer)
            .GenerateRivers(rivers, riverSize, true,
                riverStraightness, riverDepth, riverNatPref, 0, 0)
            .SimpleLandCleanup(false, 0)
            .BuildShalllows(shoreSwamps, 0)
            .ExpandShallows(1, 0)
            .GrowForests(forestSize, forestAmount, 0,
                false, forestDensity)
            .Desertifacation(1, 0, true, weatherDirection, 0, 0)
            .Desertifacation(desertCycles, 0, false,
                weatherDirection, weatherForce, desertVariance)
            .SimpleLandCleanup(false, 0)
            .SimpleLandCleanup(false, 0)
            .SetCloudBorder(borderThickness)
            );
        // clear map and paint
        AssetDatabase.Refresh();
        ClearMap();
        Tile[] types = MapUtil.GetTileSet(
            cloudTile,
            oceanTile, swampTile, wetlandsTile, shallowTile,
            plainsTile, mountainsTile, forestTile,
            desertTile, dryForestTile);
        for (int x = 0; x < mapSize; x++)
            for (int y = 0; y < mapSize; y++)
            {
                terrMap.SetTile(new(x, y), types[constr[x, y]]);
            }
        watch.Stop();
        Debug.Log($"Terr exe time: {watch.ElapsedMilliseconds} ms");
    }
}