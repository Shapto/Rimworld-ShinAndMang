using RimWorld;
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
    /// Visual effects for Shin (心): the rising embers and the activation burst.
    /// </summary>
    public static class ShinVisuals
    {
        private const float SparkSpawnRadius = 0.45f;

        /// <summary>
        /// One ember rising from around the pawn, drifting slightly to either side.
        /// </summary>
        public static void ThrowSpark(Pawn pawn)
        {
            Vector3 spawnPosition = pawn.DrawPos + new Vector3(Rand.Range(-SparkSpawnRadius, SparkSpawnRadius), 0f, Rand.Range(-SparkSpawnRadius, SparkSpawnRadius * 0.5f));
            FleckCreationData sparkData = FleckMaker.GetDataStatic(spawnPosition, pawn.Map, ShinDefOf.Fleck_ShinSpark, Rand.Range(0.7f, 1.2f));
            sparkData.velocityAngle = Rand.Range(-20f, 20f);
            sparkData.velocitySpeed = Rand.Range(0.4f, 0.9f);
            pawn.Map.flecks.CreateFleck(sparkData);
        }

        /// <summary>
        /// One spark of the given kind at a given spot, flying in a given direction (clockwise angle, 0 = up the screen).
        /// </summary>
        public static void ThrowSparkAt(FleckDef sparkDef, Map map, Vector3 position, float velocityAngle, float speed)
        {
            FleckCreationData sparkData = FleckMaker.GetDataStatic(position, map, sparkDef, Rand.Range(0.6f, 1f));
            sparkData.velocityAngle = velocityAngle;
            sparkData.velocitySpeed = speed;
            map.flecks.CreateFleck(sparkData);
        }
    }
}
