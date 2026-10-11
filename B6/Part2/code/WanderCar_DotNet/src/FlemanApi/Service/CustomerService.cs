using System.Net;
using AutoMapper;
using FlemanApi.DTO;
using FlemanApi.Exceptions;
using FlemanApi.Models;
using FlemanApi.Repository;
using FlemanApi.Security;
using Microsoft.Extensions.Logging;

namespace FlemanApi.Service;

// Mirrors com.fleman.service.impl.CustomerServiceImpl — see ICustomerService
// for why Customer plays the "Employee" role here.
public class CustomerService : ICustomerService
{
    // Every hub in the seed data is Hub #1 (Mumbai) for a mock staff
    // session — there is no real staff/hub-assignment table, matching the
    // Java app's own hardcoded single-credential staff login.
    private const string DefaultStaffHubName = "WanderCar — Mumbai Hub";
    private const long DefaultStaffHubId = 1L;
    private const long StaffId = 1L;
    private const string StaffEmail = "Team2@gmail.com";
    private const string StaffPassword = "123456789";

    private readonly IGenericRepository<Customer, long> _customers;
    private readonly IGenericRepository<City, long> _cities;
    private readonly IGenericRepository<State, long> _states;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(
        IGenericRepository<Customer, long> customers,
        IGenericRepository<City, long> cities,
        IGenericRepository<State, long> states,
        IJwtTokenService jwtTokenService,
        IEmailService emailService,
        IMapper mapper,
        ILogger<CustomerService> logger)
    {
        _customers = customers;
        _cities = cities;
        _states = states;
        _jwtTokenService = jwtTokenService;
        _emailService = emailService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var customer = await _customers.FirstOrDefaultAsync(c => c.Email.ToLower() == request.Email.ToLower())
            ?? throw new ApiException("Invalid email or password", HttpStatusCode.Unauthorized);

        if (customer.PasswordHash is null || !BCrypt.Net.BCrypt.Verify(request.Password, customer.PasswordHash))
        {
            throw new ApiException("Invalid email or password", HttpStatusCode.Unauthorized);
        }

        var token = _jwtTokenService.GenerateToken(customer.CustomerId, customer.Email, "CUSTOMER");
        return new AuthResponse(token, _mapper.Map<CustomerDTO>(customer));
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existing = await _customers.FirstOrDefaultAsync(c => c.Email.ToLower() == request.Email.ToLower());

        if (existing is not null)
        {
            if (existing.PasswordHash is not null)
            {
                throw new ApiException("An account with this email already exists", HttpStatusCode.Conflict);
            }

            // Guest account being claimed — upgrade in place rather than
            // creating a duplicate customer row.
            existing.FullName = request.FullName;
            existing.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            existing.Provider = AuthProvider.LOCAL;
            existing.EmailVerified = false;
            existing.Status = CustomerStatus.ACTIVE;

            await _customers.SaveChangesAsync();

            var upgradeToken = _jwtTokenService.GenerateToken(existing.CustomerId, existing.Email, "CUSTOMER");
            return new AuthResponse(upgradeToken, _mapper.Map<CustomerDTO>(existing));
        }

        var customer = new Customer
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Status = CustomerStatus.ACTIVE,
            Provider = AuthProvider.LOCAL,
            EmailVerified = false,
        };
        await _customers.AddAsync(customer);
        await _customers.SaveChangesAsync();

        try
        {
            var body = $"""
                Dear {customer.FullName},

                Welcome to WonderCar!

                Your account has been created successfully.

                You can now log in and book rental vehicles from our platform.

                Thank you for choosing WonderCar.

                Regards,
                Gryffindors Team
                """;
            await _emailService.SendEmailAsync(customer.Email, "Welcome to WonderCar", body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send welcome email to {Email}", customer.Email);
        }

        var token = _jwtTokenService.GenerateToken(customer.CustomerId, customer.Email, "CUSTOMER");
        return new AuthResponse(token, _mapper.Map<CustomerDTO>(customer));
    }

    public Task<StaffAuthResponse> StaffLoginAsync(StaffLoginRequest request)
    {
        var username = request.Username?.Trim() ?? string.Empty;
        var password = request.Password ?? string.Empty;

        if (!string.Equals(StaffEmail, username, StringComparison.OrdinalIgnoreCase) || StaffPassword != password)
        {
            throw new ApiException("Invalid staff email or password", HttpStatusCode.Unauthorized);
        }

        var token = _jwtTokenService.GenerateToken(StaffId, StaffEmail, "STAFF");
        var staff = new StaffDTO(username, DefaultStaffHubName, DefaultStaffHubId);
        return Task.FromResult(new StaffAuthResponse(token, staff));
    }

