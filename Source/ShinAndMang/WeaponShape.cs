using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ShinAndMang
{
    /// <summary>
    /// Where Mang (望) rings sit on a weapon sprite, in sprite units (the texture spans -0.5 to 0.5).
    /// The ring zone is a stretch of the weapon's spine near its tip; rings fill it from the handle side toward the tip.
    /// </summary>
    public class WeaponShape
    {
        public Vector2 forward;
        public List<Vector2> spinePoints = new List<Vector2>();   // blade center line across the zone, back to front
        public List<float> spineWidths = new List<float>();       // weapon width at each spine point

        /// <summary>
        /// Where ring number "ringIndex" (0 = first formed, at the back of the zone) of "ringCount" sits,
        /// which way the blade runs there, and how wide it is.
        /// </summary>
        public bool TryGetRingPlacement(int ringIndex, int ringCount, out Vector2 position, out Vector2 direction, out float width)
        {
            position = Vector2.zero;
            direction = forward;
            width = 0f;
            if (spinePoints.Count == 0) return false;
            if (spinePoints.Count == 1)
            {
                position = spinePoints[0];
                width = spineWidths[0];
                return true;
            }

            // How far through the zone this ring sits: a single ring goes in the middle.
            float zoneFraction = ringCount <= 1 ? 0.5f : (float)ringIndex / (ringCount - 1);
            float exactIndex = zoneFraction * (spinePoints.Count - 1);
            int lowerIndex = Mathf.Clamp(Mathf.FloorToInt(exactIndex), 0, spinePoints.Count - 2);
            float blend = exactIndex - lowerIndex;

            position = Vector2.Lerp(spinePoints[lowerIndex], spinePoints[lowerIndex + 1], blend);
            width = Mathf.Lerp(spineWidths[lowerIndex], spineWidths[lowerIndex + 1], blend);

            Vector2 localDirection = spinePoints[lowerIndex + 1] - spinePoints[lowerIndex];
            direction = localDirection.sqrMagnitude > 0f ? localDirection.normalized : forward;
            return true;
        }
    }
}
