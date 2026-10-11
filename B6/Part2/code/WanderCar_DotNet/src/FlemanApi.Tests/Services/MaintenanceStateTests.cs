using System.Net;
using FlemanApi.Exceptions;
using FlemanApi.Models;
using FlemanApi.Service.Maintenance;
using FluentAssertions;

namespace FlemanApi.Tests.Services;

// The State pattern on its own: no service, no repository, no mocks.
[TestFixture]
public class MaintenanceStateTests
{
    private static readonly DateTime Now = new(2026, 10, 10, 9, 30, 0);

    private static MaintenanceContext MakeContext(
        MaintenanceStatus status, CarStatus carStatus, out MaintenanceSchedule schedule, out Car car)
    {
        schedule = new MaintenanceSchedule
        {
            MaintenanceScheduleId = 1,
            CarId = 7,
            MaintenanceType = MaintenanceType.SERVICE,
            ScheduledDate = Now.Date,
            Status = status,
        };
        car = new Car
        {
            CarId = 7,
            VehicleNumber = "MH01AA1111",
            Status = carStatus,
            IsAvailable = carStatus == CarStatus.AVAILABLE,
        };
        return new MaintenanceContext(schedule, car, Now);
    }

    private static void AssertConflictAndNothingChanged(
        Action act, MaintenanceSchedule schedule, Car car, MaintenanceStatus status, CarStatus carStatus)
    {
        act.Should().Throw<ApiException>().Which.Status.Should().Be(HttpStatusCode.Conflict);

        schedule.Status.Should().Be(status);
        schedule.StartedAt.Should().BeNull();
        schedule.CompletedAt.Should().BeNull();
        car.Status.Should().Be(carStatus);
        car.IsAvailable.Should().Be(carStatus == CarStatus.AVAILABLE);
    }

    [TestCase(MaintenanceStatus.SCHEDULED, typeof(ScheduledState))]
    [TestCase(MaintenanceStatus.IN_PROGRESS, typeof(InProgressState))]
    [TestCase(MaintenanceStatus.COMPLETED, typeof(CompletedState))]
    [TestCase(MaintenanceStatus.CANCELLED, typeof(CancelledState))]
    public void Context_PicksStateMatchingPersistedStatus(MaintenanceStatus status, Type expectedState)
    {
        var context = MakeContext(status, CarStatus.AVAILABLE, out _, out _);

        context.State.Should().BeOfType(expectedState);
        context.State.Status.Should().Be(status);
    }

    // ---- SCHEDULED ---------------------------------------------------------

    [Test]
    public void Scheduled_Start_AvailableCar_MovesToInProgressAndTakesCarOutOfService()
    {
        var context = MakeContext(MaintenanceStatus.SCHEDULED, CarStatus.AVAILABLE, out var schedule, out var car);

        context.Start();

        schedule.Status.Should().Be(MaintenanceStatus.IN_PROGRESS);
        schedule.StartedAt.Should().Be(Now);
        context.State.Should().BeSameAs(InProgressState.Instance);
        car.Status.Should().Be(CarStatus.UNDER_MAINTENANCE);
        car.IsAvailable.Should().BeFalse();
    }

    [TestCase(CarStatus.BOOKED)]
    [TestCase(CarStatus.UNDER_MAINTENANCE)]
    public void Scheduled_Start_CarNotAvailable_ThrowsConflictAndChangesNothing(CarStatus carStatus)
    {
        var context = MakeContext(MaintenanceStatus.SCHEDULED, carStatus, out var schedule, out var car);

        AssertConflictAndNothingChanged(context.Start, schedule, car, MaintenanceStatus.SCHEDULED, carStatus);
    }

    [Test]
    public void Scheduled_Complete_ThrowsConflictAndChangesNothing()
    {
        var context = MakeContext(MaintenanceStatus.SCHEDULED, CarStatus.AVAILABLE, out var schedule, out var car);

        AssertConflictAndNothingChanged(context.Complete, schedule, car, MaintenanceStatus.SCHEDULED, CarStatus.AVAILABLE);
    }

    [Test]
    public void Scheduled_Cancel_MovesToCancelledAndLeavesCarAlone()
    {
        var context = MakeContext(MaintenanceStatus.SCHEDULED, CarStatus.BOOKED, out var schedule, out var car);

        context.Cancel();

        schedule.Status.Should().Be(MaintenanceStatus.CANCELLED);
        context.State.Should().BeSameAs(CancelledState.Instance);
        car.Status.Should().Be(CarStatus.BOOKED);
        car.IsAvailable.Should().BeFalse();
    }

