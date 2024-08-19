using PoliticalEntities;
using ResourceDecks;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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

    [Serializable]
    public class RoyalHouse
    {
        public readonly Member Founder;
        private Member _head;

        // helps find a next-in-line
        private readonly List<Member> _noSuccession = new();

        public Member GetFounder() => Founder;
        public Member GetHeadOfHouse() => _head;

        public RoyalHouse(LoreCharacter founder)
        {
            Founder = Member.CreateFounder(founder);
            _head = Founder;
        }

        /// <summary>
        /// Set the head of house to the next in line if 
        /// the current head is dead. Otherwise do nothing.
        /// </summary>
        public void RefreshHeadOfHouse()
        {
            Member find = _head;
            while (find != null)
            {
                // if living, stop looking
                if (find.Character.IsAlive()) break;
                // find an unsearched heir (living or dead)
                bool hasHeir = false;
                foreach (Member h in find.GetHeirs())
                    if (!_noSuccession.Contains(h))
                    { find = h; hasHeir = true; break; }
                // if none found, look at predessor
                if (!hasHeir)
                { find = find.Predecessor; _noSuccession.Add(find); }
            }
            // sets head to null if the house is empty
            _head = find;
        }

        public bool IsDiminished() => _head == null;

        [Serializable]
        public class Member
        {
            public readonly LoreCharacter Character;
            public readonly Member Predecessor;

            private LoreCharacter _spouse = null;
            private readonly List<Member> _heirs = new();

            private Member(LoreCharacter character, Member predecessor)
            {
                Character = character;
                Predecessor = predecessor;
            }

            public static Member CreateFounder(LoreCharacter founder)
                => new(founder, null);

            public bool AssignSpouse(LoreCharacter spouse)
            {
                if (IsWed()) return false;
                _spouse = spouse;
                return true;
            }

            public bool IsWed() => _spouse != null;

            public bool CreateHeir(LoreCharacter heir)
            {
                foreach (Member h in _heirs)
                { if (h.Character == heir) return false; }
                Member created = new(heir, this);
                _heirs.Add(created);
                return true;
            }

            public Member[] GetHeirs() => _heirs.ToArray();
        }
    }

    [Serializable]
    public class LoreCharacter
    {
        private readonly string _name;

        private bool _alive = true;

        public LoreCharacter(string characterName)
        {
            _name = characterName;
        }

        public string GetName() => _name;

        public bool IsAlive() => _alive;
        public void Kill() => _alive = false;
    }

}