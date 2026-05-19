using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MedievalWarSim.Rendering.Shapes;

public class ShapeRenderer : IDisposable
{
    private readonly Texture2D _fillTexture;
    private readonly Texture2D _borderTexture;
    private readonly Dictionary<int, (Texture2D Fill, Texture2D Border)> _polyTextures = new();
    private readonly Texture2D _pixel;
    private const int TexRadius = 512;
    private const int TexDiameter = TexRadius * 2;
    private const float BorderPixels = 96f;

    private static readonly int[] PolySides = { 3, 4, 5, 6, 8 };

    public ShapeRenderer(GraphicsDevice graphicsDevice)
    {
        _fillTexture = CreateFillTexture(graphicsDevice);
        _borderTexture = CreateBorderTexture(graphicsDevice);
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        foreach (int sides in PolySides)
        {
            var fill = CreatePolygonFillTexture(graphicsDevice, sides);
            var border = CreatePolygonBorderTexture(graphicsDevice, sides);
            _polyTextures[sides] = (fill, border);
        }
    }

    public void DrawShape(SpriteBatch spriteBatch, float x, float y, float radius, int sides, float rotation, Color fillColor, Color? borderColor)
    {
        float scale = radius / TexRadius;
        var origin = new Vector2(TexRadius, TexRadius);
        var pos = new Vector2(x, y);

        if (_polyTextures.TryGetValue(sides, out var pair))
        {
            spriteBatch.Draw(pair.Fill, pos, null, fillColor, rotation, origin, scale, SpriteEffects.None, 0f);
            Color bColor = borderColor ?? Color.Black;
            spriteBatch.Draw(pair.Border, pos, null, bColor, rotation, origin, scale, SpriteEffects.None, 0f);
        }
        else
        {
            DrawCircle(spriteBatch, x, y, radius, fillColor, borderColor);
        }
    }

    public void DrawCircle(SpriteBatch spriteBatch, float x, float y, float radius, Color fillColor, Color? borderColor)
    {
        float scale = radius / TexRadius;
        var origin = new Vector2(TexRadius, TexRadius);
        var pos = new Vector2(x, y);

        spriteBatch.Draw(_fillTexture, pos, null, fillColor, 0f, origin, scale, SpriteEffects.None, 0f);

        Color bColor = borderColor ?? Color.Black;
        spriteBatch.Draw(_borderTexture, pos, null, bColor, 0f, origin, scale, SpriteEffects.None, 0f);
    }

    public void DrawCircleBorder(SpriteBatch spriteBatch, float cx, float cy, float radius, float thickness, Color color)
    {
        int segments = (int)(MathF.PI * radius / 3f);
        if (segments < 16) segments = 16;
        if (segments > 360) segments = 360;

        float angleStep = MathF.PI * 2f / segments;
        for (int i = 0; i < segments; i++)
        {
            float a1 = i * angleStep;
            float a2 = (i + 1) * angleStep;
            float x1 = cx + MathF.Cos(a1) * radius;
            float y1 = cy + MathF.Sin(a1) * radius;
            float x2 = cx + MathF.Cos(a2) * radius;
            float y2 = cy + MathF.Sin(a2) * radius;

            float dx = x2 - x1;
            float dy = y2 - y1;
            float len = MathF.Sqrt(dx * dx + dy * dy);
            if (len < 0.01f) continue;
            float angle = MathF.Atan2(dy, dx);

            spriteBatch.Draw(_pixel, new Vector2(x1, y1), null, color, angle,
                Vector2.Zero, new Vector2(len + thickness, thickness), SpriteEffects.None, 0f);
        }
    }

    public void DrawRectangle(SpriteBatch spriteBatch, float x, float y, float w, float h, Color fillColor, Color borderColor, float borderWidth = 1f)
    {
        if (w <= 0 || h <= 0) return;

        spriteBatch.Draw(_pixel, new Vector2(x, y), null, fillColor, 0f, Vector2.Zero, new Vector2(w, h), SpriteEffects.None, 0f);

        spriteBatch.Draw(_pixel, new Vector2(x, y), null, borderColor, 0f, Vector2.Zero, new Vector2(w, borderWidth), SpriteEffects.None, 0f);
        spriteBatch.Draw(_pixel, new Vector2(x, y + h - borderWidth), null, borderColor, 0f, Vector2.Zero, new Vector2(w, borderWidth), SpriteEffects.None, 0f);
        spriteBatch.Draw(_pixel, new Vector2(x, y), null, borderColor, 0f, Vector2.Zero, new Vector2(borderWidth, h), SpriteEffects.None, 0f);
        spriteBatch.Draw(_pixel, new Vector2(x + w - borderWidth, y), null, borderColor, 0f, Vector2.Zero, new Vector2(borderWidth, h), SpriteEffects.None, 0f);
    }

    // ---- Circle textures (existing) ----

    private static Texture2D CreateFillTexture(GraphicsDevice gd)
    {
        int dia = TexDiameter;
        int rad = TexRadius;
        float fillEdge = rad - BorderPixels;
        Color[] data = new Color[dia * dia];

        for (int y = 0; y < dia; y++)
        {
            for (int x = 0; x < dia; x++)
            {
                float dx = x - rad + 0.5f;
                float dy = y - rad + 0.5f;
                float dist = MathF.Sqrt(dx * dx + dy * dy);

                if (dist < fillEdge)
                {
                    data[y * dia + x] = Color.White;
                }
                else if (dist < fillEdge + 1.5f)
                {
                    float t = (dist - fillEdge) / 1.5f;
                    int a = (int)((1f - t) * 255);
                    data[y * dia + x] = new Color(255, 255, 255, a);
                }
                else
                {
                    data[y * dia + x] = Color.Transparent;
                }
            }
        }

        var tex = new Texture2D(gd, dia, dia);
        tex.SetData(data);
        return tex;
    }