    // ---- IN_PROGRESS -------------------------------------------------------

    [Test]
    public void InProgress_Start_ThrowsConflictAndChangesNothing()
    {
        var context = MakeContext(MaintenanceStatus.IN_PROGRESS, CarStatus.UNDER_MAINTENANCE, out var schedule, out var car);

        AssertConflictAndNothingChanged(context.Start, schedule, car, MaintenanceStatus.IN_PROGRESS, CarStatus.UNDER_MAINTENANCE);
    }

    [Test]
    public void InProgress_Complete_MovesToCompletedAndReleasesCar()
    {
        var context = MakeContext(MaintenanceStatus.IN_PROGRESS, CarStatus.UNDER_MAINTENANCE, out var schedule, out var car);

        context.Complete();

        schedule.Status.Should().Be(MaintenanceStatus.COMPLETED);
        schedule.CompletedAt.Should().Be(Now);
        context.State.Should().BeSameAs(CompletedState.Instance);
        car.Status.Should().Be(CarStatus.AVAILABLE);
        car.IsAvailable.Should().BeTrue();
    }

    [Test]
    public void InProgress_Cancel_MovesToCancelledAndReleasesCar()
    {
        var context = MakeContext(MaintenanceStatus.IN_PROGRESS, CarStatus.UNDER_MAINTENANCE, out var schedule, out var car);

        context.Cancel();

        schedule.Status.Should().Be(MaintenanceStatus.CANCELLED);
        schedule.CompletedAt.Should().BeNull();
        context.State.Should().BeSameAs(CancelledState.Instance);
        car.Status.Should().Be(CarStatus.AVAILABLE);
        car.IsAvailable.Should().BeTrue();
    }

    // ---- COMPLETED (terminal) ----------------------------------------------

    [Test]
    public void Completed_Start_ThrowsConflictAndChangesNothing()
    {
        var context = MakeContext(MaintenanceStatus.COMPLETED, CarStatus.AVAILABLE, out var schedule, out var car);

        AssertConflictAndNothingChanged(context.Start, schedule, car, MaintenanceStatus.COMPLETED, CarStatus.AVAILABLE);
    }

    [Test]
    public void Completed_Complete_ThrowsConflictAndChangesNothing()
    {
        var context = MakeContext(MaintenanceStatus.COMPLETED, CarStatus.AVAILABLE, out var schedule, out var car);

        AssertConflictAndNothingChanged(context.Complete, schedule, car, MaintenanceStatus.COMPLETED, CarStatus.AVAILABLE);
    }

    [Test]
    public void Completed_Cancel_ThrowsConflictAndChangesNothing()
    {
        var context = MakeContext(MaintenanceStatus.COMPLETED, CarStatus.AVAILABLE, out var schedule, out var car);

        AssertConflictAndNothingChanged(context.Cancel, schedule, car, MaintenanceStatus.COMPLETED, CarStatus.AVAILABLE);
    }

    // ---- CANCELLED (terminal) ----------------------------------------------

    [Test]
    public void Cancelled_Start_ThrowsConflictAndChangesNothing()
    {
        var context = MakeContext(MaintenanceStatus.CANCELLED, CarStatus.AVAILABLE, out var schedule, out var car);

        AssertConflictAndNothingChanged(context.Start, schedule, car, MaintenanceStatus.CANCELLED, CarStatus.AVAILABLE);
    }

    [Test]
    public void Cancelled_Complete_ThrowsConflictAndChangesNothing()
    {
        var context = MakeContext(MaintenanceStatus.CANCELLED, CarStatus.AVAILABLE, out var schedule, out var car);

        AssertConflictAndNothingChanged(context.Complete, schedule, car, MaintenanceStatus.CANCELLED, CarStatus.AVAILABLE);
    }

    [Test]
    public void Cancelled_Cancel_ThrowsConflictAndChangesNothing()
    {
        var context = MakeContext(MaintenanceStatus.CANCELLED, CarStatus.AVAILABLE, out var schedule, out var car);

        AssertConflictAndNothingChanged(context.Cancel, schedule, car, MaintenanceStatus.CANCELLED, CarStatus.AVAILABLE);
    }
}
