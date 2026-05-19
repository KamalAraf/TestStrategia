using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MedievalWarSim.Core.Data;
using MedievalWarSim.Core.Enums;
using MedievalWarSim.Game.Data;

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

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_visionMode != VisionMode.None)
        {
            int w = _viewport.Width, h = _viewport.Height;

            // Ensure _rtFinal matches viewport
            if (_rtFinal == null || _rtW != w || _rtH != h)
            {
                _rtFinal?.Dispose();
                _rtFinal = new RenderTarget2D(_graphicsDevice, w, h);
                _rtW = w; _rtH = h;
            }

            // ---- A. Accumulate explored circles (CPU, deduplicated per unit) ----
            foreach (int i in _entityManager.ActiveEntities)
            {
                if (_visionMode == VisionMode.ShowSingle && i != _visionUnitId) continue;
                var pos = _entityManager.GetPosition(i);
                float sight = _entityManager.GetVision(i).SightRange;

                float dx = pos.X - _lastExploredX[i];
                float dy = pos.Y - _lastExploredY[i];
                if (dx * dx + dy * dy > MinExploreDist * MinExploreDist)
                {
                    long cellKey = (long)(int)MathF.Floor(pos.X / ExploredCellSize) << 32 |
                                   (uint)(int)MathF.Floor(pos.Y / ExploredCellSize);
                    if (_exploredCellKeys.Add(cellKey))
                        _exploredCircles.Add((pos.X, pos.Y, sight));
                    _lastExploredX[i] = pos.X;
                    _lastExploredY[i] = pos.Y;
                }
            }

            // ---- B. Build final fog mask on _rtFinal ----
            _graphicsDevice.SetRenderTarget(_rtFinal);
            _graphicsDevice.Clear(Color.Black);

            float invZ = 1f / _camera.Zoom;
            float viewLeft   = _camera.X;
            float viewRight  = _camera.X + w * invZ;
            float viewTop    = _camera.Y;
            float viewBottom = _camera.Y + h * invZ;
            float drawMarginWorld = DrawMargin * invZ;

            // Draw explored circles (grey) + vision circles (white) in one batch (same MaxBlend)
            spriteBatch.Begin(SpriteSortMode.Deferred, MaxBlend);
            foreach (var (wx, wy, radius) in _exploredCircles)
            {
                if (wx + radius < viewLeft  - drawMarginWorld ||
                    wx - radius > viewRight + drawMarginWorld ||
                    wy + radius < viewTop   - drawMarginWorld ||
                    wy - radius > viewBottom + drawMarginWorld) continue;
                float sx = (wx - _camera.X) * _camera.Zoom;
                float sy = (wy - _camera.Y) * _camera.Zoom;
                float sr = radius * _camera.Zoom;
                _shapeRenderer.DrawCircle(spriteBatch, sx, sy, sr, new Color(180, 180, 180), Color.Transparent);
            }
            foreach (int i in _entityManager.ActiveEntities)
            {
                if (_visionMode == VisionMode.ShowSingle && i != _visionUnitId) continue;
                var  pos   = _entityManager.GetPosition(i);
                var (sx, sy) = _camera.WorldToScreen(pos.X, pos.Y);
                float sight = _entityManager.GetVision(i).SightRange * _camera.Zoom;
                if (sx + sight < -DrawMargin || sx - sight > w + DrawMargin ||
                    sy + sight < -DrawMargin || sy - sight > h + DrawMargin) continue;
                _shapeRenderer.DrawCircle(spriteBatch, sx, sy, sight, Color.White, Color.Transparent);
            }
            spriteBatch.End();

            // ---- C. Draw units onto backbuffer ----
            _graphicsDevice.SetRenderTarget(null);
            _graphicsDevice.Clear(new Color(30, 30, 30));
            spriteBatch.Begin();
            foreach (int i in _entityManager.ActiveEntities)
            {
                var    pos      = _entityManager.GetPosition(i);
                var   (sx, sy) = _camera.WorldToScreen(pos.X, pos.Y);
                var    type     = _entityManager.GetUnitType(i).Type;
                float  radius   = UnitStats.GetBaseRadius(type);
                float  sr       = radius * _camera.Zoom;
                if (sx + sr < -DrawMargin || sx - sr > w + DrawMargin ||
                    sy + sr < -DrawMargin || sy - sr > h + DrawMargin) continue;
                int    sides    = UnitTypeToSides(type);
                ref var move    = ref _entityManager.GetMove(i);
                float  rotation = move.FacingAngle;
                Color? borderColor = _selectedUnitIds.Contains(i) ? Color.Blue : null;
                var stamina = _entityManager.GetStamina(i);
                float t = 1f - stamina.CurrentStamina / stamina.MaxStamina;
                Color baseColor = TeamColors.GetColor(_entityManager.GetTeam(i).Team);
                Color unitColor = _entityManager.IsDying(i)
                    ? new Color(100, 100, 100)
                    : Color.Lerp(baseColor, new Color(100, 100, 100), t);
                _shapeRenderer.DrawShape(spriteBatch, sx, sy, sr, sides, rotation, unitColor, borderColor);
                var hp = _entityManager.GetHealth(i);
                if (hp.CurrentHP < hp.MaxHP && sr > 4f)
                {
                    float barW = sr * 2f * 0.85f, barH = 3f;
                    float barX = sx - barW / 2f, barY = sy - sr - barH - 2f;
                    float ratio = hp.CurrentHP / hp.MaxHP;
                    Color barColor = ratio > 0.6f ? Color.Lime : ratio > 0.3f ? Color.Yellow : Color.Red;
                    _shapeRenderer.DrawRectangle(spriteBatch, barX, barY, barW, barH, new Color(30, 30, 30, 180), Color.White * 0.4f, 0.5f);
                    _shapeRenderer.DrawRectangle(spriteBatch, barX, barY, barW * ratio, barH, barColor, Color.Transparent, 0f);
                }
            }
            spriteBatch.End();

            // ---- D. Apply fog multiply: white=visible, grey=explored, black=unexplored ----
            spriteBatch.Begin(SpriteSortMode.Deferred, FogBlend);
            spriteBatch.Draw(_rtFinal, Vector2.Zero, Color.White);
            spriteBatch.End();

            // ---- E. UI overlay ----
            spriteBatch.Begin();
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
            spriteBatch.DrawString(_font, fpsText, new Vector2(w - fpsSize.X - 8, 4), Color.Lime);
            string zoomText = $"Zoom: {_camera.Zoom:F2}x";
            Vector2 zoomSize = _font.MeasureString(zoomText);
            spriteBatch.DrawString(_font, zoomText, new Vector2(w - zoomSize.X - 8, h - zoomSize.Y - 4), Color.White);
            spriteBatch.End();
        }
        else
        {
            // ---- No vision mode: normal draw ----
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            foreach (int i in _entityManager.ActiveEntities)
            {
                var    pos      = _entityManager.GetPosition(i);
                var   (sx, sy) = _camera.WorldToScreen(pos.X, pos.Y);
                var    type     = _entityManager.GetUnitType(i).Type;
                float  radius   = UnitStats.GetBaseRadius(type);
                float  sr       = radius * _camera.Zoom;
                if (sx + sr < -DrawMargin || sx - sr > _viewport.Width + DrawMargin ||
                    sy + sr < -DrawMargin || sy - sr > _viewport.Height + DrawMargin)
                    continue;

                int    sides       = UnitTypeToSides(type);
                ref var move       = ref _entityManager.GetMove(i);
                float  rotation    = move.FacingAngle;
                Color? borderColor = _selectedUnitIds.Contains(i) ? Color.Blue : null;
                var stamina = _entityManager.GetStamina(i);
                float t = 1f - stamina.CurrentStamina / stamina.MaxStamina;
                Color baseColor = TeamColors.GetColor(_entityManager.GetTeam(i).Team);
                Color unitColor = _entityManager.IsDying(i)
                    ? new Color(100, 100, 100)
                    : Color.Lerp(baseColor, new Color(100, 100, 100), t);
                _shapeRenderer.DrawShape(spriteBatch, sx, sy, sr, sides, rotation, unitColor, borderColor);

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
            spriteBatch.End();
        }
    }
}
