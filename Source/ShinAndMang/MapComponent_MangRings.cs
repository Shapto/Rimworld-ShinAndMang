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
    /// Shows every held Mang (望) ring on the map: on the melee weapon if one is drawn, otherwise around the torso.
    /// Rings stay glued to their spot, only blending across when they switch spots, and grow in when formed.
    /// </summary>
    public class MapComponent_MangRings : MapComponent
    {
        // Weapon
        private const float RingGapFactor = 2.6f;
        private const float WeaponAltitudeOffset = 0.003f;

        // Barrel
        private const float BarrelFirstRingDistance = 0.08f;   // gap between the muzzle and the first ring, relative to the weapon's length
        private const float BarrelRingSpacing = 0.1f;          // gap between rings, relative to the weapon's length
        private const float BarrelRingWidthFactor = 3f;        // ring diameter relative to the barrel's width
        private const float BarrelRingMinimumSize = 0.18f;     // ring diameter at least this, relative to the weapon's length

        // Rail
        private const float RailStreakSeconds = 0.35f;
        private const float RailStreakWidth = 0.35f;
        private const int RailImpactSparkCount = 5;

        // Torso
        private const float TorsoRingDiameter = 0.8f;
        private const float TorsoLowestOffset = -0.3f;
        private const float TorsoHighestOffset = 0.45f;
        private const float TorsoAltitudeOffset = 0.1f;
        private const float TorsoRingIntensity = 0.8f;   // torso rings are dimmer than weapon rings
        private const float TorsoRingGapFactor = 1.2f;       // ring diameter relative to the torso's width, leaving a gap
        private const float TorsoRingLowestShare = 0.2f;     // rings span this part of the body's height
        private const float TorsoRingHighestShare = 0.7f;

        // Timing
        private const float ModeTransitionSeconds = 0.35f;
        private const float GrowSeconds = 0.15f;
        private const float ShimmerSpeed = 4f;
        private const float ShimmerPhasePerRing = 1.3f;
        private const float BurstSeconds = 0.25f;
        private const float BurstMaximumScale = 2.8f;   // how far the burst expands, relative to the ring
        private const float FlareStartScale = 0.9f;   // starts slightly inside the ring's line, so the flames grow out of it
        private const float FlareEndScale = 1.8f;          // how far the flames swell outward

        // Sparks
        private const int SparkIntervalTicks = 20;             // about 3 sparks per second, per ring
        private const float RingEllipseAspect = 32f / 94f;      // the ring's width relative to its height, from the texture
        private const float SparkMinimumSpeed = 0.5f;
        private const float SparkMaximumSpeed = 1.0f;

        // Unstable
        private const float UnstableFlickerSpeed = 25f;     // how fast it cuts in and out
        private const float UnstableWobbleSpeed = 12f;
        private const float UnstableWobble = 0.15f;         // how much its size wavers

        // Endings
        private const float FadeOutSeconds = 0.4f;
        private const float ShatterSeconds = 0.45f;
        private const float ShatterDistance = 0.6f;          // how far the halves fly apart, relative to the ring's diameter
        private const float FizzleFormSeconds = 0.35f;       // how long a failing ring flickers before it breaks
        private const int ShatterSparkCount = 6;
        private const float FlareOutSeconds = 0.3f;

        private enum RingMode { Weapon, Barrel, Torso }

        private struct RingAnchor
        {
            public Vector3 position;
            public float angle;
            public float diameter;
            public float backAltitude;
            public float frontAltitude;
            public bool swapHalves;
            public float intensity;
        }

        private class PawnRingState
        {
            public RingMode mode;
            public float transitionProgress = 1f;
            public float visualTime;
            public Vector3 pawnPosition;
            public List<RingAnchor> transitionStart = new List<RingAnchor>();
            public List<RingAnchor> lastShown = new List<RingAnchor>();
            public List<float> ringAges = new List<float>();
        }

        private class GhostRing
        {
            public Pawn pawn;
            public RingAnchor anchor;
            public Vector3 pawnPositionAtStart;
            public MangRingEnding ending;
            public int ringIndex;
            public float age;
            public bool hasBurst;
        }

        private readonly List<GhostRing> ghostRings = new List<GhostRing>();
        private readonly List<RailStreak> railStreaks = new List<RailStreak>();
        private readonly Dictionary<Pawn, PawnRingState> statesByPawn = new Dictionary<Pawn, PawnRingState>();
        private readonly List<RingAnchor> anchorBuffer = new List<RingAnchor>();
        private readonly List<Pawn> pawnsToForget = new List<Pawn>();

        public MapComponent_MangRings(Map map) : base(map)
        {
        }

        private class RailStreak
        {
            public Vector3 start;
            public Vector3 end;
            public float age;
        }

        /// <summary>
        /// True if the pawn is fighting in melee right now: on a melee attack job, or in the pause around a melee strike.
        /// </summary>
        private static bool IsFightingInMelee(Pawn pawn)
        {
            if (pawn.CurJobDef == JobDefOf.AttackMelee) return true;
            return pawn.stances?.curStance is Stance_Busy busyStance && busyStance.verb is Verb_MeleeAttack;
        }

        public override void MapComponentUpdate()
        {
            if (Find.CurrentMap != map) return;
            float deltaTime = Find.TickManager.Paused ? 0f : Time.deltaTime;

            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                int ringCount = MangMechanics.CurrentRings(pawn);
                if (ringCount == 0) continue;

                anchorBuffer.Clear();
                int maximumRings = Mathf.Max(ringCount, MangMechanics.MaximumRings(pawn));
                RingMode mode = TryAddWeaponAnchors(pawn, ringCount, maximumRings, out RingMode weaponMode) ? weaponMode : RingMode.Torso;
                if (mode == RingMode.Torso) AddTorsoAnchors(pawn, ringCount, maximumRings);

                PawnRingState state = GetState(pawn, mode);
                state.pawnPosition = pawn.DrawPos;
                state.visualTime += deltaTime;
                UpdateRingAges(state, ringCount, deltaTime);

                // A new spot: remember where the rings are now, and blend from there.
                if (mode != state.mode)
                {
                    state.mode = mode;
                    state.transitionProgress = 0f;
                    state.transitionStart.Clear();
                    state.transitionStart.AddRange(state.lastShown);
                }
                state.transitionProgress = Mathf.Min(1f, state.transitionProgress + deltaTime / ModeTransitionSeconds);
                float blend = Mathf.SmoothStep(0f, 1f, state.transitionProgress);

                state.lastShown.Clear();
                for (int ringIndex = 0; ringIndex < ringCount; ringIndex++)
                {
                    RingAnchor anchor = anchorBuffer[ringIndex];
                    RingAnchor shown = anchor;

                    // Only while switching spots: blend from where the ring was. Otherwise it's glued to its anchor.
                    if (blend < 1f && ringIndex < state.transitionStart.Count)
                    {
                        RingAnchor start = state.transitionStart[ringIndex];
                        shown.position = Vector3.Lerp(start.position, anchor.position, blend);
                        shown.angle = Mathf.LerpAngle(start.angle, anchor.angle, blend);
                        shown.diameter = Mathf.Lerp(start.diameter, anchor.diameter, blend);
                        shown.intensity = Mathf.Lerp(start.intensity, anchor.intensity, blend);
                    }
                    state.lastShown.Add(shown);

                    float growth = Mathf.SmoothStep(0f, 1f, state.ringAges[ringIndex] / GrowSeconds);
                    float brightness = 0.5f + 0.5f * Mathf.Sin(state.visualTime * ShimmerSpeed + ringIndex * ShimmerPhasePerRing);
                    float diameter = shown.diameter * growth;
                    float intensity = shown.intensity;

                    // An unstable ring (still in the unreliable zone) flickers and wavers for as long as it's held.
                    if (!MangMechanics.IsRingStable(pawn, ringIndex + 1))
                    {
                        intensity *= UnstableFlicker(state.visualTime, ringIndex);
                        diameter *= UnstableWobbleFactor(state.visualTime, ringIndex);
                        brightness = 0f;
                    }
                    MangRingRenderer.DrawRing(shown.position, shown.angle, diameter, shown.backAltitude, shown.frontAltitude, shown.swapHalves, brightness, intensity);

                    // Formation burst: a flash, flames swelling outward around the ring, and a shockwave ring.
                    float burstAge = state.ringAges[ringIndex];
                    float burstProgress = burstAge / BurstSeconds;
                    if (burstProgress < 1f)
                    {
                        float easedProgress = 1f - (1f - burstProgress) * (1f - burstProgress);   // fast at first, slowing down

                        // Flare: stays bright for a moment, then fades as it spreads.
                        float flareScale = Mathf.Lerp(FlareStartScale, FlareEndScale, easedProgress);
                        float flareIntensity = 1f - burstProgress * burstProgress;
                        MangRingRenderer.DrawFlare(shown.position, shown.angle, shown.diameter, flareScale, shown.backAltitude, shown.frontAltitude, shown.swapHalves, flareIntensity);

                        // Shockwave ring.
                        float burstScale = Mathf.Lerp(1f, BurstMaximumScale, easedProgress);
                        MangRingRenderer.DrawRing(shown.position, shown.angle, shown.diameter * burstScale, shown.backAltitude, shown.frontAltitude, shown.swapHalves, 1f, 1f - burstProgress);
                    }
                }
            }
            // Endings: fading, shattering and fizzling rings, drawn until they finish.
            for (int ghostIndex = ghostRings.Count - 1; ghostIndex >= 0; ghostIndex--)
            {
                GhostRing ghost = ghostRings[ghostIndex];
                ghost.age += deltaTime;
                if (!DrawGhost(ghost, deltaTime > 0f)) ghostRings.RemoveAt(ghostIndex);
            }
            // Rail shots: a beam that snaps on, then thins and fades.
            float beamAltitude = AltitudeLayer.MoteOverhead.AltitudeFor();
            for (int streakIndex = railStreaks.Count - 1; streakIndex >= 0; streakIndex--)
            {
                RailStreak streak = railStreaks[streakIndex];
                streak.age += deltaTime;

                float streakProgress = streak.age / RailStreakSeconds;
                if (streakProgress >= 1f)
                {
                    railStreaks.RemoveAt(streakIndex);
                    continue;
                }

                float remaining = 1f - streakProgress;
                MangRingRenderer.DrawBeam(streak.start, streak.end, RailStreakWidth * remaining, beamAltitude, remaining);
            }

            ForgetPawnsWithoutRings();
        }

        // Anchors

        private bool TryAddWeaponAnchors(Pawn pawn, int ringCount, int maximumRings, out RingMode weaponMode)
        {

            weaponMode = RingMode.Weapon;
            if (!WeaponDrawRecord.TryGetCurrent(pawn, out WeaponDrawRecord record) || record.weaponDef == null) return false;

            WeaponShape shape = WeaponShapeAnalyzer.GetShape(record.weaponDef);
            // Weapons with a gun use the barrel, except while the pawn is fighting in melee (bayonets, gunlances and the like).
            if (record.weaponDef.IsRangedWeapon && !IsFightingInMelee(pawn))
            {
                weaponMode = RingMode.Barrel;
                AddBarrelAnchors(record, shape, ringCount);
                return true;
            }
            if (record.weaponDef.tools.NullOrEmpty() && !record.weaponDef.IsMeleeWeapon) return false;

            for (int ringIndex = 0; ringIndex < ringCount; ringIndex++)
            {
                if (!shape.TryGetRingPlacement(ringIndex, maximumRings, out Vector2 spritePosition, out Vector2 spriteDirection, out float spriteWidth))
                {
                    anchorBuffer.Clear();
                    return false;
                }

                Vector3 worldPosition = record.SpritePointToWorld(spritePosition);
                Vector3 worldDirection = record.SpritePointToWorld(spritePosition + spriteDirection * 0.05f) - worldPosition;
                float bladeAngle = Mathf.Atan2(worldDirection.x, worldDirection.z) * Mathf.Rad2Deg;

                anchorBuffer.Add(new RingAnchor
                {
                    position = worldPosition,
                    angle = bladeAngle + 90f,
                    diameter = record.SpriteLengthToWorld(spriteWidth) * RingGapFactor,
                    backAltitude = worldPosition.y - WeaponAltitudeOffset,
                    frontAltitude = worldPosition.y + WeaponAltitudeOffset,
                    swapHalves = record.flipped,
                    intensity = 1f
                });
            }
            return true;
        }

        /// <summary>
        /// Rings in front of the muzzle, lined up along the barrel, the first closest to the gun.
        /// </summary>
        private void AddBarrelAnchors(WeaponDrawRecord record, WeaponShape shape, int ringCount)
        {
            Vector3 muzzleWorld = record.SpritePointToWorld(shape.muzzlePoint);
            Vector3 barrelDirection = record.SpritePointToWorld(shape.muzzlePoint + shape.forward * 0.05f) - muzzleWorld;
            barrelDirection.y = 0f;
            barrelDirection.Normalize();
            float barrelAngle = Mathf.Atan2(barrelDirection.x, barrelDirection.z) * Mathf.Rad2Deg;

            float weaponWorldLength = record.SpriteLengthToWorld(shape.length);
            float diameter = Mathf.Max(record.SpriteLengthToWorld(shape.muzzleWidth) * BarrelRingWidthFactor, weaponWorldLength * BarrelRingMinimumSize);

            for (int ringIndex = 0; ringIndex < ringCount; ringIndex++)
            {
                float distanceFromMuzzle = weaponWorldLength * (BarrelFirstRingDistance + ringIndex * BarrelRingSpacing);
                Vector3 ringPosition = muzzleWorld + barrelDirection * distanceFromMuzzle;

                anchorBuffer.Add(new RingAnchor
                {
                    position = ringPosition,
                    angle = barrelAngle + 90f,
                    diameter = diameter,
                    backAltitude = muzzleWorld.y - WeaponAltitudeOffset,
                    frontAltitude = muzzleWorld.y + WeaponAltitudeOffset,
                    swapHalves = record.flipped,
                    intensity = 1f
                });
            }
        }

        private void AddTorsoAnchors(Pawn pawn, int ringCount, int maximumRings)
        {
            Vector3 torsoCenter = pawn.DrawPos;

            // Sized from the actual body when possible, otherwise from fixed values scaled by body size.
            float diameter;
            float lowestOffset;
            float highestOffset;
            if (TryGetBodyShape(pawn, out BodyShapeAnalyzer.BodyShape bodyShape))
            {
                float bodyDrawSize = HumanlikeMeshPoolUtility.HumanlikeBodyWidthForPawn(pawn);
                diameter = bodyShape.torsoWidth * bodyDrawSize * TorsoRingGapFactor;
                lowestOffset = Mathf.Lerp(bodyShape.bottom, bodyShape.top, TorsoRingLowestShare) * bodyDrawSize;
                highestOffset = Mathf.Lerp(bodyShape.bottom, bodyShape.top, TorsoRingHighestShare) * bodyDrawSize;
            }
            else
            {
                float sizeFactor = Mathf.Sqrt(pawn.BodySize);
                diameter = TorsoRingDiameter * sizeFactor;
                lowestOffset = TorsoLowestOffset * sizeFactor;
                highestOffset = TorsoHighestOffset * sizeFactor;
            }

            for (int ringIndex = 0; ringIndex < ringCount; ringIndex++)
            {
                float heightFraction = maximumRings <= 1 ? 0.5f : (float)ringIndex / (maximumRings - 1);
                float heightOffset = Mathf.Lerp(lowestOffset, highestOffset, heightFraction);

                anchorBuffer.Add(new RingAnchor
                {
                    position = torsoCenter + new Vector3(0f, 0f, heightOffset),
                    angle = 90f,
                    diameter = diameter,
                    backAltitude = torsoCenter.y - TorsoAltitudeOffset,
                    frontAltitude = torsoCenter.y + TorsoAltitudeOffset,
                    swapHalves = false,
                    intensity = TorsoRingIntensity
                });
            }
        }

        // State

        private PawnRingState GetState(Pawn pawn, RingMode mode)
        {
            if (!statesByPawn.TryGetValue(pawn, out PawnRingState state))
            {
                state = new PawnRingState { mode = mode };
                statesByPawn[pawn] = state;
            }
            return state;
        }

        /// <summary>
        /// New rings start at age 0 (so they grow in); removed rings are dropped.
        /// </summary>
        private static void UpdateRingAges(PawnRingState state, int ringCount, float deltaTime)
        {
            while (state.ringAges.Count < ringCount) state.ringAges.Add(0f);
            if (state.ringAges.Count > ringCount) state.ringAges.RemoveRange(ringCount, state.ringAges.Count - ringCount);
            for (int ringIndex = 0; ringIndex < state.ringAges.Count; ringIndex++) state.ringAges[ringIndex] += deltaTime;
        }

        private void ForgetPawnsWithoutRings()
        {
            pawnsToForget.Clear();
            foreach (Pawn pawn in statesByPawn.Keys)
            {
                if (!pawn.Spawned || pawn.Map != map || MangMechanics.CurrentRings(pawn) == 0) pawnsToForget.Add(pawn);
            }
            foreach (Pawn pawn in pawnsToForget) statesByPawn.Remove(pawn);
        }

        // Sparks

        public override void MapComponentTick()
        {
            int currentTick = Find.TickManager.TicksGame;
            foreach (KeyValuePair<Pawn, PawnRingState> pawnAndState in statesByPawn)
            {
                Pawn pawn = pawnAndState.Key;
                if (!pawn.Spawned || pawn.Map != map) continue;

                List<RingAnchor> shownRings = pawnAndState.Value.lastShown;
                for (int ringIndex = 0; ringIndex < shownRings.Count; ringIndex++)
                {
                    // Offset per pawn and per ring, so rings don't all spark on the same tick.
                    if ((currentTick + pawn.thingIDNumber + ringIndex * 7) % SparkIntervalTicks != 0) continue;
                    ThrowRingSpark(shownRings[ringIndex]);
                }
            }
        }

        /// <summary>
        /// Throws one spark from a random point on the ring's outline, flying outward from its center.
        /// </summary>
        private void ThrowRingSpark(RingAnchor ring)
        {
            float aroundRing = Rand.Range(0f, Mathf.PI * 2f);
            float longRadius = ring.diameter / 2f;

            // A point on the ellipse (thin across, long along), then turned to the ring's angle.
            Vector3 pointOnRing = new Vector3(Mathf.Cos(aroundRing) * longRadius * RingEllipseAspect, 0f, Mathf.Sin(aroundRing) * longRadius);
            Vector3 offsetFromCenter = Quaternion.AngleAxis(ring.angle, Vector3.up) * pointOnRing;

            float outwardAngle = Mathf.Atan2(offsetFromCenter.x, offsetFromCenter.z) * Mathf.Rad2Deg;
            ShinVisuals.ThrowSparkAt(ShinDefOf.Fleck_MangSpark, map, ring.position + offsetFromCenter, outwardAngle, Rand.Range(SparkMinimumSpeed, SparkMaximumSpeed));
        }

        // Endings

        /// <summary>
        /// The pawn's rings were removed: they shatter on collapse, otherwise fade out.
        /// </summary>
        public static void NotifyRingsEnded(Pawn pawn, MangRingEnding ending)
        {
            MapComponent_MangRings ringComponent = pawn.MapHeld?.GetComponent<MapComponent_MangRings>();
            if (ringComponent == null || !ringComponent.statesByPawn.TryGetValue(pawn, out PawnRingState state)) return;

            for (int ringIndex = 0; ringIndex < state.lastShown.Count; ringIndex++)
            {
                ringComponent.ghostRings.Add(new GhostRing
                {
                    pawn = pawn,
                    anchor = state.lastShown[ringIndex],
                    pawnPositionAtStart = state.pawnPosition,
                    ending = ending,
                    ringIndex = ringIndex
                });
            }
            ringComponent.statesByPawn.Remove(pawn);
        }

        /// <summary>
        /// A rail shot was fired: draws its streak and throws sparks at each impact.
        /// </summary>
        public static void NotifyRailFired(Map map, Vector3 start, Vector3 end, List<Vector3> impactPoints)
        {
            MapComponent_MangRings ringComponent = map?.GetComponent<MapComponent_MangRings>();
            if (ringComponent == null) return;

            ringComponent.railStreaks.Add(new RailStreak { start = start, end = end });

            // Sparks spray forward along the shot from everything it hit.
            Vector3 shotDirection = end - start;
            float shotAngle = Mathf.Atan2(shotDirection.x, shotDirection.z) * Mathf.Rad2Deg;
            foreach (Vector3 impactPoint in impactPoints)
            {
                for (int sparkIndex = 0; sparkIndex < RailImpactSparkCount; sparkIndex++)
                {
                    ShinVisuals.ThrowSparkAt(ShinDefOf.Fleck_MangSpark, map, impactPoint, shotAngle + Rand.Range(-35f, 35f), Rand.Range(SparkMinimumSpeed, SparkMaximumSpeed * 1.5f));
                }
            }
        }

        /// <summary>
        /// Ring number "ringNumber" failed to form: an unstable ring flickers in at its spot, then shatters.
        /// </summary>
        public static void NotifyFizzle(Pawn pawn, int ringNumber)
        {
            MapComponent_MangRings ringComponent = pawn.Map?.GetComponent<MapComponent_MangRings>();
            if (ringComponent == null) return;

            ringComponent.anchorBuffer.Clear();
            int maximumRings = Mathf.Max(ringNumber, MangMechanics.MaximumRings(pawn));
            if (!ringComponent.TryAddWeaponAnchors(pawn, ringNumber, maximumRings, out _)) ringComponent.AddTorsoAnchors(pawn, ringNumber, maximumRings);

            ringComponent.ghostRings.Add(new GhostRing
            {
                pawn = pawn,
                anchor = ringComponent.anchorBuffer[ringNumber - 1],
                pawnPositionAtStart = pawn.DrawPos,
                ending = MangRingEnding.Fizzle,
                ringIndex = ringNumber - 1
            });
        }

        /// <summary>
        /// Draws one ending ring. Returns false once it has finished.
        /// </summary>
        private bool DrawGhost(GhostRing ghost, bool gameRunning)
        {
            // Follow the pawn (or its corpse) as it moves or falls.
            Vector3 currentPawnPosition = ghost.pawn.Spawned ? ghost.pawn.DrawPos : ghost.pawn.Corpse?.DrawPos ?? ghost.pawnPositionAtStart;
            RingAnchor anchor = ghost.anchor;
            anchor.position += currentPawnPosition - ghost.pawnPositionAtStart;

            switch (ghost.ending)
            {
                case MangRingEnding.FadeOut:
                    {
                        float fadeProgress = ghost.age / FadeOutSeconds;
                        if (fadeProgress >= 1f) return false;
                        MangRingRenderer.DrawRing(anchor.position, anchor.angle, anchor.diameter, anchor.backAltitude, anchor.frontAltitude, anchor.swapHalves, 0.5f, 1f - fadeProgress);
                        return true;
                    }

                case MangRingEnding.Shatter:
                    return DrawShatter(ghost, anchor, ghost.age, gameRunning);

                case MangRingEnding.Flare:
                    {
                        // Fired: the ring bursts into its flare one last time, brightening as it vanishes.
                        float flareOutProgress = ghost.age / FlareOutSeconds;
                        if (flareOutProgress >= 1f) return false;

                        float easedProgress = 1f - (1f - flareOutProgress) * (1f - flareOutProgress);
                        float fade = 1f - flareOutProgress;
                        MangRingRenderer.DrawFlare(anchor.position, anchor.angle, anchor.diameter, Mathf.Lerp(FlareStartScale, FlareEndScale, easedProgress), anchor.backAltitude, anchor.frontAltitude, anchor.swapHalves, fade * anchor.intensity);
                        MangRingRenderer.DrawRing(anchor.position, anchor.angle, anchor.diameter, anchor.backAltitude, anchor.frontAltitude, anchor.swapHalves, 1f, fade * anchor.intensity);
                        return true;
                    }

                case MangRingEnding.Fizzle:
                    {
                        // First it flickers into place, unstable...
                        if (ghost.age < FizzleFormSeconds)
                        {
                            float growth = Mathf.SmoothStep(0f, 1f, ghost.age / FizzleFormSeconds);
                            float diameter = anchor.diameter * growth * UnstableWobbleFactor(ghost.age, ghost.ringIndex);
                            MangRingRenderer.DrawRing(anchor.position, anchor.angle, diameter, anchor.backAltitude, anchor.frontAltitude, anchor.swapHalves, 0f, UnstableFlicker(ghost.age, ghost.ringIndex));
                            return true;
                        }
                        // ...then cracks and shatters.
                        return DrawShatter(ghost, anchor, ghost.age - FizzleFormSeconds, gameRunning);
                    }
            }
            return false;
        }

        /// <summary>
        /// The halves break apart and fade, with a burst of sparks as they crack.
        /// </summary>
        private bool DrawShatter(GhostRing ghost, RingAnchor anchor, float shatterAge, bool gameRunning)
        {
            float shatterProgress = shatterAge / ShatterSeconds;
            if (shatterProgress >= 1f) return false;

            if (!ghost.hasBurst && gameRunning)
            {
                ghost.hasBurst = true;
                for (int sparkIndex = 0; sparkIndex < ShatterSparkCount; sparkIndex++) ThrowRingSpark(anchor);
            }

            float easedProgress = 1f - (1f - shatterProgress) * (1f - shatterProgress);
            float separation = easedProgress * anchor.diameter * ShatterDistance;
            MangRingRenderer.DrawRing(anchor.position, anchor.angle, anchor.diameter, anchor.backAltitude, anchor.frontAltitude, anchor.swapHalves, 0f, 1f - shatterProgress, separation);
            return true;
        }

        /// <summary>
        /// The body texture's shape for the pawn's current facing. False for pawns without a body type (animals, mechs).
        /// </summary>
        private static bool TryGetBodyShape(Pawn pawn, out BodyShapeAnalyzer.BodyShape bodyShape)
        {
            bodyShape = null;
            string bodyPath = pawn.story?.bodyType?.bodyNakedGraphicPath;
            if (bodyPath.NullOrEmpty()) return false;

            // Body textures have one image per facing; west uses the east image, mirrored.
            string facingSuffix = pawn.Rotation == Rot4.North ? "_north" : pawn.Rotation == Rot4.South ? "_south" : "_east";
            Texture2D bodyTexture = ContentFinder<Texture2D>.Get(bodyPath + facingSuffix, false);
            if (bodyTexture == null) return false;

            bodyShape = BodyShapeAnalyzer.GetShape(bodyTexture);
            return true;
        }

        // Unstable

        /// <summary>
        /// Cuts between visible and nearly gone, irregularly.
        /// </summary>
        private static float UnstableFlicker(float time, int ringIndex) => Mathf.PerlinNoise(time * UnstableFlickerSpeed, ringIndex * 3.7f) > 0.4f ? 1f : 0.25f;

        /// <summary>
        /// A size multiplier wavering around 1.
        /// </summary>
        private static float UnstableWobbleFactor(float time, int ringIndex) => 1f + (Mathf.PerlinNoise(time * UnstableWobbleSpeed, 11.3f + ringIndex) - 0.5f) * UnstableWobble;
    }
}
