using PlayerObjects;
using PoliticalEntities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameSession
{
    private readonly World _world;

    private readonly List<PlayerInstance> _players = new();

    public World GetWorld() => _world;

    public GameSession(World world, Realm humanControlled)
    {
        _world = world;
        foreach (Realm r in world.GetRealms())
            _players.Add(new(r == humanControlled, r));
    }

}