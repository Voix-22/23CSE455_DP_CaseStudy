using System.Net;
using FlemanApi.DTO;
using FlemanApi.Exceptions;
using FlemanApi.Models;
using FlemanApi.Repository;
using FlemanApi.Security;
using FlemanApi.Service;
using FlemanApi.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlemanApi.Tests.Services;

[TestFixture]
public class CustomerServiceTests
{
    private List<Customer> _customers = null!;
    private List<City> _cities = null!;
    private List<State> _states = null!;
    private Mock<IJwtTokenService> _jwtTokenServiceMock = null!;
    private Mock<IEmailService> _emailServiceMock = null!;
    private CustomerService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _customers = new List<Customer>();
        _cities = new List<City> { new() { CityId = 1, CityName = "Mumbai", StateId = 1 } };
        _states = new List<State> { new() { StateId = 1, StateName = "Maharashtra" } };

        var customersRepo = MockRepositoryFactory.Create<Customer, long>(_customers, c => c.CustomerId, (c, id) => c.CustomerId = id);
        var citiesRepo = MockRepositoryFactory.Create<City, long>(_cities, c => c.CityId);
        var statesRepo = MockRepositoryFactory.Create<State, long>(_states, s => s.StateId);

        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _jwtTokenServiceMock.Setup(j => j.GenerateToken(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("fake-jwt-token");
        _emailServiceMock = new Mock<IEmailService>();

        _service = new CustomerService(
            customersRepo.Object,
            citiesRepo.Object,
            statesRepo.Object,
            _jwtTokenServiceMock.Object,
            _emailServiceMock.Object,
            TestMapperFactory.Create(),
            NullLogger<CustomerService>.Instance);
    }

    private Customer AddCustomer(string email, string? passwordHash, long id = 1)
    {
        var customer = new Customer
        {
            CustomerId = id,
            FullName = "Jane Doe",
            Email = email,
            PasswordHash = passwordHash,
            Status = CustomerStatus.ACTIVE,
            Provider = AuthProvider.LOCAL,
        };
        _customers.Add(customer);
        return customer;
    }

    // ---------- LoginAsync ----------

    [Test]
    public async Task LoginAsync_CorrectPassword_ReturnsAuthResponseWithToken()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("correct-password");
        AddCustomer("jane@example.com", hash);

        var result = await _service.LoginAsync(new LoginRequest { Email = "jane@example.com", Password = "correct-password" });

        result.Token.Should().Be("fake-jwt-token");
        result.Customer.Email.Should().Be("jane@example.com");
        _jwtTokenServiceMock.Verify(j => j.GenerateToken(1, "jane@example.com", "CUSTOMER"), Times.Once);
    }

    [Test]
    public async Task LoginAsync_WrongPassword_ThrowsUnauthorized()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("correct-password");
        AddCustomer("jane@example.com", hash);

