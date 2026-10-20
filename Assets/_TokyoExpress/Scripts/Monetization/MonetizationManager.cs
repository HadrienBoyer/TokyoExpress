using System;
using UnityEngine;
using TokyoExpress.Data;

namespace TokyoExpress.Monetization
{
    public enum AdPlacementType
    {
        RewardedRevive,     // "Watch Ad to Continue" on Crash screen
        RewardedDoubleCoins, // "2x Coins" on Level Cleared
        InterstitialLevelEnd // Between runs
    }

    /// <summary>
    /// Multi-platform monetization manager for iOS, Android, and tablets.
    /// Handles IAP (No Ads, Currency packs) and Rewarded Video Ads / Interstitials.
    /// </summary>
    public class MonetizationManager : MonoBehaviour
    {
        public static MonetizationManager Instance { get; private set; }

        public event Action<bool> OnRewardedAdCompleted;
        public event Action<string> OnPurchaseSuccessful;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (transform.parent != null) transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #region In-App Purchases (IAP)
        public void BuyRemoveAds()
        {
            Debug.Log("[Monetization] Initiating IAP: Remove Ads");
            // Mock purchase completion (plug into Unity IAP StoreListener)
            var profile = SaveSystem.LoadProfile();
            profile.hasRemovedAds = true;
            SaveSystem.SaveProfile(profile);

            OnPurchaseSuccessful?.Invoke("remove_ads");
        }

        public void BuyCoinPack(int coinsAmount)
        {
            Debug.Log($"[Monetization] Purchasing coin pack: +{coinsAmount} Coins");
            var profile = SaveSystem.LoadProfile();
            profile.coins += coinsAmount;
            SaveSystem.SaveProfile(profile);

            OnPurchaseSuccessful?.Invoke($"coins_{coinsAmount}");
        }

        public void RestorePurchases()
        {
            Debug.Log("[Monetization] Restoring iOS / Android purchases...");
#if UNITY_IOS || UNITY_IPHONE
            // Call Apple StoreKit restoreTransactions
#endif
        }
        #endregion

        #region Ads (Rewarded & Interstitial)
        public bool IsRewardedAdReady()
        {
            // Ready status from AdMob / LevelPlay / Unity Ads
            return true;
        }

        public void ShowRewardedAd(AdPlacementType placement, Action<bool> callback)
        {
            Debug.Log($"[Monetization] Showing Rewarded Video Ad for {placement}...");
            
            // Simulating ad display & reward callback
            // Replace with Google Mobile Ads or Unity Ads SDK call
            bool userWatchedEntirely = true;
            callback?.Invoke(userWatchedEntirely);
            OnRewardedAdCompleted?.Invoke(userWatchedEntirely);
        }

        public void ShowInterstitialIfAllowed()
        {
            var profile = SaveSystem.LoadProfile();
            if (profile != null && profile.hasRemovedAds)
            {
                Debug.Log("[Monetization] Interstitial skipped (User owns 'No Ads').");
                return;
            }

            Debug.Log("[Monetization] Showing Interstitial Ad.");
        }
        #endregion
    }
}
