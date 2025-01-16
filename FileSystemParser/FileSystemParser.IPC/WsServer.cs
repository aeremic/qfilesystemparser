using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace FileSystemParser.IPC
{
    public class WsConfigurationMessage
    {
        public required string Path { get; init; }
        public int CheckInterval { get; init; }
        public int MaximumConcurrentProcessing { get; init; }
    }

    public static class WsServer
    {
        private static WebSocket? _webSocket;        
        
        public static event EventHandler<string>? TriggerReceivedMessage;

        public static async Task InitializeServerAsync()
        {
            var httpListener = new HttpListener();
            
            httpListener.Prefixes.Add("http://localhost:9006/");
            httpListener.Start();
            
            var context = await httpListener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                var webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null); 
                
                _webSocket = webSocketContext.WebSocket;
                ReceiveMessagesFromClientAsync();
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }

        public static async Task WriteMessageToClientAsync(string message)
        {
            if (_webSocket?.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true,
                    CancellationToken.None);
            }
        }

        private static async Task ReceiveMessagesFromClientAsync()
        {
            var receiveBuffer = new byte[1024];
            while (true)
            {
                if (_webSocket?.State == WebSocketState.Open)
                {
                    var receiveResult = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(receiveBuffer),
                        CancellationToken.None);
                    var receivedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, receiveResult.Count);
                    
                    TriggerReceivedMessage?.Invoke(null, receivedMessage);
                }
                else
                {
                    break;
                }
            }          
        }

        private static void ServerCallbackWaitingHandler(IAsyncResult ar)
        {
        }
    }
}