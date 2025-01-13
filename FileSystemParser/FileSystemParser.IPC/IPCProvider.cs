using System.IO.Pipes;
using System.Text;

namespace FileSystemParser.IPC
{
	public class IpcMessage
	{
		public required string Path { get; set; }
		public int CheckInterval { get; set; }
		public int MaximumConcurrentProcessing { get; set; }
	}

	//public class MessageReceived : EventArgs
	//{
	//	public required string ReceivedData { get; set; }
	//}

	public class IpcProvider
	{
		private static NamedPipeServerStream? _namedPipeServerStream;

		// public static event EventHandler<MessageReceived>? TriggerReceivedMessage = null;

		public static void Initialize()
		{
			_namedPipeServerStream = new NamedPipeServerStream("FileSystemParserPipe");
		}

		public static void WriteMessage(string message)
		{
			_namedPipeServerStream!.WaitForConnection();

			byte[] byteArray = Encoding.UTF8.GetBytes(message);
			var writeResult = _namedPipeServerStream!.BeginWrite(byteArray, 0, byteArray.Length, DisposeAfterMessageSent, _namedPipeServerStream);
		}

		//public static void ReceiveMessage(string message)
		//{
		//	if (!string.IsNullOrEmpty(message))
		//	{
		//		var eventArgs = new MessageReceived
		//		{
		//			ReceivedData = message
		//		};

		//		TriggerReceivedMessage?.Invoke(null, eventArgs);
		//	}
		//}

		private static void DisposeAfterMessageSent(IAsyncResult iar)
		{
			var pipeStream = iar.AsyncState as NamedPipeServerStream;
			pipeStream!.EndWrite(iar);
			pipeStream!.Flush();
			pipeStream!.Close();
			pipeStream!.Dispose();
		}
	}
}
