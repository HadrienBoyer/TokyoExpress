using UnityEngine;

namespace TokyoExpress.Data
{
    [CreateAssetMenu(fileName = "NewSkin", menuName = "TokyoExpress/Skin Data")]
    public class SkinItemData : ScriptableObject
    {
        public string id;
        public string skinName;
        public Sprite icon;
        public Material characterMaterial;
        public int priceCoins = 200;
    }
}
