using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using FileSystemParser.Common;

namespace FileSystemParser.Console;

internal static class Program
{
    // TODO: Stop mechanism.
    private static async Task Main()
    {
        try
        {
            System.Console.WriteLine("Connecting...");

            using var ws = new ClientWebSocket();

            var uri = new Uri("ws://localhost:9006");
            await ws.ConnectAsync(uri, CancellationToken.None);
            
            System.Console.WriteLine("Connected");
            
            var receiveBuffer = new byte[1024];
            var receiveResult = await ws.ReceiveAsync(
                new ArraySegment<byte>(receiveBuffer),
                CancellationToken.None);
            
            var serverInputData = Encoding.UTF8.GetString(receiveBuffer, 0, receiveResult.Count);
            var ipcMessage = JsonSerializer.Deserialize<ConfigurationMessage>(serverInputData);

            if (ipcMessage == null)
            {
                return;
            }

            var foldersPath = ipcMessage.Path;
            System.Console.WriteLine($"Received folders path from the server: {foldersPath}");

            var retryTimeInMs = ipcMessage.CheckInterval;
            System.Console.WriteLine($"Received retry time in ms from the server: {retryTimeInMs}");

            var maxDegreeOfParallelism = ipcMessage.MaximumConcurrentProcessing;
            System.Console.WriteLine(
                $"Received maximum degree of parallelism from the server: {maxDegreeOfParallelism}");

            if (maxDegreeOfParallelism == 0)
            {
                maxDegreeOfParallelism = 1;
            }

            if (!string.IsNullOrEmpty(foldersPath))
            {
                System.Console.WriteLine("Processing started...");
                while (true)
                {
                    System.Console.WriteLine("Processing...");

                    var filePaths = (from path in
                            Directory.EnumerateFiles(foldersPath, "*.json", SearchOption.AllDirectories)
                        select new
                        {
                            path,
                        }).ToList();

                    await Parallel.ForEachAsync(filePaths,
                        new ParallelOptions
                        {
                            MaxDegreeOfParallelism = maxDegreeOfParallelism
                        },
                        async (filePath, cancellationToken) =>
                        {
                            System.Console.WriteLine($"Processing file: {filePath.path}");

                            var numberOfComponents = 0;
                            try
                            {
                                await using var stream = File.OpenRead(filePath.path);
                                var quest = await JsonSerializer.DeserializeAsync<Quest>(stream,
                                    cancellationToken: cancellationToken);

                                if (quest != null)
                                {
                                    numberOfComponents = quest.Components.Count;
                                }
                                
                                var buffer = Encoding.UTF8.GetBytes($"File at path {filePath.path}" +
                                                                    $" successfully processed with {numberOfComponents}" +
                                                                    $" components.");
                                await ws.SendAsync(
                                    new ArraySegment<byte>(buffer), 
                                    WebSocketMessageType.Text, true, 
                                    CancellationToken.None);

                                System.Console.WriteLine($"File at path {filePath.path} successfully processed" +
                                                         $" with {numberOfComponents} components.");
                            }
                            catch (JsonException ex)
                            {
                                System.Console.WriteLine($"Error while parsing Json file at path {filePath.path}" +
                                                         $"Error: {ex.Message}");
                            }
                            catch (Exception ex)
                            {
                                System.Console.WriteLine($"Error while processing file at path {filePath.path}" +
                                                         $"Error: {ex.Message}");
                            }
                        });

                    Thread.Sleep(retryTimeInMs);
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error while reading IPC message. Error: {ex.Message}");
        }
    }
}