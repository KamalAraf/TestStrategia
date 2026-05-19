namespace MedievalWarSim.Screens;

public partial class GameScreen
{
    private void RegisterMoveCommand()
    {
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
    }
}
