using GenerationTools;
using PoliticalEntities;
using SeedTools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.Tilemaps;
using Color = UnityEngine.Color;
using WP = GenerationTools.WorldPopulator;

public class Minimap : MonoBehaviour
{
    public MinimapSelectManager manager;

    public Texture2D[] tiles;

    private Color[,] terColCache;
    private Color[,] ethColCache;
    private bool nullTerCache = true;
    private bool nullEthCache = true;

    public void SetCacheNull() { nullTerCache = true; nullEthCache = true; }

    public void DrawTerrain(byte[,] reference)
    {
        int mapLength = reference.GetLength(0);
        Texture2D mapTexture = new(mapLength, mapLength)
        { filterMode = FilterMode.Point };
        if (nullTerCache) ImportTerr(reference, false);
        for (int x = 0; x < mapLength; x++)
            for (int y = 0; y < mapLength; y++)
            {
                mapTexture.SetPixel(x, y, terColCache[x, y]);
            }
        mapTexture.Apply();
        Material m = new(Shader.Find("UI/Default"));
        m.mainTexture = mapTexture;
        MeshRenderer r = GetComponent<MeshRenderer>();
        r.material = m;
    }

    public void DrawEthnics(string[,,] reference, bool reg)
    {
        // tile width/length are always 1 in this context
        int mapLength = reference.GetLength(0);
        Texture2D mapTexture = new(mapLength, mapLength)
        { filterMode = FilterMode.Point };
        if (nullEthCache) ImportPol(reference, reg, false);
        for (int x = 0; x < mapLength; x++)
            for (int y = 0; y < mapLength; y++)
            {
                mapTexture.SetPixel(x, y, ethColCache[x, y]);
            }
        mapTexture.Apply();
        Material m = new(Shader.Find("UI/Default"));
        m.mainTexture = mapTexture;
        MeshRenderer r = GetComponent<MeshRenderer>();
        r.material = m;
    }

    // NOTE: forcibly overrides the caches each time
    public void DrawWorld(World world)
    {
        int wL = world.GetTerr().GetLength(0);
        int e = 0;
        ImportTerr(world.GetTerr(), true);
        ImportPol(world.GetPol(), false, true);
        Dictionary<Color, Minimap> ethMap = new(); // pol maps
        Dictionary<Color, Texture2D> texs = new(); // pol textures
        Texture2D main = new(wL, wL) // terrain on this one
        { filterMode = FilterMode.Point };
        Texture2D temp = new(1, 1);
        for (int x = 0; x < wL; x++)
            for (int y = 0; y < wL; y++)
            {
                // set terrain
                Color t = terColCache[x, y];
                main.SetPixel(x, y, t);
                // try to find colour in dict
                Color eth = ethColCache[x, y];
                temp.SetPixel(x, y, eth);
                temp.Apply();
                eth = temp.GetPixel(x, y);
                if (eth.a == 0f) continue;
                if (!ethMap.ContainsKey(eth))
                {
                    ethMap[eth] = Instantiate(this);
                    ethMap[eth].transform.SetParent(transform.parent, false);
                    ethMap[eth].name = "Eth" + e++;
                    texs[eth] = new(wL, wL) { filterMode = FilterMode.Point };
                    for (int i = 0; i < wL; i++) for (int j = 0; j < wL; j++)
                        { texs[eth].SetPixel(i, j, Color.clear); }
                }
                // set ethnic border
                texs[eth].SetPixel(x, y, eth);

            }
        // for each colour there will be a map and a texture
        // these can both be bound one by one
        foreach (Color c in ethMap.Keys)
        {
            texs[c].Apply();
            ethMap[c].GetComponent<MeshRenderer>().material
                = new(Shader.Find("UI/Default"))
                { mainTexture = texs[c] };
        }
        // apply terrain map in background
        main.Apply();
        GetComponent<MeshRenderer>().material
            = new(Shader.Find("UI/Default"))
            { mainTexture = main };
        // push the map dict
        manager.Bind(ethMap);
    }

