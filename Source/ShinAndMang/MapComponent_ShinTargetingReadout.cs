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
    /// Shows the selected Shin (心) user's variant readout next to the cursor when hovering a pawn.
    /// Works the same for melee and ranged, independent of vanilla tooltips.
    /// </summary>
    public class MapComponent_ShinTargetingReadout : MapComponent
    {
        private const float CursorOffsetX = 30f;
        private const float CursorOffsetY = 30f;
        private const float BoxWidth = 260f;
        private const float BoxPadding = 6f;
        private static readonly Color ShinGold = new Color(1f, 0.82f, 0.35f);

        public MapComponent_ShinTargetingReadout(Map map) : base(map)
        {
        }

        public override void MapComponentOnGUI()
        {
            // Only draw once per frame, and only on the map being viewed.
            if (Event.current.type != EventType.Repaint || Find.CurrentMap != map) return;

            if (!(Find.Selector.SingleSelectedThing is Pawn shinUser) || !shinUser.Drafted) return;

            IntVec3 mouseCell = UI.MouseCell();
            if (!mouseCell.InBounds(map)) return;

            Pawn hoveredPawn = mouseCell.GetFirstPawn(map);
            if (hoveredPawn == null || hoveredPawn == shinUser) return;

            string readout = ShinVariantHooks.TargetingReadout(shinUser, hoveredPawn);
            if (readout.NullOrEmpty()) return;

            DrawReadout(readout);
        }

        private static void DrawReadout(string readout)
        {
            Text.Font = GameFont.Small;
            float textHeight = Text.CalcHeight(readout, BoxWidth - BoxPadding * 2f);

            Vector2 mousePosition = Event.current.mousePosition;
            Rect boxRect = new Rect(mousePosition.x + CursorOffsetX, mousePosition.y + CursorOffsetY, BoxWidth, textHeight + BoxPadding * 2f);

            GUI.color = ShinGold;
            Widgets.Label(boxRect.ContractedBy(BoxPadding), readout);
            GUI.color = Color.white;
        }
    }
}
