namespace FlemanApi.Service;

// Bound from the "Ai" section of appsettings. Provider "none" (the
// default) means no API key is configured — AiInsightsService then falls
// back to a plain, non-AI summary instead of failing the request.
public class AiSettings
{
    public const string SectionName = "Ai";

    public string Provider { get; set; } = "none";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";

    // Optional — set for an OpenAI-compatible endpoint that isn't
    // api.openai.com (Azure OpenAI, a local Ollama/LM Studio server, etc).
    public string Endpoint { get; set; } = string.Empty;
}
