using System.Net;
using FlemanApi.DTO;
using FlemanApi.Exceptions;
using FlemanApi.Models;
using FlemanApi.Repository;
using FlemanApi.Service.Maintenance;
using FlemanApi.Validators;

namespace FlemanApi.Service;

// CH03 - fleet-maintenance scheduling. Which transition is legal, and what
// it does to the car, is decided by the state objects in
// Service/Maintenance; this class never branches on Status.
public class MaintenanceService : IMaintenanceService
{
    private const int MaxNotesLength = ScheduleMaintenanceRequestValidator.MaxNotesLength;

    private readonly IGenericRepository<MaintenanceSchedule, long> _schedules;
    private readonly IGenericRepository<Car, long> _cars;
    private readonly TimeProvider _timeProvider;

    public MaintenanceService(
        IGenericRepository<MaintenanceSchedule, long> schedules,
        IGenericRepository<Car, long> cars,
        TimeProvider timeProvider)
    {
        _schedules = schedules;
        _cars = cars;
        _timeProvider = timeProvider;
    }

    // Server-local wall clock, like the rest of the application.
    private DateTime Now => _timeProvider.GetLocalNow().DateTime;
    private DateTime Today => Now.Date;

    public async Task<MaintenanceScheduleDTO> ScheduleAsync(ScheduleMaintenanceRequest request)
    {
        if (request.CarId is null)
            throw new ApiException("carId is required", HttpStatusCode.BadRequest);
        if (request.MaintenanceType is null)
            throw new ApiException("maintenanceType is required", HttpStatusCode.BadRequest);
        if (request.ScheduledDate is null)
            throw new ApiException("scheduledDate is required", HttpStatusCode.BadRequest);

        var car = await _cars.GetByIdAsync(request.CarId.Value)
            ?? throw new ResourceNotFoundException($"Car not found: {request.CarId}");

        // The column is a date, so the time part is dropped.
        var scheduledDate = request.ScheduledDate.Value.Date;
        if (scheduledDate < Today)
            throw new ApiException("scheduledDate cannot be in the past", HttpStatusCode.BadRequest);

        var schedule = new MaintenanceSchedule
        {
            CarId = car.CarId,
            MaintenanceType = request.MaintenanceType.Value,
            ScheduledDate = scheduledDate,
            Status = MaintenanceStatus.SCHEDULED,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
        };

        await _schedules.AddAsync(schedule);
        await _schedules.SaveChangesAsync();
        return ToDto(schedule);
    }

    public async Task<MaintenanceScheduleDTO> GetByIdAsync(long id) => ToDto(await FindScheduleAsync(id));

    public async Task<IReadOnlyList<MaintenanceScheduleDTO>> GetDueAsync()
    {
        var today = Today;
        var due = await _schedules.FindAsync(m =>
            m.Status == MaintenanceStatus.SCHEDULED && m.ScheduledDate == today);
        return due.OrderBy(m => m.MaintenanceScheduleId).Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<MaintenanceScheduleDTO>> GetOverdueAsync()
    {
        var today = Today;
        var overdue = await _schedules.FindAsync(m =>
            m.Status == MaintenanceStatus.SCHEDULED && m.ScheduledDate < today);
        return overdue.OrderBy(m => m.ScheduledDate).ThenBy(m => m.MaintenanceScheduleId).Select(ToDto).ToList();
    }

    public Task<MaintenanceScheduleDTO> StartAsync(long id, MaintenanceTransitionRequest request) =>
        TransitionAsync(id, "Start", request, context => context.Start());

    public Task<MaintenanceScheduleDTO> CompleteAsync(long id, MaintenanceTransitionRequest request) =>
        TransitionAsync(id, "Complete", request, context => context.Complete());

    public Task<MaintenanceScheduleDTO> CancelAsync(long id, MaintenanceTransitionRequest request) =>
        TransitionAsync(id, "Cancel", request, context => context.Cancel());

    // Client of the State pattern: builds the context and lets the current
    // state decide. Everything that can reject the request runs before
    // anything is changed, so a refused transition leaves no trace.
    private async Task<MaintenanceScheduleDTO> TransitionAsync(
        long id, string label, MaintenanceTransitionRequest request, Action<MaintenanceContext> transition)
    {
        var schedule = await FindScheduleAsync(id);
        var car = await _cars.GetByIdAsync(schedule.CarId)
            ?? throw new ResourceNotFoundException($"Car not found: {schedule.CarId}");

        var notes = AppendNote(schedule.Notes, label, request.Notes);

        transition(new MaintenanceContext(schedule, car, Now));

        schedule.Notes = notes;
        await _schedules.SaveChangesAsync();
        return ToDto(schedule);
    }

    private static string? AppendNote(string? existing, string label, string? note)
    {
        if (string.IsNullOrWhiteSpace(note)) return existing;

        var labelled = $"{label}: {note.Trim()}";
        var combined = string.IsNullOrWhiteSpace(existing) ? labelled : $"{existing} | {labelled}";
        if (combined.Length > MaxNotesLength)
            throw new ApiException($"notes would exceed {MaxNotesLength} characters", HttpStatusCode.BadRequest);
        return combined;
    }

    private async Task<MaintenanceSchedule> FindScheduleAsync(long id) =>
        await _schedules.GetByIdAsync(id)
            ?? throw new ResourceNotFoundException($"Maintenance schedule not found: {id}");

    private static MaintenanceScheduleDTO ToDto(MaintenanceSchedule schedule) => new()
    {
        MaintenanceScheduleId = schedule.MaintenanceScheduleId,
        CarId = schedule.CarId,
        MaintenanceType = schedule.MaintenanceType,
        ScheduledDate = schedule.ScheduledDate,
        Status = schedule.Status,
        StartedAt = schedule.StartedAt,
        CompletedAt = schedule.CompletedAt,
        Notes = schedule.Notes,
    };
}
