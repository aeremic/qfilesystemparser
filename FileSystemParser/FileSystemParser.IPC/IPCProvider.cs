using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace FileSystemParser.IPC
{
    public class IpcConfigurationMessage
    {
        public required string Path { get; init; }
        public int CheckInterval { get; init; }
        public int MaximumConcurrentProcessing { get; init; }
    }

    public class IpcResultMessage
    {
        public required string Result { get; init; }
    }

    public static class IpcProvider
    {
        private static NamedPipeServerStream? _namedPipeServerStream;

        public static event EventHandler<IpcResultMessage?>? TriggerReceivedMessage = null;

        public static void InitializeServer()
        {
            if (_namedPipeServerStream == null)
            {
                _namedPipeServerStream = new NamedPipeServerStream("FileSystemParserPipe",
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);
                _namedPipeServerStream.BeginWaitForConnection(ServerCallbackWaitingHandler,
                    _namedPipeServerStream);
            }
        }

        public static async Task WriteMessageToClient(string message)
        {
            // var byteArray = Encoding.UTF8.GetBytes(message);
            // _namedPipeServerStream
            //     .BeginWrite(byteArray, 0, byteArray.Length, ar => { }, _namedPipeServerStream);
            //
            // return Task.CompletedTask;

            await using var sw = new StreamWriter(_namedPipeServerStream!);
            sw.AutoFlush = true;
            await sw.WriteAsync(message);

            int temp;
//             await sw.FlushAsync();
//
// #pragma warning disable CA1416
//             _namedPipeServerStream!.WaitForPipeDrain();
// #pragma warning restore CA1416
        }

        private static void ServerCallbackWaitingHandler(IAsyncResult ar)
        {
            _namedPipeServerStream!.EndWaitForConnection(ar);

            try
            {
                string message;
                using (var reader = new StreamReader(_namedPipeServerStream, leaveOpen: true))
                {
                    message = reader.ReadLine() ?? string.Empty;
                }

                if (string.IsNullOrEmpty(message))
                {
                    return;
                }

                TriggerReceivedMessage?.Invoke(null,
                    JsonSerializer.Deserialize<IpcResultMessage>(message));
            }
            catch (Exception ex)
            {
                // TODO: Log.	
            }
        }
    }
}