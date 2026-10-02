using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace ShinAndMang
{
    public class HediffCompProperties_ShinActivation : HediffCompProperties
    {
        public float activationMoodCost = 0.10f;
        public string iconPath = "Icons/Shin";
        public HediffCompProperties_ShinActivation() => compClass = typeof(HediffComp_ShinActivation);
    }

    public class HediffComp_ShinActivation : HediffComp
    {
        private static Texture2D cachedIcon;

        public HediffCompProperties_ShinActivation Props => (HediffCompProperties_ShinActivation)props;

        private Texture2D Icon
        {
            get
            {
                if (cachedIcon == null)
                    cachedIcon = ContentFinder<Texture2D>.Get(Props.iconPath, reportFailure: false) ?? BaseContent.BadTex;
                return cachedIcon;
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            Pawn pawn = parent.pawn;
            if (pawn.Faction != Faction.OfPlayer) yield break;

            var shinToggle = new Command_Toggle
            {
                defaultLabel = "ShinAndMang_ShinGizmoLabel".Translate(),
                defaultDesc = "ShinAndMang_ShinGizmoDescription".Translate(Props.activationMoodCost.ToStringPercent()),
                icon = Icon,
                isActive = () => ShinMechanics.IsShinActive(pawn),
                toggleAction = () =>
                {
                    if (ShinMechanics.IsShinActive(pawn)) ShinMechanics.EndShin(pawn);
                    else ShinMechanics.TryActivateShin(pawn);
                }
            };

            if (!ShinMechanics.IsShinActive(pawn) && !ShinMechanics.CanActivateShin(pawn, out string disabledReason))
                shinToggle.Disable(disabledReason);

            yield return shinToggle;

            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Shin Mastery +10%",
                    action = () =>
                    {
                        ShinMechanics.GainMastery(parent.pawn, 0.1f);
                    }
                };
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Shin Mastery -10%",
                    action = () =>
                    {
                        parent.Severity = Mathf.Min(1f, parent.Severity + 0.1f);
                        ShinMechanics.NotifyMasteryChanged(parent.pawn);
                    }
                };
            }
        }
    }
}
