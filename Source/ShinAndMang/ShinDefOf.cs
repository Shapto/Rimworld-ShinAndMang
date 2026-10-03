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

        public static ThingDef Mote_ShinAura;

        public static FleckDef Fleck_ShinSpark;

        public static HediffDef Mang_Rings;

        public static JobDef FormMang;
        static ShinDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(ShinDefOf));
    }
}
