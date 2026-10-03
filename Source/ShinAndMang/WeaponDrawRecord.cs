using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ShinAndMang
{
    // <summary>
    /// Where a pawn's weapon was drawn this frame. Filled by the vanilla drawing patch,
    /// and later by animation mod patches, so the rings always follow the weapon as it's actually shown.
    /// </summary>
    public class WeaponDrawRecord
    {
        public ThingDef weaponDef;
        public Vector3 position;
        public float drawAngle;     // degrees, clockwise on screen
        public bool flipped;        // drawn mirrored
        public Vector2 drawSize;
        public int frame;

        private static readonly Dictionary<Pawn, WeaponDrawRecord> recordsByPawn = new Dictionary<Pawn, WeaponDrawRecord>();

        public static void Record(Pawn pawn, ThingDef weaponDef, Vector3 position, float drawAngle, bool flipped, Vector2 drawSize)
        {
            if (!recordsByPawn.TryGetValue(pawn, out WeaponDrawRecord record))
            {
                record = new WeaponDrawRecord();
                recordsByPawn[pawn] = record;
            }
            record.weaponDef = weaponDef;
            record.position = position;
            record.drawAngle = drawAngle;
            record.flipped = flipped;
            record.drawSize = drawSize;
            record.frame = Time.frameCount;
        }



        /// <summary>
        /// The pawn's weapon transform, only if it was drawn this frame (otherwise the weapon isn't visible).
        /// </summary>
        public static bool TryGetCurrent(Pawn pawn, out WeaponDrawRecord record)
        {
            return recordsByPawn.TryGetValue(pawn, out record) && record.frame >= Time.frameCount - 1;
        }

        /// <summary>
        /// Turns a length in sprite units into world units, using the weapon's average draw size.
        /// </summary>
        public float SpriteLengthToWorld(float spriteLength) => spriteLength * (drawSize.x + drawSize.y) / 2f;

        /// <summary>
        /// Turns a point on the weapon's sprite (in sprite units, -0.5 to 0.5) into a position in the world.
        /// </summary>
        public Vector3 SpritePointToWorld(Vector2 spritePoint)
        {
            float localX = spritePoint.x * drawSize.x * (flipped ? -1f : 1f);
            float localY = spritePoint.y * drawSize.y;

            Vector3 rotatedOffset = Quaternion.AngleAxis(drawAngle, Vector3.up) * new Vector3(localX, 0f, localY);
            return position + rotatedOffset;
        }
    }
}
