using System.Collections.Generic;
using UnityEngine;

public class AffinitySystem : MonoBehaviour
{
    private Dictionary<string, int> affinities = new Dictionary<string, int>();
    private List<string> logs = new List<string>();
    private int maxLogs = 3;

    void Start()
    {
        affinities["craft"] = 0;
        affinities["melee"] = 0;
        affinities["fire"] = 0;
        affinities["nature"] = 0;
    }

    public void AddXp(string affinity, int amount)
    {
        if (affinities.ContainsKey(affinity))
        {
            affinities[affinity] += amount;
            Debug.Log($"[Affinity] {affinity}: +{amount} XP. Total: {affinities[affinity]}");
        }
    }

    public int GetXp(string affinity)
    {
        return affinities.ContainsKey(affinity) ? affinities[affinity] : 0;
    }

    public int GetLevel(string affinity)
    {
        int xp = GetXp(affinity);
        return xp / 100; // Simple level calculation: every 100 XP = 1 level
    }

    public string GetArchetype()
    {
        int craftLevel = GetLevel("craft");
        int meleeLevel = GetLevel("melee");
        int fireLevel = GetLevel("fire");
        int natureLevel = GetLevel("nature");

        if (fireLevel >= 2 && meleeLevel < fireLevel)
            return "Pyromancer";
        if (meleeLevel >= 2 && craftLevel < meleeLevel)
            return "Battle Smith";
        if (craftLevel >= 2)
            return "Rune Smith";
        if (natureLevel >= 2)
            return "Naturalist";
        if (fireLevel >= 1)
            return "Necromancer";
        
        return "Novice";
    }

    public string GetArchetypeAbility()
    {
        string archetype = GetArchetype();
        return archetype switch
        {
            "Necromancer" => "Bone Spark",
            "Rune Smith" => "Forge Rune",
            "Battle Smith" => "Forge Strike",
            "Pyromancer" => "Inferno",
            "Naturalist" => "Growth",
            _ => "None"
        };
    }

    public void AddLog(string message)
    {
        logs.Insert(0, message);
        if (logs.Count > maxLogs)
        {
            logs.RemoveAt(maxLogs);
        }
    }

    public string GetLogText()
    {
        return string.Join("\n", logs);
    }

    public string GetStatusText()
    {
        return $"Craft: {GetXp("craft")}XP ({GetLevel("craft")}L) | " +
               $"Melee: {GetXp("melee")}XP ({GetLevel("melee")}L) | " +
               $"Fire: {GetXp("fire")}XP ({GetLevel("fire")}L) | " +
               $"Nature: {GetXp("nature")}XP ({GetLevel("nature")}L)";
    }

    public string GetMeleeWeaponName()
    {
        int level = GetLevel("melee");
        return level switch
        {
            >= 3 => "Diamond Sword",
            >= 2 => "Iron Sword",
            >= 1 => "Stone Sword",
            _ => "Wooden Sword"
        };
    }

    public int GetMeleeDamage()
    {
        int level = GetLevel("melee");
        return 5 + (level * 2);
    }

    protected Dictionary<string, int> GetAffinities()
    {
        return affinities;
    }

    public void SetAffinities(Dictionary<string, int> newAffinities)
    {
        affinities = new Dictionary<string, int>(newAffinities);
    }
}
