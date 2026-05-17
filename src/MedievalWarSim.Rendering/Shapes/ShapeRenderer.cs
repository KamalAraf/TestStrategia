using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MedievalWarSim.Rendering.Shapes;

public class ShapeRenderer : IDisposable
{
    private readonly Texture2D _fillTexture;
    private readonly Texture2D _borderTexture;
    private readonly Texture2D _pixel;
    private const int TexRadius = 64;
    private const int TexDiameter = TexRadius * 2;
    private const float BorderPixels = 12f;

    public ShapeRenderer(GraphicsDevice graphicsDevice)
    {
        _fillTexture = CreateFillTexture(graphicsDevice);
        _borderTexture = CreateBorderTexture(graphicsDevice);
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

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

    public void DrawCircle(SpriteBatch spriteBatch, float x, float y, float radius, Color? borderColor)
    {
        float scale = radius / TexRadius;
        var origin = new Vector2(TexRadius, TexRadius);
        var pos = new Vector2(x, y);

        spriteBatch.Draw(_fillTexture, pos, null, Color.White, 0f, origin, scale, SpriteEffects.None, 0f);

        Color bColor = borderColor ?? Color.Black;
        spriteBatch.Draw(_borderTexture, pos, null, bColor, 0f, origin, scale, SpriteEffects.None, 0f);
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

    public void Dispose()
    {
        _fillTexture.Dispose();
        _borderTexture.Dispose();
        _pixel.Dispose();
    }
}
