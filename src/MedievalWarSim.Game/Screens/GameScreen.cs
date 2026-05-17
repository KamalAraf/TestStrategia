using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MedievalWarSim.Core.Components;
using MedievalWarSim.Core.Enums;
using MedievalWarSim.Core.Managers;
using MedievalWarSim.Rendering.Shapes;
using MedievalWarSim.UI.Console;

namespace MedievalWarSim.Screens;

public class GameScreen : IDisposable
{
    private readonly EntityManager _entityManager;
    private readonly ShapeRenderer _shapeRenderer;
    private readonly DevConsole _console;
    private KeyboardState _prevKeyboard;
    private MouseState _prevMouse;
    private readonly HashSet<int> _selectedUnitIds = new();
    //private bool _showClick;  // uncomment with showclick command
    private IntPtr _gameWindowHandle;
    private Viewport _viewport;
    private bool _isDragging;
    private int _dragStartX, _dragStartY;
    private int _dragEndX, _dragEndY;

    private const float UnitRadius = 16f;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private static bool IsCtrlHeld()
        => (GetAsyncKeyState(0xA2) & 0x8000) != 0 ||
           (GetAsyncKeyState(0xA3) & 0x8000) != 0;

    private bool IsClickOnGameWindow()
    {
        if (_gameWindowHandle == IntPtr.Zero)
        {
            using var process = System.Diagnostics.Process.GetCurrentProcess();
            _gameWindowHandle = process.MainWindowHandle;
        }

        return _gameWindowHandle != IntPtr.Zero &&
               GetForegroundWindow() == _gameWindowHandle;
    }

    public GameScreen(GraphicsDevice graphicsDevice)
    {

        _entityManager = new EntityManager();
        _shapeRenderer = new ShapeRenderer(graphicsDevice);
        _console = new DevConsole();
        _viewport = graphicsDevice.Viewport;

        _entityManager.Create();
        _entityManager.GetPosition(0) = new PositionComponent
        {
            X = _viewport.Width / 2f,
            Y = _viewport.Height / 2f
        };
        _entityManager.GetUnitType(0) = new UnitTypeComponent { Type = UnitType.Infantry };

        RegisterCommands();
    }

