using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MedievalWarSim.Core;

namespace MedievalWarSim.Game;

public class CameraController
{
    private readonly Camera _camera;
    private MouseState _prevMouse;

    public CameraController(Camera camera)
    {
        _camera = camera;
    }

    public void Update(GameTime gameTime, MouseState currentMouse)
    {
        KeyboardState currentKey = Keyboard.GetState();
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        float panDx = 0, panDy = 0;
        if (currentKey.IsKeyDown(Keys.W) || currentKey.IsKeyDown(Keys.Up))    panDy -= 1;
        if (currentKey.IsKeyDown(Keys.S) || currentKey.IsKeyDown(Keys.Down))  panDy += 1;
        if (currentKey.IsKeyDown(Keys.A) || currentKey.IsKeyDown(Keys.Left))  panDx -= 1;
        if (currentKey.IsKeyDown(Keys.D) || currentKey.IsKeyDown(Keys.Right)) panDx += 1;
        if (panDx != 0 || panDy != 0)
        {
            float len = MathF.Sqrt(panDx * panDx + panDy * panDy);
            _camera.X += panDx / len * Camera.PanSpeed * dt / _camera.Zoom;
            _camera.Y += panDy / len * Camera.PanSpeed * dt / _camera.Zoom;
        }

        int scrollDelta = currentMouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
        if (scrollDelta != 0)
        {
            float oldZoom = _camera.Zoom;
            float newZoom = Math.Clamp(oldZoom * (scrollDelta > 0 ? 1.1f : 0.9f), Camera.MinZoom, Camera.MaxZoom);

            float worldX = currentMouse.X / oldZoom + _camera.X;
            float worldY = currentMouse.Y / oldZoom + _camera.Y;

            _camera.Zoom = newZoom;
            _camera.X = worldX - currentMouse.X / newZoom;
            _camera.Y = worldY - currentMouse.Y / newZoom;
        }

        if (currentMouse.MiddleButton == ButtonState.Pressed &&
            _prevMouse.MiddleButton   == ButtonState.Pressed)
        {
            _camera.X -= (currentMouse.X - _prevMouse.X) / _camera.Zoom;
            _camera.Y -= (currentMouse.Y - _prevMouse.Y) / _camera.Zoom;
        }

        _prevMouse = currentMouse;
    }
}
