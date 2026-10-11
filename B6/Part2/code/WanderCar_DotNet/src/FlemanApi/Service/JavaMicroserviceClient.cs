using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlemanApi.DTO;

namespace FlemanApi.Service;

public class JavaMicroserviceClient : IJavaMicroserviceClient
{
    // Java's LocalDateTime has no timezone concept at all — the default
    // System.Text.Json DateTime converter writes a "+05:30"/"Z" offset
    // whenever Kind is Local/Utc, which Jackson's LocalDateTimeDeserializer
    // then fails to parse ("unparsed text found"). This converter always
    // writes the plain wall-clock value, matching what LocalDateTime expects.
    private sealed class NaiveDateTimeConverter : JsonConverter<DateTime>
    {
        private const string Format = "yyyy-MM-ddTHH:mm:ss.ffffff";

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => DateTime.Parse(reader.GetString()!, CultureInfo.InvariantCulture);

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
    }

    // PostAsJsonAsync doesn't pick up the CamelCase policy configured for
    // MVC in Program.cs (that only applies to ASP.NET Core's own controller
    // serialization, not outgoing HttpClient calls) — without this it
    // defaults to PascalCase property names, which Jackson on the Java side
    // doesn't recognise (fail-on-unknown-properties is on by default there),
    // so every field came through as null and the request 500'd.
    private static readonly JsonSerializerOptions JavaJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new NaiveDateTimeConverter() },
    };

    private readonly HttpClient _httpClient;

    public JavaMicroserviceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetStatesAsync()
    {
        var response = await _httpClient.GetAsync("/api/states");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> GetHealthAsync()
    {
        // Not /actuator/health: SecurityConfig's anyRequest().authenticated()
        // catches every actuator endpoint too (management.endpoints.web
        // .exposure.include=* only decides which ones REGISTER, not their
        // auth requirement), so an anonymous call there 302s into the
        // Google OAuth2 login flow instead of returning JSON. "/profile" is
        // the one endpoint SecurityConfig explicitly permitAll()s that
        // still proves the app is up and responding.
        var response = await _httpClient.GetAsync("/profile");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(InvoicePdfRequestDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/invoices/pdf", request, JavaJsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }
}
