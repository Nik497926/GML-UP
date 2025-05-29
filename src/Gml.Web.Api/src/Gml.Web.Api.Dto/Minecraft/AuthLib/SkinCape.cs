using Newtonsoft.Json;

namespace Gml.Web.Api.Dto.Minecraft.AuthLib;

public class SkinCape
{
    [JsonProperty("url")] 
    public string Url { get; set; }
    
    [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
    public SkinMetadata? Metadata { get; set; }
}

public class SkinMetadata
{
    [JsonProperty("model")] 
    public string Model { get; set; }
}
