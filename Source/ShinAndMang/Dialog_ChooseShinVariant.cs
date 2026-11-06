using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ShinAndMang
{
    public class Dialog_ChooseShinVariant : Window
    {
        private const float TitleHeight = 45f;
        private const float RowHeight = 34f;

        private readonly Hediff_ShinMastery mastery;
        private readonly List<HediffDef> variants;
        private Vector2 scrollPosition;

        public override Vector2 InitialSize => new Vector2(420f, 540f);

        public Dialog_ChooseShinVariant(Hediff_ShinMastery mastery)
        {
            this.mastery = mastery;
            variants = DefDatabase<HediffDef>.AllDefs
                .Where(ShinMechanics.IsShinVariantDef)
                .OrderBy(variantDef => variantDef.label)
                .ToList();
            forcePause = true;
            doCloseX = false;
            closeOnCancel = false;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, TitleHeight), "ShinAndMang_ChooseVariantTitle".Translate(mastery.pawn.LabelShort));
            Text.Font = GameFont.Small;

            Rect outRect = new Rect(0f, TitleHeight, inRect.width, inRect.height - TitleHeight);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, (variants.Count + 1) * RowHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            Rect randomRow = new Rect(0f, 0f, viewRect.width, RowHeight - 4f);
            if (Widgets.ButtonText(randomRow, "ShinAndMang_ChooseVariantRandom".Translate())) Choose(null);

            for (int variantIndex = 0; variantIndex < variants.Count; variantIndex++)
            {
                HediffDef variantDef = variants[variantIndex];
                Rect row = new Rect(0f, (variantIndex + 1) * RowHeight, viewRect.width, RowHeight - 4f);
                if (!variantDef.description.NullOrEmpty()) TooltipHandler.TipRegion(row, variantDef.description);
                if (Widgets.ButtonText(row, variantDef.LabelCap)) Choose(variantDef);
            }

            Widgets.EndScrollView();
        }

        /// <summary>
        /// Confirms the choice (null keeps the random roll) and closes the window.
        /// </summary>
        private void Choose(HediffDef chosenVariant)
        {
            mastery.ConfirmVariant(chosenVariant);
            Close();
        }
    }
}