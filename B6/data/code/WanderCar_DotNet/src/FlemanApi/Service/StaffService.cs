using System.Net;
using FlemanApi.DTO;
using FlemanApi.Exceptions;
using FlemanApi.Models;
using FlemanApi.Repository;
using FlemanApi.Util;
using Microsoft.Extensions.Logging;

namespace FlemanApi.Service;

// Mirrors com.fleman.service.impl.StaffServiceImpl.
public class StaffService : IStaffService
{
    // Fuel level is stored in quarters of a tank: 25/50/75/100.
    private const int FuelLevelPerQuarter = 25;
    private const double FuelChargePerQuarter = 2.0;

    private readonly IGenericRepository<BookingHeader, long> _bookings;
    private readonly IGenericRepository<BookingDetail, long> _bookingDetails;
    private readonly IGenericRepository<Car, long> _cars;
    private readonly IGenericRepository<CarType, long> _carTypes;
    private readonly IGenericRepository<Customer, long> _customers;
    private readonly IGenericRepository<Addon, long> _addons;
    private readonly IGenericRepository<InvoiceHeader, long> _invoiceHeaders;
    private readonly IGenericRepository<InvoiceDetail, long> _invoiceDetails;
    private readonly IAvailabilityRepository _availability;
    private readonly IBookingService _bookingService;
    private readonly IVehicleService _vehicleService;
    private readonly IEmailService _emailService;
    private readonly IInvoicePdfService _invoicePdfService;
    private readonly ILogger<StaffService> _logger;

    public StaffService(
        IGenericRepository<BookingHeader, long> bookings,
        IGenericRepository<BookingDetail, long> bookingDetails,
        IGenericRepository<Car, long> cars,
        IGenericRepository<CarType, long> carTypes,
        IGenericRepository<Customer, long> customers,
        IGenericRepository<Addon, long> addons,
        IGenericRepository<InvoiceHeader, long> invoiceHeaders,
        IGenericRepository<InvoiceDetail, long> invoiceDetails,
        IAvailabilityRepository availability,
        IBookingService bookingService,
        IVehicleService vehicleService,
        IEmailService emailService,
        IInvoicePdfService invoicePdfService,
        ILogger<StaffService> logger)
    {
        _bookings = bookings;
        _bookingDetails = bookingDetails;
        _cars = cars;
        _carTypes = carTypes;
        _customers = customers;
        _addons = addons;
        _invoiceHeaders = invoiceHeaders;
        _invoiceDetails = invoiceDetails;
        _availability = availability;
        _bookingService = bookingService;
        _vehicleService = vehicleService;
        _emailService = emailService;
        _invoicePdfService = invoicePdfService;
        _logger = logger;
    }

    public async Task<BookingResponseDTO> HandoverVehicleAsync(string confirmationNo, HandoverRequest request)
    {
        var header = await FindHeaderAsync(confirmationNo);

        if (header.CarId is not null)
            throw new ApiException("This booking already has a vehicle handed over", HttpStatusCode.Conflict);
        if (request.CarId is null)
            throw new ApiException("Pick a vehicle to hand over", HttpStatusCode.BadRequest);

        // The car staff picked must actually fit this booking — right hub,
        // right reserved category, and physically free right now.
        var car = await _cars.GetByIdAsync(request.CarId.Value)
            ?? throw new ResourceNotFoundException($"Car not found: {request.CarId}");
        if (car.HubId != header.PickupHubId)
            throw new ApiException("That vehicle isn't at this booking's pickup hub", HttpStatusCode.Conflict);
        if (car.CarTypeId != header.CarTypeId)
            throw new ApiException("That vehicle isn't the reserved category for this booking", HttpStatusCode.Conflict);
        if (car.Status != CarStatus.AVAILABLE)
            throw new ApiException("That vehicle is no longer available", HttpStatusCode.Conflict);

        car.Status = CarStatus.BOOKED;
        car.IsAvailable = false;
        if (request.FuelStatus is not null) car.FuelLevel = request.FuelStatus;

        header.CarId = car.CarId;
        header.BookingStatus = BookingStatus.ONGOING;
        header.ActualHandoverDatetime = DateTime.Now;
        // Snapshot onto the booking — Car.FuelLevel gets overwritten again
        // at return, so this is the only place the return-time fuel-
        // shortfall charge can still read it from.
        header.HandoverFuelLevel = car.FuelLevel;
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            var existing = header.Remarks ?? string.Empty;
            header.Remarks = $"{existing} | Handover: {request.Notes}".Trim();
        }

        await _bookings.SaveChangesAsync();

