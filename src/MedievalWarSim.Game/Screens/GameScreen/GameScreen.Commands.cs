using MedievalWarSim.Core;
using MedievalWarSim.Core.Components;
using MedievalWarSim.Core.Data;
using MedievalWarSim.Core.Enums;

namespace MedievalWarSim.Screens;

public partial class GameScreen
{
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
                System.Console.WriteLine("  Properties: type, selected, speed");
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
                    _entityManager.GetMove(id).Speed = UnitStats.RollSpeed(newType);
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

                case "speed":
                    if (args.Length < 3)
                    {
                        System.Console.WriteLine("Usage: set <id> speed <value|default>");
                        return;
                    }
                    if (args[2].ToLowerInvariant() == "default")
                    {
                        var unitType = _entityManager.GetUnitType(id).Type;
                        _entityManager.GetMove(id).Speed = UnitStats.RollSpeed(unitType);
                        System.Console.WriteLine($"Unit {id} speed reset to default ({_entityManager.GetMove(id).Speed:F1}).");
                    }
                    else if (float.TryParse(args[2], out float speedVal))
                    {
                        _entityManager.GetMove(id).Speed = speedVal;
                        System.Console.WriteLine($"Unit {id} speed set to {speedVal}.");
                    }
                    else
                    {
                        System.Console.WriteLine("Usage: set <id> speed <value|default>");
                    }
                    break;

                default:
                    System.Console.WriteLine($"Unknown property: {property}. Available: type, selected, speed");
                    break;
            }
        });

        _console.RegisterCommand("info", args =>
        {
            if (args.Length == 0)
            {
                System.Console.WriteLine("Usage: info <id> | info selected");
                return;
            }

            if (args[0] == "selected")
            {
                if (_selectedUnitIds.Count == 0)
                {
                    System.Console.WriteLine("No units selected.");
                }
                else if (_selectedUnitIds.Count == 1)
                {
                    PrintUnitInfo(_selectedUnitIds.First());
                }
                else
                {
                    System.Console.WriteLine($"Selected units ({_selectedUnitIds.Count}):");
                    foreach (int sid in _selectedUnitIds.Order())
                    {
                        var pos = _entityManager.GetPosition(sid);
                        System.Console.WriteLine($"  {sid} at ({pos.X:F1};{pos.Y:F1})");
                    }
                }
                return;
            }

            if (!int.TryParse(args[0], out int id))
            {
                System.Console.WriteLine("Usage: info <id> | info selected");
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
                System.Console.WriteLine($"  {id} at ({pos.X:F1};{pos.Y:F1})");
            }
        });

        _console.RegisterCommand("create", args =>
        {
            if (args.Length == 0)
            {
                System.Console.WriteLine("Usage: create random [count] | create <type> <x> <y>");
                return;
            }

            if (args[0] == "random")
            {
                int count = 1;
                if (args.Length >= 2 && (!int.TryParse(args[1], out count) || count < 1))
                {
                    System.Console.WriteLine("Invalid count. Usage: create random [count]");
                    return;
                }

                int created = 0;
                for (int n = 0; n < count; n++)
                {
                    float vw = _viewport.Width / _camera.Zoom;
                    float vh = _viewport.Height / _camera.Zoom;
                    float rx = _camera.X + Random.Shared.NextSingle() * vw;
                    float ry = _camera.Y + Random.Shared.NextSingle() * vh;

                    int id = _entityManager.Create();
                    if (id < 0) break;
                    _entityManager.GetPosition(id) = new PositionComponent { X = rx, Y = ry };
                    var rt = (UnitType)Random.Shared.Next(5);
                    _entityManager.GetUnitType(id) = new UnitTypeComponent { Type = rt };
                    _entityManager.GetMove(id).Speed = UnitStats.RollSpeed(rt);
                    created++;
                }

                System.Console.WriteLine($"Created {created} unit(s).");
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
                System.Console.WriteLine("Usage: create random [count] | create <type> <x> <y>");
                return;
            }

            if (!float.TryParse(args[1], out float cx) || !float.TryParse(args[2], out float cy))
            {
                System.Console.WriteLine("Invalid coordinates.");
                return;
            }

            int newId = _entityManager.Create();
            if (newId < 0)
            {
                System.Console.WriteLine("ERROR: entity limit reached (100000 max).");
                return;
            }
            _entityManager.GetPosition(newId) = new PositionComponent { X = cx, Y = cy };
            _entityManager.GetUnitType(newId) = new UnitTypeComponent { Type = parsedType };
            _entityManager.GetMove(newId).Speed = UnitStats.RollSpeed(parsedType);
            System.Console.WriteLine($"Created {parsedType} unit {newId} at ({cx:F0}, {cy:F0}).");
        });

        _console.RegisterCommand("move", args =>
        {
            if (args.Length == 0)
            {
                System.Console.WriteLine("Usage: move <id> <x> <y> | move selected <x> <y> | move random");
                return;
            }

            if (args[0] == "random")
            {
                if (_selectedUnitIds.Count == 0)
                {
                    System.Console.WriteLine("No units selected.");
                    return;
                }
                foreach (int id in _selectedUnitIds)
                {
                    ref var move = ref _entityManager.GetMove(id);
                    float vw = _viewport.Width / _camera.Zoom;
                    float vh = _viewport.Height / _camera.Zoom;
                    move.TargetX = _camera.X + Random.Shared.NextSingle() * vw;
                    move.TargetY = _camera.Y + Random.Shared.NextSingle() * vh;
                    move.IsMoving = true;
                }
                System.Console.WriteLine($"Moving {_selectedUnitIds.Count} unit(s) to random positions.");
                return;
            }

            if (args[0] == "selected")
            {
                if (args.Length < 3)
                {
                    System.Console.WriteLine("Usage: move selected <x> <y>");
                    return;
                }
                if (_selectedUnitIds.Count == 0)
                {
                    System.Console.WriteLine("No units selected.");
                    return;
                }
                if (!float.TryParse(args[1], out float sx) || !float.TryParse(args[2], out float sy))
                {
                    System.Console.WriteLine("Invalid coordinates.");
                    return;
                }
                foreach (int id in _selectedUnitIds)
                {
                    ref var move = ref _entityManager.GetMove(id);
                    move.TargetX = sx;
                    move.TargetY = sy;
                    move.IsMoving = true;
                }
                System.Console.WriteLine($"Moving {_selectedUnitIds.Count} unit(s) to ({sx:F0}, {sy:F0}).");
                return;
            }

            if (args.Length < 3 || !int.TryParse(args[0], out int moveId))
            {
                System.Console.WriteLine("Usage: move <id> <x> <y> | move selected <x> <y> | move random");
                return;
            }

            if (!_entityManager.IsAlive(moveId))
            {
                System.Console.WriteLine($"Unit {moveId} does not exist.");
                return;
            }

            if (!float.TryParse(args[1], out float mx) || !float.TryParse(args[2], out float my))
            {
                System.Console.WriteLine("Invalid coordinates.");
                return;
            }

            ref var mv = ref _entityManager.GetMove(moveId);
            mv.TargetX = mx;
            mv.TargetY = my;
            mv.IsMoving = true;
            System.Console.WriteLine($"Unit {moveId} moving to ({mx:F0}, {my:F0}).");
        });

        _console.RegisterCommand("zoom", _ =>
        {
            System.Console.WriteLine($"Zoom: {_camera.Zoom:F2}x (min={Camera.MinZoom:F1}, max={Camera.MaxZoom:F1})");
        });
    }
}
