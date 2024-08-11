using PoliticalEntities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NonHumanBrain
{
    private readonly RealmType _brainType;

    public NonHumanBrain(RealmType type) => _brainType = type;

    public void TriggerChapterTurn()
    {

    }
}
