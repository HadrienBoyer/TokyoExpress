using UnityEngine;
using TokyoExpress.Grid;

namespace TokyoExpress.Player
{
    public class TokyoFollowCamera : MonoBehaviour
    {
        [Header("Target Tracking")]
        [SerializeField] private Transform target;

        [Header("Top-Down Perspective Settings")]
        [Tooltip("Plongeante quasi-verticale avec une légère inclinaison pour révéler le volume 3D")]
        [SerializeField] private Vector3 topDownAngle = new Vector3(82f, 0f, 0f);
        [SerializeField] private float cameraHeight = 18f;
        [SerializeField] private float zOffset = -2.5f;
        [SerializeField] private float smoothSpeed = 10f;
        [SerializeField] private float fov = 45f;

        private Camera cam;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.fieldOfView = fov;
            }
        }

        private void Start()
        {
            if (target == null)
            {
                var player = FindAnyObjectByType<TokyoMotoController>();
                if (player != null) target = player.transform;
            }

            transform.rotation = Quaternion.Euler(topDownAngle);
            if (target != null)
            {
                transform.position = new Vector3(target.position.x, cameraHeight, target.position.z + zOffset);
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                var player = FindAnyObjectByType<TokyoMotoController>();
                if (player != null) target = player.transform;
                if (target == null) return;
            }

            // Always keep the player centered in the viewport
            Vector3 targetPosition = new Vector3(target.position.x, cameraHeight, target.position.z + zOffset);
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                transform.position = new Vector3(target.position.x, cameraHeight, target.position.z + zOffset);
            }
        }

        public void PositionCameraForLevelFrame()
        {
            if (target != null)
            {
                transform.position = new Vector3(target.position.x, cameraHeight, target.position.z + zOffset);
            }
            else if (LevelRuntimeBuilder.Instance != null && LevelRuntimeBuilder.Instance.ActiveLevel != null)
            {
                var level = LevelRuntimeBuilder.Instance.ActiveLevel;
                Vector3 center = LevelRuntimeBuilder.Instance.GridToWorldPosition(level.startCoord, level.cellSize);
                transform.position = new Vector3(center.x, cameraHeight, center.z + zOffset);
            }
        }
    }
}
