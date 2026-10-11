namespace FlemanApi.DTO;

public class StaffAuthResponse
{
    public string Token { get; set; } = string.Empty;
    public StaffDTO Staff { get; set; } = null!;

    public StaffAuthResponse() { }
    public StaffAuthResponse(string token, StaffDTO staff)
    {
        Token = token;
        Staff = staff;
    }
}
