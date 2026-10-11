namespace FlemanApi.Security;

// Bound from the "Jwt" section of appsettings — mirrors Java's
// app.jwt.secret / app.jwt.expiration-ms.
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public long ExpirationMs { get; set; } = 86_400_000;
}
