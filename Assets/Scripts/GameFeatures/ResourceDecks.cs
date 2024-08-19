using PlayerObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ResourceDecks
{
    public enum CivicCard
    {
        // TODO
    }

    public enum TroopCard
    {
        INFANTRY_LIGHT,
        INFANTRY_MEDIUM,
        INFANTRY_HEAVY,
        CAVALRY_LIGHT,
        CAVALRY_MEDIUM,
        CAVALRY_HEAVY,
        ARCHERY_LIGHT,
        ARCHERY_MEDIUM,
        ARCHERY_HEAVY,
        ARTILLERY_LIGHT,
        ARTILLERY_MEDIUM,
        ARTILLERY_HEAVY,
        JAZZ
    }

    public enum ShipCard
    {
        TRANSPORT_LIGHT,
        TRANSPORT_HEAVY,
        FRIGATE_LIGHT,
        FRIGATE_HEAVY,
    }

    public enum CharacterType
    {
        // TODO
    }

    public enum IncomeType
    {
        GOLD,
        SILVER
    }

    // Stores a random collection of different cards of a given type
    [Serializable]
    public abstract class PileDeck<T>
    {
        private readonly List<T> _deck = new();

        public bool HasItem(T item) => _deck.Contains(item);

        public void Add(T item) => _deck.Add(item);

        public bool Remove(T item) => _deck.Remove(item);

        public bool Transfer(T item, PileDeck<T> to)
        {
            if (!HasItem(item)) return false;
            Remove(item);
            to.Add(item);
            return true;
        }
    }

    [Serializable]
    public class CivicDeck : PileDeck<CivicCard> { }

    [Serializable]
    public class ArmyDeck : PileDeck<TroopCard> { }

    [Serializable]
    public class NavalDeck : PileDeck<ShipCard> { }

    // Stores a quantity of a certain income card
    [Serializable]
    public class IncomeDeck
    {
        public readonly IncomeType Type;

        private int _count = 0;

        public IncomeDeck(IncomeType type) => Type = type;

        public void Incr(int amount) => _count += amount;
        public void Decr(int amount) => _count -= amount;
        public int Count() => _count;
    }

    // Stores a single LoreCharacter as a unique character card
    [Serializable]
    public class CharacterDeck
    {
        public CharacterType Type;

        private readonly LoreCharacter _char;

        public LoreCharacter Character() => _char;

        public CharacterDeck(CharacterType type, LoreCharacter character)
        {
            Type = type;
            _char = character;
        }
    }

}
