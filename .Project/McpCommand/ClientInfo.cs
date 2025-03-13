using System.Net.Sockets;

namespace McpCommand;

internal class ClientInfo
{
    public readonly TcpClient client;
    public readonly NetworkStream stream;
    public readonly BinaryReader reader;
    public readonly BinaryWriter writer;

    public ClientInfo(TcpClient client)
    {
        this.client = client;
        stream = client.GetStream();
        reader = new BinaryReader(stream);
        writer = new BinaryWriter(stream);
    }

    public bool Connected => client.Connected;

    ~ClientInfo()
    {
        writer.Dispose();
        reader.Dispose();
        stream.Dispose();
        client.Dispose();
    }
}