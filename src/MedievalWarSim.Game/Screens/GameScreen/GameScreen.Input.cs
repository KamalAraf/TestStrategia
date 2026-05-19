using Microsoft.Xna.Framework.Input;

namespace MedievalWarSim.Screens;

public partial class GameScreen
{
    private void ProcessMouseInput(MouseState currentMouse, bool isGameFocused)
    {
        if (isGameFocused)
        {
            if (currentMouse.LeftButton == ButtonState.Pressed &&
                _prevMouse.LeftButton   == ButtonState.Released)
            {
                _isDragging = true;
                _dragStartX = currentMouse.X;
                _dragStartY = currentMouse.Y;
                _dragEndX   = currentMouse.X;
                _dragEndY   = currentMouse.Y;
                if (!IsCtrlHeld())
                    _selectedUnitIds.Clear();
            }
        }

        if (_isDragging && currentMouse.LeftButton == ButtonState.Pressed)
        {
            _dragEndX = currentMouse.X;
            _dragEndY = currentMouse.Y;
        }

        if (_isDragging && currentMouse.LeftButton == ButtonState.Released &&
            _prevMouse.LeftButton   == ButtonState.Pressed)
        {
            if (isGameFocused)
            {
                int sdx = _dragEndX - _dragStartX;
                int sdy = _dragEndY - _dragStartY;

                var (wx, wy) = _camera.ScreenToWorld(_dragEndX, _dragEndY);

                if (sdx * sdx + sdy * sdy < 25)
                {
                    HandleClick(wx, wy, IsCtrlHeld());
                }
                else
                {
                    var (wsx, wsy) = _camera.ScreenToWorld(Math.Min(_dragStartX, _dragEndX), Math.Min(_dragStartY, _dragEndY));
                    var (wex, wey) = _camera.ScreenToWorld(Math.Max(_dragStartX, _dragEndX), Math.Max(_dragStartY, _dragEndY));

                    if (!IsCtrlHeld())
                        _selectedUnitIds.Clear();

                    foreach (int i in _entityManager.ActiveEntities)
                    {
                        var pos = _entityManager.GetPosition(i);
                        if (pos.X >= wsx && pos.X <= wex && pos.Y >= wsy && pos.Y <= wey)
                            _selectedUnitIds.Add(i);
                    }
                }
            }

            _isDragging = false;
        }

        if (isGameFocused)
        {
            bool rightJustPressed = currentMouse.RightButton == ButtonState.Pressed &&
                                    _prevMouse.RightButton == ButtonState.Released;
            if (rightJustPressed && _selectedUnitIds.Count > 0)
            {
                var (wx, wy) = _camera.ScreenToWorld(currentMouse.X, currentMouse.Y);
                foreach (int id in _selectedUnitIds)
                {
                    ref var move = ref _entityManager.GetMove(id);
                    move.TargetX = wx;
                    move.TargetY = wy;
                    move.IsMoving = true;
                    move.StuckTimer = 0f;
                    move.DistCheckTimer = 0f;
                    move.PrevDist = 0f;
                }
            }
        }
    }

    private void HandleClick(float mouseX, float mouseY, bool addToSelection)
    {
        int? clickedUnit = null;
        foreach (int i in _entityManager.ActiveEntities)
        {
            var   pos    = _entityManager.GetPosition(i);
            var   type   = _entityManager.GetUnitType(i).Type;
            float r      = GetUnitRadius(type);
            float dx     = mouseX - pos.X;
            float dy     = mouseY - pos.Y;
            if (dx * dx + dy * dy <= r * r)
            {
                clickedUnit = i;
                break;
            }
        }

        if (clickedUnit.HasValue)
        {
            if (addToSelection)
            {
                if (!_selectedUnitIds.Add(clickedUnit.Value))
                    _selectedUnitIds.Remove(clickedUnit.Value);
            }
            else
            {
                _selectedUnitIds.Clear();
                _selectedUnitIds.Add(clickedUnit.Value);
            }
        }
        else if (!addToSelection)
        {
            _selectedUnitIds.Clear();
        }
    }
}
