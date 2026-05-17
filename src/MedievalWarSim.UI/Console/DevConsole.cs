using System.Runtime.InteropServices;

namespace MedievalWarSim.UI.Console;

public class DevConsole
{
    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandlerDelegate handler, bool add);

    private delegate bool ConsoleCtrlHandlerDelegate(CtrlType type);

    private enum CtrlType
    {
        CtrlC = 0,
        CtrlBreak = 1,
        CtrlClose = 2
    }

    private Thread? _inputThread;
    private bool _running;
    private readonly Queue<string> _pendingCommands = new();
    private readonly Dictionary<string, Action<string[]>> _commands = new();
    private ConsoleCtrlHandlerDelegate? _ctrlHandler;

    public bool IsOpen => _running;

    public void RegisterCommand(string name, Action<string[]> handler)
    {
        lock (_commands)
        {
            _commands[name.ToLowerInvariant()] = handler;
        }
    }

    public void Toggle()
    {
        if (_running)
            Close();
        else
            Open();
    }

    public void Open()
    {
        if (_running) return;

        if (!AllocConsole()) return;

        // Reopen stdout — AllocConsole + SDL2 can leave the console stream in
        // an invalid state, causing IOException on first WriteLine.
        try
        {
            var stdout = System.Console.OpenStandardOutput();
            System.Console.SetOut(new System.IO.StreamWriter(stdout) { AutoFlush = true });
        }
        catch { /* console output unavailable */ }

        _running = true;
        try { System.Console.Title = "MedievalWarSim - Dev Console"; } catch { }
        try { System.Console.WriteLine("Dev Console opened. Type 'help' for commands. F12 to close."); } catch { }

        _ctrlHandler = _ => _running = false;
        SetConsoleCtrlHandler(_ctrlHandler, true);

        _inputThread = new Thread(ReadLoop)
        {
            IsBackground = true,
            Name = "DevConsole Input"
        };
        _inputThread.Start();
    }

    public void Close()
    {
        if (!_running) return;
        _running = false;

        _inputThread?.Join(100);

        if (_ctrlHandler != null)
        {
            SetConsoleCtrlHandler(_ctrlHandler, false);
            _ctrlHandler = null;
        }

        FreeConsole();

        _inputThread?.Join(500);
        _inputThread = null;
    }

    public string? ReadCommand()
    {
        lock (_pendingCommands)
        {
            return _pendingCommands.Count > 0 ? _pendingCommands.Dequeue() : null;
        }
    }

    private void ReadLoop()
    {
        try
        {
            var input = new System.Text.StringBuilder();

            while (_running)
            {
                if (System.Console.KeyAvailable)
                {
                    var keyInfo = System.Console.ReadKey(true);

                    if (keyInfo.Key == ConsoleKey.F12)
                    {
                        break;
                    }

                    if (keyInfo.Key == ConsoleKey.Enter)
                    {
                        string cmd = input.ToString().Trim();
                        input.Clear();
                        System.Console.WriteLine();

                        if (cmd.Length > 0)
                        {
                            lock (_pendingCommands)
                            {
                                _pendingCommands.Enqueue(cmd);
                            }
                        }
                    }
                    else if (keyInfo.Key == ConsoleKey.Backspace)
                    {
                        if (input.Length > 0)
                        {
                            input.Remove(input.Length - 1, 1);
                            System.Console.Write("\b \b");
                        }
                    }
                    else if (!char.IsControl(keyInfo.KeyChar))
                    {
                        input.Append(keyInfo.KeyChar);
                        System.Console.Write(keyInfo.KeyChar);
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DevConsole] ReadLoop exception: {ex.Message}");
        }

        if (_running)
        {
            _running = false;
            FreeConsole();
        }
    }

    private static void SafeWrite(string? line)
    {
        try { System.Console.WriteLine(line); } catch { }
    }

    public void ExecuteCommand(string line)
    {
        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        string   name = parts[0].ToLowerInvariant();
        string[] args = parts.Length > 1 ? parts[1..] : [];

        if (name == "help")
        {
            SafeWrite("Available commands:");
            lock (_commands)
            {
                foreach (var kvp in _commands)
                {
                    SafeWrite($"  {kvp.Key}");
                }
            }
            SafeWrite("  // showclick");
            return;
        }

        Action<string[]>? handler;
        lock (_commands)
        {
            _commands.TryGetValue(name, out handler);
        }

        if (handler != null)
            handler(args);
        else
            SafeWrite($"Unknown command: {name}. Type 'help'.");
    }
}
