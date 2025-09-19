using UnityEngine;
using System;

namespace DeadCells.Data
{
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
}