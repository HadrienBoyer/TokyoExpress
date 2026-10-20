using System;
using System.Collections.Generic;
using UnityEngine;

namespace TokyoExpress.Grid
{
    public enum CellTileType
    {
        Empty = 0,
        StraightHorizontal = 1,
        StraightVertical = 2,
        TurnNE = 3,
        TurnNW = 4,
        TurnSE = 5,
        TurnSW = 6,
        TIntersection = 7,
        Crossroad = 8,
        Building = 9,
        Park = 10,
        Water = 11,
        Bridge = 12
    }

    [Serializable]
    public class LevelTileEntry
    {
        public Vector2Int coordinates;
        public CellTileType tileType;
        public int rotationSteps; // 0, 90, 180, 270 deg
        public bool hasObstacle;
        public bool isStartStation;
        public bool isDeliveryTarget;
    }

    [CreateAssetMenu(fileName = "NewTokyoLevel", menuName = "TokyoExpress/Level Data")]
    public class LevelData : ScriptableObject
    {
        public int levelId = 1;
        public string levelName = "Shinjuku Crossing";
        public Vector2Int gridSize = new Vector2Int(12, 20);
        public float cellSize = 2.0f;
        
        [Header("Mission Rules")]
        public Core.PackageType packageType = Core.PackageType.Food;
        public float timeLimit = 40f;
        public int rewardCoins = 150;
        public int trafficCount = 4;

        [Header("Level Grid Elements")]
        public List<LevelTileEntry> tileEntries = new List<LevelTileEntry>();
        
        public Vector2Int startCoord = new Vector2Int(2, 2);
        public Vector2Int destinationCoord = new Vector2Int(9, 18);

        public LevelTileEntry GetEntryAt(Vector2Int coord)
        {
            return tileEntries.Find(e => e.coordinates == coord);
        }

        public void SetTile(Vector2Int coord, CellTileType type, int rotation = 0)
        {
            var entry = GetEntryAt(coord);
            if (entry != null)
            {
                entry.tileType = type;
                entry.rotationSteps = rotation;
            }
            else
            {
                tileEntries.Add(new LevelTileEntry
                {
                    coordinates = coord,
                    tileType = type,
                    rotationSteps = rotation
                });
            }
        }
    }
}
