using System.Net;
using System.Security.Cryptography;
using FlemanApi.DTO;
using FlemanApi.Exceptions;
using FlemanApi.Models;
using FlemanApi.Repository;
using FlemanApi.Security;
using FlemanApi.Util;
using Microsoft.Extensions.Logging;

namespace FlemanApi.Service;

// Mirrors com.fleman.service.impl.BookingServiceImpl. A booking reserves a
// car TYPE at a hub for a date range, not any specific vehicle — the
// physical car is only assigned at staff hand-over (StaffService). See
// IAvailabilityRepository for the capacity/overlap check this relies on.
public class BookingService : IBookingService
{
    private static readonly BookingStatus[] ActiveStatuses = { BookingStatus.CONFIRMED, BookingStatus.ONGOING };
    private const string ConfirmationLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string ConfirmationDigits = "0123456789";
    private const int ConfirmationLetterCount = 3;
    private const int ConfirmationDigitCount = 5;

    private readonly IGenericRepository<BookingHeader, long> _bookings;
    private readonly IGenericRepository<BookingDetail, long> _bookingDetails;
    private readonly IGenericRepository<CarType, long> _carTypes;
    private readonly IGenericRepository<Customer, long> _customers;
    private readonly IGenericRepository<Hub, long> _hubs;
    private readonly IGenericRepository<Addon, long> _addons;
    private readonly IGenericRepository<Car, long> _cars;
    private readonly IAvailabilityRepository _availability;

