using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TokyoExpress.Data;

namespace TokyoExpress.UI
{
    public class GarageAndShopUI : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject garagePanel;
        [SerializeField] private TextMeshProUGUI coinsText;
        [SerializeField] private Transform bikeCardsContainer;
        [SerializeField] private Transform skinCardsContainer;

        [Header("Data References")]
        [SerializeField] private List<BikeItemData> availableBikes = new List<BikeItemData>();
        [SerializeField] private List<SkinItemData> availableSkins = new List<SkinItemData>();

        [Header("Selected Preview")]
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI itemDescText;
        [SerializeField] private TextMeshProUGUI itemStatsText;
        [SerializeField] private Button buyOrEquipButton;
        [SerializeField] private TextMeshProUGUI buttonLabelText;

        private PlayerProfileData currentProfile;
        private BikeItemData selectedBike;
        private SkinItemData selectedSkin;

        private void Start()
        {
            RefreshUI();
        }

        public void RefreshUI()
        {
            currentProfile = SaveSystem.LoadProfile();
            if (coinsText != null) coinsText.text = currentProfile.coins.ToString();

            // Setup default selected bike
            if (availableBikes.Count > 0 && selectedBike == null)
            {
                selectedBike = availableBikes.Find(b => b.id == currentProfile.equippedBikeId) ?? availableBikes[0];
            }
            UpdatePreviewPanel();
        }

        public void SelectBike(BikeItemData bike)
        {
            selectedBike = bike;
            selectedSkin = null;
            UpdatePreviewPanel();
        }

        public void SelectSkin(SkinItemData skin)
        {
            selectedSkin = skin;
            selectedBike = null;
            UpdatePreviewPanel();
        }

        private void UpdatePreviewPanel()
        {
            if (selectedBike != null)
            {
                if (itemNameText != null) itemNameText.text = selectedBike.bikeName;
                if (itemDescText != null) itemDescText.text = selectedBike.description;
                if (itemStatsText != null)
                {
                    itemStatsText.text = $"Vitesse: {selectedBike.baseSpeed} | Saut: {selectedBike.jumpForce}\nCapacité: {selectedBike.ability}";
                }

                bool isUnlocked = currentProfile.unlockedBikes.Contains(selectedBike.id);
                bool isEquipped = currentProfile.equippedBikeId == selectedBike.id;

                if (isEquipped)
                {
                    buttonLabelText.text = "ÉQUIPÉE";
                    buyOrEquipButton.interactable = false;
                }
                else if (isUnlocked)
                {
                    buttonLabelText.text = "ÉQUIPER";
                    buyOrEquipButton.interactable = true;
                    buyOrEquipButton.onClick.RemoveAllListeners();
                    buyOrEquipButton.onClick.AddListener(EquipCurrentItem);
                }
                else
                {
                    buttonLabelText.text = $"DÉBLOQUER ({selectedBike.priceCoins} Coins)";
                    buyOrEquipButton.interactable = currentProfile.coins >= selectedBike.priceCoins;
                    buyOrEquipButton.onClick.RemoveAllListeners();
                    buyOrEquipButton.onClick.AddListener(BuyCurrentItem);
                }
            }
            else if (selectedSkin != null)
            {
                if (itemNameText != null) itemNameText.text = selectedSkin.skinName;
                if (itemDescText != null) itemDescText.text = "Skin pour le pilote Tokyo Express";
                if (itemStatsText != null) itemStatsText.text = "Cosmétique Tokyo Style";

                bool isUnlocked = currentProfile.unlockedSkins.Contains(selectedSkin.id);
                bool isEquipped = currentProfile.equippedSkinId == selectedSkin.id;

                if (isEquipped)
                {
                    buttonLabelText.text = "ÉQUIPÉ";
                    buyOrEquipButton.interactable = false;
                }
                else if (isUnlocked)
                {
                    buttonLabelText.text = "ÉQUIPER";
                    buyOrEquipButton.interactable = true;
                    buyOrEquipButton.onClick.RemoveAllListeners();
                    buyOrEquipButton.onClick.AddListener(EquipCurrentItem);
                }
                else
                {
                    buttonLabelText.text = $"ACHETER ({selectedSkin.priceCoins} Coins)";
                    buyOrEquipButton.interactable = currentProfile.coins >= selectedSkin.priceCoins;
                    buyOrEquipButton.onClick.RemoveAllListeners();
                    buyOrEquipButton.onClick.AddListener(BuyCurrentItem);
                }
            }
        }

        private void BuyCurrentItem()
        {
            if (selectedBike != null && currentProfile.coins >= selectedBike.priceCoins)
            {
                currentProfile.coins -= selectedBike.priceCoins;
                currentProfile.unlockedBikes.Add(selectedBike.id);
                currentProfile.equippedBikeId = selectedBike.id;
                SaveSystem.SaveProfile(currentProfile);
                RefreshUI();
            }
            else if (selectedSkin != null && currentProfile.coins >= selectedSkin.priceCoins)
            {
                currentProfile.coins -= selectedSkin.priceCoins;
                currentProfile.unlockedSkins.Add(selectedSkin.id);
                currentProfile.equippedSkinId = selectedSkin.id;
                SaveSystem.SaveProfile(currentProfile);
                RefreshUI();
            }
        }

        private void EquipCurrentItem()
        {
            if (selectedBike != null)
            {
                currentProfile.equippedBikeId = selectedBike.id;
                SaveSystem.SaveProfile(currentProfile);
                RefreshUI();
            }
            else if (selectedSkin != null)
            {
                currentProfile.equippedSkinId = selectedSkin.id;
                SaveSystem.SaveProfile(currentProfile);
                RefreshUI();
            }
        }

        public void UnlockRandom()
        {
            int randomCost = 250;
            if (currentProfile.coins >= randomCost)
            {
                currentProfile.coins -= randomCost;
                // pick locked bike or skin
                var lockedBikes = availableBikes.FindAll(b => !currentProfile.unlockedBikes.Contains(b.id));
                if (lockedBikes.Count > 0)
                {
                    var chosen = lockedBikes[UnityEngine.Random.Range(0, lockedBikes.Count)];
                    currentProfile.unlockedBikes.Add(chosen.id);
                    selectedBike = chosen;
                }
                SaveSystem.SaveProfile(currentProfile);
                RefreshUI();
            }
        }
    }
}
