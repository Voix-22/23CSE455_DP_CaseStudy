namespace FlemanApi.DTO;

public class AirportRequest
{
    public string AirportName { get; set; } = string.Empty;
    public string AirportCode { get; set; } = string.Empty;
    public long? CityId { get; set; }
    public long? StateId { get; set; }
    public long? HubId { get; set; }
}
