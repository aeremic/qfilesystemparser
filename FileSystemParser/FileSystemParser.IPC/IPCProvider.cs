using System.IO.Pipes;
using System.Text;

namespace FileSystemParser.IPC
{
	public class MessageReceived : EventArgs
	{
		public required string ReceivedData { get; set; }
	}

	public class IPCProvider
	{
		private AutoResetEvent? _autoResetEvent = null;
		private NamedPipeServerStream? _namedPipeServerStream = null;
		private readonly object _objReceivedDataLock = new object();

		private static readonly object objLock = new object();
		private static IPCProvider? _instance = null;

		public static event EventHandler<MessageReceived>? TriggerReceivedMessage = null;

		public IPCProvider()
		{
			_autoResetEvent = new AutoResetEvent(false);
		}

		public static IPCProvider Instance
		{
			get
			{
				if (_instance == null)
				{
					lock (objLock)
					{
						_instance ??= new IPCProvider();
					}
				}

				return _instance;
			}
		}

		private void CallbackWaitingHandler(IAsyncResult asyncResult)
		{
			try
			{
				var byteArray = new byte[1024];
				
				var namedPipeServerStream = asyncResult.AsyncState as NamedPipeServerStream;
				if (namedPipeServerStream == null)
				{
					return;
				}

				namedPipeServerStream.EndWaitForConnection(asyncResult);
				namedPipeServerStream.Read(byteArray, 0, byteArray.Length);

				var message = Encoding.UTF8.GetString(byteArray);
				lock (_objReceivedDataLock)
				{
					ReceiveMessage(message);
					_autoResetEvent!.Set();
				}

				namedPipeServerStream.Close();
				namedPipeServerStream = null;

				namedPipeServerStream = new NamedPipeServerStream(
					"FileSystemParserPipe",
					PipeDirection.InOut,
					1,
					PipeTransmissionMode.Byte,
					PipeOptions.Asynchronous
				);
				namedPipeServerStream.BeginWaitForConnection(new AsyncCallback(CallbackWaitingHandler), namedPipeServerStream);
			}
			catch (Exception ex)
			{
				// TODO: Log.
			}
		}

		public void StartIPCServer()
		{
			try
			{
				_namedPipeServerStream = new NamedPipeServerStream(
					"FileSystemParserPipe",
					PipeDirection.InOut,
					1,
					PipeTransmissionMode.Byte,
					PipeOptions.Asynchronous
				);
				_namedPipeServerStream.BeginWaitForConnection(new AsyncCallback(CallbackWaitingHandler), _namedPipeServerStream);

				var thread = new Thread(() =>
				{
					try
					{
						while (true)
						{
							if (_autoResetEvent != null && _autoResetEvent.WaitOne())
							{
								_autoResetEvent.Reset();
							}
						}
					}
					catch (Exception ex)
					{
						// TODO: Log.
					}
				});

				thread.Start();
			}
			catch (Exception ex)
			{
				// TODO: Log.
			}
		}

		public static void ReceiveMessage(string message)
		{
			if (!string.IsNullOrEmpty(message))
			{
				var eventArgs = new MessageReceived
				{
					ReceivedData = message
				};

				TriggerReceivedMessage?.Invoke(null, eventArgs);
			}
		}

		private void RetryMechanism(int maximumCounter, TimeSpan delay, Action action)
		{
			var counter = 0;
			do
			{
				try
				{
					counter++;
					action();

					break;
				}
				catch (Exception ex)
				{
					// TODO: Log.
					Task.Delay(delay).Wait();
				}
			} while (counter < maximumCounter);
		}

		private void MessageSentCallback(IAsyncResult asyncResult)
		{
			var namedPipeClientStream = asyncResult.AsyncState as NamedPipeClientStream;
			
			if(namedPipeClientStream == null)
			{
				return;
			}
			
			namedPipeClientStream.EndWrite(asyncResult);
			namedPipeClientStream.Flush();
			namedPipeClientStream.Close();
			namedPipeClientStream.Dispose();
		}

		public void TransmitDataToIPCServer(string data)
		{
			try
			{
				var namedPipeClientStream = new NamedPipeClientStream(
					".",
					"FileSystemParserPipe",
					PipeDirection.InOut,
					PipeOptions.Asynchronous
				);

				RetryMechanism(10, TimeSpan.FromMilliseconds(400),
					() =>
					{
						namedPipeClientStream.Connect(10000);
					});

				if(!namedPipeClientStream.IsConnected)
				{
					return;
				}

				if (!namedPipeClientStream.CanWrite)
				{
					return;
				}

				var byteArray = Encoding.UTF8.GetBytes(data);
				var writeResult = namedPipeClientStream.BeginWrite(byteArray, 0, byteArray.Length, MessageSentCallback, namedPipeClientStream);
				writeResult.AsyncWaitHandle.WaitOne();
			}
			catch (Exception ex)
			{
				// TODO: Log.
			}
		}
	}
}
