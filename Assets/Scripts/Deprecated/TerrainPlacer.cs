using MapConstructorTools;
using SeedTools;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using WorldBuilderTools;

public class TerrainPlacer : MonoBehaviour
{
    public TextAsset sourceText;
    public Tilemap targetMap;

    public Tile oceanTile;
    public Tile plainsTile;
    public Tile forestTile;
    public Tile mountainTile;
    public Tile desertTile;

    public int constructSize;
    public int constructSeed;
    public int plates, plateGap;
    public double seaPlatePercent;
    public int seaLevel, mountLevel;
    public int sculptCycles;
    public double dryPower, errosionPower;

    private TerrainEntryMap entryMap = new TerrainEntryMap();

    [ContextMenu("Full Refresh")]
    private void FullRefresh()
    {
        Reconstruct();
        AssetDatabase.Refresh();
        ClearAndPaint();
    }

    [ContextMenu("Reconstruct")]
    private void Reconstruct()
    {
        int[,] map = MapConstructor.ExportTerr(constructSize, new Seed(constructSeed),
            (c) => c
            .EBuildNoiseMap()
            .ETectonicsProcedure(plates, plateGap, seaPlatePercent)
            .ESmoothingProcedure()
            .TTileSettingProcedure(seaLevel, mountLevel)
            .TLandSculptProcedure(sculptCycles, dryPower, 0)
            .TLandSculptProcedure(sculptCycles, 0, errosionPower) 
            );

        Debug.Log("Map Construct Done!");

        File.WriteAllText(
                Application.dataPath + "/Resources/active_terrain.json",
                JsonUtility.ToJson(new TerrainEntryMap(map, constructSize))
            );

        Debug.Log("Construct written to filesave!");
    }

    [ContextMenu("Clear and Paint")]
    private void ClearAndPaint()
    {
        Clear();
        entryMap = JsonUtility.FromJson<TerrainEntryMap>(sourceText.text);
        Tile[] types = GetTerrainTypes();
        for (int i = 0; i < entryMap.TerrainEntries.Length; i++)
        {
            targetMap.SetTile(
                new Vector3Int(
                    WorldBuilderTools.GridTools.XFromGridId(i, entryMap.MapLength),
                    WorldBuilderTools.GridTools.YFromGridId(i, entryMap.MapLength)
                ),
                types[entryMap.TerrainEntries[i]]
            );
        }
    }

    [ContextMenu("Clear")]
    private void Clear()
    {
        targetMap.ClearAllTiles();
    }

    private Tile[] GetTerrainTypes()
    {
        Tile[] types = new Tile[WorldBuilder.NUM_TYPES];
        types[WorldBuilder.OCEAN] = oceanTile;
        types[WorldBuilder.PLAINS] = plainsTile;
        types[WorldBuilder.FOREST] = forestTile;
        types[WorldBuilder.MOUNTAIN] = mountainTile;
        types[WorldBuilder.DESERT] = desertTile;
        return types;
    }

}
