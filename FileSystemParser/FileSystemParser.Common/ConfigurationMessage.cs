namespace FileSystemParser.Common;

public class ConfigurationMessage
{
    public required string Path { get; init; }
    public int CheckInterval { get; init; }
    public int MaximumConcurrentProcessing { get; init; }
}