using PoliticalEntities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StaticTerraform
{
    private static World _world = null;

    public static World Get() => _world;

    public static void Bind(World world) => _world = world;

    public static void Dispose() => _world = null;

    public static bool Exists() => _world != null;
}
