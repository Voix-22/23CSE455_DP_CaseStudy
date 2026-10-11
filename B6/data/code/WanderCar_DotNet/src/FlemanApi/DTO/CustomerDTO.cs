namespace FlemanApi.DTO;

// Never carries the password hash — this is what login/register/getById
// all return to the browser.
public class CustomerDTO
{
    public long CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? DrivingLicenseNo { get; set; }
    public string? PassportNo { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public long? CityId { get; set; }
    public long? StateId { get; set; }
    public string? Pincode { get; set; }
    public string? Status { get; set; }

    // The full base64 document is deliberately NOT here — it would bloat
    // every customer read. See GET /api/customers/{id}/govt-id for that.
    public string? GovtIdDocumentName { get; set; }
    public string? GovtIdDocumentType { get; set; }
}
