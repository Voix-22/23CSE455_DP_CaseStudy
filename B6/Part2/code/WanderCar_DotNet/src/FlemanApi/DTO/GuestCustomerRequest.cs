namespace FlemanApi.DTO;

// Matches what the guest-checkout page collects — upsertGuestCustomer
// finds-or-creates a Customer row from this so the booking always carries
// a real customerId.
public class GuestCustomerRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address1 { get; set; }
    public string? Zip { get; set; }
    public string? DrivingLicenseNo { get; set; }
    public string? GovtIdDocument { get; set; }
    public string? GovtIdDocumentName { get; set; }
    public string? GovtIdDocumentType { get; set; }
}
