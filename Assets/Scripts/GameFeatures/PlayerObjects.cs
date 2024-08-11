using PoliticalEntities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerObjects
{
    [Serializable]
    public class PlayerInstance
    {
        private readonly Realm _realmTarget;
        private readonly NonHumanBrain _nonHumanBrain = null;

        public PlayerInstance(bool human, Realm target)
        {
            _realmTarget = target;
            if (!human) _nonHumanBrain = new(target.Type);
        }

        public bool IsHuman() => _nonHumanBrain == null;

    }

}