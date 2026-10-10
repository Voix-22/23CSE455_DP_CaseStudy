# Change 4 — Booking lifecycle notifications (Observer)

Owner: Vani Sugovind S R · Branch: `ch04-booking-notifications`, cut from `baseline` = `dfc61ab8` and **not rebased onto `ch03`** · Before: `dfc61ab8dd6f9bbb2301171457dc03946a249576` · After: `9be38a6ff4cd5b10b813aedb42c3de8a1e82730d` (tag `ch04`), one commit on top of the baseline.

## Baseline behaviour

- `BookingService.CreateBookingAsync` builds the confirmation e-mail text inline (`Service/BookingService.cs:167-196`). It calls `IEmailService.SendEmailAsync` directly inside a try/catch that logs and swallows any error.
- `ModifyBookingAsync` (`:223`) and `CancelBookingAsync` (`:323`) notify nobody.
- No booking event is written to any audit log.
- E-mail is queued through `EmailBackgroundQueue` and sent by `EmailQueueHostedService` through `MailKitEmailSender`.

## Requirement

Customers are notified when a booking is created, modified or cancelled, and each event is written to an audit log. The design uses the **Observer** pattern, so a new reaction can be added without editing `BookingService`.

## Design

All new production code is in `FlemanApi/Service/Notifications/`, namespace `FlemanApi.Service.Notifications`.

- **Event:** `BookingEvent` is an immutable record. It carries:
  - the event kind (`BookingEventKind`: CREATED / MODIFIED / CANCELLED)
  - the booking id and confirmation number
  - the customer id, name and e-mail
  - the car type name
  - the pickup and return times, the estimated amount and the booking status

  It is a snapshot taken after the save, so observers never receive the tracked `BookingHeader` entity.
- **Observer:** `IBookingEventObserver.OnBookingEventAsync(BookingEvent)`.
- **Subject:** `IBookingEventPublisher` with `Subscribe`, `Unsubscribe` and `PublishAsync`.
- **ConcreteSubject:** `BookingEventPublisher`.
  - It receives `IEnumerable<IBookingEventObserver>` from DI and subscribes each one in registration order.
  - `PublishAsync` notifies the observers one after another.
  - An observer that throws is logged as a warning and skipped. The remaining observers still run, and `PublishAsync` itself never throws.
- **ConcreteObservers:**
  - `EmailBookingObserver` sends through the existing `IEmailService.SendEmailAsync`. For CREATED it sends the baseline subject and body unchanged. For MODIFIED and CANCELLED it sends the new messages listed below.
  - `AuditLogBookingObserver` writes one structured `ILogger` Information entry per event.
- **BookingService:**
  - It receives `IBookingEventPublisher` in place of `IEmailService`.
  - After each successful `SaveChangesAsync` in create, modify and cancel, it calls `PublishBookingEventAsync`. That helper looks up the customer and car type, builds the event and publishes it.
  - It no longer builds any e-mail text.
- **Registration:** one commented CH04 block in `Program.cs`, placed after the existing registrations (scoped):

  | Interface | Implementation | Order |
  |---|---|---|
  | `IBookingEventObserver` | `EmailBookingObserver` | 1st |
  | `IBookingEventObserver` | `AuditLogBookingObserver` | 2nd |
  | `IBookingEventPublisher` | `BookingEventPublisher` | – |

- **Out of scope:** no change to `StaffService`, `CustomerService`, `EmailService`, `EmailBackgroundQueue`, `EmailQueueHostedService`, `MailKitEmailSender` or any controller. No database change and no migration.

## Business values chosen

