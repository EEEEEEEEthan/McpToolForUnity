using System.Net.Sockets;

namespace McpCommand;

internal class Client
{
    readonly NetworkStream stream;
    readonly BinaryReader reader;
    readonly BinaryWriter writer;

    public Client(int port = 5000)
    {
        var tcpClient = new TcpClient("localhost", port);
        stream = tcpClient.GetStream();
        reader = new BinaryReader(stream);
        writer = new BinaryWriter(stream);
    }

    public void Send(string message)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(message);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    public string Receive()
    {
        var length = reader.ReadInt32();
        var bytes = reader.ReadBytes(length);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    ~Client()
    {
        writer.Dispose();
        reader.Dispose();
        stream.Dispose();
    }
}

internal static class Program
{
    static void Main(string[] args)
    {
        // Get port from args or use default 5000
        var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 5000;
        var logPath = args.Length > 1 ? args[1] : "";
        StreamWriter writer = null;
        if (!string.IsNullOrEmpty(logPath))
        {
            if (!File.Exists(logPath)) File.WriteAllText(logPath, "");
            writer = new StreamWriter(logPath, true);
        }
        while (true)
        {
            var input = Console.ReadLine();
            if (string.IsNullOrEmpty(input)) continue;
            writer?.WriteLine("send: " + input);
            writer?.Flush();
            var client = new Client(port);
            client.Send(input);
            var response = client.Receive();
            writer?.WriteLine("recv: " + response);
            writer?.Flush();
            Console.WriteLine(response);
            Console.Out.Flush();
        }
        // ReSharper disable once FunctionNeverReturns
    }
}