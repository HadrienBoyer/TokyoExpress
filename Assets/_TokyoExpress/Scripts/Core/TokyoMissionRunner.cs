using UnityEngine;
using TokyoExpress.Grid;
using TokyoExpress.Delivery;

namespace TokyoExpress.Core
{
    public class TokyoMissionRunner : MonoBehaviour
    {
        public LevelData levelToRun;

        private void Start()
        {
            if (DeliveryMissionManager.Instance != null && levelToRun != null)
            {
                DeliveryMissionManager.Instance.StartMission(levelToRun);
            }
        }
    }
}
