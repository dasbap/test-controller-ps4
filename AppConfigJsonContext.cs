// AppConfigJsonContext.cs
using System.Text.Json;
using System.Text.Json.Serialization;

namespace controller_ps4
{
    public class AppConfig
    {
        [JsonPropertyName("keyMappings")]
        public Dictionary<string, JsonElement> KeyMappings { get; set; } = new();
    }

    [JsonSerializable(typeof(AppConfig))]
    [JsonSourceGenerationOptions(
        WriteIndented = true,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        GenerationMode = JsonSourceGenerationMode.Serialization)]
    public partial class AppConfigJsonContext : JsonSerializerContext
    {
    }
}