using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MedievalWarSim.Core.Enums;
using MedievalWarSim.Game;

namespace MedievalWarSim.Screens;

public partial class GameScreen
{
    private static int UnitTypeToSides(UnitType type) => type switch
    {
        UnitType.Infantry => 4,
        UnitType.Archer   => 3,
        UnitType.Cavalry  => 5,
        UnitType.Ballista => 8,
        UnitType.Medic    => 6,
        _ => 4
    };

    public void Update(GameTime gameTime)
    {
        KeyboardState currentKey   = Keyboard.GetState();
        MouseState    currentMouse = Mouse.GetState();

        if (currentKey.IsKeyDown(Keys.F12) && _prevKeyboard.IsKeyUp(Keys.F12))
            _console.Toggle();

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _cameraController.Update(gameTime, currentMouse);

        while (true)
        {
            string? cmd = _console.ReadCommand();
            if (cmd == null) break;
            _console.ExecuteCommand(cmd);
        }

        ProcessMouseInput(currentMouse);

        // ---- Spatial grid: snapshot positions before movement ----
        _spatialGrid.Clear();
        for (int i = 0; i < _entityManager.HighWaterMark; i++)
        {
            if (!_entityManager.IsAlive(i)) continue;
            var p = _entityManager.GetPosition(i);
            _spatialGrid.Insert(i, p.X, p.Y);
        }

        _tick++;
        for (int i = 0; i < _entityManager.HighWaterMark; i++)
        {
            if (!_entityManager.IsAlive(i)) continue;
            ref var move = ref _entityManager.GetMove(i);
            if (!move.IsMoving) continue;

            ref var pos = ref _entityManager.GetPosition(i);
            float radius = GetUnitRadius(_entityManager.GetUnitType(i).Type);
            var (sx, sy) = _camera.WorldToScreen(pos.X, pos.Y);
            float sr = radius * _camera.Zoom;
            if (sx + sr < -FarMargin || sx - sr > _viewport.Width + FarMargin ||
                sy + sr < -FarMargin || sy - sr > _viewport.Height + FarMargin)
            {
                if (_tick % FarUpdateInterval != 0)
                    continue;
            }

            float dx = move.TargetX - pos.X;
            float dy = move.TargetY - pos.Y;
            float dist = MathF.Sqrt(dx * dx + dy * dy);

            if (dist > 0.001f)
                move.FacingAngle = MathF.Atan2(dy, dx) + MathF.PI / 2f;

            if (dist < 1f)
            {
                move.IsMoving = false;
                continue;
            }

            // ---- Compute desired velocity ----
            float step = move.Speed * dt;
            if (step >= dist) step = dist;
            float vx = dx / dist * step;
            float vy = dy / dist * step;

            // ---- Sliding collision ----
            float queryR = radius + 50f;
            _nearbyBuffer.Clear();
            _spatialGrid.Query(pos.X, pos.Y, queryR, _nearbyBuffer);

            foreach (int j in _nearbyBuffer)
            {
                if (j == i || !_entityManager.IsAlive(j)) continue;
                ref var posJ = ref _entityManager.GetPosition(j);
                float rJ = GetUnitRadius(_entityManager.GetUnitType(j).Type);
                float minDist = radius + rJ + 3f;

                float rdx = posJ.X - pos.X;
                float rdy = posJ.Y - pos.Y;
                float rDistSq = rdx * rdx + rdy * rdy;
                if (rDistSq < 0.0001f || rDistSq >= minDist * minDist) continue;

                float rDist = MathF.Sqrt(rDistSq);
                float nx = rdx / rDist;
                float ny = rdy / rDist;

                // Separate overlapping positions
                float overlap = minDist - rDist;
                pos.X -= nx * overlap;
                pos.Y -= ny * overlap;

                // Remove velocity component toward the other unit (sliding)
                float dot = vx * nx + vy * ny;
                if (dot > 0f)
                {
                    vx -= dot * nx;
                    vy -= dot * ny;
                }
            }

            // ---- Stuck detection (crowded arrival) ----
            float effectiveSpeed = MathF.Sqrt(vx * vx + vy * vy);
            if (effectiveSpeed < 0.5f && dist < radius * 4f)
            {
                move.IsMoving = false;
                continue;
            }

            pos.X += vx;
            pos.Y += vy;
        }

        // ---- Stationary separation (resolve overlaps for all entities) ----
        for (int i = 0; i < _entityManager.HighWaterMark; i++)
        {
            if (!_entityManager.IsAlive(i)) continue;
            ref var pos = ref _entityManager.GetPosition(i);
            float radius = GetUnitRadius(_entityManager.GetUnitType(i).Type);

            _nearbyBuffer.Clear();
            _spatialGrid.Query(pos.X, pos.Y, radius + 50f, _nearbyBuffer);

            foreach (int j in _nearbyBuffer)
            {
                if (j <= i || !_entityManager.IsAlive(j)) continue;
                ref var posJ = ref _entityManager.GetPosition(j);
                float rJ = GetUnitRadius(_entityManager.GetUnitType(j).Type);
                float minDist = radius + rJ + 3f;

                float rdx = posJ.X - pos.X;
                float rdy = posJ.Y - pos.Y;
                float rDistSq = rdx * rdx + rdy * rdy;
                if (rDistSq < 0.0001f || rDistSq >= minDist * minDist) continue;

                float rDist = MathF.Sqrt(rDistSq);
                float overlap = (minDist - rDist) * 0.5f;
                float nx = rdx / rDist;
                float ny = rdy / rDist;
                pos.X -= nx * overlap;
                pos.Y -= ny * overlap;
                posJ.X += nx * overlap;
                posJ.Y += ny * overlap;
            }
        }

        _frameCount++;
        _elapsedFpsTime += gameTime.ElapsedGameTime.TotalSeconds;
        if (_elapsedFpsTime >= 0.5)
        {
            _fps = (int)(_frameCount / _elapsedFpsTime);
            _frameCount = 0;
            _elapsedFpsTime = 0;
        }

        _prevKeyboard = currentKey;
        _prevMouse    = currentMouse;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        for (int i = 0; i < _entityManager.HighWaterMark; i++)
        {
            if (!_entityManager.IsAlive(i)) continue;
            var    pos      = _entityManager.GetPosition(i);
            var   (sx, sy) = _camera.WorldToScreen(pos.X, pos.Y);
            var    type     = _entityManager.GetUnitType(i).Type;
            float  radius   = GetUnitRadius(type);
            float  sr       = radius * _camera.Zoom;
            if (sx + sr < -DrawMargin || sx - sr > _viewport.Width + DrawMargin ||
                sy + sr < -DrawMargin || sy - sr > _viewport.Height + DrawMargin)
                continue;

            int    sides       = UnitTypeToSides(type);
            ref var move       = ref _entityManager.GetMove(i);
            float  rotation    = move.FacingAngle;
            Color? borderColor = _selectedUnitIds.Contains(i) ? Color.Blue : null;
            _shapeRenderer.DrawShape(spriteBatch, sx, sy, sr, sides, rotation, borderColor);

            var hp = _entityManager.GetHealth(i);
            if (hp.CurrentHP < hp.MaxHP && sr > 4f)
            {
                float barW = sr * 2f * 0.85f;
                float barH = 3f;
                float barX = sx - barW / 2f;
                float barY = sy - sr - barH - 2f;
                float ratio = hp.CurrentHP / hp.MaxHP;
                Color barColor = ratio > 0.6f ? Color.Lime : ratio > 0.3f ? Color.Yellow : Color.Red;
                _shapeRenderer.DrawRectangle(spriteBatch, barX, barY, barW, barH, new Color(30, 30, 30, 180), Color.White * 0.4f, 0.5f);
                _shapeRenderer.DrawRectangle(spriteBatch, barX, barY, barW * ratio, barH, barColor, Color.Transparent, 0f);
            }
        }

        if (_isDragging)
        {
            int x1 = _dragStartX, y1 = _dragStartY;
            int x2 = _dragEndX,   y2 = _dragEndY;
            float rx = Math.Min(x1, x2);
            float ry = Math.Min(y1, y2);
            float rw = Math.Abs(x2 - x1);
            float rh = Math.Abs(y2 - y1);
            if (rw > 2 || rh > 2)
                _shapeRenderer.DrawRectangle(spriteBatch, rx, ry, rw, rh, new Color(0, 120, 0, 60), Color.LimeGreen, 1.5f);
        }

        string fpsText = $"FPS: {_fps}";
        Vector2 fpsSize = _font.MeasureString(fpsText);
        spriteBatch.DrawString(_font, fpsText, new Vector2(_viewport.Width - fpsSize.X - 8, 4), Color.Lime);

        string zoomText = $"Zoom: {_camera.Zoom:F2}x";
        Vector2 zoomSize = _font.MeasureString(zoomText);
        spriteBatch.DrawString(_font, zoomText, new Vector2(_viewport.Width - zoomSize.X - 8, _viewport.Height - zoomSize.Y - 4), Color.White);
    }

    private void PrintUnitInfo(int id)
    {
        var pos  = _entityManager.GetPosition(id);
        var type = _entityManager.GetUnitType(id);
        var move = _entityManager.GetMove(id);
        var hp   = _entityManager.GetHealth(id);
        System.Console.WriteLine($"Unit {id}:");
        System.Console.WriteLine($"  Type:     {type.Type}");
        System.Console.Write($"  Position: ({pos.X:F1};{pos.Y:F1})");
        if (move.IsMoving)
            System.Console.Write($" -> ({move.TargetX:F1};{move.TargetY:F1})");
        System.Console.WriteLine();
        System.Console.WriteLine($"  HP:       {hp.CurrentHP:F1}/{hp.MaxHP:F1}");
        System.Console.WriteLine($"  Speed:    {move.Speed:F1}");
        System.Console.WriteLine($"  Selected: {_selectedUnitIds.Contains(id)}");
        System.Console.WriteLine($"  Moving:   {move.IsMoving}");
    }
}
