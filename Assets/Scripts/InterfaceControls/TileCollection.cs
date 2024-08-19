using GenerationTools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileCollection : MonoBehaviour
{
    public Tile
        cloudTile, upperCloudTile, mountainsTile,
        oceanTile, shallowTile, swampTile,
        wetlandsTile, plainsTile, forestTile,
        desertTile, dryForestTile;

    public Tile[] GetCollection()
        => MapUtil.GetTileSet(
            cloudTile, upperCloudTile,
            oceanTile, swampTile, wetlandsTile, shallowTile,
            plainsTile, mountainsTile, forestTile,
            desertTile, dryForestTile);
}
