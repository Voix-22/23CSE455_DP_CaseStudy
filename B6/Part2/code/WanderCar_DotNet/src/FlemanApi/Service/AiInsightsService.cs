using System.ClientModel;
using FlemanApi.DTO;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;

namespace FlemanApi.Service;

// Requirement #3 — Microsoft.Extensions.AI, wired to a small, self-
// contained feature: a natural-language summary of the staff dashboard's
// fleet-availability numbers (GET /api/staff/dashboard/summary). Falls
// back to a plain templated summary — never throws — when no AI provider
// is configured, so the rest of the API never depends on an API key existing.
public class AiInsightsService : IAiInsightsService
{
    private readonly AiSettings _settings;
    private readonly ILogger<AiInsightsService> _logger;

    public AiInsightsService(IOptions<AiSettings> settings, ILogger<AiInsightsService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> GenerateFleetSummaryAsync(IReadOnlyList<CarTypeAvailabilityDTO> stats)
    {
        var fallback = BuildFallbackSummary(stats);

        if (string.IsNullOrWhiteSpace(_settings.ApiKey) ||
            string.Equals(_settings.Provider, "none", StringComparison.OrdinalIgnoreCase))
        {
            return $"{fallback} (AI summary unavailable — no AI provider configured; set Ai:Provider/Ai:ApiKey.)";
        }

        try
        {
            var client = BuildChatClient();
            var prompt = $"""
                You are a fleet operations assistant for a car rental company. In 2-3 short sentences,
                summarize the following per-category fleet availability for staff. Call out any category
                that's fully booked or has very low availability. Data (carType: total/available/booked/maintenance):
                {string.Join("; ", stats.Select(s => $"{s.CarTypeName}: {s.TotalCars}/{s.AvailableCars}/{s.BookedCars}/{s.MaintenanceCars}"))}
                """;

            var completion = await client.CompleteAsync(
                new List<ChatMessage> { new(ChatRole.User, prompt) });

            var text = completion.Message.Text;
            return string.IsNullOrWhiteSpace(text) ? fallback : text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI fleet summary failed, falling back to templated summary");
            return $"{fallback} (AI summary unavailable — provider call failed.)";
        }
    }

    private IChatClient BuildChatClient()
    {
        var options = string.IsNullOrWhiteSpace(_settings.Endpoint)
            ? null
            : new OpenAIClientOptions { Endpoint = new Uri(_settings.Endpoint) };

        var openAiClient = new OpenAIClient(new ApiKeyCredential(_settings.ApiKey), options);
        return openAiClient.AsChatClient(_settings.Model);
    }

    private static string BuildFallbackSummary(IReadOnlyList<CarTypeAvailabilityDTO> stats)
    {
        if (stats.Count == 0) return "No fleet data available.";

        var totalAvailable = stats.Sum(s => s.AvailableCars);
        var lowStock = stats.Where(s => s.AvailableCars == 0).Select(s => s.CarTypeName).ToList();

        var summary = $"{totalAvailable} car(s) available across {stats.Count} categories.";
        if (lowStock.Count > 0)
        {
            summary += $" Fully booked: {string.Join(", ", lowStock)}.";
        }
        return summary;
    }
}