    /// <summary>
    /// Converts the given Terr Map construct into a 2D colour pixel array
    /// representing the terrain and biomes of the map.
    /// </summary>
    public void ImportTerr(byte[,] bytes, bool trans)
    {
        int size = bytes.GetLength(0);
        Color[,] export = new Color[size, size];
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                Texture2D t = tiles[bytes[x, y]];
                Color c = t.GetPixel(0, 0);
                if (trans) c.a = 0.5f;
                export[x, y] = c;
            }
        terColCache = export;
        nullTerCache = false;
    }

    /// <summary>
    /// Converts the given Pol Map construct into a 2D colour pixel array
    /// representing the ethnic and regional borders of the map.
    /// </summary>
    public void ImportPol(string[,,] p_constr, bool reg, bool trans)
    {
        // setup colour mappings for eth/reg
        int size = p_constr.GetLength(0);
        Dictionary<string, Color> cs = new();
        Dictionary<string, float> reg_cs = new();
        Color[,] export = new Color[size, size];
        List<Color> usedCols = new();
        Seed seed = new();
        int l = WP.LAYER_ETH;
        int reg_l = WP.LAYER_REG;
        string w = WP.IntToId(WP.MAX_ETHNICS, WP.LEN_ETH_ID);
        int cRt = (int)Math.Ceiling(Math.Pow(WP.MAX_ETHNICS, 1d / 2d));
        string nil = WP.BaseId(WP.LEN_ETH_ID);
        string nilrea = WP.BaseId(WP.LEN_REA_ID);
        // loop across map grid
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                // force solid white border
                if (!trans &&
                    (x == 0 || y == 0 || x == size - 1 || y == size - 1))
                {
                    export[x, y] = new(1f, 1f, 1f);
                    continue;
                }
                // continue otherwise
                string str = p_constr[x, y, l];
                string regStr = p_constr[x, y, reg_l];
                bool rea = p_constr[x, y, WP.LAYER_REA] != nilrea;
                // create a new reg key if necessary
                if (!reg_cs.ContainsKey(regStr))
                {
                    reg_cs[regStr] = rea ? (float)seed.PerRaw() * 0.1f + 0.9f
                        : (float)seed.PerRaw() * 0.3f + 0.2f;
                }
                // eth case 1: a non-null non-wild tile
                if (str != nil && str != w)
                {
                    // create a new eth key if necessary
                    // every eth must have a different colour
                    if (!cs.ContainsKey(str))
                    {
                        // if this stays black then an error occured
                        Color c = new(0, 0, 0);
                        bool foundNew;
                        int maxIter = 1024, iter = 0;
                        do
                        {
                            if (iter++ > maxIter)
                                throw new Exception(
                                    "PCM01: Your while-loop fucking sucks");
                            foundNew = true;
                            float r = seed.RangeRoll(0, cRt);
                            float g = seed.RangeRoll(0, cRt);
                            float b = seed.RangeRoll(0, cRt);
                            if (r == 0 || g == 0 || b == 0)
                            { foundNew = false; continue; }
                            float rgbF = 2f / (r + g + b);
                            c = new(r * rgbF, g * rgbF, b * rgbF);
                            if (c.r > 1.5f || c.g > 1.5f || c.b > 1.5f)
                            { foundNew = false; continue; }
                            foreach (Color u in usedCols)
                                if (u.r == c.r && u.g == c.g)
                                { foundNew = false; break; }
                        } while (!foundNew);
                        cs[str] = c;
                        usedCols.Add(c);
                    }
                    // set the tile map (colour=eth, shade=reg)
                    float mult = reg ? reg_cs[regStr] : 1f;
                    export[x, y] = new(
                        cs[str].r * mult,
                        cs[str].g * mult,
                        cs[str].b * mult
                        );
                }
                // eth case 2: the tile is impass, thus no eth
                else if (str.Equals(nil)) export[x, y]
                        = new(0f, 0f, 0f, trans ? 0f : 1f);
                // eth case 3: the tile is wild
                else export[x, y] = trans
                        ? new(0f, 0f, 0f, 0f)
                        : new(
                            0.3f * reg_cs[regStr],
                            0.3f * reg_cs[regStr],
                            0.3f * reg_cs[regStr]
                        );
            }
        // export colour map
        ethColCache = export;
        nullEthCache = false;
    }

}