using MedievalWarSim.Core.Components;
using MedievalWarSim.Core.Data;
using MedievalWarSim.Core.Enums;

namespace MedievalWarSim.Screens;

public partial class GameScreen
{
    private void RegisterUnitCommands()
    {
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
                float st = UnitStats.RollMaxStamina(t);
                _entityManager.GetStamina(eid) = new StaminaComponent { MaxStamina = st, CurrentStamina = st, DrainRate = 0.2f, RecoveryRate = 1.0f };
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

        _console.RegisterCommand("stamina", args =>
        {
            if (args.Length < 3 && !(args.Length == 2 && args[1] == "random"))
            {
                System.Console.WriteLine("Usage: stamina <id|all> add|remove|set|random [amount]");
                return;
            }

            string targetArg = args[0].ToLowerInvariant();
            string op = args[1].ToLowerInvariant();

            void ApplyStamina(int eid, string o, float val)
            {
                ref var s = ref _entityManager.GetStamina(eid);
                switch (o)
                {
                    case "add":
                        s.CurrentStamina = Math.Min(s.MaxStamina, s.CurrentStamina + val);
                        break;
                    case "remove":
                        s.CurrentStamina = Math.Max(0f, s.CurrentStamina - val);
                        break;
                    case "set":
                        s.CurrentStamina = Math.Clamp(val, 0f, s.MaxStamina);
                        break;
                }
            }

            if (op == "random")
            {
                if (targetArg == "all")
                {
                    int count = 0;
                    foreach (int i in _entityManager.ActiveEntities)
                    {
                        var ut = _entityManager.GetUnitType(i).Type;
                        float rst = UnitStats.RollMaxStamina(ut);
                        ref var s = ref _entityManager.GetStamina(i);
                        s.MaxStamina = rst;
                        s.CurrentStamina = rst;
                        count++;
                    }
                    System.Console.WriteLine($"Randomized stamina for {count} unit(s).");
                }
                else if (int.TryParse(args[0], out int eid) && _entityManager.IsAlive(eid))
                {
                    var ut = _entityManager.GetUnitType(eid).Type;
                    float rst = UnitStats.RollMaxStamina(ut);
                    ref var s = ref _entityManager.GetStamina(eid);
                    s.MaxStamina = rst;
                    s.CurrentStamina = rst;
                    System.Console.WriteLine($"Unit {eid} stamina randomized to {rst:F1}.");
                }
                else
                {
                    System.Console.WriteLine($"Unit {args[0]} does not exist.");
                }
                return;
            }

            if (op != "add" && op != "remove" && op != "set")
            {
                System.Console.WriteLine("Usage: stamina <id|all> add|remove|set|random [amount]");
                return;
            }

            if (!float.TryParse(args[2], out float amount))
            {
                System.Console.WriteLine("Invalid amount.");
                return;
            }

            if (targetArg == "all")
            {
                int count = 0;
                foreach (int i in _entityManager.ActiveEntities)
                {
                    ApplyStamina(i, op, amount);
                    count++;
                }
                System.Console.WriteLine($"Applied stamina {op} {amount} to {count} unit(s).");
            }
            else if (int.TryParse(args[0], out int eid))
            {
                if (!_entityManager.IsAlive(eid))
                {
                    System.Console.WriteLine($"Unit {eid} does not exist.");
                    return;
                }
                ApplyStamina(eid, op, amount);
                var s = _entityManager.GetStamina(eid);
                System.Console.WriteLine($"Unit {eid} stamina: {s.CurrentStamina:F1}/{s.MaxStamina:F1}");
            }
            else
            {
                System.Console.WriteLine("Usage: stamina <id|all> add|remove|set|random [amount]");
            }
        });

        _console.RegisterCommand("team", args =>
        {
            if (args.Length < 2)
            {
                System.Console.WriteLine("Usage: team <id|all> white|red|blue|green|yellow");
                return;
            }

            string targetArg = args[0].ToLowerInvariant();
            Team newTeam = ParseTeamArg(args[1]);
            if ((int)newTeam < 0) return;

            if (targetArg == "all")
            {
                int count = 0;
                foreach (int i in _entityManager.ActiveEntities)
                {
                    _entityManager.GetTeam(i) = new TeamComponent { Team = newTeam };
                    count++;
                }
                System.Console.WriteLine($"Set all {count} unit(s) to team {newTeam}.");
            }
            else if (int.TryParse(args[0], out int eid))
            {
                if (!_entityManager.IsAlive(eid))
                {
                    System.Console.WriteLine($"Unit {eid} does not exist.");
                    return;
                }
                _entityManager.GetTeam(eid) = new TeamComponent { Team = newTeam };
                System.Console.WriteLine($"Unit {eid} set to team {newTeam}.");
            }
            else
            {
                System.Console.WriteLine("Usage: team <id|all> white|red|blue|green|yellow");
            }
        });
    }
}
