using UnityEngine;
using System;

namespace DeadCells.Data
{
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