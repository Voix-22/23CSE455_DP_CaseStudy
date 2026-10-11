namespace FlemanApi.DTO;

// Separate from CustomerDTO on purpose — the base64 payload is only fetched
// when something actually needs to render/download it, not on every read.
public class GovtIdDocumentDTO
{
    public string? GovtIdDocument { get; set; }
    public string? GovtIdDocumentName { get; set; }
    public string? GovtIdDocumentType { get; set; }
}
