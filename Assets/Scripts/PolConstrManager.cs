using GenerationTools;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using WP = GenerationTools.WorldPopulator;
using System;
using RNG = System.Random;
using SeedTools;
using PoliticalEntities;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class PolConstrManager : MonoBehaviour
{
    public TerrConstrManager terrainManager;

    public Tilemap polMap;
    public Tile landFiller;
    public Tile waterFiller;
    public Tile mountFiller;

    public bool showWilds;

    public int maxEthnics;
    public int maxRealmsPerEthnic;
    public RegS regionType;
    public double regionRoughness;
    public bool forceWildDeserts;

    [ContextMenu("Clear Map")]
    void ClearMap() { polMap.ClearAllTiles(); }

    [ContextMenu("Populate Map (Overlay Normal)")]
    void PopulateOverlayN() => Populate(true, false);

    [ContextMenu("Populate Map (Overlay Advanced)")]
    void PopulateOverlayA() => Populate(true, true);

    [ContextMenu("Populate Map (Offset Normal)")]
    void PopulateOffsetN() => Populate(false, false);

    [ContextMenu("Populate Map (Offset Advanced)")]
    void PopulateOffsetA() => Populate(false, true);

    void Populate(bool overlay, bool regs)
    {
        // run populator algorithm
        var watch = Stopwatch.StartNew();
        byte[,] exp = terrainManager.Export();
        Seed seed = new(terrainManager.mapSeed);
        World world = WP.Export(
            exp, terrainManager.mapSeed,
            maxEthnics, regionType, regionRoughness,
            forceWildDeserts);
        world.SpawnWorldRealms(maxRealmsPerEthnic);
        string[,,] p_constr = world.GetPol();
        // clear and position map
        AssetDatabase.Refresh();
        ClearMap();
        int size = terrainManager.mapSize;
        Vector3 tPos = terrainManager.terrMap.transform.position;
        polMap.transform.position = new(
            tPos.x, //+ (overlay ? 0 : (size / 2 + 10)),
            tPos.y - (overlay ? 0 : (size / 2 + 10)),
            tPos.z
            );
        // setup colour mappings for eth/reg
        Dictionary<string, Color> cs = new();
        Dictionary<string, float> reg_cs = new();
        List<Color> usedCols = new();
        int l = WP.LAYER_ETH;
        int reg_l = WP.LAYER_REG;
        string w = WP.IntToId(maxEthnics + 1, WP.LEN_ETH_ID);
        Vector3Int get;
        float r, g, b, rgbF;
        int cRt = (int)Math.Ceiling(Math.Pow(maxEthnics, 1d / 3d));
        Color c;
        string nil = WP.BaseId(WP.LEN_ETH_ID);
        string nilrea = WP.BaseId(WP.LEN_REA_ID);
        // loop across map grid
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                get = new(x, y);
                string str = p_constr[x, y, l];
                string regStr = p_constr[x, y, reg_l];
                bool rea = p_constr[x, y, WP.LAYER_REA] != nilrea;
                // create a new reg key if necessary
                if (!reg_cs.ContainsKey(regStr))
                {
                    reg_cs[regStr] = regs ?
                        (rea ? 0.0f : (float)seed.PerRaw() * 0.5f + 0.5f)
                        : 1.0f;
                }
                // eth case 1: a non-null non-wild tile
                if (str != nil && str != w)
                {
                    // create a new eth key if necessary
                    // every eth must have a different colour
                    if (!cs.ContainsKey(str))
                    {
                        bool foundNew;
                        int maxIter = 1024, iter = 0;
                        do
                        {
                            if (iter++ > maxIter)
                                throw new Exception(
                                    "PCM01: Your while-loop fucking sucks");
                            foundNew = true;
                            r = seed.RangeRoll(0, cRt);
                            g = seed.RangeRoll(0, cRt);
                            b = seed.RangeRoll(0, cRt);
                            rgbF = 2f / (r + g + b);
                            c = new(r * rgbF, g * rgbF, b * rgbF);
                            if (c.r > 1.5f || c.g > 1.5f || c.b > 1.5f)
                            { foundNew = false; }
                            foreach (Color u in usedCols)
                                if (u.r == c.r && u.g == c.g)
                                { foundNew = false; break; }
                        } while (!foundNew);
                        cs[str] = c;
                        usedCols.Add(c);
                    }
                    // set the tile map (colour=eth, shade=reg)
                    polMap.SetTile(get, landFiller);
                    polMap.RemoveTileFlags(get, TileFlags.LockColor);
                    polMap.SetColor(get, new(
                        cs[str].r * reg_cs[regStr],
                        cs[str].g * reg_cs[regStr],
                        cs[str].b * reg_cs[regStr]
                        ));
                }
                // eth case 2: the tile is impass, thus no eth
                else if (!overlay && (str == nil || !showWilds))
                {
                    // water tile
                    if (MapUtil.IsAquatic(exp[x, y]))
                        polMap.SetTile(get, waterFiller);
                    // mountain tile
                    else polMap.SetTile(get, mountFiller);
                }
                // eth case 3: the tile is wild
                else if (!overlay)
                {
                    // wilds tile
                    polMap.SetTile(get, landFiller);
                    polMap.RemoveTileFlags(get, TileFlags.LockColor);
                    if (regs)
                        polMap.SetColor(get, new(
                            0.3f * reg_cs[regStr],
                            0.3f * reg_cs[regStr],
                            0.3f * reg_cs[regStr]
                            ));
                    else
                        polMap.SetColor(get, Color.black);
                }
            }
        // set transparency and quit
        Color bC = polMap.color;
        bC.a = overlay ? 0.25f : 1f;
        polMap.color = bC;
        watch.Stop();
        Debug.Log($"Pol exe time: {watch.ElapsedMilliseconds} ms  " +
                  $"({world.EthnicsCount()} Ethnics, " +
                  $"{world.RegionsCount()} Regions)");
    }
}
