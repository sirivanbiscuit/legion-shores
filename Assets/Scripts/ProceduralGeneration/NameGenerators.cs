using PoliticalEntities;
using SeedTools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NameGenerators
{
    private static string[] PART_VOW = new string[]
    {
        // duplicated to represent commonality
        "a", "a", "a", "a",
        "e", "e", "e", "e", "e", "e",
        "i", "i", "i", "i",
        "o", "o", "o", "o",
        "u", "u",
        "y",
    };

    private static string[] PART_VCB = new string[]
    {
        "ae", "ai", "au",
        "ea", "eo", "eu",
        "ia", "io", "ie",
        "oa", "oi", "ou",
        "ua", "ue", "ui"
    };

    private static string[] PART_CON = new string[]
    {
        // duplicated to represent commonality
        "b", "b",
        "c", "c",
        "d", "d", "d", "d",
        "f", "f",
        "g", "g",
        "h", "h", "h", "h", "h",
        "k",
        "l", "l", "l", "l",
        "m", "m",
        "n", "n", "n", "n", "n", "n", "n", "n",
        "p", "p",
        "r", "r", "r", "r", "r", "r",
        "s", "s", "s", "s", "s", "s",
        "t", "t", "t", "t", "t", "t", "t", "t", "t", "t",
        "v",
        "w", "w",
        "x",
        "y",
    };

    private static string[] PART_CCB = new string[]
    {
        "bl", "cl", "fl", "gl", "pl", "sl",
        "br", "cr", "dr", "fr", "gr", "pr", "tr",
        "sc", "sk", "sm", "sn", "sp", "st", "sw",
        "dw", "tw", "str", "thr"
    };

    private static string[] PART_SUF = new string[]
    {
        "stan", "land", "os", "ia"
    };

    private static readonly List<string> _usage = new();

    private static string AbstractName(PolType type, Seed seed)
    {
        Vector2Int lenBounds = type switch
        {
            PolType.ETHNIC => new(1, 1),
            PolType.REALM => new(1, 2),
            PolType.REGION => new(2, 3),
            _ => throw new System.ArgumentException("NG01: Bad type")
        };
        string build = "";
        if (type == PolType.REALM
            || (type == PolType.ETHNIC && seed.PerRoll(0.25d)))
            build += WeightedVowel(seed, 0.05d);
        build += WeightedConsonant(seed, 0.2d);
        int len = seed.RangeRoll(lenBounds.x, lenBounds.y);
        for (int i = 1; i <= len; i++)
        {
            if (type == PolType.REALM && seed.PerRoll(0.025d)) build += "-";
            build += WeightedVowel(seed, 0.1d);
            build += WeightedConsonant(seed, i == len ? 0d : 0.2d);
        }
        if (type == PolType.ETHNIC) build += SomeElemOf(seed, PART_SUF);
        build = MakeUpper(build);
        // check to make sure its not a duplicate
        if (_usage.Contains(build)) AbstractName(type, seed);
        else _usage.Add(build);
        return build;
    }

    private static string WeightedVowel(Seed seed, double combWeight)
    {
        if (seed.PerRoll(combWeight)) return SomeElemOf(seed, PART_VCB);
        else return SomeElemOf(seed, PART_VOW);
    }

    private static string WeightedConsonant(Seed seed, double combWeight)
    {
        if (seed.PerRoll(combWeight))
        {
            if (seed.PerRoll(0.5d)) return SomeElemOf(seed, PART_CCB);
            else return SomeElemOf(seed, PART_CON) + SomeElemOf(seed, PART_CCB);
        }
        else return SomeElemOf(seed, PART_CON);
    }

    private static string SomeElemOf(Seed seed, string[] options)
        => options[seed.RangeRoll(options.Length - 1)];

    private static string MakeUpper(string source)
        => source[0].ToString().ToUpper() + source[1..];

    /// <summary>
    /// Generates a short name ending in -stan, -land, -os, or -ia
    /// </summary>
    public static string NewEthnicName(Seed seed)
        => AbstractName(PolType.ETHNIC, seed);

    /// <summary>
    /// Generates a moderate-length name starting with a vowel.
    /// </summary>
    public static string NewRealmName(Seed seed)
        => AbstractName(PolType.REALM, seed);

    /// <summary>
    /// Generates a long name starting with a consonant.
    /// </summary>
    public static string NewRegionName(Seed seed)
        => AbstractName(PolType.REGION, seed);

    /// <summary>
    /// Resets all name string availability and clears usage history.
    /// </summary>
    public static void Purge() => _usage.Clear();

}
