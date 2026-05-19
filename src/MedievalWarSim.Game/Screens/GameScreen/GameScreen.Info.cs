namespace MedievalWarSim.Screens;

public partial class GameScreen
{
    private void PrintUnitInfo(int id)
    {
        var pos  = _entityManager.GetPosition(id);
        var type = _entityManager.GetUnitType(id);
        var move = _entityManager.GetMove(id);
        var hp   = _entityManager.GetHealth(id);
        var vis  = _entityManager.GetVision(id);
        System.Console.WriteLine($"Unit {id}:");
        System.Console.WriteLine($"  Type:      {type.Type}");
        System.Console.Write($"  Position:  ({pos.X:F1};{pos.Y:F1})");
        if (move.IsMoving)
            System.Console.Write($" -> ({move.TargetX:F1};{move.TargetY:F1})");
        System.Console.WriteLine();
        System.Console.WriteLine($"  HP:        {hp.CurrentHP:F1}/{hp.MaxHP:F1}");
        var stamina = _entityManager.GetStamina(id);
        var team = _entityManager.GetTeam(id);
        System.Console.WriteLine($"  Team:      {team.Team}");
        System.Console.WriteLine($"  Speed:     {move.Speed:F1}");
        System.Console.WriteLine($"  Sight:     {vis.SightRange:F1}");
        System.Console.WriteLine($"  Stamina:   {stamina.CurrentStamina:F1}/{stamina.MaxStamina:F1}");
        System.Console.WriteLine($"  Selected:  {_selectedUnitIds.Contains(id)}");
        System.Console.WriteLine($"  Moving:    {move.IsMoving}");
    }
}
