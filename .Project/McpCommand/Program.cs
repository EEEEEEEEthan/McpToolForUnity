namespace McpCommand;

internal static class Program
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    static Server server;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    static void Main(string[] args)
    {
        // Get port from args or use default 5000
        var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 5000;
        var logPath = args.Length > 1 ? args[1] : "";
        TextWriter? logWriter = null;
        if (!string.IsNullOrEmpty(logPath))
        {
            if (!File.Exists(logPath)) File.WriteAllText(logPath, "");
            logWriter = new StreamWriter(logPath, true);
        }
        server = new Server(logWriter, port);
        Task.Run(InputRoutine);
        while (true)
        {
            while (server.TryReceiveMessage(out var message))
                Console.WriteLine(message);
            Thread.Sleep(100);
        }
        // ReSharper disable once FunctionNeverReturns
    }

    static void InputRoutine()
    {
        while (true)
        {
            var input = Console.ReadLine();
            if (input is not null)
                server.SendMessage(input);
        }
        // ReSharper disable once FunctionNeverReturns
    }
}