    public async Task<CustomerDTO> UpdateCustomerAsync(UpdateCustomerRequest request)
    {
        var customer = await _customers.GetByIdAsync(request.CustomerId)
            ?? throw new ResourceNotFoundException($"Customer not found: {request.CustomerId}");

        if (request.FullName is not null) customer.FullName = request.FullName;
        if (request.Phone is not null) customer.Phone = request.Phone;
        if (request.DateOfBirth is not null) customer.DateOfBirth = request.DateOfBirth;
        if (request.DrivingLicenseNo is not null) customer.DrivingLicenseNo = request.DrivingLicenseNo;
        if (request.PassportNo is not null) customer.PassportNo = request.PassportNo;
        if (request.Address1 is not null) customer.Address1 = request.Address1;
        if (request.Address2 is not null) customer.Address2 = request.Address2;
        if (request.Pincode is not null) customer.Pincode = request.Pincode;

        if (request.CityId is not null)
        {
            if (!await _cities.ExistsAsync(c => c.CityId == request.CityId))
                throw new ResourceNotFoundException($"City not found: {request.CityId}");
            customer.CityId = request.CityId;
        }
        if (request.StateId is not null)
        {
            if (!await _states.ExistsAsync(s => s.StateId == request.StateId))
                throw new ResourceNotFoundException($"State not found: {request.StateId}");
            customer.StateId = request.StateId;
        }
        if (request.GovtIdDocument is not null)
        {
            customer.GovtIdDocument = request.GovtIdDocument;
            customer.GovtIdDocumentName = request.GovtIdDocumentName;
            customer.GovtIdDocumentType = request.GovtIdDocumentType;
        }

        await _customers.SaveChangesAsync();
        return _mapper.Map<CustomerDTO>(customer);
    }

    public async Task<CustomerDTO?> GetCustomerByIdAsync(long customerId)
    {
        var customer = await _customers.GetByIdAsync(customerId);
        return customer is null ? null : _mapper.Map<CustomerDTO>(customer);
    }

    public async Task<CustomerDTO> UpsertGuestCustomerAsync(GuestCustomerRequest request)
    {
        var existing = await _customers.FirstOrDefaultAsync(c => c.Email.ToLower() == request.Email.ToLower());

        if (existing is not null)
        {
            existing.FullName = $"{request.FirstName} {request.LastName ?? string.Empty}".Trim();
            if (!string.IsNullOrWhiteSpace(request.Phone)) existing.Phone = request.Phone;
            if (!string.IsNullOrWhiteSpace(request.Address1)) existing.Address1 = request.Address1;
            if (!string.IsNullOrWhiteSpace(request.DrivingLicenseNo)) existing.DrivingLicenseNo = request.DrivingLicenseNo;
            if (!string.IsNullOrWhiteSpace(request.GovtIdDocument))
            {
                existing.GovtIdDocument = request.GovtIdDocument;
                existing.GovtIdDocumentName = request.GovtIdDocumentName;
                existing.GovtIdDocumentType = request.GovtIdDocumentType;
            }
            await _customers.SaveChangesAsync();
            return _mapper.Map<CustomerDTO>(existing);
        }

        var customer = new Customer
        {
            FullName = $"{request.FirstName} {request.LastName ?? string.Empty}".Trim(),
            Email = request.Email,
            Phone = request.Phone ?? string.Empty,
            DrivingLicenseNo = request.DrivingLicenseNo ?? string.Empty,
            Address1 = request.Address1 ?? string.Empty,
            Address2 = string.Empty,
            Pincode = request.Zip ?? string.Empty,
            Status = CustomerStatus.ACTIVE,
            Provider = AuthProvider.LOCAL,
            EmailVerified = false,
            GovtIdDocument = request.GovtIdDocument,
            GovtIdDocumentName = request.GovtIdDocumentName,
            GovtIdDocumentType = request.GovtIdDocumentType,
            // No PasswordHash — a guest checkout never sets one; LoginAsync
            // correctly rejects this account until the customer registers.
        };
        await _customers.AddAsync(customer);
        await _customers.SaveChangesAsync();
        return _mapper.Map<CustomerDTO>(customer);
    }

    public async Task<GovtIdDocumentDTO> GetGovtIdDocumentAsync(long customerId)
    {
        var customer = await _customers.GetByIdAsync(customerId)
            ?? throw new ResourceNotFoundException($"Customer not found: {customerId}");
        return new GovtIdDocumentDTO
        {
            GovtIdDocument = customer.GovtIdDocument,
            GovtIdDocumentName = customer.GovtIdDocumentName,
            GovtIdDocumentType = customer.GovtIdDocumentType,
        };
    }

    public string IssueCustomerToken(Customer customer) =>
        _jwtTokenService.GenerateToken(customer.CustomerId, customer.Email, "CUSTOMER");
}