    private static Texture2D CreateBorderTexture(GraphicsDevice gd)
    {
        int dia = TexDiameter;
        int rad = TexRadius;
        float borderInner = rad - BorderPixels;
        Color[] data = new Color[dia * dia];

        for (int y = 0; y < dia; y++)
        {
            for (int x = 0; x < dia; x++)
            {
                float dx = x - rad + 0.5f;
                float dy = y - rad + 0.5f;
                float dist = MathF.Sqrt(dx * dx + dy * dy);

                if (dist < borderInner || dist >= rad)
                {
                    data[y * dia + x] = Color.Transparent;
                }
                else
                {
                    float alpha = 1f;

                    if (dist < borderInner + 1.5f)
                    {
                        float t = (dist - borderInner) / 1.5f;
                        alpha = t;
                    }
                    else if (dist > rad - 1.5f)
                    {
                        float t = (rad - dist) / 1.5f;
                        alpha = t;
                    }

                    int a = (int)(alpha * 255);
                    data[y * dia + x] = new Color(255, 255, 255, a);
                }
            }
        }

        var tex = new Texture2D(gd, dia, dia);
        tex.SetData(data);
        return tex;
    }

    // ---- Polygon textures ----

    private static Texture2D CreatePolygonFillTexture(GraphicsDevice gd, int sides)
    {
        int dia = TexDiameter;
        int rad = TexRadius;
        float fillR = rad - BorderPixels;
        var verts = GenerateVertices(sides, fillR, rad, rad);
        Color[] data = new Color[dia * dia];

        for (int y = 0; y < dia; y++)
        {
            for (int x = 0; x < dia; x++)
            {
                if (!PointInPolygon(x, y, verts))
                {
                    data[y * dia + x] = Color.Transparent;
                    continue;
                }

                float minDist = MinDistToEdge(x, y, verts);
                if (minDist < 1.5f)
                {
                    int a = (int)((minDist / 1.5f) * 255);
                    data[y * dia + x] = new Color(255, 255, 255, a);
                }
                else
                {
                    data[y * dia + x] = Color.White;
                }
            }
        }

        var tex = new Texture2D(gd, dia, dia);
        tex.SetData(data);
        return tex;
    }

    private static Texture2D CreatePolygonBorderTexture(GraphicsDevice gd, int sides)
    {
        int dia = TexDiameter;
        int rad = TexRadius;
        float innerR = rad - BorderPixels;
        var innerVerts = GenerateVertices(sides, innerR, rad, rad);
        var outerVerts = GenerateVertices(sides, rad, rad, rad);
        Color[] data = new Color[dia * dia];

        for (int y = 0; y < dia; y++)
        {
            for (int x = 0; x < dia; x++)
            {
                bool insideOuter = PointInPolygon(x, y, outerVerts);
                bool insideInner = PointInPolygon(x, y, innerVerts);

                if (insideOuter && !insideInner)
                {
                    float distToInner = MinDistToEdge(x, y, innerVerts);
                    float distToOuter = MinDistToEdge(x, y, outerVerts);
                    float minDist = Math.Min(distToInner, distToOuter);

                    if (minDist < 1.5f)
                    {
                        int a = (int)((1f - minDist / 1.5f) * 255);
                        data[y * dia + x] = new Color(255, 255, 255, a);
                    }
                    else
                    {
                        data[y * dia + x] = Color.White;
                    }
                }
                else
                {
                    data[y * dia + x] = Color.Transparent;
                }
            }
        }

        var tex = new Texture2D(gd, dia, dia);
        tex.SetData(data);
        return tex;
    }

    private static Vector2[] GenerateVertices(int sides, float radius, float cx, float cy)
    {
        var verts = new Vector2[sides];
        float vertexAngleOffset = sides % 2 == 0 ? MathF.PI / sides : 0f;
        for (int i = 0; i < sides; i++)
        {
            float angle = -MathF.PI / 2f + vertexAngleOffset + i * (MathF.PI * 2f / sides);
            verts[i] = new Vector2(cx + MathF.Cos(angle) * radius, cy + MathF.Sin(angle) * radius);
        }
        return verts;
    }

    private static bool PointInPolygon(float px, float py, Vector2[] verts)
    {
        bool inside = false;
        int n = verts.Length;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            if ((verts[i].Y > py) != (verts[j].Y > py) &&
                px < (verts[j].X - verts[i].X) * (py - verts[i].Y) / (verts[j].Y - verts[i].Y) + verts[i].X)
                inside = !inside;
        }
        return inside;
    }

    private static float DistToSegment(float px, float py, Vector2 a, Vector2 b)
    {
        float dx = b.X - a.X;
        float dy = b.Y - a.Y;
        float lengthSq = dx * dx + dy * dy;
        if (lengthSq < 1e-10f)
            return MathF.Sqrt((px - a.X) * (px - a.X) + (py - a.Y) * (py - a.Y));
        float t = Math.Clamp(((px - a.X) * dx + (py - a.Y) * dy) / lengthSq, 0f, 1f);
        float cx = a.X + t * dx;
        float cy = a.Y + t * dy;
        return MathF.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
    }

    private static float MinDistToEdge(float px, float py, Vector2[] verts)
    {
        float minDist = float.MaxValue;
        int n = verts.Length;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            float d = DistToSegment(px, py, verts[j], verts[i]);
            if (d < minDist) minDist = d;
        }
        return minDist;
    }

    public void Dispose()
    {
        _fillTexture.Dispose();
        _borderTexture.Dispose();
        foreach (var pair in _polyTextures.Values)
        {
            pair.Fill.Dispose();
            pair.Border.Dispose();
        }
        _pixel.Dispose();
    }
}