| Decision | Value |
|---|---|
| `IEmailService` in `BookingService` | **Removed.** Its only use was the confirmation e-mail, which moved to `EmailBookingObserver`. Keeping an unused dependency would be a new smell. In `BookingServiceTests` the argument is replaced, not added (see `refactoring.md`). |
| CREATED e-mail | Subject `Booking Confirmation - Fleeman`, with the body unchanged from the baseline. |
| MODIFIED e-mail | Subject `Booking Updated - Fleeman`. The body gives the booking number, vehicle type, pickup and return dates, the new total amount, and "If you did not make this change, please contact us." |
| CANCELLED e-mail | Subject `Booking Cancelled - Fleeman`. The body gives the booking number, vehicle type and dates, and says the booking has been cancelled. |
| Observer order | E-mail first, then audit (DI registration order). |
| When an event is published | Only after the booking is saved. A request rejected with 400, 403, 404 or 409 publishes nothing. |
| Customer row missing | No e-mail is sent, as in the baseline. The audit entry is still written. |
| Modify request that changes no field | It still succeeds and saves, so it publishes MODIFIED. The baseline service has no "nothing changed" check, and none is added. |
| Cancelling an already cancelled booking | The baseline allows it. It publishes CANCELLED again. |
| Failure handling | A failing observer is logged by `BookingEventPublisher` at Warning level. A failed customer or car-type lookup while building the event is logged by `BookingService` at Warning level. The HTTP result never changes. |
| Audit log target | The existing `ILogger` pipeline (console). No new table, file or sink. |
| Publisher lifetime | Scoped, the same as `EmailService` and `BookingService`. The observer list therefore belongs to one request, so `Subscribe` and `Unsubscribe` at run time affect only that request. |

## Acceptance criteria

Test run: `dotnet test src/FlemanApi.Tests/FlemanApi.Tests.csproj`, .NET SDK 8.0.425, 2026-10-10. Build: 0 warnings, 0 errors. Total 201, passed 200, failed 1, skipped 0 (`test_result.txt`). The same command on the baseline gives total 183, passed 182, failed 1 (`test_result_baseline.txt`).


| # | Criterion | Observed | Evidence |
|---|---|---|---|
| 1 | Creating a booking still queues one confirmation e-mail with the same recipient, subject and body as the baseline. | Met | `BookingServiceNotificationTests.CreateBookingAsync_QueuesOneConfirmationEmail_WithBaselineRecipientSubjectAndBody` (real `EmailService` with a mocked queue, then the queued item is run against a mock `IEmailSender`); `BookingObserverTests.EmailObserver_Created_SendsExactlyTheBaselineSubjectAndBody` |
| 2 | Modifying and cancelling a booking each queue one e-mail and write one audit entry. | Met | `BookingServiceNotificationTests.ModifyBookingAsync_QueuesOneUpdateEmail_AndWritesOneAuditEntry`, `CancelBookingAsync_QueuesOneCancellationEmail_AndWritesOneAuditEntry` |
| 3 | An observer that throws does not change the HTTP result, and the remaining observers still run. | Met | `BookingServiceNotificationTests.CreateBookingAsync_ObserverThrows_BookingStillSucceeds_AndLaterObserversRun`; `BookingEventPublisherTests.PublishAsync_ObserverThrows_LogsWarningAndStillNotifiesTheOthers` |
| 4 | All existing tests pass. The only edit to `BookingServiceTests` is the constructor argument. | Met, with a caveat: 182 of the 183 existing tests pass both before and after. The one failure, `StaffServiceTests.ProcessReturnAsync_CreatesInvoiceHeaderAndDetails`, already fails at the baseline because it depends on the clock (see Change1 `refactoring.md`). CH04 does not touch it. | `test_result.txt`; the diff of `BookingServiceTests.cs` in `diff.patch` (one argument, 8 lines added and 1 removed) |
| 5 | New tests cover the publisher (subscribe, unsubscribe, ordering, failure isolation) and each observer. | Met: 18 of 18 pass | `BookingEventPublisherTests` (6 tests), `BookingObserverTests` (4 e-mail tests + 3 audit test cases), `BookingServiceNotificationTests` (5 tests). That makes 18 new tests in total. |
| – | Extra check: a rejected request publishes nothing. | Met | `BookingServiceNotificationTests.ModifyBookingAsync_Rejected_PublishesNoEvent` |
