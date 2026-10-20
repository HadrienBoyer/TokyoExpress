using UnityEngine;

namespace TokyoExpress.Data
{
    public enum SpecialAbilityType
    {
        None,
        HighJump,       // Super jump clearance over roadblocks & trains/motos
        BulletTime,     // Slow down world time to dodge complex intersections
        NitroBoost,     // Swift surge of speed + invincibility frames
        ShieldArmor     // Protects fragile parcels from 1 collision
    }

    [CreateAssetMenu(fileName = "NewBike", menuName = "TokyoExpress/Bike Data")]
    public class BikeItemData : ScriptableObject
    {
        public string id;
        public string bikeName;
        [TextArea] public string description;
        public Sprite icon;
        public GameObject bikePrefab;
        
        public int priceCoins = 500;
        public float baseSpeed = 12f;
        public float jumpForce = 8f;
        public SpecialAbilityType ability = SpecialAbilityType.None;
        public float abilityDuration = 3f;
        public float abilityCooldown = 10f;
    }
}
