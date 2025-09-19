using UnityEngine;
using System;
using DeadCellsTestFramework.Combat;

namespace DeadCellsTestFramework.Data
{
    [Serializable]
    public class WeaponData
    {
        public string id;
        public string name;
        public string description;
        public string weaponType; // Melee, Ranged, Magic
        public float baseDamage;
        public float attackSpeed;
        public float range;
        public float critChance;
        public float critMultiplier;
        public string damageType; // Physical, Magic, Fire, etc.
        public string iconPath;
        public string prefabPath;
        public WeaponStats stats;
    }
    
    [Serializable]
    public class WeaponStats
    {
        public float knockback;
        public float hitstunDuration;
        public int maxAmmo; // For ranged weapons
        public float reloadTime;
        public bool piercing;
        public int maxPierceCount;
    }
    
    [Serializable]
    public class EnemyData
    {
        public string id;
        public string name;
        public string description;
        public float maxHealth;
        public float moveSpeed;
        public float attackDamage;
        public float detectionRange;
        public float attackRange;
        public float attackCooldown;
        public string aiType; // Basic, Aggressive, Ranged, etc.
        public string prefabPath;
        public EnemyRewards rewards;
        public DamageResistanceData[] resistances;
    }
    
    [Serializable]
    public class EnemyRewards
    {
        public int goldMin;
        public int goldMax;
        public float itemDropChance;
        public string[] possibleItems;
    }
    
    [Serializable]
    public class DamageResistanceData
    {
        public string damageType;
        public float resistancePercent;
    }
    
    [Serializable]
    public class ItemData
    {
        public string id;
        public string name;
        public string description;
        public string itemType; // Consumable, Equipment, Key, etc.
        public string rarity; // Common, Rare, Epic, Legendary
        public int value;
        public string iconPath;
        public string prefabPath;
        public ItemEffect[] effects;
    }
    
    [Serializable]
    public class ItemEffect
    {
        public string effectType; // Heal, Buff, Damage, etc.
        public float value;
        public float duration;
    }
    
    [Serializable]
    public class RoomData
    {
        public string id;
        public string name;
        public string roomType; // Combat, Treasure, Shop, Boss
        public string ldtkLevelPath;
        public int difficulty;
        public EnemySpawnData[] enemySpawns;
        public string[] possibleRewards;
        public RoomSettings settings;
    }
    
    [Serializable]
    public class EnemySpawnData
    {
        public string enemyId;
        public int minCount;
        public int maxCount;
        public float spawnChance;
    }
    
    [Serializable]
    public class RoomSettings
    {
        public bool lockDoorsOnEntry;
        public string backgroundMusic;
        public string ambientSound;
        public Vector2 playerStartPosition;
    }
}