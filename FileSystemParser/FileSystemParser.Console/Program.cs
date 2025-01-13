using System.IO.Pipes;
using System.Text.Json;
using FileSystemParser.Console;
using FileSystemParser.IPC;

internal class Program
{
	private static async Task Main(string[] args)
	{
		// Testing files path: @"C:\Users\Andrija\qfilesystemparser\FileSystemParser\Files";

		var foldersPath = string.Empty; 
		var maxDegreeOfParallelism = 0;
		var retryTimeInMs = 0;

		try
		{
			using var client = new NamedPipeClientStream(".", "FileSystemParserPipe", PipeDirection.InOut);

			Console.WriteLine($"Connecting...");
			client.Connect();

			string serverInputData;
			using (var reader = new StreamReader(client))
			{
				serverInputData = reader.ReadLine() ?? string.Empty;
			}

			var ipcMessage = JsonSerializer.Deserialize<IpcMessage>(serverInputData);

			if (ipcMessage == null)
			{
				return;
			}

			foldersPath = ipcMessage.Path;
			Console.WriteLine($"Received folders path from the server: {foldersPath}");

			retryTimeInMs = ipcMessage.CheckInterval;
			Console.WriteLine($"Received retry time in ms from the server: {retryTimeInMs}");

			maxDegreeOfParallelism = ipcMessage.MaximumConcurrentProcessing;
			Console.WriteLine($"Received maximum degree of parallelism from the server: {maxDegreeOfParallelism}");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error while reading IPC message. Error: {ex.Message}");
		}

		if (maxDegreeOfParallelism == 0)
		{
			maxDegreeOfParallelism = 1;
		}

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
	}
}