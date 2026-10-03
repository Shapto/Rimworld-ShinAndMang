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

        // Torso
        private const float TorsoRingDiameter = 0.8f;
        private const float TorsoLowestOffset = -0.15f;
        private const float TorsoHighestOffset = 0.25f;
        private const float TorsoAltitudeOffset = 0.1f;

        // Timing
        private const float ModeTransitionSeconds = 0.35f;
        private const float GrowSeconds = 0.25f;
        private const float ShimmerSpeed = 4f;
        private const float ShimmerPhasePerRing = 1.3f;

        private enum RingMode { Weapon, Torso }

        private struct RingAnchor
        {
            public Vector3 position;
            public float angle;
            public float diameter;
            public float backAltitude;
            public float frontAltitude;
            public bool swapHalves;
        }

        private class PawnRingState
        {
            public RingMode mode;
            public float transitionProgress = 1f;
            public float visualTime;
            public List<RingAnchor> transitionStart = new List<RingAnchor>();
            public List<RingAnchor> lastShown = new List<RingAnchor>();
            public List<float> ringAges = new List<float>();
        }

        private readonly Dictionary<Pawn, PawnRingState> statesByPawn = new Dictionary<Pawn, PawnRingState>();
        private readonly List<RingAnchor> anchorBuffer = new List<RingAnchor>();
        private readonly List<Pawn> pawnsToForget = new List<Pawn>();

        public MapComponent_MangRings(Map map) : base(map)
        {
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
                RingMode mode = TryAddWeaponAnchors(pawn, ringCount, maximumRings) ? RingMode.Weapon : RingMode.Torso;
                if (mode == RingMode.Torso) AddTorsoAnchors(pawn, ringCount, maximumRings);

                PawnRingState state = GetState(pawn, mode);
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
                    }
                    state.lastShown.Add(shown);

                    float growth = Mathf.SmoothStep(0f, 1f, state.ringAges[ringIndex] / GrowSeconds);
                    float brightness = 0.5f + 0.5f * Mathf.Sin(state.visualTime * ShimmerSpeed + ringIndex * ShimmerPhasePerRing);
                    MangRingRenderer.DrawRing(shown.position, shown.angle, shown.diameter * growth, shown.backAltitude, shown.frontAltitude, shown.swapHalves, brightness);
                }
            }

            ForgetPawnsWithoutRings();
        }

        // Anchors

        private bool TryAddWeaponAnchors(Pawn pawn, int ringCount, int maximumRings)
        {
            if (!WeaponDrawRecord.TryGetCurrent(pawn, out WeaponDrawRecord record)) return false;
            if (record.weaponDef == null || !record.weaponDef.IsMeleeWeapon) return false;

            WeaponShape shape = WeaponShapeAnalyzer.GetShape(record.weaponDef);
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
                    swapHalves = record.flipped
                });
            }
            return true;
        }

        private void AddTorsoAnchors(Pawn pawn, int ringCount, int maximumRings)
        {
            Vector3 torsoCenter = pawn.DrawPos;
            for (int ringIndex = 0; ringIndex < ringCount; ringIndex++)
            {
                float heightFraction = maximumRings <= 1 ? 0.5f : (float)ringIndex / (maximumRings - 1);
                float heightOffset = Mathf.Lerp(TorsoLowestOffset, TorsoHighestOffset, heightFraction);

                anchorBuffer.Add(new RingAnchor
                {
                    position = torsoCenter + new Vector3(0f, 0f, heightOffset),
                    angle = 90f,
                    diameter = TorsoRingDiameter,
                    backAltitude = torsoCenter.y - TorsoAltitudeOffset,
                    frontAltitude = torsoCenter.y + TorsoAltitudeOffset,
                    swapHalves = false
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
    }
}
