using GenerationTools;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using WP = GenerationTools.WorldPopulator;

public class PolConstrManager : MonoBehaviour
{
    public TerrConstrManager terrainManager;

    public Tilemap polMap;
    public Tile landFiller;
    public Tile waterFiller;
    public Tile mountFiller;

    public int maxEthnics;
    public int maxRealmsPerEthnic;
    public int realmSize;
    public bool forceWildDeserts;

    readonly Color[] cols = { Color.blue, Color.cyan, Color.green,
                              Color.magenta, Color.red, Color.yellow };

    [ContextMenu("Clear Map")]
    void ClearMap() { polMap.ClearAllTiles(); }

    [ContextMenu("Populate Map Construct")]
    void Populate()
    {
        byte[,] exp = terrainManager.Export();
        string[,,] p_constr = WP.Export(
            exp,
            terrainManager.mapSeed,
            maxEthnics,
            maxRealmsPerEthnic,
            realmSize,
            forceWildDeserts).Map;
        AssetDatabase.Refresh();
        ClearMap();
        int size = terrainManager.mapSize;
        Vector3 tPos = terrainManager.terrMap.transform.position;
        polMap.transform.position = new(
            tPos.x + size / 2 + 10,
            tPos.y - size / 4 - 10,
            tPos.z
            );
        Dictionary<string, Color> cs = new();
        int colId = 0;
        string w = WP.IntToId(maxEthnics + 1, WP.LEN_ETH_ID);
        Vector3Int get;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                get = new(x, y);
                string str = p_constr[x, y, WP.LAYER_ETH];
                if (str != "00" && str != w)
                {
                    // ethnic tile
                    if (!cs.ContainsKey(str))
                    {
                        if (colId > 5) colId = 0;
                        cs[str] = cols[colId];
                        colId++;
                    }
                    polMap.SetTile(get, landFiller);
                    polMap.RemoveTileFlags(get, TileFlags.LockColor);
                    polMap.SetColor(get, cs[str]);
                }
                else if (str == "00")
                {
                    // water tile
                    if (MapUtil.IsAquatic(exp[x, y]))
                        polMap.SetTile(get, waterFiller);
                    // mountain tile
                    else polMap.SetTile(get, mountFiller);
                }
                else
                {
                    // wilds tile
                    polMap.SetTile(get, landFiller);
                    polMap.RemoveTileFlags(get, TileFlags.LockColor);
                    polMap.SetColor(get, Color.black);
                }
            }
    }
}
