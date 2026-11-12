using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShinAndMang
{
    public static class ShinMath
    {
        /// <summary>
        /// True if the value is a real number (not NaN, not infinity).
        /// </summary>
        public static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        /// <summary>
        /// The value if it's a real number, otherwise the fallback.
        /// </summary>
        public static float Finite(float value, float fallback) => IsFinite(value) ? value : fallback;
    }
}
