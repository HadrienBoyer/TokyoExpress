using System;
using TokyoExpress.Core;
using TokyoExpress.Data;
using TokyoExpress.Grid;
using TokyoExpress.Player;
using UnityEngine;

namespace TokyoExpress.Delivery
{
    public class DeliveryMissionManager : MonoBehaviour
    {
        public static DeliveryMissionManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private TokyoMotoController playerMoto;

        [Header("Current Mission")]
        [SerializeField] private DeliveryMission activeMission;
        [SerializeField] private bool isMissionActive;

        public event Action<DeliveryMission> OnMissionStarted;
        public event Action<DeliveryMission> OnMissionSuccess;
        public event Action<string> OnMissionFailed; // Reason (Timer Expired, Parcel Broken)
        public event Action<float> OnTimerTick;

        public DeliveryMission ActiveMission => activeMission;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (playerMoto == null)
            {
                playerMoto = FindAnyObjectByType<TokyoMotoController>();
            }

            if (playerMoto != null)
            {
                playerMoto.OnCrashWithObstacle += HandlePlayerCollision;
            }
        }

        private void OnDestroy()
        {
            if (playerMoto != null)
            {
                playerMoto.OnCrashWithObstacle -= HandlePlayerCollision;
            }
        }

        public void StartMission(LevelData level)
        {
            if (level == null) return;

            var pkg = new PackageData
            {
                packageId = $"pkg_{level.levelId}_{level.packageType}",
                title = $"{level.packageType} Delivery",
                type = level.packageType,
                timeLimitSeconds = level.timeLimit,
                baseRewardCoins = level.rewardCoins,
                bonusPoints = 100,
                maxHitsAllowed = level.packageType == PackageType.Fragile ? 0 : 2
            };

            activeMission = new DeliveryMission
            {
                missionId = Guid.NewGuid().ToString(),
                package = pkg,
                startCell = level.startCoord,
                targetCell = level.destinationCoord,
                remainingTime = pkg.timeLimitSeconds,
                hitsTaken = 0,
                isCompleted = false,
                isFailed = false
            };

            isMissionActive = true;
            OnMissionStarted?.Invoke(activeMission);
        }

        private void Update()
        {
            if (!isMissionActive || activeMission == null || activeMission.isCompleted || activeMission.isFailed)
                return;

            // Timer reduction
            activeMission.remainingTime -= Time.deltaTime;
            OnTimerTick?.Invoke(activeMission.remainingTime);

            if (activeMission.remainingTime <= 0f)
            {
                FailMission("Le temps est écoulé ! La nourriture est froide / Livraison en retard.");
                return;
            }

            // Check if player reached target cell
            if (playerMoto != null && LevelRuntimeBuilder.Instance != null && LevelRuntimeBuilder.Instance.ActiveLevel != null)
            {
                Vector2Int playerCell = playerMoto.CurrentGridCoord;
                if (playerCell == activeMission.targetCell)
                {
                    CompleteMission();
                }
            }
        }

        private void HandlePlayerCollision(Collider obstacle)
        {
            if (!isMissionActive || activeMission == null) return;

            activeMission.hitsTaken++;

            // If fragile parcel (Zero tolerance rule)
            if (activeMission.package.type == PackageType.Fragile && activeMission.hitsTaken > 0)
            {
                FailMission("Colis précieux brisé ! Aucun choc autorisé.");
                return;
            }

            // Standard threshold
            if (activeMission.hitsTaken > activeMission.package.maxHitsAllowed)
            {
                FailMission("Moto accidentée !");
            }
        }

        private void CompleteMission()
        {
            isMissionActive = false;
            activeMission.isCompleted = true;

            // Update player profile
            var profile = SaveSystem.LoadProfile();
            profile.coins += activeMission.package.baseRewardCoins;
            profile.score += activeMission.package.bonusPoints;
            profile.currentLevelIndex++;
            SaveSystem.SaveProfile(profile);

            if (Audio.TokyoAudioManager.Instance != null)
            {
                Audio.TokyoAudioManager.Instance.PlaySFX(Audio.SoundEffect.DeliverSuccess);
            }
            OnMissionSuccess?.Invoke(activeMission);
        }

        public void FailMission(string reason)
        {
            isMissionActive = false;
            activeMission.isFailed = true;

            if (Audio.TokyoAudioManager.Instance != null)
            {
                Audio.TokyoAudioManager.Instance.PlaySFX(Audio.SoundEffect.DeliverFail);
            }
            OnMissionFailed?.Invoke(reason);
        }

        public void ReviveWithAd()
        {
            if (activeMission != null && activeMission.isFailed)
            {
                activeMission.isFailed = false;
                activeMission.remainingTime = Mathf.Max(activeMission.remainingTime, 20f);
                activeMission.hitsTaken = 0;
                isMissionActive = true;
                OnMissionStarted?.Invoke(activeMission);
            }
        }
    }
}
