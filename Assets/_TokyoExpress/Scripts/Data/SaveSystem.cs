using System;
using System.Collections.Generic;
using UnityEngine;

namespace TokyoExpress.Data
{
    [Serializable]
    public class PlayerProfileData
    {
        public int coins = 250;
        public int score = 0;
        public int currentLevelIndex = 1;
        public string equippedBikeId = "bike_scooter_classic";
        public string equippedSkinId = "skin_tokyo_rider";
        
        public List<string> unlockedBikes = new List<string> { "bike_scooter_classic" };
        public List<string> unlockedSkins = new List<string> { "skin_tokyo_rider" };
        
        // Upgrades
        public int speedUpgradeLevel = 1;
        public int jumpUpgradeLevel = 1;
        public int nitroDurationLevel = 1;

        // Monetization & Settings
        public bool hasRemovedAds = false;
        public bool soundEnabled = true;
        public bool hapticEnabled = true;
    }

    public static class SaveSystem
    {
        private const string SAVE_KEY = "TokyoExpress_SaveData_v1";

        public static PlayerProfileData LoadProfile()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                try
                {
                    string json = PlayerPrefs.GetString(SAVE_KEY);
                    var data = JsonUtility.FromJson<PlayerProfileData>(json);
                    if (data != null) return data;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SaveSystem] Failed to parse save data: {e.Message}. Reverting to default.");
                }
            }
            var newProfile = new PlayerProfileData();
            SaveProfile(newProfile);
            return newProfile;
        }

        public static void SaveProfile(PlayerProfileData profile)
        {
            if (profile == null) return;
            string json = JsonUtility.ToJson(profile, true);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        public static void ResetProfile()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();
        }
    }
}
