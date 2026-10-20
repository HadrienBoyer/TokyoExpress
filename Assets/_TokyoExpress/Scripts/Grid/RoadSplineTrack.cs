using UnityEngine;
using UnityEngine.Splines;

namespace TokyoExpress.Grid
{
    /// <summary>
    /// Attach to each Road Tile (straight, curved, or intersection).
    /// Holds the SplineContainer that vehicles (player & traffic) follow precisely.
    /// </summary>
    public class RoadSplineTrack : MonoBehaviour
    {
        [SerializeField] private SplineContainer splineContainer;

        public SplineContainer SplineContainer => splineContainer;

        private void Awake()
        {
            if (splineContainer == null)
            {
                splineContainer = GetComponent<SplineContainer>();
            }
        }

        public void SetSplineContainer(SplineContainer container)
        {
            splineContainer = container;
        }

        /// <summary>
        /// Evaluates a world position and tangent on the spline at normalized progress t [0, 1]
        /// taking entry and travel direction into account.
        /// </summary>
        public void Evaluate(float t, Vector3 travelDir, out Vector3 worldPos, out Vector3 worldTangent)
        {
            t = Mathf.Clamp01(t);

            if (splineContainer != null && splineContainer.Splines != null && splineContainer.Splines.Count > 0)
            {
                int splineIndex = 0;
                bool isReversed = false;

                if (splineContainer.Splines.Count == 1)
                {
                    // Single spline: Check if native spline direction aligns with travel direction
                    splineContainer.Evaluate(0, 0f, out var p0, out _, out _);
                    splineContainer.Evaluate(0, 1f, out var p1, out _, out _);
                    Vector3 nativeDir = (transform.TransformPoint(p1) - transform.TransformPoint(p0)).normalized;
                    if (Vector3.Dot(nativeDir, travelDir) < -0.1f)
                    {
                        isReversed = true;
                    }
                }
                else
                {
                    // Multiple splines (e.g. crossroad): Pick the spline that best matches travel direction
                    float bestDot = -999f;
                    for (int i = 0; i < splineContainer.Splines.Count; i++)
                    {
                        splineContainer.Evaluate(i, 0f, out var p0, out _, out _);
                        splineContainer.Evaluate(i, 1f, out var p1, out _, out _);
                        Vector3 dir = (transform.TransformPoint(p1) - transform.TransformPoint(p0)).normalized;
                        float dot = Vector3.Dot(dir, travelDir);
                        if (dot > bestDot)
                        {
                            bestDot = dot;
                            splineIndex = i;
                            isReversed = false;
                        }
                        // Also check reverse of this spline
                        float revDot = Vector3.Dot(-dir, travelDir);
                        if (revDot > bestDot)
                        {
                            bestDot = revDot;
                            splineIndex = i;
                            isReversed = true;
                        }
                    }
                }

                float sampleT = isReversed ? (1f - t) : t;
                splineContainer.Evaluate(splineIndex, sampleT, out var localPos, out var localTangent, out _);
                worldPos = transform.TransformPoint(localPos);
                worldTangent = transform.TransformDirection(localTangent);
                if (isReversed)
                {
                    worldTangent = -worldTangent;
                }

                if (worldTangent.sqrMagnitude > 0.001f)
                {
                    worldTangent = worldTangent.normalized;
                }
                else
                {
                    worldTangent = travelDir;
                }
            }
            else
            {
                // Fallback straight evaluation across tile
                worldPos = transform.position + travelDir * ((t - 0.5f) * 2f);
                worldTangent = travelDir;
            }
        }

        public float GetLength()
        {
            if (splineContainer != null && splineContainer.Spline != null)
            {
                return splineContainer.CalculateLength();
            }
            return 2.0f; // Default grid cell size
        }

        private void OnDrawGizmos()
        {
            if (splineContainer == null || splineContainer.Splines == null) return;

            Gizmos.color = new Color(0.1f, 0.95f, 0.95f, 0.85f); // Neon Cyan

            for (int s = 0; s < splineContainer.Splines.Count; s++)
            {
                int steps = 16;
                Vector3 prevPoint = Vector3.zero;
                for (int i = 0; i <= steps; i++)
                {
                    float t = (float)i / steps;
                    splineContainer.Evaluate(s, t, out var localPos, out _, out _);
                    Vector3 worldPos = transform.TransformPoint(localPos);

                    if (i > 0)
                    {
                        Gizmos.DrawLine(prevPoint, worldPos);
                    }
                    prevPoint = worldPos;
                }
            }
        }
    }
}
