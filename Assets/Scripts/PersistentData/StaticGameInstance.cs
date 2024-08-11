using PoliticalEntities;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public static class StaticGameInstance
{
    // All-encompassing object for game data
    private static GameSession _gameSession;

    public static GameSession Get() => _gameSession;

    public static void Bind(GameSession session) => _gameSession = session;

    public static void Dispose() => _gameSession = null;

    public static bool Exists() => _gameSession != null;

    private const string DIR = "LegionShores";
    private const string FILE = "world.legionshores";

    private static string DataPath()
        => Path.Combine(Application.dataPath, Path.Combine(DIR, FILE));

    public static void SaveInstance()
    {
        BinaryFormatter formatter = new();
        using (FileStream stream = new(DataPath(), FileMode.Create))
        { formatter.Serialize(stream, _gameSession); }
    }

    public static void LoadInstance()
    {
        if (!File.Exists(DataPath()))
            throw new FileNotFoundException("GW01: No Save File!");
        BinaryFormatter formatter = new();
        using (FileStream stream = new(DataPath(), FileMode.Open))
        { Bind(formatter.Deserialize(stream) as GameSession); }
    }

    public static void BindAndSave(GameSession session)
    {
        Bind(session);
        SaveInstance();
    }

}