    private void RegisterCommands()
    {
        _console.RegisterCommand("remove", args =>
        {
            if (args.Length == 0)
            {
                System.Console.WriteLine("Usage: remove <id> | all");
                return;
            }

            if (args[0] == "all")
            {
                int count = 0;
                for (int i = 0; i < _entityManager.HighWaterMark; i++)
                {
                    if (_entityManager.IsAlive(i))
                    {
                        _entityManager.Destroy(i);
                        _selectedUnitIds.Remove(i);
                        count++;
                    }
                }
                System.Console.WriteLine($"Removed {count} unit(s).");
                return;
            }

            if (int.TryParse(args[0], out int id))
            {
                if (_entityManager.IsAlive(id))
                {
                    _entityManager.Destroy(id);
                    _selectedUnitIds.Remove(id);
                    System.Console.WriteLine($"Removed unit {id}.");
                }
                else
                {
                    System.Console.WriteLine($"Unit {id} does not exist.");
                }
            }
            else
            {
                System.Console.WriteLine($"Invalid id: {args[0]}");
            }
        });

        _console.RegisterCommand("set", args =>
        {
            if (args.Length < 2)
            {
                System.Console.WriteLine("Usage: set <id|all> <property> <value>");
                System.Console.WriteLine("Properties: type, selected");
                return;
            }

            string idArg    = args[0].ToLowerInvariant();
            string property = args[1].ToLowerInvariant();

            if (idArg == "all" && property == "selected" && args.Length >= 3)
            {
                string selArg = args[2].ToLowerInvariant();
                if (selArg == "true" || selArg == "1")
                {
                    _selectedUnitIds.Clear();
                    for (int i = 0; i < _entityManager.HighWaterMark; i++)
                        if (_entityManager.IsAlive(i))
                            _selectedUnitIds.Add(i);
                    System.Console.WriteLine($"Selected all {_selectedUnitIds.Count} unit(s).");
                }
                else if (selArg == "false" || selArg == "0")
                {
                    _selectedUnitIds.Clear();
                    System.Console.WriteLine("Deselected all units.");
                }
                else
                {
                    System.Console.WriteLine("Usage: set all selected true|false");
                }
                return;
            }

            if (!int.TryParse(args[0], out int id))
            {
                System.Console.WriteLine($"Invalid id: {args[0]}");
                return;
            }

            if (!_entityManager.IsAlive(id))
            {
                System.Console.WriteLine($"Unit {id} does not exist.");
                return;
            }

            switch (property)
            {
                case "type":
                    if (args.Length < 3)
                    {
                        System.Console.WriteLine("Usage: set <id> type <typename|id>");
                        return;
                    }
                    UnitType newType;
                    if (int.TryParse(args[2], out int typeInt))
                    {
                        if (Enum.IsDefined(typeof(UnitType), typeInt))
                            newType = (UnitType)typeInt;
                        else
                        {
                            System.Console.WriteLine($"Invalid type value: {typeInt}. Valid: 0=Infantry, 1=Archer, 2=Cavalry, 3=Ballista, 4=Medic");
                            return;
                        }
                    }
                    else
                    {
                        if (!Enum.TryParse(args[2], true, out newType))
                        {
                            System.Console.WriteLine($"Invalid type name: {args[2]}. Valid: Infantry, Archer, Cavalry, Ballista, Medic");
                            return;
                        }
                    }
                    _entityManager.GetUnitType(id) = new UnitTypeComponent { Type = newType };
                    System.Console.WriteLine($"Unit {id} type set to {newType}.");
                    break;

                case "selected":
                    if (args.Length < 3)
                    {
                        System.Console.WriteLine("Usage: set <id> selected true/false");
                        return;
                    }
                    string selArg = args[2].ToLowerInvariant();
                    if (selArg == "true" || selArg == "1")
                    {
                        _selectedUnitIds.Add(id);
                        System.Console.WriteLine($"Unit {id} selected.");
                    }
                    else if (selArg == "false" || selArg == "0")
                    {
                        _selectedUnitIds.Remove(id);
                        System.Console.WriteLine($"Unit {id} deselected.");
                    }
                    else
                    {
                        System.Console.WriteLine("Usage: set <id> selected true/false");
                    }
                    break;

                default:
                    System.Console.WriteLine($"Unknown property: {property}. Available: type, selected");
                    break;
            }
        });

        _console.RegisterCommand("info", args =>
        {
            if (args.Length == 0 || !int.TryParse(args[0], out int id))
            {
                System.Console.WriteLine("Usage: info <id>");
                return;
            }
            if (!_entityManager.IsAlive(id))
            {
                System.Console.WriteLine($"Unit {id} does not exist.");
                return;
            }
            PrintUnitInfo(id);
        });

        _console.RegisterCommand("selected", _ =>
        {
            if (_selectedUnitIds.Count == 0)
            {
                System.Console.WriteLine("No units selected.");
                return;
            }
            System.Console.WriteLine($"Selected units ({_selectedUnitIds.Count}):");
            foreach (int id in _selectedUnitIds.Order())
            {
                var pos = _entityManager.GetPosition(id);
                System.Console.WriteLine($"  {id} at ({pos.X:F1}, {pos.Y:F1})");
            }
        });

        _console.RegisterCommand("create", args =>
        {
            if (args.Length == 0)
            {
                System.Console.WriteLine("Usage: create random | create <x> <y>");
                return;
            }

            float x, y;
            if (args[0] == "random")
            {
                x = Random.Shared.Next(50, _viewport.Width  - 50);
                y = Random.Shared.Next(50, _viewport.Height - 50);
            }
            else if (args.Length >= 2)
            {
                if (!float.TryParse(args[0], out x) || !float.TryParse(args[1], out y))
                {
                    System.Console.WriteLine("Invalid coordinates.");
                    return;
                }
            }
            else
            {
                System.Console.WriteLine("Usage: create random | create <x> <y>");
                return;
            }

            int id = _entityManager.Create();
            if (id < 0)
            {
                System.Console.WriteLine("ERROR: entity limit reached (2000 max).");
                return;
            }
            _entityManager.GetPosition(id) = new PositionComponent { X = x, Y = y };
            _entityManager.GetUnitType(id) = new UnitTypeComponent { Type = UnitType.Infantry };
            System.Console.WriteLine($"Created unit {id} at ({x:F0}, {y:F0}).");
        });

        // showclick — DEBUG ONLY: prints click coords & focus info
        // Uncomment by removing the // below and rebuilding.
        //_console.RegisterCommand("showclick", args =>
        //{
        //    if (args.Length == 0 || (args[0] != "true" && args[0] != "false"))
        //    {
        //        System.Console.WriteLine("Usage: showclick true|false");
        //        return;
        //    }
        //    _showClick = args[0] == "true";
        //    System.Console.WriteLine($"Click debug: {(_showClick ? "ON" : "OFF")}");
        //});
    }

