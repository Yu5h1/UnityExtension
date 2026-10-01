using UnityEngine;

namespace Yu5h1LibTest
{
    /// <summary>
    /// Paints the demo's two placeholder avatars (a doodle-style user, a chibi "AI" girl) directly into a
    /// <see cref="Texture2D"/> with simple circle/ellipse/line stamps - no art asset, no image-generation API
    /// key required. This is demo decoration only; the package itself needs nothing more than
    /// <see cref="Yu5h1Lib.UIToolkit.ChatParticipant.Avatar"/> already provides.
    /// </summary>
    internal static class ProceduralAvatarGenerator
    {
        private const int Size = 128;

        public static Sprite CreateUserAvatar() => ToSprite(PaintUserAvatar());
        public static Sprite CreateAiAvatar() => ToSprite(PaintAiAvatar());

        private static Texture2D PaintUserAvatar()
        {
            var px = new Color[Size * Size];
            Fill(px, new Color(.99f, .95f, .87f));

            var skin = new Color(1f, .78f, .82f);
            var outline = new Color(.15f, .12f, .12f);
            var cap = new Color(.98f, .98f, .98f);

            // Body, then limbs, then head+cap+face on top - so the head reads as "in front of" the body
            // and the raised arm reads as "behind" the head where they cross, the same draw order a hand-
            // drawn doodle would build up in.
            FillEllipseOutlined(px, 64, 88, 22, 26, skin, outline, 3);
            DrawCapsule(px, 46, 80, 36, 100, 9, skin, outline); // resting arm
            DrawCapsule(px, 82, 78, 98, 54, 9, skin, outline);  // raised, waving arm
            FillCircleOutlined(px, 98, 52, 8, skin, outline, 3);
            DrawCapsule(px, 56, 108, 50, 124, 10, skin, outline); // legs
            DrawCapsule(px, 72, 108, 78, 124, 10, skin, outline);
            FillCircleOutlined(px, 50, 124, 7, skin, outline, 3);
            FillCircleOutlined(px, 78, 124, 7, skin, outline, 3);

            FillCircleOutlined(px, 64, 54, 26, skin, outline, 3);

            // Sailor cap: brim, then dome, then a thin band along the seam.
            FillEllipseOutlined(px, 64, 42, 30, 8, cap, outline, 2);
            FillEllipseOutlined(px, 64, 30, 22, 16, cap, outline, 2);
            FillEllipse(px, 64, 38, 23, 3, outline);

            FillCircle(px, 54, 52, 3, outline);
            FillCircle(px, 74, 52, 3, outline);
            DrawCapsule(px, 56, 61, 64, 65, 2, outline, outline);
            DrawCapsule(px, 64, 65, 72, 61, 2, outline, outline);

            return Build(px);
        }

        private static Texture2D PaintAiAvatar()
        {
            var px = new Color[Size * Size];
            Fill(px, new Color(.90f, .92f, 1f));

            var hair = new Color(.62f, .52f, .86f);
            var skin = new Color(1f, .89f, .80f);
            var skinOutline = new Color(.45f, .32f, .24f);
            var iris = new Color(.18f, .28f, .55f);
            var blush = new Color(1f, .75f, .78f);
            var ribbon = new Color(.31f, .55f, 1f);

            // Hair volume and twin-tails sit behind the face.
            FillEllipse(px, 64, 60, 34, 40, hair);
            FillCircle(px, 26, 72, 13, hair);
            FillCircle(px, 102, 72, 13, hair);

            FillCircleOutlined(px, 64, 62, 30, skin, skinOutline, 2);

            // Fringe strands drawn on top of the forehead, after the face.
            FillEllipse(px, 48, 40, 10, 14, hair);
            FillEllipse(px, 64, 35, 11, 16, hair);
            FillEllipse(px, 80, 40, 10, 14, hair);

            PaintEye(px, 52, 64, iris);
            PaintEye(px, 76, 64, iris);

            FillCircle(px, 44, 76, 6, blush);
            FillCircle(px, 84, 76, 6, blush);

            DrawCapsule(px, 60, 83, 64, 86, 2, new Color(.8f, .35f, .4f), new Color(.8f, .35f, .4f));
            DrawCapsule(px, 64, 86, 68, 83, 2, new Color(.8f, .35f, .4f), new Color(.8f, .35f, .4f));

            // A small bow at the crown, tying into the composer's accent blue.
            FillEllipse(px, 58, 22, 6, 4, ribbon);
            FillEllipse(px, 70, 22, 6, 4, ribbon);
            FillCircle(px, 64, 22, 3, new Color(.2f, .35f, .7f));

            return Build(px);
        }

