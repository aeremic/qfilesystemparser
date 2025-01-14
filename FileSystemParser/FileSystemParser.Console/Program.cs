using System.IO.Pipes;
using System.Text.Json;
using FileSystemParser.IPC;

namespace FileSystemParser.Console;

internal static class Program
{
    private static async Task Main()
    {
        try
        {
            await using var namedPipeClientStream = new NamedPipeClientStream(
                ".",
                "FileSystemParserPipe",
                PipeDirection.InOut,
                PipeOptions.Asynchronous);

            System.Console.WriteLine($"Connecting...");
            await namedPipeClientStream.ConnectAsync();

            string serverInputData;
            using (var reader = new StreamReader(namedPipeClientStream, leaveOpen: true))
            {
                serverInputData = await reader.ReadLineAsync() ?? string.Empty;
            }

            var ipcMessage = JsonSerializer.Deserialize<IpcConfigurationMessage>(serverInputData);

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

                                await using (var writer = new StreamWriter(namedPipeClientStream))
                                {
                                    writer.AutoFlush = true;
                                    await writer.WriteLineAsync($"File at path {filePath.path} successfully processed" +
                                                                $" with {numberOfComponents} components.");
                                }

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