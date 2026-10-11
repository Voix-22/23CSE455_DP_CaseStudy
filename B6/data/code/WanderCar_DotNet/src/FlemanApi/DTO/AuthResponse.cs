namespace FlemanApi.DTO;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public CustomerDTO Customer { get; set; } = null!;

    public AuthResponse() { }
    public AuthResponse(string token, CustomerDTO customer)
    {
        Token = token;
        Customer = customer;
    }
}