    // Reusing the sibling services' own DTO builders instead of duplicating
    // that mapping logic here — one source of truth per entity type.
    private readonly IVehicleService _vehicleService;
    private readonly ICustomerService _customerService;
    private readonly ILocationService _locationService;
    private readonly IAddonService _addonService;
    private readonly IEmailService _emailService;
    private readonly IAuthenticatedUserAccessor _currentUser;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IGenericRepository<BookingHeader, long> bookings,
        IGenericRepository<BookingDetail, long> bookingDetails,
        IGenericRepository<CarType, long> carTypes,
        IGenericRepository<Customer, long> customers,
        IGenericRepository<Hub, long> hubs,
        IGenericRepository<Addon, long> addons,
        IGenericRepository<Car, long> cars,
        IAvailabilityRepository availability,
        IVehicleService vehicleService,
        ICustomerService customerService,
        ILocationService locationService,
        IAddonService addonService,
        IEmailService emailService,
        IAuthenticatedUserAccessor currentUser,
        ILogger<BookingService> logger)
    {
        _bookings = bookings;
        _bookingDetails = bookingDetails;
        _carTypes = carTypes;
        _customers = customers;
        _hubs = hubs;
        _addons = addons;
        _cars = cars;
        _availability = availability;
        _vehicleService = vehicleService;
        _customerService = customerService;
        _locationService = locationService;
        _addonService = addonService;
        _emailService = emailService;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<BookingResponseDTO> CreateBookingAsync(CreateBookingRequest request)
    {
        _logger.LogInformation("Booking process started");

        long customerId;
        if (_currentUser.IsAuthenticated)
        {
            if (_currentUser.GetCurrentRole() == "STAFF")
            {
                if (request.CustomerId is null) throw new ApiException("Customer is required.", HttpStatusCode.BadRequest);
                customerId = request.CustomerId.Value;
            }
            else
            {
                customerId = _currentUser.GetCurrentUserId();
            }
        }
        else
        {
            if (request.CustomerId is null) throw new ApiException("Customer is required.", HttpStatusCode.BadRequest);
            customerId = request.CustomerId.Value;
        }

        var carType = await _carTypes.GetByIdAsync(request.CarTypeId!.Value)
            ?? throw new ResourceNotFoundException($"Car type not found: {request.CarTypeId}");

        if (!(request.ReturnDatetime!.Value > request.PickupDatetime!.Value))
            throw new ApiException("Return date must be after pickup date", HttpStatusCode.BadRequest);

        // Faithfully mirrors the Java source: this checks request.CustomerId
        // (the raw request field), not the resolved customerId above — the
        // two can differ when a logged-in customer's JWT id doesn't match
        // whatever customerId they happened to submit in the body.
        if (!await _customers.ExistsAsync(c => c.CustomerId == request.CustomerId))
            throw new ResourceNotFoundException($"Customer not found: {request.CustomerId}");

        var dropHubId = request.DropHubId ?? request.PickupHubId!.Value;
        if (!await _hubs.ExistsAsync(h => h.HubId == request.PickupHubId))
            throw new ResourceNotFoundException($"Hub not found: {request.PickupHubId}");
        if (!await _hubs.ExistsAsync(h => h.HubId == dropHubId))
            throw new ResourceNotFoundException($"Hub not found: {dropHubId}");

        await AssertCategoryHasCapacityAsync(request.PickupHubId!.Value, request.CarTypeId!.Value,
            request.PickupDatetime.Value, request.ReturnDatetime.Value, null);

        var header = new BookingHeader
        {
            ConfirmationNo = await GenerateConfirmationNoAsync(),
            BookingDate = DateTime.Now,
            CustomerId = customerId,
            CarTypeId = carType.CarTypeId,
            CarId = null,
            PickupHubId = request.PickupHubId.Value,
            DropHubId = dropHubId,
            PickupDatetime = request.PickupDatetime.Value,
            ReturnDatetime = request.ReturnDatetime.Value,
            BookingStatus = BookingStatus.CONFIRMED,
            Remarks = request.Remarks ?? string.Empty,
        };

        var days = DaysBetween(request.PickupDatetime.Value, request.ReturnDatetime.Value);
        var rentalAmount = RentalRateCalculator.CalculateRentalAmount(carType, days);

        await _bookings.AddAsync(header);
        await _bookings.SaveChangesAsync();

        var addonAmount = 0.0;
        if (request.Addons is not null)
        {
            foreach (var line in request.Addons)
            {
                var addon = line.AddonId is null ? null : await _addons.GetByIdAsync(line.AddonId.Value);
                if (addon is null) continue;
                var subtotal = addon.DailyRate * (line.Quantity ?? 0) * days;
                await _bookingDetails.AddAsync(new BookingDetail
                {
                    BookingId = header.BookingId,
                    AddonId = addon.AddonId,
                    AddonRate = addon.DailyRate,
                    Quantity = line.Quantity,
                    Subtotal = subtotal,
                });
                addonAmount += subtotal;
            }
        }

        header.EstimatedAmount = rentalAmount + addonAmount;
        await _bookings.SaveChangesAsync();

        try
        {
            var customer = await _customers.GetByIdAsync(header.CustomerId);
            if (customer is not null)
            {
                var body = $"""
                    Dear {customer.FullName},

                    Your booking has been confirmed successfully.

                    Booking Details
                    -------------------------
                    Booking No : {header.ConfirmationNo}
                    Vehicle Type : {carType.CarTypeName}
                    Pickup Date : {header.PickupDatetime}
                    Return Date : {header.ReturnDatetime}
                    Total Amount : ₹{header.EstimatedAmount:F2}

                    Thank you for choosing Fleeman.

                    Regards,
                    Fleeman Team
                    """;
                await _emailService.SendEmailAsync(customer.Email, "Booking Confirmation - Fleeman", body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send booking confirmation email for {ConfirmationNo}", header.ConfirmationNo);
        }

        _logger.LogInformation("Booking process completed, confirmationNo={ConfirmationNo}", header.ConfirmationNo);
        return await ToResponseDtoAsync(header);
    }

    public async Task<BookingResponseDTO> GetBookingByConfirmationAsync(string confirmationNo)
    {
        var header = await FindHeaderAsync(confirmationNo);
        ValidateBookingAccess(header);
        return await ToResponseDtoAsync(header);
    }

    public async Task<BookingResponseDTO?> GetLastBookingForCustomerAsync(long customerId)
    {
        var mine = await GetActiveBookingsOrderedAsync(customerId);
        return mine.Count == 0 ? null : await ToResponseDtoAsync(mine[0]);
    }

    public async Task<IReadOnlyList<BookingResponseDTO>> GetBookingsForCustomerAsync(long customerId)
    {
        var mine = await GetActiveBookingsOrderedAsync(customerId);
        var result = new List<BookingResponseDTO>();
        foreach (var h in mine) result.Add(await ToResponseDtoAsync(h));
        return result;
    }

    public async Task<BookingResponseDTO> ModifyBookingAsync(string confirmationNo, ModifyBookingRequest request)
    {
        var header = await FindHeaderAsync(confirmationNo);
        ValidateBookingAccess(header);

        // Once a real car has been handed to the customer, the pickup leg
        // is history — only the still-pending return leg may still change.
        var pickupHubChanged = request.PickupHubId is not null && request.PickupHubId != header.PickupHubId;
        var pickupTimeChanged = request.PickupDatetime is not null && request.PickupDatetime != header.PickupDatetime;
        if ((pickupHubChanged || pickupTimeChanged) && header.CarId is not null)
        {
            throw new ApiException("Vehicle has already been handed over — pickup location and time can no longer be changed", HttpStatusCode.Conflict);
        }

        if (request.PickupHubId is not null)
        {
            if (!await _hubs.ExistsAsync(h => h.HubId == request.PickupHubId))
                throw new ResourceNotFoundException($"Hub not found: {request.PickupHubId}");
            header.PickupHubId = request.PickupHubId.Value;
        }
        if (request.DropHubId is not null)
        {
            if (!await _hubs.ExistsAsync(h => h.HubId == request.DropHubId))
                throw new ResourceNotFoundException($"Hub not found: {request.DropHubId}");
            header.DropHubId = request.DropHubId.Value;
        }
        if (request.PickupDatetime is not null) header.PickupDatetime = request.PickupDatetime.Value;
        if (request.ReturnDatetime is not null) header.ReturnDatetime = request.ReturnDatetime.Value;
        if (request.Remarks is not null) header.Remarks = request.Remarks;

        // Changing the reserved category: only while no real car has been
        // handed over yet.
        var typeChanged = request.CarTypeId is not null && request.CarTypeId != header.CarTypeId;
        if (typeChanged)
        {
            if (header.CarId is not null)
                throw new ApiException("Vehicle has already been handed over — category can no longer be changed", HttpStatusCode.Conflict);
            if (!await _carTypes.ExistsAsync(ct => ct.CarTypeId == request.CarTypeId))
                throw new ResourceNotFoundException($"Car type not found: {request.CarTypeId}");
            header.CarTypeId = request.CarTypeId!.Value;
        }

        // Any change to hub, dates, or category needs a fresh capacity
        // check against the booking's own final values, excluding itself
        // from the demand count. Skipped once a car is already assigned.
        var needsRecheck = typeChanged || request.PickupHubId is not null
            || request.PickupDatetime is not null || request.ReturnDatetime is not null;
        if (needsRecheck && header.CarId is null)
        {
            await AssertCategoryHasCapacityAsync(header.PickupHubId, header.CarTypeId,
                header.PickupDatetime, header.ReturnDatetime, header.BookingId);
        }

        // Replacing the add-on lines: null means "leave as-is", an empty
        // list means "every add-on was removed". Locked once a car has
        // been handed over, same as the pickup-hub/time guard above.
        if (request.Addons is not null && header.CarId is not null)
        {
            throw new ApiException("Vehicle has already been handed over — add-ons can no longer be changed", HttpStatusCode.Conflict);
        }

        var days = DaysBetween(header.PickupDatetime, header.ReturnDatetime);
        double addonAmount;
        if (request.Addons is not null)
        {
            var existingLines = await _bookingDetails.FindAsync(d => d.BookingId == header.BookingId);
            foreach (var line in existingLines) _bookingDetails.Remove(line);

            var total = 0.0;
            foreach (var line in request.Addons)
            {
                var addon = line.AddonId is null ? null : await _addons.GetByIdAsync(line.AddonId.Value);
                if (addon is null) continue;
                var subtotal = addon.DailyRate * (line.Quantity ?? 0) * days;
                await _bookingDetails.AddAsync(new BookingDetail
                {
                    BookingId = header.BookingId,
                    AddonId = addon.AddonId,
                    AddonRate = addon.DailyRate,
                    Quantity = line.Quantity,
                    Subtotal = subtotal,
                });
                total += subtotal;
            }
            addonAmount = total;
        }
        else
        {
            var currentLines = await _bookingDetails.FindAsync(d => d.BookingId == header.BookingId);
            addonAmount = currentLines.Sum(d => d.Subtotal ?? 0);
        }

        var currentCarType = await _carTypes.GetByIdAsync(header.CarTypeId);
        var rentalAmount = currentCarType is not null ? RentalRateCalculator.CalculateRentalAmount(currentCarType, days) : 0.0;
        header.EstimatedAmount = rentalAmount + addonAmount;

        await _bookings.SaveChangesAsync();
        return await ToResponseDtoAsync(header);
    }

    public async Task<BookingResponseDTO> CancelBookingAsync(string confirmationNo)
    {
        var header = await FindHeaderAsync(confirmationNo);
        ValidateBookingAccess(header);
        header.BookingStatus = BookingStatus.CANCELLED;

        // A car has only ever been touched if it was actually handed over —
        // release it back to the fleet in that case. Otherwise there's
        // nothing to release: the booking only ever held category capacity.
        if (header.CarId is not null)
        {
            var car = await _cars.GetByIdAsync(header.CarId.Value);
            if (car is not null)
            {
                car.Status = CarStatus.AVAILABLE;
                car.IsAvailable = true;
            }
        }

        await _bookings.SaveChangesAsync();
        return await ToResponseDtoAsync(header);
    }

    // Capacity (physical fleet of this type at this hub, minus maintenance)
    // minus demand (other CONFIRMED/ONGOING bookings whose dates overlap)
    // must leave at least one car free, or the reservation is rejected.
    private async Task AssertCategoryHasCapacityAsync(
        long hubId, long carTypeId, DateTime pickupDatetime, DateTime returnDatetime, long? excludeBookingId)
    {
        var capacity = await _availability.CountCapacityAsync(hubId, carTypeId);
        var overlapping = await _availability.CountOverlappingAsync(hubId, carTypeId, pickupDatetime, returnDatetime, excludeBookingId);
        if (capacity - overlapping <= 0)
        {
            throw new ApiException("That car type is no longer available at this hub for these dates", HttpStatusCode.Conflict);
        }
    }

    private async Task<BookingHeader> FindHeaderAsync(string confirmationNo)
    {
        var lower = confirmationNo.ToLower();
        return await _bookings.FirstOrDefaultAsync(b => b.ConfirmationNo.ToLower() == lower)
            ?? throw new ResourceNotFoundException("No booking found for that confirmation number");
    }

    private async Task<List<BookingHeader>> GetActiveBookingsOrderedAsync(long customerId)
    {
        var mine = await _bookings.FindAsync(b => b.CustomerId == customerId && b.BookingStatus != BookingStatus.CANCELLED);
        return mine.OrderByDescending(b => b.BookingDate).ToList();
    }

    // Fully random each time (3 letters + 5 digits, e.g. "QXR48213") —
    // re-rolls on the rare chance of a collision with an existing
    // confirmation number.
    private async Task<string> GenerateConfirmationNoAsync()
    {
        string candidate;
        do
        {
            var chars = new char[ConfirmationLetterCount + ConfirmationDigitCount];
            for (var i = 0; i < ConfirmationLetterCount; i++)
                chars[i] = ConfirmationLetters[RandomNumberGenerator.GetInt32(ConfirmationLetters.Length)];
            for (var i = 0; i < ConfirmationDigitCount; i++)
                chars[ConfirmationLetterCount + i] = ConfirmationDigits[RandomNumberGenerator.GetInt32(ConfirmationDigits.Length)];
            candidate = new string(chars);
        } while (await _bookings.ExistsAsync(b => b.ConfirmationNo.ToLower() == candidate.ToLower()));
        return candidate;
    }

    private static int DaysBetween(DateTime pickup, DateTime returnDt)
    {
        var hours = (returnDt - pickup).TotalHours;
        var fullDays = (long)Math.Ceiling(hours / 24.0);
        return (int)Math.Max(1, fullDays);
    }

    private void ValidateBookingAccess(BookingHeader booking)
    {
        var role = _currentUser.GetCurrentRole();
        if (role == "STAFF") return;

        var customerId = _currentUser.GetCurrentUserId();
        if (booking.CustomerId != customerId)
        {
            throw new ApiException("Access denied.", HttpStatusCode.Forbidden);
        }
    }

    // The .NET equivalent of the Java enrich(header) function — every
    // booking-related endpoint returns this same nested shape. No entity
    // relationships are traversed; every nested object is fetched by its
    // plain id column through the owning service.
    private async Task<BookingResponseDTO> ToResponseDtoAsync(BookingHeader header)
    {
        var dto = new BookingResponseDTO
        {
            BookingId = header.BookingId,
            ConfirmationNo = header.ConfirmationNo,
            BookingDate = header.BookingDate,
            CustomerId = header.CustomerId,
            CarId = header.CarId,
            PickupHubId = header.PickupHubId,
            DropHubId = header.DropHubId,
            PickupDatetime = header.PickupDatetime,
            ReturnDatetime = header.ReturnDatetime,
            ActualHandoverDatetime = header.ActualHandoverDatetime,
            ActualReturnDatetime = header.ActualReturnDatetime,
            HandoverFuelLevel = header.HandoverFuelLevel,
            BookingStatus = header.BookingStatus?.ToString(),
            EstimatedAmount = header.EstimatedAmount,
            Remarks = header.Remarks,
        };

        dto.CarType = await _vehicleService.GetCarTypeByIdAsync(header.CarTypeId);
        if (header.CarId is not null)
        {
            dto.Car = await _vehicleService.GetCarByIdAsync(header.CarId.Value);
        }
        dto.Customer = await _customerService.GetCustomerByIdAsync(header.CustomerId);
        dto.PickupHub = await _locationService.GetHubByIdAsync(header.PickupHubId);
        dto.DropHub = await _locationService.GetHubByIdAsync(header.DropHubId);

        var allAddons = await _addonService.GetAddonsAsync();
        var lines = await _bookingDetails.FindAsync(d => d.BookingId == header.BookingId);
        dto.AddonLines = lines.Select(detail => new BookingDetailDTO
        {
            BookingDetailId = detail.BookingDetailId,
            BookingId = header.BookingId,
            AddonId = detail.AddonId,
            AddonRate = detail.AddonRate,
            Quantity = detail.Quantity,
            Subtotal = detail.Subtotal,
            Addon = allAddons.FirstOrDefault(a => a.AddonId == detail.AddonId),
        }).ToList();

        return dto;
    }
}
