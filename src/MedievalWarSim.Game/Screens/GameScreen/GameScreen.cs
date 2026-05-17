using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MedievalWarSim.Core;
using MedievalWarSim.Core.Components;
using MedievalWarSim.Core.Data;
using MedievalWarSim.Core.DataStructures;
using MedievalWarSim.Core.Enums;
using MedievalWarSim.Core.Managers;
using MedievalWarSim.Game;
using MedievalWarSim.Rendering.Shapes;
using MedievalWarSim.UI.Console;

namespace MedievalWarSim.Screens;

public partial class GameScreen : IDisposable
{
    private readonly Camera _camera = new();
    private readonly CameraController _cameraController;
    private readonly EntityManager _entityManager;
    private readonly ShapeRenderer _shapeRenderer;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly DevConsole _console;
    private readonly SpatialGrid _spatialGrid = new();
    private readonly List<int> _nearbyBuffer = new();
    private KeyboardState _prevKeyboard;
    private MouseState _prevMouse;
    private readonly HashSet<int> _selectedUnitIds = new();
    private Viewport _viewport;
    private bool _isDragging;
    private int _dragStartX, _dragStartY;
    private int _dragEndX, _dragEndY;
    private readonly SpriteFont _font;
    private int _frameCount;
    private double _elapsedFpsTime;
    private int _fps;
    private const float DrawMargin = 200f;
    private const float FarMargin = 400f;
    private const int FarUpdateInterval = 5;
    private int _tick;

    private enum VisionMode { None, ShowSingle, ShowAll }
    private VisionMode _visionMode;
    private int _visionUnitId = -1;

    private RenderTarget2D? _rtVision;  // corrente: bianco=visibile, nero=no
    private RenderTarget2D? _rtExplored; // permanente: grigio=esplorato, accumulato
    private RenderTarget2D? _rtFog;      // combinato per multiply pass
    private int _rtW, _rtH;
    private static readonly BlendState FogBlend = new()
    {
        ColorBlendFunction = BlendFunction.Add,
        ColorSourceBlend = Blend.Zero,
        ColorDestinationBlend = Blend.SourceColor,
        AlphaBlendFunction = BlendFunction.Add,
        AlphaSourceBlend = Blend.Zero,
        AlphaDestinationBlend = Blend.SourceAlpha
    };

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    private static bool IsCtrlHeld()
        => (GetAsyncKeyState(0xA2) & 0x8000) != 0 ||
           (GetAsyncKeyState(0xA3) & 0x8000) != 0;

    private readonly IntPtr _gameWindowHandle;

    private bool IsGameFocused() => GetForegroundWindow() == _gameWindowHandle;

    private static float GetUnitRadius(UnitType type) => UnitStats.GetBaseRadius(type);

    public GameScreen(GraphicsDevice graphicsDevice, SpriteFont font)
    {
        _font = font;
        _gameWindowHandle = FindWindow(null, "MedievalWarSim");
        _cameraController = new CameraController(_camera);
        _entityManager = new EntityManager();
        _shapeRenderer = new ShapeRenderer(graphicsDevice);
        _graphicsDevice = graphicsDevice;
        _console = new DevConsole();
        _viewport = graphicsDevice.Viewport;

        _entityManager.Create();
        _entityManager.GetPosition(0) = new PositionComponent
        {
            X = _viewport.Width / 2f,
            Y = _viewport.Height / 2f
        };
        var rt = (UnitType)Random.Shared.Next(5);
        _entityManager.GetUnitType(0) = new UnitTypeComponent { Type = rt };
        _entityManager.GetMove(0).Speed = UnitStats.RollSpeed(rt);
        _entityManager.GetVision(0).SightRange = UnitStats.RollSightRange(rt);
        float hp = UnitStats.RollHP(rt);
        _entityManager.GetHealth(0) = new HealthComponent { MaxHP = hp, CurrentHP = hp };

        RegisterCommands();
    }

    public void Dispose()
    {
        _shapeRenderer.Dispose();
        _rtVision?.Dispose();
        _rtExplored?.Dispose();
        _rtFog?.Dispose();
    }
}
