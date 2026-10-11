using FlemanApi.DTO;

namespace FlemanApi.Service;

// The "Employee"-pattern exemplar for requirement #9: a person entity
// (Customer, the closest analogue this domain has to Employee) with real
// business logic — login/register/guest-upgrade/staffLogin — layered on
// top of generic CRUD (get-by-id, update) for the plain parts.
public interface ICustomerService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<StaffAuthResponse> StaffLoginAsync(StaffLoginRequest request);
    Task<CustomerDTO> UpdateCustomerAsync(UpdateCustomerRequest request);
    Task<CustomerDTO?> GetCustomerByIdAsync(long customerId);
    Task<CustomerDTO> UpsertGuestCustomerAsync(GuestCustomerRequest request);
    Task<GovtIdDocumentDTO> GetGovtIdDocumentAsync(long customerId);

    // Used by OAuth callback / DataSeeder-style flows to issue a token for
    // an already-resolved customer, mirroring OAuth2SuccessHandler.
    string IssueCustomerToken(FlemanApi.Models.Customer customer);
}
