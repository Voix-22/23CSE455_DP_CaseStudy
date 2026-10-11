using System.Net;
using FlemanApi.DTO;
using FlemanApi.Exceptions;
using FlemanApi.Models;
using FlemanApi.Service;
using FlemanApi.Tests.TestHelpers;
using FluentAssertions;

namespace FlemanApi.Tests.Services;

[TestFixture]
public class MaintenanceServiceTests
{
    // A Saturday morning; "today" is 2026-10-10.
    private static readonly DateTime FixedNow = new(2026, 10, 10, 9, 30, 0);
    private static readonly DateTime Today = FixedNow.Date;

    private List<MaintenanceSchedule> _schedules = null!;
    private List<Car> _cars = null!;
    private FixedTimeProvider _clock = null!;
    private MaintenanceService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _schedules = new List<MaintenanceSchedule>();
        _cars = new List<Car>
        {
            new() { CarId = 1, CarTypeId = 1, HubId = 1, VehicleNumber = "MH01AA1111", Status = CarStatus.AVAILABLE, IsAvailable = true },
            new() { CarId = 2, CarTypeId = 1, HubId = 1, VehicleNumber = "MH01AA2222", Status = CarStatus.BOOKED, IsAvailable = false },
        };
        _clock = new FixedTimeProvider(FixedNow);

