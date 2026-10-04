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
    /// The Mang (望) rail shot: an instant, perfectly straight trace from the shooter through the target and beyond,
    /// piercing pawns and walls depending on the ring count, and losing damage with every thing it pierces.
    /// </summary>
    public static class MangRail
    {
        private const float DefaultDamageFalloff = 0.75f;
        private const int DefaultMaximumWallsPierced = 3;

        public static void Fire(Pawn shooter, Projectile projectile, LocalTargetInfo target, int ringCount, float range)
        {
            Map map = shooter.Map;
            if (map == null || projectile.def.projectile == null) return;

            MangSettings settings = ShinDefOf.Mang_Rings.GetModExtension<MangSettings>();
            float damageFalloff = settings?.railDamageFalloff ?? DefaultDamageFalloff;
            int maximumWallsPierced = settings?.maximumWallsPierced ?? DefaultMaximumWallsPierced;

            // The launched projectile decides damage type, amount and armor penetration,
            // including anything the weapon's quality or another mod changed about it.
            Thing weapon = shooter.equipment?.Primary;
            DamageDef damageDef = projectile.def.projectile.damageDef;
            float damage = projectile.DamageAmount * MangMechanics.DamageMultiplier(ringCount);
            float armorPenetration = projectile.ArmorPenetration;

            // One pawn per ring (the target, plus one more pierced per extra ring), one wall per two rings.
            int pawnsAllowed = ringCount;
            int wallsAllowed = Mathf.Min(maximumWallsPierced, ringCount / 2);

            // The line: from the shooter, through the target's center, out to the weapon's range.
            Vector3 origin = shooter.DrawPos;
            Vector3 direction = (target.CenterVector3 - origin).Yto0().normalized;
            IntVec3 endCell = (origin + direction * range).ToIntVec3();
            float hitAngle = direction.AngleFlat();

            int pawnsHit = 0;
            int wallsHit = 0;
            bool stopped = false;
            var alreadyHit = new HashSet<Thing>();

            Vector3 muzzle = MuzzlePosition(shooter);
            var impactPoints = new List<Vector3>();
            Vector3 endPoint = endCell.ToVector3Shifted();

            foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(shooter.Position, endCell))
            {
                if (!cell.InBounds(map)) break;
                endPoint = cell.ToVector3Shifted();
                // Walls: pierced up to the limit; the next one takes the hit and stops the shot.
                Building wall = cell.GetEdifice(map);
                if (wall != null && wall.def.Fillage == FillCategory.Full)
                {
                    DealDamage(wall, damageDef, damage, armorPenetration, hitAngle, shooter, weapon);
                    if (wallsHit >= wallsAllowed) break;
                    wallsHit++;
                    damage *= damageFalloff;
                    continue;
                }

                // Pawns: each hit counts toward the limit. Downed pawns are skipped unless they're the target.
                foreach (Thing thing in cell.GetThingList(map).ToList())
                {
                    if (!(thing is Pawn hitPawn) || hitPawn == shooter || hitPawn.Dead) continue;
                    if (hitPawn.Downed && hitPawn != target.Thing) continue;
                    if (!alreadyHit.Add(hitPawn)) continue;

                    DealDamage(hitPawn, damageDef, damage, armorPenetration, hitAngle, shooter, weapon);
                    pawnsHit++;
                    damage *= damageFalloff;
                    if (pawnsHit >= pawnsAllowed)
                    {
                        stopped = true;
                        break;
                    }
                }
                if (stopped) break;
            }

            MapComponent_MangRings.NotifyRailFired(map, muzzle, endPoint, impactPoints);
        }

        /// <summary>
        /// Where the rail starts: the gun's muzzle as currently drawn, or the shooter if the gun isn't visible.
        /// </summary>
        private static Vector3 MuzzlePosition(Pawn shooter)
        {
            if (!WeaponDrawRecord.TryGetCurrent(shooter, out WeaponDrawRecord record) || record.weaponDef == null) return shooter.DrawPos;
            return record.SpritePointToWorld(WeaponShapeAnalyzer.GetShape(record.weaponDef).muzzlePoint);
        }
        private static void DealDamage(Thing target, DamageDef damageDef, float damage, float armorPenetration, float hitAngle, Pawn shooter, Thing weapon)
        {
            var damageInfo = new DamageInfo(damageDef, damage, armorPenetration, hitAngle, shooter, null, weapon?.def);
            target.TakeDamage(damageInfo);
        }
    }
}
