using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TokyoExpress.Core;
using TokyoExpress.Delivery;
using TokyoExpress.Monetization;

namespace TokyoExpress.UI
{
    public class InGameDeliveryHUD : MonoBehaviour
    {
        [Header("HUD Displays")]
        [SerializeField] private TextMeshProUGUI levelLabelText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI packageInfoText;
        [SerializeField] private Image timerFillBar;

        [Header("Crash / GameOver Modal")]
        [SerializeField] private GameObject crashModal;
        [SerializeField] private TextMeshProUGUI crashReasonText;
        [SerializeField] private Button watchAdReviveButton;
        [SerializeField] private Button restartButton;

        [Header("Victory Modal")]
        [SerializeField] private GameObject victoryModal;
        [SerializeField] private TextMeshProUGUI rewardCoinsText;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button doubleCoinsAdButton;

        private void Start()
        {
            if (DeliveryMissionManager.Instance != null)
            {
                DeliveryMissionManager.Instance.OnMissionStarted += HandleMissionStarted;
                DeliveryMissionManager.Instance.OnTimerTick += HandleTimerTick;
                DeliveryMissionManager.Instance.OnMissionSuccess += HandleMissionSuccess;
                DeliveryMissionManager.Instance.OnMissionFailed += HandleMissionFailed;
            }

            if (watchAdReviveButton != null)
            {
                watchAdReviveButton.onClick.AddListener(OnWatchAdClicked);
            }
        }

        private void OnDestroy()
        {
            if (DeliveryMissionManager.Instance != null)
            {
                DeliveryMissionManager.Instance.OnMissionStarted -= HandleMissionStarted;
                DeliveryMissionManager.Instance.OnTimerTick -= HandleTimerTick;
                DeliveryMissionManager.Instance.OnMissionSuccess -= HandleMissionSuccess;
                DeliveryMissionManager.Instance.OnMissionFailed -= HandleMissionFailed;
            }
        }

        private void HandleMissionStarted(DeliveryMission mission)
        {
            if (crashModal != null) crashModal.SetActive(false);
            if (victoryModal != null) victoryModal.SetActive(false);

            if (packageInfoText != null)
            {
                string fragileWarning = mission.package.type == PackageType.Fragile ? " [!] FRAGILE (Choc interdit!)" : "";
                packageInfoText.text = $"{mission.package.title}{fragileWarning}";
            }
        }

        private void HandleTimerTick(float remaining)
        {
            if (timerText != null)
            {
                int seconds = Mathf.Max(0, Mathf.CeilToInt(remaining));
                timerText.text = $"{seconds}s";
            }

            if (timerFillBar != null && DeliveryMissionManager.Instance.ActiveMission != null)
            {
                float total = DeliveryMissionManager.Instance.ActiveMission.package.timeLimitSeconds;
                timerFillBar.fillAmount = Mathf.Clamp01(remaining / total);
            }
        }

        private void HandleMissionSuccess(DeliveryMission mission)
        {
            if (victoryModal != null)
            {
                victoryModal.SetActive(true);
                if (rewardCoinsText != null)
                {
                    rewardCoinsText.text = $"+{mission.package.baseRewardCoins} Coins";
                }
            }
        }

        private void HandleMissionFailed(string reason)
        {
            if (crashModal != null)
            {
                crashModal.SetActive(true);
                if (crashReasonText != null)
                {
                    crashReasonText.text = reason;
                }
            }
        }

        private void OnWatchAdClicked()
        {
            if (MonetizationManager.Instance != null)
            {
                MonetizationManager.Instance.ShowRewardedAd(AdPlacementType.RewardedRevive, (success) =>
                {
                    if (success)
                    {
                        DeliveryMissionManager.Instance.ReviveWithAd();
                    }
                });
            }
        }
    }
}