        _service = new MaintenanceService(
            MockRepositoryFactory.Create<MaintenanceSchedule, long>(
                _schedules, m => m.MaintenanceScheduleId, (m, id) => m.MaintenanceScheduleId = id).Object,
            MockRepositoryFactory.Create<Car, long>(_cars, c => c.CarId).Object,
            _clock);
    }

    private static ScheduleMaintenanceRequest MakeRequest(long? carId, DateTime date) => new()
    {
        CarId = carId,
        MaintenanceType = MaintenanceType.SERVICE,
        ScheduledDate = date,
    };

    private MaintenanceSchedule Given(MaintenanceStatus status, DateTime date, long carId = 1, string? notes = null)
    {
        var schedule = new MaintenanceSchedule
        {
            MaintenanceScheduleId = _schedules.Count + 1,
            CarId = carId,
            MaintenanceType = MaintenanceType.SERVICE,
            ScheduledDate = date,
            Status = status,
            Notes = notes,
        };
        _schedules.Add(schedule);
        return schedule;
    }

    private Car CarById(long id) => _cars.Single(c => c.CarId == id);

    private static async Task AssertStatusAsync(Func<Task> act, HttpStatusCode expected) =>
        (await act.Should().ThrowAsync<ApiException>()).Which.Status.Should().Be(expected);

    // ---- Schedule ----------------------------------------------------------

    [Test]
    public async Task ScheduleAsync_FutureDate_CreatesScheduledItem()
    {
        var request = MakeRequest(1, Today.AddDays(3).AddHours(15));
        request.MaintenanceType = MaintenanceType.INSPECTION;
        request.Notes = "  annual check  ";

        var result = await _service.ScheduleAsync(request);

        result.MaintenanceScheduleId.Should().Be(1);
        result.CarId.Should().Be(1);
        result.MaintenanceType.Should().Be(MaintenanceType.INSPECTION);
        result.Status.Should().Be(MaintenanceStatus.SCHEDULED);
        result.ScheduledDate.Should().Be(Today.AddDays(3), "the time part is dropped");
        result.Notes.Should().Be("annual check");
        result.StartedAt.Should().BeNull();
        _schedules.Should().ContainSingle();
        CarById(1).Status.Should().Be(CarStatus.AVAILABLE, "scheduling alone does not take the car out of service");
    }

    [Test]
    public async Task ScheduleAsync_Today_IsAllowed()
    {
        var result = await _service.ScheduleAsync(MakeRequest(1, Today));

        result.ScheduledDate.Should().Be(Today);
        result.Status.Should().Be(MaintenanceStatus.SCHEDULED);
    }

    [Test]
    public async Task ScheduleAsync_Yesterday_ThrowsBadRequest()
    {
        await AssertStatusAsync(() => _service.ScheduleAsync(MakeRequest(1, Today.AddDays(-1))), HttpStatusCode.BadRequest);

        _schedules.Should().BeEmpty();
    }

    [Test]
    public async Task ScheduleAsync_UnknownCar_ThrowsNotFound()
    {
        await AssertStatusAsync(() => _service.ScheduleAsync(MakeRequest(99, Today)), HttpStatusCode.NotFound);

        _schedules.Should().BeEmpty();
    }

    [Test]
    public async Task ScheduleAsync_MissingCarId_ThrowsBadRequest()
    {
        await AssertStatusAsync(() => _service.ScheduleAsync(MakeRequest(null, Today)), HttpStatusCode.BadRequest);

        _schedules.Should().BeEmpty();
    }

    [Test]
    public async Task ScheduleAsync_CarCurrentlyBooked_IsStillAllowed()
    {
        var result = await _service.ScheduleAsync(MakeRequest(2, Today.AddDays(1)));

        result.Status.Should().Be(MaintenanceStatus.SCHEDULED);
        CarById(2).Status.Should().Be(CarStatus.BOOKED);
    }

    // ---- Start -------------------------------------------------------------

    [Test]
    public async Task StartAsync_ScheduledAndCarAvailable_TakesCarOutOfService()
    {
        var schedule = Given(MaintenanceStatus.SCHEDULED, Today);

        var result = await _service.StartAsync(schedule.MaintenanceScheduleId, new MaintenanceTransitionRequest());

        result.Status.Should().Be(MaintenanceStatus.IN_PROGRESS);
        result.StartedAt.Should().Be(FixedNow);
        CarById(1).Status.Should().Be(CarStatus.UNDER_MAINTENANCE);
        CarById(1).IsAvailable.Should().BeFalse();
    }

    [Test]
    public async Task StartAsync_CarBooked_ThrowsConflictAndChangesNothing()
    {
        var schedule = Given(MaintenanceStatus.SCHEDULED, Today, carId: 2);

        await AssertStatusAsync(
            () => _service.StartAsync(schedule.MaintenanceScheduleId, new MaintenanceTransitionRequest { Notes = "ignored" }),
            HttpStatusCode.Conflict);

        schedule.Status.Should().Be(MaintenanceStatus.SCHEDULED);
        schedule.StartedAt.Should().BeNull();
        schedule.Notes.Should().BeNull();
        CarById(2).Status.Should().Be(CarStatus.BOOKED);
    }

    [Test]
    public async Task StartAsync_FromCompleted_ThrowsConflictAndChangesNothing()
    {
        var schedule = Given(MaintenanceStatus.COMPLETED, Today);

        await AssertStatusAsync(
            () => _service.StartAsync(schedule.MaintenanceScheduleId, new MaintenanceTransitionRequest()),
            HttpStatusCode.Conflict);

        schedule.Status.Should().Be(MaintenanceStatus.COMPLETED);
        schedule.StartedAt.Should().BeNull();
        CarById(1).Status.Should().Be(CarStatus.AVAILABLE);
    }

    [Test]
    public async Task StartAsync_UnknownSchedule_ThrowsNotFound()
    {
        await AssertStatusAsync(() => _service.StartAsync(42, new MaintenanceTransitionRequest()), HttpStatusCode.NotFound);
    }

    [Test]
    public async Task StartAsync_WithNotes_AppendsLabelledNote()
    {
        var schedule = Given(MaintenanceStatus.SCHEDULED, Today, notes: "oil change");

        var result = await _service.StartAsync(
            schedule.MaintenanceScheduleId, new MaintenanceTransitionRequest { Notes = "bay 2" });

        result.Notes.Should().Be("oil change | Start: bay 2");
    }

    [Test]
    public async Task StartAsync_NotesWouldOverflowColumn_ThrowsBadRequestAndChangesNothing()
    {
        var existing = new string('x', 495);
        var schedule = Given(MaintenanceStatus.SCHEDULED, Today, notes: existing);

        await AssertStatusAsync(
            () => _service.StartAsync(schedule.MaintenanceScheduleId, new MaintenanceTransitionRequest { Notes = "bay 2" }),
            HttpStatusCode.BadRequest);

        schedule.Status.Should().Be(MaintenanceStatus.SCHEDULED);
        schedule.StartedAt.Should().BeNull();
        schedule.Notes.Should().Be(existing);
        CarById(1).Status.Should().Be(CarStatus.AVAILABLE);
        CarById(1).IsAvailable.Should().BeTrue();
    }

    // ---- Complete ----------------------------------------------------------

    [Test]
    public async Task CompleteAsync_InProgress_ReleasesCar()
    {
        var schedule = Given(MaintenanceStatus.SCHEDULED, Today);
        await _service.StartAsync(schedule.MaintenanceScheduleId, new MaintenanceTransitionRequest());
        _clock.SetNow(FixedNow.AddHours(4));

        var result = await _service.CompleteAsync(schedule.MaintenanceScheduleId, new MaintenanceTransitionRequest());

        result.Status.Should().Be(MaintenanceStatus.COMPLETED);
        result.StartedAt.Should().Be(FixedNow);
        result.CompletedAt.Should().Be(FixedNow.AddHours(4));
        CarById(1).Status.Should().Be(CarStatus.AVAILABLE);
        CarById(1).IsAvailable.Should().BeTrue();
    }

    [Test]
    public async Task CompleteAsync_FromScheduled_ThrowsConflictAndChangesNothing()
    {
        var schedule = Given(MaintenanceStatus.SCHEDULED, Today, carId: 2);

        await AssertStatusAsync(
            () => _service.CompleteAsync(schedule.MaintenanceScheduleId, new MaintenanceTransitionRequest()),
            HttpStatusCode.Conflict);

        schedule.Status.Should().Be(MaintenanceStatus.SCHEDULED);
        schedule.CompletedAt.Should().BeNull();
        CarById(2).Status.Should().Be(CarStatus.BOOKED, "a refused transition must not release a car it never took");
    }

    // ---- Cancel ------------------------------------------------------------

    [Test]
    public async Task CancelAsync_FromScheduled_LeavesCarAlone()
    {
        var schedule = Given(MaintenanceStatus.SCHEDULED, Today, carId: 2);

        var result = await _service.CancelAsync(
            schedule.MaintenanceScheduleId, new MaintenanceTransitionRequest { Notes = "not needed" });

        result.Status.Should().Be(MaintenanceStatus.CANCELLED);
        result.Notes.Should().Be("Cancel: not needed");
        CarById(2).Status.Should().Be(CarStatus.BOOKED);
        CarById(2).IsAvailable.Should().BeFalse();
    }

    [Test]
    public async Task CancelAsync_FromInProgress_ReleasesCar()
    {
        var schedule = Given(MaintenanceStatus.SCHEDULED, Today);
        await _service.StartAsync(schedule.MaintenanceScheduleId, new MaintenanceTransitionRequest());

        var result = await _service.CancelAsync(schedule.MaintenanceScheduleId, new MaintenanceTransitionRequest());

        result.Status.Should().Be(MaintenanceStatus.CANCELLED);
        result.CompletedAt.Should().BeNull();
        CarById(1).Status.Should().Be(CarStatus.AVAILABLE);
        CarById(1).IsAvailable.Should().BeTrue();
    }

    // ---- Due / overdue -----------------------------------------------------

    [Test]
    public async Task GetDueAsync_ReturnsOnlyScheduledItemsForToday()
    {
        Given(MaintenanceStatus.SCHEDULED, Today.AddDays(-1));
        var dueToday = Given(MaintenanceStatus.SCHEDULED, Today);
        Given(MaintenanceStatus.SCHEDULED, Today.AddDays(1));
        Given(MaintenanceStatus.IN_PROGRESS, Today);
        Given(MaintenanceStatus.COMPLETED, Today);
        Given(MaintenanceStatus.CANCELLED, Today);

        var due = await _service.GetDueAsync();

        due.Select(d => d.MaintenanceScheduleId).Should().Equal(dueToday.MaintenanceScheduleId);
    }

    [Test]
    public async Task GetOverdueAsync_ReturnsOnlyScheduledItemsBeforeToday()
    {
        var yesterday = Given(MaintenanceStatus.SCHEDULED, Today.AddDays(-1));
        var lastWeek = Given(MaintenanceStatus.SCHEDULED, Today.AddDays(-7));
        Given(MaintenanceStatus.SCHEDULED, Today);
        Given(MaintenanceStatus.IN_PROGRESS, Today.AddDays(-2));
        Given(MaintenanceStatus.COMPLETED, Today.AddDays(-2));
        Given(MaintenanceStatus.CANCELLED, Today.AddDays(-2));

        var overdue = await _service.GetOverdueAsync();

        overdue.Select(d => d.MaintenanceScheduleId)
            .Should().Equal(lastWeek.MaintenanceScheduleId, yesterday.MaintenanceScheduleId);
    }

    [Test]
    public async Task DueAndOverdue_AtMidnight_TodayIsDueAndYesterdayIsOverdue()
    {
        var yesterday = Given(MaintenanceStatus.SCHEDULED, Today.AddDays(-1));
        var today = Given(MaintenanceStatus.SCHEDULED, Today);
        _clock.SetNow(Today);

        var due = await _service.GetDueAsync();
        var overdue = await _service.GetOverdueAsync();

        due.Select(d => d.MaintenanceScheduleId).Should().Equal(today.MaintenanceScheduleId);
        overdue.Select(d => d.MaintenanceScheduleId).Should().Equal(yesterday.MaintenanceScheduleId);
    }

    [Test]
    public async Task DueAndOverdue_OneTickBeforeMidnight_StillTheSameDay()
    {
        var today = Given(MaintenanceStatus.SCHEDULED, Today);
        var tomorrow = Given(MaintenanceStatus.SCHEDULED, Today.AddDays(1));
        _clock.SetNow(Today.AddDays(1).AddTicks(-1));

        var due = await _service.GetDueAsync();
        var overdue = await _service.GetOverdueAsync();

        due.Select(d => d.MaintenanceScheduleId).Should().Equal(today.MaintenanceScheduleId);
        overdue.Should().BeEmpty();

        // One tick later the day rolls over.
        _clock.SetNow(Today.AddDays(1));
        (await _service.GetDueAsync()).Select(d => d.MaintenanceScheduleId).Should().Equal(tomorrow.MaintenanceScheduleId);
        (await _service.GetOverdueAsync()).Select(d => d.MaintenanceScheduleId).Should().Equal(today.MaintenanceScheduleId);
    }
}
