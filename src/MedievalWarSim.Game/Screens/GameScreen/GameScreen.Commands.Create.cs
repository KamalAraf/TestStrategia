using MedievalWarSim.Core.Components;
using MedievalWarSim.Core.Data;
using MedievalWarSim.Core.Enums;

namespace MedievalWarSim.Screens;

public partial class GameScreen
{
    private void RegisterCreateCommand()
    {
        _console.RegisterCommand("create", args =>
        {
            if (args.Length == 0)
            {
                System.Console.WriteLine("Usage: create random [count] [team] | create <type> <x> <y> [team]");
                return;
            }

            Team defaultTeam = Team.White;

            if (args[0] == "random")
            {
                int count = 1;
                int argIdx = 1;
                if (args.Length >= 2 && int.TryParse(args[1], out count) && count >= 1)
                    argIdx = 2;
                Team team = args.Length > argIdx ? ParseTeamArg(args[argIdx]) : defaultTeam;
                if ((int)team < 0) return;

                int created = 0;
                for (int n = 0; n < count; n++)
                {
                    float vw = _viewport.Width / _camera.Zoom;
                    float vh = _viewport.Height / _camera.Zoom;
                    float rx = _camera.X + Random.Shared.NextSingle() * vw;
                    float ry = _camera.Y + Random.Shared.NextSingle() * vh;

                    int id = _entityManager.Create();
                    if (id < 0) break;
                    _lastExploredX[id] = -1000000f;
                    _lastExploredY[id] = -1000000f;
                    _entityManager.GetPosition(id) = new PositionComponent { X = rx, Y = ry };
                    var rt = (UnitType)Random.Shared.Next(5);
                    _entityManager.GetUnitType(id) = new UnitTypeComponent { Type = rt };
                    _entityManager.GetTeam(id) = new TeamComponent { Team = team };
                    _entityManager.GetMove(id).Speed = UnitStats.RollSpeed(rt);
                    _entityManager.GetVision(id).SightRange = UnitStats.RollSightRange(rt);
                    float rhp = UnitStats.RollHP(rt);
                    _entityManager.GetHealth(id) = new HealthComponent { MaxHP = rhp, CurrentHP = rhp };
                    float rst = UnitStats.RollMaxStamina(rt);
                    _entityManager.GetStamina(id) = new StaminaComponent { MaxStamina = rst, CurrentStamina = rst, DrainRate = 0.2f, RecoveryRate = 1.0f };
                    created++;
                }

                System.Console.WriteLine($"Created {created} unit(s) ({team}).");
                return;
            }

            UnitType parsedType;
            if (int.TryParse(args[0], out int typeInt))
            {
                if (!Enum.IsDefined(typeof(UnitType), typeInt))
                {
                    System.Console.WriteLine($"Invalid type id: {typeInt}. Valid: 0=Infantry, 1=Archer, 2=Cavalry, 3=Ballista, 4=Medic");
                    return;
                }
                parsedType = (UnitType)typeInt;
            }
            else if (!Enum.TryParse(args[0], true, out parsedType))
            {
                System.Console.WriteLine($"Unknown type: {args[0]}. Valid: Infantry, Archer, Cavalry, Ballista, Medic");
                return;
            }

            if (args.Length < 3)
            {
                System.Console.WriteLine("Usage: create random [count] [team] | create <type> <x> <y> [team]");
                return;
            }

            if (!float.TryParse(args[1], out float cx) || !float.TryParse(args[2], out float cy))
            {
                System.Console.WriteLine("Invalid coordinates.");
                return;
            }

            Team team2 = args.Length >= 4 ? ParseTeamArg(args[3]) : defaultTeam;
            if ((int)team2 < 0) return;

            int newId = _entityManager.Create();
            if (newId < 0)
            {
                System.Console.WriteLine("ERROR: entity limit reached (100000 max).");
                return;
            }
            _lastExploredX[newId] = -1000000f;
            _lastExploredY[newId] = -1000000f;
            _entityManager.GetPosition(newId) = new PositionComponent { X = cx, Y = cy };
            _entityManager.GetUnitType(newId) = new UnitTypeComponent { Type = parsedType };
            _entityManager.GetTeam(newId) = new TeamComponent { Team = team2 };
            _entityManager.GetMove(newId).Speed = UnitStats.RollSpeed(parsedType);
            _entityManager.GetVision(newId).SightRange = UnitStats.RollSightRange(parsedType);
            float hp2 = UnitStats.RollHP(parsedType);
            _entityManager.GetHealth(newId) = new HealthComponent { MaxHP = hp2, CurrentHP = hp2 };
            float st2 = UnitStats.RollMaxStamina(parsedType);
            _entityManager.GetStamina(newId) = new StaminaComponent { MaxStamina = st2, CurrentStamina = st2, DrainRate = 0.2f, RecoveryRate = 1.0f };
            System.Console.WriteLine($"Created {team2} {parsedType} unit {newId} at ({cx:F0}, {cy:F0}).");
        });
    }
}
