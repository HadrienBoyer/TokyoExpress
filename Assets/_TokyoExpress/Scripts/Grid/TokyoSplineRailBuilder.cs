using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace TokyoExpress.Grid
{
    /// <summary>
    /// Generates smooth Bézier spline paths for rails, curves, and track segments.
    /// Provides precise spline evaluation for vehicle placement and rail mesh extrusion.
    /// </summary>
    public static class TokyoSplineRailBuilder
    {
        public static SplineContainer CreateCurvedTurnSpline(GameObject parent, Vector3 localStart, Vector3 localEnd, Vector3 cornerApex)
        {
            SplineContainer container = parent.AddComponent<SplineContainer>();
            Spline spline = container.Spline;
            spline.Clear();

            // Quadratic / Cubic Bézier control points
            BezierKnot knotStart = new BezierKnot(localStart)
            {
                TangentOut = (cornerApex - localStart) * 0.55f
            };

            BezierKnot knotEnd = new BezierKnot(localEnd)
            {
                TangentIn = (cornerApex - localEnd) * 0.55f
            };

            spline.Add(knotStart);
            spline.Add(knotEnd);

            return container;
        }

        public static SplineContainer CreateStraightSpline(GameObject parent, Vector3 localStart, Vector3 localEnd)
        {
            SplineContainer container = parent.AddComponent<SplineContainer>();
            Spline spline = container.Spline;
            spline.Clear();

            spline.Add(new BezierKnot(localStart));
            spline.Add(new BezierKnot(localEnd));

            return container;
        }

        public static SplineContainer CreateCrossroadSplines(GameObject parent)
        {
            SplineContainer container = parent.AddComponent<SplineContainer>();
            // Spline 0: South to North
            Spline splineNS = container.Spline;
            splineNS.Clear();
            splineNS.Add(new BezierKnot(new Vector3(0f, 0.01f, -1f)));
            splineNS.Add(new BezierKnot(new Vector3(0f, 0.01f, 1f)));

            // Spline 1: West to East
            Spline splineWE = container.AddSpline();
            splineWE.Clear();
            splineWE.Add(new BezierKnot(new Vector3(-1f, 0.01f, 0f)));
            splineWE.Add(new BezierKnot(new Vector3(1f, 0.01f, 0f)));

            return container;
        }
    }
}
