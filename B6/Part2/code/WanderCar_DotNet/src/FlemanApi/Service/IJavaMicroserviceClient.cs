using FlemanApi.DTO;

namespace FlemanApi.Service;

// Requirement #11 — calls the still-running Java Spring Boot backend
// (fleman-backend, port 8080 by default) over HTTP, demonstrating a
// strangler-pattern integration. The rest of this API is fully
// self-sufficient — this is opt-in, not a hard runtime dependency.
public interface IJavaMicroserviceClient
{
    Task<string> GetStatesAsync();
    Task<string> GetHealthAsync();
    Task<byte[]> GenerateInvoicePdfAsync(InvoicePdfRequestDto request);
}
