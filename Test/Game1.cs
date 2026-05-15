using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Test;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    Texture2D pixel;

    List<Vector2> positions = new();
    List<float> speeds = new();

    int unitCount = 100000;
    Random rng = new Random();

    SpriteFont font;

    float fps;
    int frameCount;
    float timer;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);

        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        Window.Title = "titolo";
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // pixel 1x1
        pixel = new Texture2D(GraphicsDevice, 1, 1);
        pixel.SetData(new[] { Color.White });

        // font (devi avere Font.spritefont nel Content)
        font = Content.Load<SpriteFont>("Font");

        // inizializzazione unità
        for (int i = 0; i < unitCount; i++)
        {
            positions.Add(new Vector2(
                rng.Next(0, _graphics.PreferredBackBufferWidth),
                rng.Next(0, _graphics.PreferredBackBufferHeight)
            ));

            speeds.Add((float)(rng.NextDouble() * 3.0 + 0.2)); // mai 0
        }
    }

    protected override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // FPS counter
        timer += dt;
        frameCount++;

        if (timer >= 1f)
        {
            fps = frameCount;
            frameCount = 0;
            timer = 0f;
        }

        // exit
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // mouse target
        var mouse = Mouse.GetState();
        Vector2 target = new Vector2(mouse.X, mouse.Y);

        // update units
        for (int i = 0; i < unitCount; i++)
        {
            Vector2 direction = target - positions[i];

            if (direction.Length() > 1f)
            {
                direction.Normalize();
                positions[i] += direction * speeds[i];
            }
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(45, 45, 48));

        _spriteBatch.Begin();

        // draw units
        for (int i = 0; i < unitCount; i++)
        {
            _spriteBatch.Draw(
                pixel,
                positions[i],
                null,
                Color.Red,
                0f,
                Vector2.Zero,
                4f,
                SpriteEffects.None,
                0f
            );
        }

        // FPS
        string text = $"FPS: {fps}";
        Vector2 size = font.MeasureString(text);

        _spriteBatch.DrawString(
            font,
            text,
            new Vector2(_graphics.PreferredBackBufferWidth - size.X - 10, 10),
            Color.White
        );

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}