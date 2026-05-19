using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MedievalWarSim.Core.Enums;
using MedievalWarSim.Game;

namespace MedievalWarSim.Screens;

public partial class GameScreen
{
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

        bool isGameFocused = IsGameFocused();
        ProcessMouseInput(currentMouse, isGameFocused);

        // ---- Spatial grid: snapshot positions before movement ----
        _spatialGrid.Clear();
        foreach (int i in _entityManager.ActiveEntities)
        {
            var p = _entityManager.GetPosition(i);
            _spatialGrid.Insert(i, p.X, p.Y);
        }

        // ---- Stamina drain/recovery ----
        foreach (int i in _entityManager.ActiveEntities)
        {
            ref var move = ref _entityManager.GetMove(i);
            ref var stamina = ref _entityManager.GetStamina(i);

            if (move.IsMoving)
            {
                stamina.CurrentStamina = Math.Max(0f, stamina.CurrentStamina - stamina.DrainRate * dt);

                if (stamina.CurrentStamina < stamina.MaxStamina * 0.30f)
                {
                    float hpDrain = stamina.CurrentStamina < stamina.MaxStamina * 0.15f ? 2.0f : 0.5f;
                    ref var hp = ref _entityManager.GetHealth(i);
                    hp.CurrentHP -= hpDrain * dt;
                }
            }
            else
            {
                stamina.CurrentStamina = Math.Min(stamina.MaxStamina, stamina.CurrentStamina + stamina.RecoveryRate * dt);
            }
        }

        // ---- Death cleanup (HP <= 0 from stamina drain) ----
        var activeSnapshot = _entityManager.ActiveEntities;
        for (int i = activeSnapshot.Length - 1; i >= 0; i--)
        {
            int eid = activeSnapshot[i];
            if (_entityManager.GetHealth(eid).CurrentHP <= 0f)
            {
                _entityManager.GetHealth(eid).CurrentHP = 0f;
                _entityManager.Destroy(eid);
                _selectedUnitIds.Remove(eid);
            }
        }

        _tick++;
        foreach (int i in _entityManager.ActiveEntities)
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
                move.StuckTimer = 0f;
                continue;
            }

            // ---- Compute desired velocity ----
            var stamina = _entityManager.GetStamina(i);
            float staminaRatio = stamina.CurrentStamina / stamina.MaxStamina;
            float speedMult = staminaRatio >= 0.60f ? 1.0f :
                              staminaRatio >= 0.30f ? 0.7f :
                              staminaRatio >= 0.15f ? 0.3f :
                              0.25f;

            float step = move.Speed * speedMult * dt;
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
                if (rDistSq >= minDist * minDist) continue;

                float rDist, nx, ny;
                if (rDistSq < 0.0001f)
                {
                    float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
                    nx = MathF.Cos(angle);
                    ny = MathF.Sin(angle);
                    rDist = 0f;
                }
                else
                {
                    rDist = MathF.Sqrt(rDistSq);
                    nx = rdx / rDist;
                    ny = rdy / rDist;
                }

                // Separate overlapping positions
                float overlap = (rDist > 0f) ? (minDist - rDist) : minDist;
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
            move.DistCheckTimer += dt;
            if (move.DistCheckTimer >= 0.5f)
            {
                move.DistCheckTimer = 0f;
                if (dist > 1f)
                {
                    if (move.PrevDist <= 0f)
                        move.PrevDist = dist;
                    float progress = move.PrevDist - dist;
                    if (progress < 1f)
                        move.StuckTimer += 0.5f;
                    else
                        move.StuckTimer = 0f;
                }
                move.PrevDist = dist;
            }

            if (move.StuckTimer >= 1f && dist > 1f)
            {
                move.IsMoving = false;
                move.StuckTimer = 0f;
                move.DistCheckTimer = 0f;
                continue;
            }

            pos.X += vx;
            pos.Y += vy;
        }

        // ---- Stationary separation (resolve overlaps for all entities) ----
        foreach (int i in _entityManager.ActiveEntities)
        {
            ref var pos = ref _entityManager.GetPosition(i);
            float radius = GetUnitRadius(_entityManager.GetUnitType(i).Type);

            // Far culling: skip distant entities most frames
            var (sx, sy) = _camera.WorldToScreen(pos.X, pos.Y);
            float sr = radius * _camera.Zoom;
            if (sx + sr < -FarMargin || sx - sr > _viewport.Width + FarMargin ||
                sy + sr < -FarMargin || sy - sr > _viewport.Height + FarMargin)
            {
                if (_tick % FarUpdateInterval != 0)
                    continue;
            }

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
                if (rDistSq >= minDist * minDist) continue;

                float rDist, nx, ny;
                float overlap;
                if (rDistSq < 0.0001f)
                {
                    float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
                    nx = MathF.Cos(angle);
                    ny = MathF.Sin(angle);
                    overlap = minDist;
                }
                else
                {
                    rDist = MathF.Sqrt(rDistSq);
                    nx = rdx / rDist;
                    ny = rdy / rDist;
                    overlap = minDist - rDist;
                }

                bool movingI = _entityManager.GetMove(i).IsMoving;
                bool movingJ = _entityManager.GetMove(j).IsMoving;
                if (movingI == movingJ)
                {
                    pos.X -= nx * overlap * 0.5f;
                    pos.Y -= ny * overlap * 0.5f;
                    posJ.X += nx * overlap * 0.5f;
                    posJ.Y += ny * overlap * 0.5f;
                }
                else if (movingI)
                {
                    pos.X -= nx * overlap;
                    pos.Y -= ny * overlap;
                }
                else
                {
                    posJ.X += nx * overlap;
                    posJ.Y += ny * overlap;
                }
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
}
