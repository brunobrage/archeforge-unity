using System;
using System.Collections.Generic;
using UnityEngine;

namespace Archeforge.UnityPort
{
    public enum TileType { Empty, Solid, Resource }
    public enum TileBiome { Plains, Forest, Ruins, IronWastes, Ember }
    public enum TileMaterial
    {
        None,
        PlainsGround,
        ForestGround,
        RuinsGround,
        IronGround,
        EmberGround,
        PlainsStone,
        ForestWood,
        RuinsStone,
        IronStone,
        EmberStone,
        PlainsResource,
        ForestResource,
        RuinsResource,
        IronResource,
        EmberResource,
        WoodBlock,
        StoneBlock,
        IronBlock
    }

    public enum AffinityType { Craft, Melee, Fire, Nature }
    public enum WeaponId { RustyDagger, WoodSword, IronSword }
    public enum EnemyKind { Chicken, Santelmo, Tikbalang, DwendeBlue, Kapre }

    [Serializable]
    public sealed class WeaponDefinition
    {
        public WeaponId Id;
        public string Name = string.Empty;
        public int BaseDamage;
        public float CritChance;
        public float CritMultiplier;
    }

    [Serializable]
    public sealed class InventoryItem
    {
        public string Id = string.Empty;
        public string Name = string.Empty;
        public int Quantity;
        public int MaxStack = 99;
    }

    [Serializable]
    public sealed class Ingredient
    {
        public string ItemId = string.Empty;
        public int Quantity;
    }

    [Serializable]
    public sealed class Recipe
    {
        public string Id = string.Empty;
        public string Name = string.Empty;
        public List<Ingredient> Ingredients = new();
        public string ResultItemId = string.Empty;
        public int ResultQuantity = 1;
        public AffinityType? RequiredAffinity;
        public int RequiredAmount;
    }

    public sealed class ActorView
    {
        public GameObject GameObject = null!;
        public SpriteRenderer Renderer = null!;
        public Vector3 BaseScale = Vector3.one;
    }

    public sealed class TileView
    {
        public GameObject GameObject = null!;
        public SpriteRenderer BaseRenderer = null!;
        public SpriteRenderer AccentRenderer = null!;
        public SpriteRenderer ShadowRenderer = null!;
        public BoxCollider2D Collider = null!;
    }

    public sealed class PlayerState
    {
        public Vector2 Position = new(12f, 12f);
        public Vector2 Facing = Vector2.down;
        public int Level = 1;
        public int Xp;
        public int MaxHealth = 100;
        public int Health = 100;
        public WeaponId EquippedWeapon = WeaponId.RustyDagger;
        public readonly Dictionary<AffinityType, int> Affinities = new()
        {
            { AffinityType.Craft, 0 },
            { AffinityType.Melee, 0 },
            { AffinityType.Fire, 0 },
            { AffinityType.Nature, 0 }
        };
    }

    public sealed class CreatureState
    {
        public Vector2 Position = new(9f, 10f);
        public int Level = 1;
        public int Xp;
        public int MaxHealth = 50;
        public int Health = 50;
        public int Damage = 5;
        public float AttackCooldown;
        public float WorkCooldown;
    }

    public sealed class CollectorState
    {
        public Vector2 Position = new(7f, 10f);
        public int CollectedCount;
    }

    public sealed class EnemyState
    {
        public EnemyKind Kind;
        public Vector2 Position;
        public int MaxHealth;
        public int Health;
        public int Damage;
        public float Speed;
        public float DetectionRange;
        public float AttackRange;
        public float AttackCooldown;
        public float AttackTimer;
        public bool Alive = true;
        public int SpawnSlotIndex;
        public ActorView View = null!;
    }

    public sealed class SpawnSlot
    {
        public EnemyKind Kind;
        public Vector2 Center;
        public float Radius;
        public EnemyState? Enemy;
        public float RespawnTimer;
    }

    public sealed class WorldDrop
    {
        public string ItemId = string.Empty;
        public string Name = string.Empty;
        public int Quantity;
        public Vector2 Position;
        public ActorView View = null!;
    }

    public readonly struct ItemDrop
    {
        public ItemDrop(string itemId, string name, Color color)
        {
            ItemId = itemId;
            Name = name;
            Color = color;
        }

        public string ItemId { get; }
        public string Name { get; }
        public Color Color { get; }
    }

    [Serializable]
    public sealed class SaveData
    {
        public int PlayerLevel;
        public int PlayerXp;
        public int PlayerHealth;
        public string Weapon = string.Empty;
        public float PlayerX;
        public float PlayerY;
        public int CreatureLevel;
        public int CreatureXp;
        public int CreatureHealth;
        public float CreatureX;
        public float CreatureY;
        public int CollectorCount;
        public List<AffinityEntry> Affinities = new();
        public List<InventoryEntry> Inventory = new();
        public List<string> Hotbar = new();
        public int SelectedHotbarIndex;
    }

    [Serializable]
    public sealed class AffinityEntry
    {
        public string Key = string.Empty;
        public int Value;
    }

    [Serializable]
    public sealed class InventoryEntry
    {
        public string Id = string.Empty;
        public string Name = string.Empty;
        public int Quantity;
        public int MaxStack;
    }
}
