using System;
using System.Collections.Generic;
using UnityEngine;

namespace TokyoExpress.Core
{
    public enum PackageType
    {
        Food,       // Timed delivery (spoilage if timer runs out)
        Fragile,    // Zero collision tolerance (shatters if bumped or violent landing)
        Standard,   // Basic parcel
        VIPRush     // Tight timer + bonus reward
    }

    [Serializable]
    public class PackageData
    {
        public string packageId;
        public string title;
        public PackageType type;
        public float timeLimitSeconds = 45f;
        public int baseRewardCoins = 100;
        public int bonusPoints = 50;
        public int maxHitsAllowed = 0; // 0 for fragile!
    }

    [Serializable]
    public class DeliveryMission
    {
        public string missionId;
        public PackageData package;
        public Vector2Int startCell;
        public Vector2Int targetCell;
        public bool isCompleted;
        public bool isFailed;
        public float remainingTime;
        public int hitsTaken;
    }
}
