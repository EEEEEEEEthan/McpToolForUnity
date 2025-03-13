using System.Net.Sockets;

namespace McpCommand;

internal class Server
{
	readonly TcpListener listener;
	readonly Queue<string> sendQueue = new();
	readonly Queue<string> receiveQueue = new();
	readonly Queue<string> logQueue = new();
	readonly TextWriter? logWriter;
	ClientInfo? client;
	bool running = true;

	public Server(TextWriter? logWriter, int port = 5000)
	{
		this.logWriter = logWriter;
		listener = new TcpListener(System.Net.IPAddress.Any, port);
		client = null;
		Task.Run(WriteLogRoutine);
		Task.Run(Launch);
	}

	public void SendMessage(string message)
	{
		lock (sendQueue)
		{
			sendQueue.Enqueue(message);
		}
		while (TrySendMessage()) ;
	}

	public bool TryReceiveMessage(out string? message)
	{
		lock (receiveQueue)
		{
			return receiveQueue.TryDequeue(out message);
		}
	}

	bool TrySendMessage()
	{
		if (client is null) return false;
		string? message;
		lock (sendQueue)
		{
			if (!sendQueue.TryDequeue(out message)) return false;
		}
		client.writer.Write(message);
		client.writer.Flush();
		Log($"Sent: {message}");
		return true;
	}

	void Log(string message)
	{
		if (logWriter != null)
			lock (logQueue)
			{
				logQueue.Enqueue($"[{DateTime.Now: yyyy-MM-dd HH:mm:ss}] {message}");
			}
	}

	void WriteLogRoutine()
	{
		if (logWriter is null) return;
		while (running)
		{
			string? message;
			lock (logQueue)
			{
				if (!logQueue.TryDequeue(out message)) goto LOOP_END;
			}
			logWriter.WriteLine(message);
			logWriter.Flush();
			// Console.WriteLine(message);
			LOOP_END:
			Thread.Sleep(10);
		}
	}

	void Launch()
	{
		Log("Start");
		listener.Start();
		while (running)
		{
			client = new ClientInfo(listener.AcceptTcpClient());
			Log($"Client connected: {client.client.Client.RemoteEndPoint}");
			while (client.Connected)
			{
				while (TrySendMessage()) ;
				lock (receiveQueue)
				{
					var message = client.reader.ReadString();
					receiveQueue.Enqueue(message);
					Console.WriteLine(message);
					Log($"Recv: {message}");
				}
			}
		}
		// ReSharper disable once FunctionNeverReturns
	}

	~Server()
	{
		running = false;
	}
}