namespace FlemanApi.DTO;

public class StaffDTO
{
    public string Username { get; set; } = string.Empty;
    public string Hub { get; set; } = string.Empty;
    public long HubId { get; set; }

    public StaffDTO() { }
    public StaffDTO(string username, string hub, long hubId)
    {
        Username = username;
        Hub = hub;
        HubId = hubId;
    }
}
