using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ShinAndMang
{

    /// <summary>
    /// Like vanilla Graphic_Flicker (random frame swaps with a small jitter), but uses the def's
    /// drawSize instead of fire size, so it can be larger than 1.2 tiles.
    /// </summary>
    public class Graphic_FlickerSized : Graphic_Collection
    {
        private const int TicksPerFrame = 15;
        private const float JitterDistance = 0.05f;

        public override Material MatSingle => subGraphics[0].MatSingle;

        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            if (subGraphics.NullOrEmpty()) return;

            int seed = thing?.thingIDNumber ?? 0;
            int frameStep = (Find.TickManager.TicksGame + seed * 7) / TicksPerFrame;
            int frameIndex = (Gen.HashCombineInt(frameStep, seed) & int.MaxValue) % subGraphics.Length;

            Vector3 jitter = GenRadial.RadialPattern[frameStep % GenRadial.RadialPattern.Length].ToVector3()
                / GenRadial.MaxRadialPatternRadius * JitterDistance;

            Vector3 drawScale = new Vector3(drawSize.x, 1f, drawSize.y);
            Matrix4x4 drawMatrix = Matrix4x4.TRS(loc + jitter, Quaternion.identity, drawScale);
            Graphics.DrawMesh(MeshPool.plane10, drawMatrix, subGraphics[frameIndex].MatSingle, 0);
        }
    }
}
