using System.Text.Json.Serialization;

namespace FileSystemParser.Console;

public class Quest
{
    [JsonPropertyName("components")] public required List<Component> Components { get; set; }
}

public class Component
{
    [JsonPropertyName("names")] public required string Names { get; set; }

    [JsonPropertyName("status")] public required int Status { get; set; }
}