    private void PrintUnitInfo(int id)
    {
        var pos  = _entityManager.GetPosition(id);
        var type = _entityManager.GetUnitType(id);
        System.Console.WriteLine($"Unit {id}:");
        System.Console.WriteLine($"  Type:     {type.Type}");
        System.Console.WriteLine($"  Position: ({pos.X:F1}, {pos.Y:F1})");
        System.Console.WriteLine($"  Selected: {_selectedUnitIds.Contains(id)}");
    }

    public void Update(GameTime gameTime)
    {
        KeyboardState currentKey   = Keyboard.GetState();
        MouseState    currentMouse = Mouse.GetState();

        if (currentKey.IsKeyDown(Keys.F12) && _prevKeyboard.IsKeyUp(Keys.F12))
            _console.Toggle();

        while (true)
        {
            string? cmd = _console.ReadCommand();
            if (cmd == null) break;
            _console.ExecuteCommand(cmd);
        }

        if (currentMouse.LeftButton == ButtonState.Pressed  &&
            _prevMouse.LeftButton   == ButtonState.Released)
        {
            _isDragging = IsClickOnGameWindow();
            if (_isDragging)
            {
                _dragStartX = currentMouse.X;
                _dragStartY = currentMouse.Y;
                _dragEndX   = currentMouse.X;
                _dragEndY   = currentMouse.Y;
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
            int dx = _dragEndX - _dragStartX;
            int dy = _dragEndY - _dragStartY;

            if (dx * dx + dy * dy < 25)
            {
                HandleClick(_dragEndX, _dragEndY, IsCtrlHeld());
            }
            else
            {
                int minX = Math.Min(_dragStartX, _dragEndX);
                int maxX = Math.Max(_dragStartX, _dragEndX);
                int minY = Math.Min(_dragStartY, _dragEndY);
                int maxY = Math.Max(_dragStartY, _dragEndY);

                if (!IsCtrlHeld())
                    _selectedUnitIds.Clear();

                for (int i = 0; i < _entityManager.HighWaterMark; i++)
                {
                    if (!_entityManager.IsAlive(i)) continue;
                    var pos = _entityManager.GetPosition(i);
                    if (pos.X >= minX && pos.X <= maxX && pos.Y >= minY && pos.Y <= maxY)
                        _selectedUnitIds.Add(i);
                }
            }

            _isDragging = false;
        }

        _prevKeyboard = currentKey;
        _prevMouse    = currentMouse;
    }

    private void HandleClick(int mouseX, int mouseY, bool addToSelection)
    {
        int? clickedUnit = null;
        for (int i = 0; i < _entityManager.HighWaterMark; i++)
        {
            if (!_entityManager.IsAlive(i)) continue;
            var   pos = _entityManager.GetPosition(i);
            float dx  = mouseX - pos.X;
            float dy  = mouseY - pos.Y;
            if (dx * dx + dy * dy <= UnitRadius * UnitRadius)
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

    public void Draw(SpriteBatch spriteBatch)
    {
        for (int i = 0; i < _entityManager.HighWaterMark; i++)
        {
            if (!_entityManager.IsAlive(i)) continue;
            var    pos         = _entityManager.GetPosition(i);
            Color? borderColor = _selectedUnitIds.Contains(i) ? Color.Blue : null;
            _shapeRenderer.DrawCircle(spriteBatch, pos.X, pos.Y, UnitRadius, borderColor);
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
    }

    public void Dispose()
    {
        _shapeRenderer.Dispose();
    }
}