        return await _bookingService.GetBookingByConfirmationAsync(confirmationNo);
    }

    public async Task<InvoiceResponseDTO> ProcessReturnAsync(string confirmationNo, ProcessReturnRequest request)
    {
        var header = await FindHeaderAsync(confirmationNo);
        if (header.CarId is null)
            throw new ApiException("This booking hasn't been handed over yet — nothing to return", HttpStatusCode.Conflict);

        var car = await _cars.GetByIdAsync(header.CarId.Value)
            ?? throw new ResourceNotFoundException($"Car not found: {header.CarId}");
        var carType = await _carTypes.GetByIdAsync(car.CarTypeId)
            ?? throw new ResourceNotFoundException($"Car type not found: {car.CarTypeId}");
        var customer = await _customers.GetByIdAsync(header.CustomerId)
            ?? throw new ResourceNotFoundException($"Customer not found: {header.CustomerId}");
        var lines = await _bookingDetails.FindAsync(d => d.BookingId == header.BookingId);

        // Bill against what actually happened — hand-over time and right
        // now — not the originally booked pickup/return window.
        var actualHandover = header.ActualHandoverDatetime ?? header.PickupDatetime;
        var actualReturn = DateTime.Now;
        header.ActualReturnDatetime = actualReturn;

        var days = DaysBetween(actualHandover, actualReturn);
        var rentalAmount = RentalRateCalculator.CalculateRentalAmount(carType, days);
        // Re-priced against the actual days billed, not the subtotal frozen
        // on the line at booking time — an early return shortens rentalAmount
        // above the same way, so add-ons need to shrink with it too instead
        // of still billing for the originally planned (longer) stay.
        var addonAmount = lines.Sum(l => (l.AddonRate ?? 0) * (l.Quantity ?? 0) * days);
        var extraCharge = request.ExtraChargeAmount ?? 0.0;

        // Fuel shortfall: tank tracked in quarters (25/50/75/100). Only
        // charged when it comes back lower than handed over with.
        var handoverFuelLevel = header.HandoverFuelLevel;
        var returnFuelLevel = request.FuelStatus ?? car.FuelLevel;
        var fuelQuartersShort = handoverFuelLevel is not null && returnFuelLevel is not null
            ? Math.Max(0, (handoverFuelLevel.Value - returnFuelLevel.Value) / FuelLevelPerQuarter)
            : 0;
        var fuelCharge = fuelQuartersShort * FuelChargePerQuarter;

        var totalAmount = rentalAmount + addonAmount + extraCharge + fuelCharge;

        var invoice = new InvoiceHeader
        {
            InvoiceDate = DateTime.Now,
            HandoverDatetime = actualHandover,
            ReturnDatetime = actualReturn,
            HandoverFuelLevel = handoverFuelLevel,
            ReturnFuelLevel = returnFuelLevel,
            FuelCharge = fuelCharge,
            BookingId = header.BookingId,
            CustomerName = customer.FullName,
            Phone = customer.Phone,
            Email = customer.Email,
            VehicleNumber = car.VehicleNumber,
            PickupHub = header.PickupHubId,
            DropHub = header.DropHubId,
            RentalAmount = rentalAmount,
            AddonAmount = addonAmount,
            TotalAmount = totalAmount,
            PaymentType = request.PaymentType,
            // Assumes payment is settled at the return desk when a payment
            // type is given; otherwise left PENDING for a separate collection step.
            PaymentStatus = request.PaymentType is not null ? PaymentStatus.PAID : PaymentStatus.PENDING,
            ExtraMiles = request.ExtraMiles,
            ExtraChargeAmount = extraCharge,
            DamageNotes = request.DamageNotes,
            Days = days,
        };
        await _invoiceHeaders.AddAsync(invoice);
        await _invoiceHeaders.SaveChangesAsync();

        invoice.PaymentReference = $"PAY-{invoice.InvoiceId}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()[9..]}";
        await _invoiceHeaders.SaveChangesAsync();

        foreach (var line in lines)
        {
            // AddonId is a plain logical reference — look up the current
            // name fresh rather than trusting anything cached on the line.
            var addon = await _addons.GetByIdAsync(line.AddonId);
            await _invoiceDetails.AddAsync(new InvoiceDetail
            {
                InvoiceId = invoice.InvoiceId,
                AddonName = addon?.AddonName ?? string.Empty,
                Quantity = line.Quantity,
                AddonRate = line.AddonRate,
                Subtotal = (line.AddonRate ?? 0) * (line.Quantity ?? 0) * days,
            });
        }
        await _invoiceDetails.SaveChangesAsync();

        header.BookingStatus = BookingStatus.COMPLETED;

        // The "add one back to the category" step — this car goes back to
        // AVAILABLE, so it counts again everywhere fleet availability is computed.
        car.Status = CarStatus.AVAILABLE;
        car.IsAvailable = true;
        if (request.FuelStatus is not null) car.FuelLevel = request.FuelStatus;
        var extraMiles = request.ExtraMiles ?? 0;
        car.Odometer = (car.Odometer ?? 0) + extraMiles;

        await _bookings.SaveChangesAsync();

        try
        {
            var pdf = await _invoicePdfService.GenerateInvoiceAsync(invoice.InvoiceId);
            var bookingDto = await _bookingService.GetBookingByConfirmationAsync(confirmationNo);
            var invoiceDto = ToDto(invoice);
            await _emailService.SendBookingInvoiceAsync(customer.Email, customer.FullName, bookingDto, invoiceDto, pdf);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send return invoice email for {ConfirmationNo}", confirmationNo);
        }

        return ToDto(invoice);
    }

    public async Task<IReadOnlyList<CarDTO>> GetAvailableCarsForHandoverAsync(string confirmationNo)
    {
        var header = await FindHeaderAsync(confirmationNo);
        var cars = await _cars.FindAsync(c =>
            c.HubId == header.PickupHubId && c.CarTypeId == header.CarTypeId && c.Status == CarStatus.AVAILABLE);

        var result = new List<CarDTO>();
        foreach (var car in cars) result.Add(await _vehicleService.GetCarByIdAsync(car.CarId));
        return result;
    }

    public async Task<IReadOnlyList<BookingResponseDTO>> GetAllBookingsAsync()
    {
        // Reuses BookingService's own DTO builder rather than duplicating
        // that mapping here — one source of truth per entity type.
        var all = (await _bookings.GetAllAsync()).OrderByDescending(h => h.BookingDate).ToList();
        var result = new List<BookingResponseDTO>();
        foreach (var h in all) result.Add(await _bookingService.GetBookingByConfirmationAsync(h.ConfirmationNo));
        return result;
    }

    public async Task<IReadOnlyList<CarTypeAvailabilityDTO>> GetDashboardAsync(long? hubId)
    {
        // The custom aggregate query does the actual counting in one
        // grouped query rather than pulling every Car row into memory.
        var counts = await _availability.CountByCarTypeAsync(hubId);
        var result = new List<CarTypeAvailabilityDTO>();
        foreach (var p in counts)
        {
            var carType = await _carTypes.GetByIdAsync(p.CarTypeId);
            result.Add(new CarTypeAvailabilityDTO
            {
                CarTypeId = p.CarTypeId,
                CarTypeName = carType?.CarTypeName ?? "Unknown",
                TotalCars = p.TotalCars,
                AvailableCars = p.AvailableCars,
                BookedCars = p.BookedCars,
                MaintenanceCars = p.MaintenanceCars,
            });
        }
        return result;
    }

    private async Task<BookingHeader> FindHeaderAsync(string confirmationNo)
    {
        var lower = confirmationNo.ToLower();
        return await _bookings.FirstOrDefaultAsync(b => b.ConfirmationNo.ToLower() == lower)
            ?? throw new ResourceNotFoundException("No booking found for that confirmation number");
    }

    private static int DaysBetween(DateTime pickup, DateTime returnDt)
    {
        var hours = (returnDt - pickup).TotalHours;
        var fullDays = (long)Math.Ceiling(hours / 24.0);
        return (int)Math.Max(1, fullDays);
    }

    private static InvoiceResponseDTO ToDto(InvoiceHeader inv) => new()
    {
        InvoiceId = inv.InvoiceId,
        InvoiceDate = inv.InvoiceDate,
        BookingId = inv.BookingId,
        CustomerName = inv.CustomerName,
        Phone = inv.Phone,
        Email = inv.Email,
        VehicleNumber = inv.VehicleNumber,
        PickupHub = inv.PickupHub,
        DropHub = inv.DropHub,
        HandoverDatetime = inv.HandoverDatetime,
        ReturnDatetime = inv.ReturnDatetime,
        HandoverFuelLevel = inv.HandoverFuelLevel,
        ReturnFuelLevel = inv.ReturnFuelLevel,
        FuelCharge = inv.FuelCharge,
        RentalAmount = inv.RentalAmount,
        AddonAmount = inv.AddonAmount,
        TotalAmount = inv.TotalAmount,
        PaymentType = inv.PaymentType?.ToString(),
        PaymentReference = inv.PaymentReference,
        PaymentStatus = inv.PaymentStatus?.ToString(),
        ExtraMiles = inv.ExtraMiles,
        ExtraChargeAmount = inv.ExtraChargeAmount,
        DamageNotes = inv.DamageNotes,
        Days = inv.Days,
    };
}
