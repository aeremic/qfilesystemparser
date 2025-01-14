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
            await using var sw = new StreamWriter(_namedPipeServerStream!);
            sw.AutoFlush = true;
            await sw.WriteAsync(message);
//             await sw.FlushAsync();
//
// #pragma warning disable CA1416
//             _namedPipeServerStream!.WaitForPipeDrain();
// #pragma warning restore CA1416
        }

        private static void ServerCallbackWaitingHandler(IAsyncResult ar)
        {
            _namedPipeServerStream!.EndWaitForConnection(ar);
            
            using var reader = new StreamReader(_namedPipeServerStream);
            var message = reader.ReadLine();
            
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            try
            {
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