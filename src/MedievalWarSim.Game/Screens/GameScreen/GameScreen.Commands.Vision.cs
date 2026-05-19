namespace MedievalWarSim.Screens;

public partial class GameScreen
{
    private void RegisterVisionCommand()
    {
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
    }
}
