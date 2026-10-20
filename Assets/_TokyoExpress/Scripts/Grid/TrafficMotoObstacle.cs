using System;
using System.Collections.Generic;
using UnityEngine;

namespace TokyoExpress.Grid
{
    public class TrafficMotoObstacle : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float speed = 6f;
        [SerializeField] private Vector3 travelDirection = Vector3.forward;
        [SerializeField] private float raycastForwardDist = 2f;
        [SerializeField] private LayerMask obstacleLayer;

        [Header("Visuals & Sound")]
        [SerializeField] private Color motoColor = Color.red;

        private RoadSplineTrack currentSplineTrack;
        private float splineT = 0f;

        private void Start()
        {
            var col = GetComponent<Collider>();
            if (col == null)
            {
                var box = gameObject.AddComponent<BoxCollider>();
                box.size = new Vector3(0.8f, 1.2f, 1.8f);
                box.center = new Vector3(0f, 0.6f, 0f);
            }

            FindCurrentSplineTrack();
        }

        private void FindCurrentSplineTrack()
        {
            if (LevelRuntimeBuilder.Instance != null && LevelRuntimeBuilder.Instance.ActiveLevel != null)
            {
                Vector2Int coord = LevelRuntimeBuilder.Instance.WorldToGridPosition(transform.position, LevelRuntimeBuilder.Instance.ActiveLevel.cellSize);
                GameObject tileGO = LevelRuntimeBuilder.Instance.GetTileObjectAt(coord);
                if (tileGO != null)
                {
                    currentSplineTrack = tileGO.GetComponent<RoadSplineTrack>();
                }
            }
        }

        private void Update()
        {
            if (currentSplineTrack == null)
            {
                FindCurrentSplineTrack();
            }

            if (currentSplineTrack != null)
            {
                float length = currentSplineTrack.GetLength();
                if (length <= 0.01f) length = 2.0f;

                splineT += (speed / length) * Time.deltaTime;

                currentSplineTrack.Evaluate(splineT, travelDirection, out Vector3 worldPos, out Vector3 worldTangent);
                transform.position = new Vector3(worldPos.x, transform.position.y, worldPos.z);

                if (worldTangent.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.LookRotation(worldTangent.normalized, Vector3.up);
                }

                if (splineT >= 1.0f)
                {
                    splineT = 0f;
                    currentSplineTrack = null;
                }
            }
            else
            {
                // Fallback straight movement
                transform.position += travelDirection * (speed * Time.deltaTime);

                // Turn around or switch lanes if dead end reached
                if (Physics.Raycast(transform.position + Vector3.up * 0.5f, travelDirection, raycastForwardDist, obstacleLayer))
                {
                    travelDirection = -travelDirection;
                    transform.rotation = Quaternion.LookRotation(travelDirection, Vector3.up);
                }
            }
        }

        public void SetSpeed(float newSpeed) => speed = newSpeed;
        public void SetDirection(Vector3 dir)
        {
            travelDirection = dir.normalized;
            transform.rotation = Quaternion.LookRotation(travelDirection, Vector3.up);
        }
    }
}
