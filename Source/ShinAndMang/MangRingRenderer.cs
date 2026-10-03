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
    /// Draws a Mang (望) ring from one half-ring texture: the front half as is, the back half mirrored.
    /// The texture's ellipse is tall and thin, with its long axis along the texture's height.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class MangRingRenderer
    {
        private const float EllipseHeightShare = 188f / 256f;   // the ellipse fills this much of the texture's height
        private const float TextureAspect = 64f / 256f;         // texture width divided by height
        private const float FlareEllipseHeightShare = 300f / 512f;   // the flare's ellipse fills this much of its texture's height

        // Shimmer
        private static readonly Color DimGold = new Color(1f, 0.78f, 0.25f);
        private static readonly Color BrightGold = new Color(1f, 0.96f, 0.7f);

        private static readonly Material RingMaterial = MaterialPool.MatFrom("VFX/MangRing_Front", ShaderDatabase.MoteGlow, Color.white);
        private static readonly Material FlareMaterial = MaterialPool.MatFrom("VFX/MangBurstFlare", ShaderDatabase.MoteGlow, Color.white);
        private static readonly MaterialPropertyBlock ColorBlock = new MaterialPropertyBlock();
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

        static MangRingRenderer()
        {
            // These textures touch their left edge, so wrapping would bleed them onto the right edge as hairlines.
            RingMaterial.mainTexture.wrapMode = TextureWrapMode.Clamp;
            FlareMaterial.mainTexture.wrapMode = TextureWrapMode.Clamp;
        }

        /// <summary>
        /// Draws one ring. "angle" turns the ring's long axis (clockwise, 0 = up the screen),
        /// "diameter" is the ellipse's long axis in world units, "brightness" goes from 0 (dim) to 1 (bright),
        /// and "intensity" fades the whole ring, 0 being invisible.
        /// </summary>
        public static void DrawRing(Vector3 position, float angle, float diameter, float backAltitude, float frontAltitude, bool swapHalves, float brightness, float intensity = 1f, float halfSeparation = 0f)
        {
            Color ringColor = Color.Lerp(DimGold, BrightGold, brightness) * intensity;
            DrawHalves(RingMaterial, EllipseHeightShare, position, angle, diameter, backAltitude, frontAltitude, swapHalves, ringColor, halfSeparation);
        }

        /// <summary>
        /// Draws the flame flare of a ring's formation burst, around a ring of the given diameter.
        /// "scale" grows the flames outward; "intensity" fades them.
        /// </summary>
        public static void DrawFlare(Vector3 position, float angle, float ringDiameter, float scale, float backAltitude, float frontAltitude, bool swapHalves, float intensity)
        {
            DrawHalves(FlareMaterial, FlareEllipseHeightShare, position, angle, ringDiameter * scale, backAltitude, frontAltitude, swapHalves, BrightGold * intensity, 0f);
        }

        /// <summary>
        /// Draws a half-texture (ellipse centered on its left edge) as a full shape: the mirrored half behind, the other in front.
        /// </summary>
        private static void DrawHalves(Material material, float ellipseHeightShare, Vector3 position, float angle, float diameter, float backAltitude, float frontAltitude, bool swapHalves, Color color, float halfSeparation)
        {
            if (diameter <= 0.001f || color.maxColorComponent <= 0f) return;

            float quadHeight = diameter / ellipseHeightShare;
            Vector3 scale = new Vector3(quadHeight * TextureAspect, 1f, quadHeight);
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);

            ColorBlock.SetColor(ColorPropertyId, color);

            // The texture's ellipse is centered on its left edge, or on its right edge when mirrored,
            // so each half is shifted by half the quad's width to put that edge on the shape's center.
            // When shattering, the halves are pushed further apart by halfSeparation.
            Vector3 halfWidthOffset = rotation * new Vector3(scale.x / 2f + halfSeparation, 0f, 0f);
            Vector3 unmirroredPosition = position + halfWidthOffset;
            Vector3 mirroredPosition = position - halfWidthOffset;

            bool backIsMirrored = !swapHalves;
            Mesh backMesh = backIsMirrored ? MeshPool.plane10Flip : MeshPool.plane10;
            Mesh frontMesh = backIsMirrored ? MeshPool.plane10 : MeshPool.plane10Flip;
            Vector3 backPosition = backIsMirrored ? mirroredPosition : unmirroredPosition;
            Vector3 frontPosition = backIsMirrored ? unmirroredPosition : mirroredPosition;

            Graphics.DrawMesh(backMesh, Matrix4x4.TRS(new Vector3(backPosition.x, backAltitude, backPosition.z), rotation, scale), material, 0, null, 0, ColorBlock);
            Graphics.DrawMesh(frontMesh, Matrix4x4.TRS(new Vector3(frontPosition.x, frontAltitude, frontPosition.z), rotation, scale), material, 0, null, 0, ColorBlock);
        }
    }
}
