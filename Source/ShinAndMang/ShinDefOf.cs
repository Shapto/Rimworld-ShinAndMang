using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace ShinAndMang
{
    [DefOf]
    public class ShinDefOf
    {
        public static HediffDef Shin_Mastery;
        static ShinDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(ShinDefOf));
    }
}
