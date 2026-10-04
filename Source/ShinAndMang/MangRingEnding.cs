using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShinAndMang
{
    /// <summary>
    /// How Mang (望) rings end: shown by the ring renderer after the rings themselves are gone.
    /// </summary>
    public enum MangRingEnding
    {
        FadeOut,   // used by a melee hit, timed out, or undrafted
        Shatter,   // the holder was downed or killed
        Fizzle,    // a ring failed to form
        Flare      // fired as a rail shot
    }
}
