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
                var active = _entityManager.ActiveEntities;
                int count = active.Length;
                for (int i = count - 1; i >= 0; i--)
                {
                    _entityManager.Destroy(active[i]);
                }
                _selectedUnitIds.Clear();
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

        _console.RegisterCommand("select", args =>
        {
            if (args.Length == 0)
            {
                System.Console.WriteLine("Usage: select <id> | all");
                return;
            }

            if (args[0] == "all")
            {
                _selectedUnitIds.Clear();
                foreach (int i in _entityManager.ActiveEntities)
                    _selectedUnitIds.Add(i);
                System.Console.WriteLine($"Selected {_selectedUnitIds.Count} unit(s).");
                return;
            }

            if (int.TryParse(args[0], out int id) && _entityManager.IsAlive(id))
            {
                _selectedUnitIds.Add(id);
                System.Console.WriteLine($"Selected unit {id}.");
            }
            else
            {
                System.Console.WriteLine($"Unit {args[0]} does not exist.");
            }
        });

        _console.RegisterCommand("deselect", args =>
        {
            if (args.Length == 0)
            {
                System.Console.WriteLine("Usage: deselect <id> | all");
                return;
            }

            if (args[0] == "all")
            {
                _selectedUnitIds.Clear();
                System.Console.WriteLine("Deselected all units.");
                return;
            }

            if (int.TryParse(args[0], out int id))
            {
                if (_selectedUnitIds.Remove(id))
                    System.Console.WriteLine($"Deselected unit {id}.");
                else
                    System.Console.WriteLine($"Unit {id} was not selected.");
            }
            else
            {
                System.Console.WriteLine($"Invalid id: {args[0]}");
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
                    _lastExploredX[id] = -1000000f;
                    _lastExploredY[id] = -1000000f;
                    _entityManager.GetPosition(id) = new PositionComponent { X = rx, Y = ry };
                    var rt = (UnitType)Random.Shared.Next(5);
                    _entityManager.GetUnitType(id) = new UnitTypeComponent { Type = rt };
                    _entityManager.GetMove(id).Speed = UnitStats.RollSpeed(rt);
                    _entityManager.GetVision(id).SightRange = UnitStats.RollSightRange(rt);
                    float rhp = UnitStats.RollHP(rt);
                    _entityManager.GetHealth(id) = new HealthComponent { MaxHP = rhp, CurrentHP = rhp };
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
            _lastExploredX[newId] = -1000000f;
            _lastExploredY[newId] = -1000000f;
            _entityManager.GetPosition(newId) = new PositionComponent { X = cx, Y = cy };
            _entityManager.GetUnitType(newId) = new UnitTypeComponent { Type = parsedType };
            _entityManager.GetMove(newId).Speed = UnitStats.RollSpeed(parsedType);
            _entityManager.GetVision(newId).SightRange = UnitStats.RollSightRange(parsedType);
            float hp2 = UnitStats.RollHP(parsedType);
            _entityManager.GetHealth(newId) = new HealthComponent { MaxHP = hp2, CurrentHP = hp2 };
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
                    move.StuckTimer = 0f;
                    move.DistCheckTimer = 0f;
                    move.PrevDist = 0f;
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
                    move.StuckTimer = 0f;
                    move.DistCheckTimer = 0f;
                    move.PrevDist = 0f;
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
            mv.StuckTimer = 0f;
            mv.DistCheckTimer = 0f;
            mv.PrevDist = 0f;
            System.Console.WriteLine($"Unit {moveId} moving to ({mx:F0}, {my:F0}).");
        });

        _console.RegisterCommand("type", args =>
        {
            if (args.Length < 3 && !(args.Length == 2 && args[1] == "random"))
            {
                System.Console.WriteLine("Usage: type <id|all> set <typename|id> | type <id|all> random");
                return;
            }

            string targetArg = args[0].ToLowerInvariant();
            string op = args[1].ToLowerInvariant();

            UnitType? ResolveType(string val)
            {
                if (int.TryParse(val, out int ti))
                {
                    if (Enum.IsDefined(typeof(UnitType), ti))
                        return (UnitType)ti;
                    System.Console.WriteLine($"Invalid type id: {ti}. Valid: 0=Infantry..4=Medic");
                    return null;
                }
                if (Enum.TryParse(val, true, out UnitType parsed))
                    return parsed;
                System.Console.WriteLine($"Unknown type: {val}. Valid: Infantry, Archer, Cavalry, Ballista, Medic");
                return null;
            }

            void ApplyType(int eid, UnitType t)
            {
                _entityManager.GetUnitType(eid) = new UnitTypeComponent { Type = t };
                _entityManager.GetMove(eid).Speed = UnitStats.RollSpeed(t);
                _entityManager.GetVision(eid).SightRange = UnitStats.RollSightRange(t);
                float hp = UnitStats.RollHP(t);
                _entityManager.GetHealth(eid) = new HealthComponent { MaxHP = hp, CurrentHP = hp };
            }

            if (op == "random")
            {
                if (targetArg == "all")
                {
                    int count = 0;
                    foreach (int i in _entityManager.ActiveEntities)
                    {
                        ApplyType(i, (UnitType)Random.Shared.Next(5));
                        count++;
                    }
                    System.Console.WriteLine($"Randomized type for {count} unit(s).");
                }
                else if (int.TryParse(args[0], out int eid) && _entityManager.IsAlive(eid))
                {
                    var rt = (UnitType)Random.Shared.Next(5);
                    ApplyType(eid, rt);
                    System.Console.WriteLine($"Unit {eid} type set to {rt} (random).");
                }
                else
                {
                    System.Console.WriteLine($"Unit {args[0]} does not exist.");
                }
                return;
            }

            if (op != "set" || args.Length < 3)
            {
                System.Console.WriteLine("Usage: type <id|all> set <typename|id> | type <id|all> random");
                return;
            }

            var newType = ResolveType(args[2]);
            if (newType == null) return;

            if (targetArg == "all")
            {
                int count = 0;
                foreach (int i in _entityManager.ActiveEntities)
                {
                    ApplyType(i, newType.Value);
                    count++;
                }
                System.Console.WriteLine($"Set all {count} unit(s) type to {newType.Value}.");
            }
            else if (int.TryParse(args[0], out int eid2))
            {
                if (!_entityManager.IsAlive(eid2))
                {
                    System.Console.WriteLine($"Unit {eid2} does not exist.");
                    return;
                }
                ApplyType(eid2, newType.Value);
                System.Console.WriteLine($"Unit {eid2} type set to {newType.Value}.");
            }
            else
            {
                System.Console.WriteLine("Usage: type <id|all> set <typename|id> | type <id|all> random");
            }
        });

        _console.RegisterCommand("speed", args =>
        {
            if (args.Length < 3 && !(args.Length == 2 && args[1] == "random"))
            {
                System.Console.WriteLine("Usage: speed <id|all> set <value> | speed <id|all> random");
                return;
            }

            string targetArg = args[0].ToLowerInvariant();
            string op = args[1].ToLowerInvariant();

            if (op == "random")
            {
                if (targetArg == "all")
                {
                    int count = 0;
                    foreach (int i in _entityManager.ActiveEntities)
                    {
                        var ut = _entityManager.GetUnitType(i).Type;
                        _entityManager.GetMove(i).Speed = UnitStats.RollSpeed(ut);
                        count++;
                    }
                    System.Console.WriteLine($"Randomized speed for {count} unit(s).");
                }
                else if (int.TryParse(args[0], out int eid) && _entityManager.IsAlive(eid))
                {
                    var ut = _entityManager.GetUnitType(eid).Type;
                    _entityManager.GetMove(eid).Speed = UnitStats.RollSpeed(ut);
                    System.Console.WriteLine($"Unit {eid} speed randomized to {_entityManager.GetMove(eid).Speed:F1}.");
                }
                else
                {
                    System.Console.WriteLine($"Unit {args[0]} does not exist.");
                }
                return;
            }

            if (op != "set" || args.Length < 3 || !float.TryParse(args[2], out float val))
            {
                System.Console.WriteLine("Usage: speed <id|all> set <value> | speed <id|all> random");
                return;
            }

            if (targetArg == "all")
            {
                int count = 0;
                foreach (int i in _entityManager.ActiveEntities)
                {
                    _entityManager.GetMove(i).Speed = val;
                    count++;
                }
                System.Console.WriteLine($"Set all {count} unit(s) speed to {val}.");
            }
            else if (int.TryParse(args[0], out int eid2))
            {
                if (!_entityManager.IsAlive(eid2))
                {
                    System.Console.WriteLine($"Unit {eid2} does not exist.");
                    return;
                }
                _entityManager.GetMove(eid2).Speed = val;
                System.Console.WriteLine($"Unit {eid2} speed set to {val}.");
            }
            else
            {
                System.Console.WriteLine("Usage: speed <id|all> set <value> | speed <id|all> random");
            }
        });

        _console.RegisterCommand("health", args =>
        {
            if (args.Length < 3)
            {
                System.Console.WriteLine("Usage: health <id|all> add|remove|set <amount>");
                return;
            }

            string targetArg = args[0].ToLowerInvariant();
            string op = args[1].ToLowerInvariant();
            if (!float.TryParse(args[2], out float amount))
            {
                System.Console.WriteLine("Invalid amount.");
                return;
            }

            void ApplyHealth(int eid, float val, string o)
            {
                ref var h = ref _entityManager.GetHealth(eid);
                switch (o)
                {
                    case "add":
                        h.CurrentHP = Math.Min(h.MaxHP, h.CurrentHP + val);
                        break;
                    case "remove":
                        h.CurrentHP -= val;
                        if (h.CurrentHP <= 0f)
                        {
                            h.CurrentHP = 0f;
                            _entityManager.Destroy(eid);
                            _selectedUnitIds.Remove(eid);
                            System.Console.WriteLine($"Unit {eid} died.");
                        }
                        break;
                    case "set":
                        h.CurrentHP = Math.Clamp(val, 0f, h.MaxHP);
                        if (h.CurrentHP <= 0f)
                        {
                            _entityManager.Destroy(eid);
                            _selectedUnitIds.Remove(eid);
                            System.Console.WriteLine($"Unit {eid} died.");
                        }
                        break;
                }
            }

            if (targetArg == "all")
            {
                var active = _entityManager.ActiveEntities;
                int count = active.Length;
                for (int i = count - 1; i >= 0; i--)
                {
                    ApplyHealth(active[i], amount, op);
                }
                System.Console.WriteLine($"Applied health {op} {amount} to {count} unit(s).");
            }
            else if (int.TryParse(args[0], out int eid))
            {
                if (!_entityManager.IsAlive(eid))
                {
                    System.Console.WriteLine($"Unit {eid} does not exist.");
                    return;
                }
                ApplyHealth(eid, amount, op);
                var h = _entityManager.GetHealth(eid);
                System.Console.WriteLine($"Unit {eid} health: {h.CurrentHP:F1}/{h.MaxHP:F1}");
            }
            else
            {
                System.Console.WriteLine("Usage: health <id|all> add|remove|set <amount>");
            }
        });

        _console.RegisterCommand("vision", args =>
        {
            if (args.Length == 0)
            {
                System.Console.WriteLine("Usage:");
                System.Console.WriteLine("  vision unit all     — show sight range of all units");
                System.Console.WriteLine("  vision unit <id>    — show sight range of a unit");
                System.Console.WriteLine("  vision none         — hide all sight overlays");
                System.Console.WriteLine($"Current: mode={_visionMode}, id={_visionUnitId}");
                return;
            }

            if (args[0] == "none")
            {
                _visionMode = VisionMode.None;
                _visionUnitId = -1;
                System.Console.WriteLine("Vision overlay hidden.");
            }
            else if (args[0] == "unit")
            {
                if (args.Length < 2)
                {
                    System.Console.WriteLine("Usage: vision unit <id> | all");
                    return;
                }

                if (args[1] == "all")
                {
                    _visionMode = VisionMode.ShowAll;
                    _visionUnitId = -1;
                    System.Console.WriteLine("Showing sight range for all units.");
                }
                else if (int.TryParse(args[1], out int vid))
                {
                    if (_entityManager.IsAlive(vid))
                    {
                        _visionMode = VisionMode.ShowSingle;
                        _visionUnitId = vid;
                        System.Console.WriteLine($"Showing sight range for unit {vid}.");
                    }
                    else
                    {
                        System.Console.WriteLine($"Unit {vid} does not exist.");
                    }
                }
                else
                {
                    System.Console.WriteLine("Usage: vision unit <id> | all");
                }
            }
            else
            {
                System.Console.WriteLine("Unknown vision command.");
            }
        });

        _console.RegisterCommand("zoom", _ =>
        {
            System.Console.WriteLine($"Zoom: {_camera.Zoom:F2}x (min={Camera.MinZoom:F1}, max={Camera.MaxZoom:F1})");
        });
    }
}
