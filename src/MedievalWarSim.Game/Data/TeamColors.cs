using Microsoft.Xna.Framework;
using MedievalWarSim.Core.Enums;

namespace MedievalWarSim.Game.Data;

public static class TeamColors
{
    private static readonly Color[] _colors =
    [
        Color.White,           // White
        new Color(200, 60, 60), // Red
        new Color(60, 100, 220), // Blue
        new Color(60, 180, 60), // Green
        new Color(220, 200, 40), // Yellow
    ];

    public static Color GetColor(Team team) => _colors[(int)team];
}
