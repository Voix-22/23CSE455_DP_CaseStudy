namespace FlemanApi.DTO;

public class AirportDTO
{
    public long AirportId { get; set; }
    public string AirportCode { get; set; } = string.Empty;
    public string AirportName { get; set; } = string.Empty;
    public long CityId { get; set; }
    public long StateId { get; set; }
    public long HubId { get; set; }
}
