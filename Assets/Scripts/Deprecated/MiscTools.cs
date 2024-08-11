using PoliticalEntities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiscTools
{
    /*
     * This is a giant dump of code snippets I don't want to get rid of
     * ATM but might be useful later. Also they took too long to make
     * to make deletion worthwhile.
     */

    /*
    // test
    foreach (Ethnic e in _ethnics)
    {
        int p = 0;
        foreach (Realm r in e.Realms()) 
            if (r.Type == RealmType.PLAYER) p++;
        Debug.Log(e.GetName() + " has " 
            + e.Regions().Count + " regions, " 
            + p + " players, and " 
            + e.Realms().Count + " realms");
    }
    // test
    foreach (Ethnic e in _ethnics)
    {
        foreach (Ethnic check in _ethnics)
        {
            if (e != check)
            {
                foreach (Region r1 in e.Regions())
                    foreach (Region r2 in check.Regions())
                    {
                        if (r1 == r2)
                        {
                            Debug.Log(r1.GetName() + " is in both "
                + e.GetName() + " and " + check.GetName()); break;
                        }
                    }
            }
        }
    }

    // test
    Debug.Log(RegionsCount() + " regions via count");
    List<string> thing = new();
    for (int i = 0; i < _pol.GetLength(0); i++)
        for (int j = 0; j < _pol.GetLength(1); j++)
        {
            string s = _pol[i, j, WP.LAYER_REG];
            if (!thing.Contains(s)) thing.Add(s);
        }
    Debug.Log(thing.Count + " regions via map");
    int find = 0;
    foreach (AbstractPol p in _info.Values)
    {
        if (p is Region) find++;
    }
    Debug.Log(find + " regions via dict");*/


    /*
    class GridResource
    {
        public int MapLen;
        public string TerrStr;
        public string PopStr;

        public GridResource(int mapLen, string terrStr, string popStr)
        { MapLen = mapLen; TerrStr = terrStr; PopStr = popStr; }
    }

    static string DeconstructByteGrid(byte[,] src)
    {
        string str = "";
        int len = src.GetLength(0);
        for (int x = 0; x < len; x++)
            for (int y = 0; y < len; y++)
                str += src[x, y];
        return str;
    }

    static string DeconstructStrGrid(string[,,] src)
    {
        string str = "";
        int len = src.GetLength(1);
        for (int x = 0; x < len; x++)
            for (int y = 0; y < len; y++)
            {
                for (int l = 0; l < WP.INFO_LAYERS; l++)
                    str += src[x, y, l];
                str += WP.DIV;
            }

        return str;
    }

    static byte[,] ReconstructByteGrid(string src, int len)
    {
        byte[,] map = new byte[len, len];
        for (int i = 0; i < src.Length; i++)
            map[i / len, i % len] = Convert.ToByte(src[i] + "");
        return map;
    }

    static string[,,] ReconstructStrGrid(string src, int len)
    {
        string[,,] map = new string[len, len, WP.INFO_LAYERS];
        string term = "";
        char read;
        int t = 0;
        int x, y;
        int eth = WP.LEN_ETH_ID, eth_i = WP.LAYER_ETH;
        int rea = WP.LEN_REA_ID, rea_i = WP.LAYER_REA;
        int reg = WP.LEN_REG_ID, reg_i = WP.LAYER_REG;
        int ent = WP.LEN_ENT_ID, ent_i = WP.LAYER_ENT;
        for (int i = 0; i < src.Length; i++)
        {
            read = src[i];
            term += read;
            if (read == WP.DIV)
            {
                x = t / len;
                y = t % len;
                map[x, y, eth_i] = term[..eth];
                map[x, y, rea_i] = term[eth..(eth + rea)];
                map[x, y, reg_i] = term[rea..(eth + rea + reg)];
                map[x, y, ent_i] = term[reg..(eth + rea + reg + ent)];
                term = "";
                t++;
            }
        }
        return map;
    }

    void PaintFrom(TextAsset jsonMap)
    {
        AssetDatabase.Refresh();
        ClearMap();
        GridResource resCont =
            JsonUtility.FromJson<GridResource>(jsonMap.text);
        Tile[] types = MapUtil.GetTileSet(
            cloudTile,
            oceanTile, swampTile, wetlandsTile, shallowTile,
            plainsTile, mountainsTile, forestTile,
            desertTile, dryForestTile);
        byte[,] map = ReconstructByteGrid(resCont.TerrStr, resCont.MapLen);
        string[,,] p_map = ReconstructStrGrid(resCont.PopStr, resCont.MapLen);
        for (int x = 0; x < resCont.MapLen; x++)
            for (int y = 0; y < resCont.MapLen; y++)
                terrMap.SetTile(new(x, y), types[map[x, y]]);
        for (int x = 0; x < resCont.MapLen; x++)
            for (int y = 0; y < resCont.MapLen; y++)
                if (p_map[x, y, 0] != "0")
                {
                    terrMap.RemoveTileFlags(new(x, y), TileFlags.LockColor);
                    terrMap.SetColor(new(x, y), Color.red);
                }
    }
    */
}