        var act = async () => await _service.LoginAsync(new LoginRequest { Email = "jane@example.com", Password = "wrong-password" });

        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.Status.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task LoginAsync_NoPasswordHashOnGuestAccount_ThrowsUnauthorized()
    {
        AddCustomer("guest@example.com", null);

        var act = async () => await _service.LoginAsync(new LoginRequest { Email = "guest@example.com", Password = "anything" });

        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.Status.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task LoginAsync_UnknownEmail_ThrowsUnauthorized()
    {
        var act = async () => await _service.LoginAsync(new LoginRequest { Email = "nobody@example.com", Password = "anything" });

        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.Status.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task LoginAsync_EmailIsCaseInsensitive()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("correct-password");
        AddCustomer("jane@example.com", hash);

        var result = await _service.LoginAsync(new LoginRequest { Email = "JANE@EXAMPLE.COM", Password = "correct-password" });

        result.Should().NotBeNull();
    }

    // ---------- RegisterAsync ----------

    [Test]
    public async Task RegisterAsync_BrandNewCustomer_CreatesCustomerAndSendsWelcomeEmail()
    {
        var request = new RegisterRequest { FullName = "New Guy", Email = "newguy@example.com", Password = "secret1" };

        var result = await _service.RegisterAsync(request);

        result.Customer.Email.Should().Be("newguy@example.com");
        result.Customer.FullName.Should().Be("New Guy");
        _customers.Should().ContainSingle(c => c.Email == "newguy@example.com");
        _customers.Single().PasswordHash.Should().NotBeNullOrEmpty();
        _emailServiceMock.Verify(e => e.SendEmailAsync("newguy@example.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task RegisterAsync_GuestAccountUpgradePath_UpgradesInPlace()
    {
        var guest = AddCustomer("guest@example.com", null);
        guest.FullName = "Guest Name";

        var request = new RegisterRequest { FullName = "Real Name", Email = "guest@example.com", Password = "secret1" };

        var result = await _service.RegisterAsync(request);

        _customers.Should().ContainSingle();
        var upgraded = _customers.Single();
        upgraded.FullName.Should().Be("Real Name");
        upgraded.PasswordHash.Should().NotBeNullOrEmpty();
        upgraded.Provider.Should().Be(AuthProvider.LOCAL);
        upgraded.Status.Should().Be(CustomerStatus.ACTIVE);
        result.Customer.FullName.Should().Be("Real Name");
    }

    [Test]
    public async Task RegisterAsync_DuplicateEmailWithPassword_ThrowsConflict()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("existing-password");
        AddCustomer("jane@example.com", hash);

        var request = new RegisterRequest { FullName = "Jane Doe", Email = "jane@example.com", Password = "secret1" };

        var act = async () => await _service.RegisterAsync(request);

        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.Status.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task RegisterAsync_WelcomeEmailThrows_StillReturnsAuthResponse()
    {
        _emailServiceMock.Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));
        var request = new RegisterRequest { FullName = "New Guy", Email = "newguy@example.com", Password = "secret1" };

        var result = await _service.RegisterAsync(request);

        result.Should().NotBeNull();
        result.Customer.Email.Should().Be("newguy@example.com");
    }

    // ---------- StaffLoginAsync ----------

    [Test]
    public async Task StaffLoginAsync_CorrectHardcodedCredentials_ReturnsStaffAuthResponse()
    {
        var result = await _service.StaffLoginAsync(new StaffLoginRequest { Username = "Team2@gmail.com", Password = "123456789" });

        result.Token.Should().Be("fake-jwt-token");
        result.Staff.Username.Should().Be("Team2@gmail.com");
        result.Staff.HubId.Should().Be(1);
        _jwtTokenServiceMock.Verify(j => j.GenerateToken(1, "Team2@gmail.com", "STAFF"), Times.Once);
    }

    [Test]
    public async Task StaffLoginAsync_WrongCredentials_ThrowsUnauthorized()
    {
        var act = async () => await _service.StaffLoginAsync(new StaffLoginRequest { Username = "Team2@gmail.com", Password = "wrong" });

        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.Status.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task StaffLoginAsync_WrongUsername_ThrowsUnauthorized()
    {
        var act = async () => await _service.StaffLoginAsync(new StaffLoginRequest { Username = "someone-else@gmail.com", Password = "123456789" });

        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.Status.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task StaffLoginAsync_UsernameIsCaseInsensitive()
    {
        var result = await _service.StaffLoginAsync(new StaffLoginRequest { Username = "team2@gmail.com", Password = "123456789" });

        result.Should().NotBeNull();
    }

    // ---------- UpsertGuestCustomerAsync ----------

    [Test]
    public async Task UpsertGuestCustomerAsync_NewEmail_CreatesGuestCustomer()
    {
        var request = new GuestCustomerRequest
        {
            FirstName = "Guest",
            LastName = "User",
            Email = "guest@example.com",
            Phone = "9999999999",
        };

        var result = await _service.UpsertGuestCustomerAsync(request);

        result.FullName.Should().Be("Guest User");
        result.Email.Should().Be("guest@example.com");
        _customers.Should().ContainSingle(c => c.Email == "guest@example.com" && c.PasswordHash == null);
    }

    [Test]
    public async Task UpsertGuestCustomerAsync_ExistingEmail_MergesIntoExistingCustomer()
    {
        var existing = AddCustomer("guest@example.com", null);
        existing.FullName = "Old Name";
        existing.Phone = "1111111111";

        var request = new GuestCustomerRequest
        {
            FirstName = "New",
            LastName = "Name",
            Email = "guest@example.com",
            Phone = "2222222222",
        };

        var result = await _service.UpsertGuestCustomerAsync(request);

        _customers.Should().ContainSingle();
        result.FullName.Should().Be("New Name");
        _customers.Single().Phone.Should().Be("2222222222");
    }

    [Test]
    public async Task UpsertGuestCustomerAsync_ExistingCustomerBlankPhoneInRequest_KeepsOldPhone()
    {
        var existing = AddCustomer("guest@example.com", null);
        existing.Phone = "1111111111";

        var request = new GuestCustomerRequest { FirstName = "New", Email = "guest@example.com", Phone = "" };

        await _service.UpsertGuestCustomerAsync(request);

        _customers.Single().Phone.Should().Be("1111111111");
    }

    // ---------- UpdateCustomerAsync ----------

    [Test]
    public async Task UpdateCustomerAsync_MissingCustomer_ThrowsResourceNotFound()
    {
        var request = new UpdateCustomerRequest { CustomerId = 999, FullName = "Someone" };

        var act = async () => await _service.UpdateCustomerAsync(request);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Test]
    public async Task UpdateCustomerAsync_PartialPatch_OnlyUpdatesProvidedFields()
    {
        var existing = AddCustomer("jane@example.com", null);
        existing.FullName = "Old Name";
        existing.Phone = "1111111111";

        var request = new UpdateCustomerRequest { CustomerId = existing.CustomerId, Phone = "2222222222" };

        var result = await _service.UpdateCustomerAsync(request);

        result.FullName.Should().Be("Old Name");
        result.Phone.Should().Be("2222222222");
    }

    [Test]
    public async Task UpdateCustomerAsync_UnknownCityId_ThrowsResourceNotFound()
    {
        var existing = AddCustomer("jane@example.com", null);
        var request = new UpdateCustomerRequest { CustomerId = existing.CustomerId, CityId = 999 };

        var act = async () => await _service.UpdateCustomerAsync(request);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    // ---------- GetCustomerByIdAsync ----------

    [Test]
    public async Task GetCustomerByIdAsync_MissingCustomer_ReturnsNull()
    {
        var result = await _service.GetCustomerByIdAsync(999);

        result.Should().BeNull();
    }

    // ---------- IssueCustomerToken ----------

    [Test]
    public void IssueCustomerToken_DelegatesToJwtTokenService()
    {
        var customer = new Customer { CustomerId = 5, Email = "x@example.com" };

        var token = _service.IssueCustomerToken(customer);

        token.Should().Be("fake-jwt-token");
        _jwtTokenServiceMock.Verify(j => j.GenerateToken(5, "x@example.com", "CUSTOMER"), Times.Once);
    }
}
