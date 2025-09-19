using UnityEngine;
using System;

namespace DeadCells.Data
{
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
}