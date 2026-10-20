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
        /// taking entry and exit direction into account.
        /// </summary>
        public void Evaluate(float t, Vector3 entryDir, out Vector3 worldPos, out Vector3 worldTangent)
        {
            t = Mathf.Clamp01(t);

            if (splineContainer != null && splineContainer.Splines != null && splineContainer.Splines.Count > 0)
            {
                // Find best matching spline index if multiple splines exist (e.g. crossroad)
                int splineIndex = 0;
                if (splineContainer.Splines.Count > 1)
                {
                    float bestDot = -999f;
                    for (int i = 0; i < splineContainer.Splines.Count; i++)
                    {
                        var s = splineContainer.Splines[i];
                        if (s.Count >= 2)
                        {
                            Vector3 dir = (transform.TransformPoint((Vector3)s[1].Position) - transform.TransformPoint((Vector3)s[0].Position)).normalized;
                            float dot = Vector3.Dot(dir, entryDir);
                            if (dot > bestDot)
                            {
                                bestDot = dot;
                                splineIndex = i;
                            }
                        }
                    }
                }

                splineContainer.Evaluate(splineIndex, t, out var localPos, out var localTangent, out _);
                worldPos = transform.TransformPoint(localPos);
                worldTangent = transform.TransformDirection(localTangent);

                // If tangent is facing opposite entry direction, invert tangent
                if (Vector3.Dot(worldTangent, entryDir) < 0f && splineContainer.Splines.Count == 1)
                {
                    // Reverse evaluation if entry direction opposes spline native direction
                    splineContainer.Evaluate(splineIndex, 1f - t, out localPos, out localTangent, out _);
                    worldPos = transform.TransformPoint(localPos);
                    worldTangent = -transform.TransformDirection(localTangent);
                }
            }
            else
            {
                // Fallback straight evaluation across tile
                worldPos = transform.position + entryDir * ((t - 0.5f) * 2f);
                worldTangent = entryDir;
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
    }
}
