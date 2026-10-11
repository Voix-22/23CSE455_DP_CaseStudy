namespace FlemanApi.DTO;

public class HubDTO
{
    public long HubId { get; set; }
    public string HubName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public long CityId { get; set; }
    public long StateId { get; set; }
    public string Pincode { get; set; } = string.Empty;
    public string ContactNo { get; set; } = string.Empty;
    public string? Email { get; set; }
}
