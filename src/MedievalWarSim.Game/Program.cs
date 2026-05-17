AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    var ex = e.ExceptionObject as Exception;
    File.WriteAllText("crash.txt", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] UNHANDLED{Environment.NewLine}{ex}");
};

try
{
    using var game = new MedievalWarSim.Game1();
    game.Run();
}
catch (Exception ex)
{
    File.WriteAllText("crash.txt", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]{Environment.NewLine}{ex}");
    throw;
}
