namespace FlemanApi.DTO;

// A partial patch — any null field here is left unchanged on the existing
// Customer row.
public class UpdateCustomerRequest
{
    public long CustomerId { get; set; }
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? DrivingLicenseNo { get; set; }
    public string? PassportNo { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public long? CityId { get; set; }
    public long? StateId { get; set; }
    public string? Pincode { get; set; }
    public string? GovtIdDocument { get; set; }
    public string? GovtIdDocumentName { get; set; }
    public string? GovtIdDocumentType { get; set; }
}
