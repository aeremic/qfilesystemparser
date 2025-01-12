using System.Text.Json;
using FileSystemParser.Console;

Console.WriteLine("Enter folder path for processing: ");
var foldersPath = @"C:\Users\Andrija\Temp\FileSystemParser\Files"; // Console.ReadLine() ?? string.Empty;

Console.WriteLine("Enter maximum concurrent processing jobs: ");
var maxDegreeOfParallelism = 10; // int.Parse(Console.ReadLine() ?? string.Empty);

if (maxDegreeOfParallelism == 0)
{
    maxDegreeOfParallelism = 1;
}

const int retryTimeInMs = 5000;

if (!string.IsNullOrEmpty(foldersPath))
{
    Console.WriteLine("Processing started...");

    try
    {
        while (true)
        {
            Console.WriteLine("Processing...");

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
                    Console.WriteLine($"Processing file: {filePath.path}");

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

                        Console.WriteLine($"File at path {filePath.path} successfully processed" +
                                          $" with {numberOfComponents} components.");
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"Error while parsing Json file at path {filePath.path}" +
                                          $"Error: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error while processing file at path {filePath.path}" +
                                          $"Error: {ex.Message}");
                    }
                });

            Thread.Sleep(retryTimeInMs);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error while reading files. Error: {ex.Message}");
    }
}