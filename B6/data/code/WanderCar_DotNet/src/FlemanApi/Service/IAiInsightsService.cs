using FlemanApi.DTO;

namespace FlemanApi.Service;

public interface IAiInsightsService
{
    Task<string> GenerateFleetSummaryAsync(IReadOnlyList<CarTypeAvailabilityDTO> stats);
}