        private static void PaintEye(Color[] px, float cx, float cy, Color iris)
        {
            FillEllipse(px, cx, cy, 7, 9, iris);
            FillCircle(px, cx + 2, cy - 4, 2.5f, Color.white);
            DrawCapsule(px, cx - 7, cy - 9, cx + 7, cy - 9, 1.5f, new Color(.2f, .15f, .15f), new Color(.2f, .15f, .15f));
        }

        private static void Fill(Color[] px, Color color)
        {
            for (int i = 0; i < px.Length; i++) px[i] = color;
        }

        private static void SetPixelSafe(Color[] px, int x, int y, Color c)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size) return;
            // Every coordinate above is authored top-down (y=0 at the hat, growing toward the feet), but
            // Texture2D's own pixel buffer is bottom-up (row 0 = the bottom row) - flip here, the one place
            // that actually writes a pixel, rather than inverting every y used to describe a shape.
            int flippedY = Size - 1 - y;
            px[flippedY * Size + x] = c;
        }

        private static void FillCircle(Color[] px, float cx, float cy, float r, Color c)
        {
            FillEllipse(px, cx, cy, r, r, c);
        }

        private static void FillEllipse(Color[] px, float cx, float cy, float rx, float ry, Color c)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - rx));
            int maxX = Mathf.Min(Size - 1, Mathf.CeilToInt(cx + rx));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - ry));
            int maxY = Mathf.Min(Size - 1, Mathf.CeilToInt(cy + ry));
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = (x + .5f - cx) / rx, dy = (y + .5f - cy) / ry;
                    if (dx * dx + dy * dy <= 1f) SetPixelSafe(px, x, y, c);
                }
            }
        }

        private static void FillCircleOutlined(Color[] px, float cx, float cy, float r, Color fill, Color outline, float stroke)
        {
            FillEllipseOutlined(px, cx, cy, r, r, fill, outline, stroke);
        }

        private static void FillEllipseOutlined(Color[] px, float cx, float cy, float rx, float ry, Color fill, Color outline, float stroke)
        {
            FillEllipse(px, cx, cy, rx + stroke, ry + stroke, outline);
            FillEllipse(px, cx, cy, rx, ry, fill);
        }

        /// <summary>A straight stroke with round ends and a matching outline, stamped out of circles along
        /// the segment - cheap, but good enough at this resolution for a limb or a line-shaped mouth.</summary>
        private static void DrawCapsule(Color[] px, float ax, float ay, float bx, float by, float radius, Color fill, Color outline)
        {
            float len = Vector2.Distance(new Vector2(ax, ay), new Vector2(bx, by));
            int steps = Mathf.Max(1, Mathf.CeilToInt(len));
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float x = Mathf.Lerp(ax, bx, t), y = Mathf.Lerp(ay, by, t);
                FillCircle(px, x, y, radius + 1.5f, outline);
            }
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float x = Mathf.Lerp(ax, bx, t), y = Mathf.Lerp(ay, by, t);
                FillCircle(px, x, y, radius, fill);
            }
        }

        private static Texture2D Build(Color[] px)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static Sprite ToSprite(Texture2D tex) =>
            Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(.5f, .5f));
    }
}
