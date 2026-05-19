using MedievalWarSim.Core;
using MedievalWarSim.Core.Components;
using MedievalWarSim.Core.Data;
using MedievalWarSim.Core.Enums;

namespace MedievalWarSim.Screens;

public partial class GameScreen
{
    private void RegisterCommands()
    {
        RegisterBasicCommands();
        RegisterCreateCommand();
        RegisterMoveCommand();
        RegisterUnitCommands();
        RegisterVisionCommand();
    }

    private void RegisterBasicCommands()
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

        _console.RegisterCommand("zoom", _ =>
        {
            System.Console.WriteLine($"Zoom: {_camera.Zoom:F2}x (min={Camera.MinZoom:F1}, max={Camera.MaxZoom:F1})");
        });
    }

    private static Team ParseTeamArg(string val)
    {
        if (int.TryParse(val, out int ti) && ti >= 0 && ti <= 4)
            return (Team)ti;
        if (Enum.TryParse(val, true, out Team t))
            return t;
        System.Console.WriteLine($"Unknown team: {val}. Valid: White, Red, Blue, Green, Yellow (or 0-4)");
        return (Team)(-1);
    }
}
