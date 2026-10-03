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
    /// The Mang (望) ring setting: left-click raises the target, right-click lowers it.
    /// </summary>
    public class Command_MangTarget : Command
    {
        public Hediff_ShinMastery mastery;
        public int maximumRings;

        public override void ProcessInput(Event clickEvent)
        {
            base.ProcessInput(clickEvent);

            int currentTarget = Mathf.Min(mastery.mangTargetRings, maximumRings);
            bool isRightClick = clickEvent.button == 1;

            if (isRightClick)
            {
                mastery.mangTargetRings = currentTarget <= 1 ? maximumRings : currentTarget - 1;
            }
            else
            {
                mastery.mangTargetRings = currentTarget >= maximumRings ? 1 : currentTarget + 1;
            }
        }
    }
}
