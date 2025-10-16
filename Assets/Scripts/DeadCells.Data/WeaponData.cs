using UnityEngine;
using System;

namespace DeadCells.Data
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
        public float Damage;
        public float AttackSpeed;
        public float Range;
        public float knockback;
        public float hitstunDuration;
        public int maxAmmo; // For ranged weapons
        public float reloadTime;
        public bool piercing;
        public int maxPierceCount;
        
        // 临时别名属性，保持向后兼容
        public float damage { get => Damage; set => Damage = value; }
        public float attackSpeed { get => AttackSpeed; set => AttackSpeed = value; }
        public float range { get => Range; set => Range = value; }
    }
}