# Change 4 — Refactoring

**Existing code was refactored in this change.** The confirmation e-mail is moved out of `BookingService.CreateBookingAsync`.

## Code smell

**Long Method with mixed responsibilities** (also called Divergent Change) in `BookingService.CreateBookingAsync` (baseline `dfc61ab8`, `FlemanApi/Service/BookingService.cs:78-200`).

The 123-line method:

- resolves the customer
- validates the car type, dates, customer and hubs
- checks capacity
- builds and saves the booking header and add-on lines
- prices the booking
- then, in lines 167-196, loads the customer again, formats a 17-line e-mail body and sends it through `IEmailService` inside its own try/catch

The last 30 lines have nothing to do with booking rules. Changing the wording of the confirmation e-mail, or adding a second reaction such as an audit entry, meant editing the booking service. Notifying on modify and cancel would have copied the same block into two more methods.

## Technique

**Extract Class** + **Move Method**, followed by **Replace direct call with Observer**:

1. Move the e-mail text into a new class, `EmailBookingObserver` (`FlemanApi/Service/Notifications/EmailBookingObserver.cs`):
   - The body becomes `BuildCreatedBody` (`:35-51`) and the subject becomes `CreatedSubject` (`:8`).
   - The send call moves to `OnBookingEventAsync` (`:19-33`).
   - The interpolations change from `customer.FullName`, `header.*` and `carType.CarTypeName` to the matching `BookingEvent` fields. The literal text is unchanged.
2. Replace the inline block with one call to a new private helper, `PublishBookingEventAsync` (`BookingService.cs:338-361`). The helper keeps the baseline guarantee: it looks the customer up by `header.CustomerId`, and any exception is logged and swallowed.
3. Replace the `IEmailService` dependency of `BookingService` with `IBookingEventPublisher`.

## Diff hunk (`FlemanApi/Service/BookingService.cs`)

```diff
@@ -164,36 +165,7 @@ public class BookingService : IBookingService
         header.EstimatedAmount = rentalAmount + addonAmount;
         await _bookings.SaveChangesAsync();
 
-        try
-        {
-            var customer = await _customers.GetByIdAsync(header.CustomerId);
-            if (customer is not null)
-            {
-                var body = $"""
-                    Dear {customer.FullName},
-
-                    Your booking has been confirmed successfully.
-
-                    Booking Details
-                    -------------------------
-                    Booking No : {header.ConfirmationNo}
-                    Vehicle Type : {carType.CarTypeName}
-                    Pickup Date : {header.PickupDatetime}
-                    Return Date : {header.ReturnDatetime}
-                    Total Amount : ₹{header.EstimatedAmount:F2}
-
-                    Thank you for choosing Fleeman.
-
-                    Regards,
-                    Fleeman Team
-                    """;
-                await _emailService.SendEmailAsync(customer.Email, "Booking Confirmation - Fleeman", body);
-            }
-        }
-        catch (Exception ex)
-        {
-            _logger.LogWarning(ex, "Failed to send booking confirmation email for {ConfirmationNo}", header.ConfirmationNo);
-        }
+        await PublishBookingEventAsync(BookingEventKind.CREATED, header);
 
         _logger.LogInformation("Booking process completed, confirmationNo={ConfirmationNo}", header.ConfirmationNo);
         return await ToResponseDtoAsync(header);
```

The constructor hunk swaps `IEmailService emailService` / `_emailService` for `IBookingEventPublisher bookingEvents` / `_bookingEvents`, at lines 41, 58 and 74. The full hunks are in `diff.patch`.

## Edit to an existing test

`FlemanApi.Tests/Services/BookingServiceTests.cs`, in `SetUp` only. This is the single allowed edit (8 lines added, 1 removed). No `using` line was added; fully qualified names are used instead:

```diff
             _addonServiceMock.Object,
-            _emailServiceMock.Object,
+            // CH04: the only edit to this file. The real publisher + real e-mail
+            // observer keep CreateBookingAsync calling _emailServiceMock as before.
+            new FlemanApi.Service.Notifications.BookingEventPublisher(
+                new FlemanApi.Service.Notifications.IBookingEventObserver[]
+                {
+                    new FlemanApi.Service.Notifications.EmailBookingObserver(_emailServiceMock.Object),
+                },
+                NullLogger<FlemanApi.Service.Notifications.BookingEventPublisher>.Instance),
             _currentUserMock.Object,
```

- The argument is **replaced**, not added, because `IEmailService` was removed from the constructor (see the business values in `requirement.md`).
- At the baseline, no test in `BookingServiceTests` verifies `SendEmailAsync`. The `_emailServiceMock` field is only created and passed in. So no assertion needed protecting.
- The real `EmailBookingObserver` is still wired to that mock, so `CreateBookingAsync` makes the same `SendEmailAsync` call on it as in the baseline.
- No test body, test name or assertion was changed or removed.

## Behaviour-preservation evidence

- **Existing booking tests:** the 21 tests in `BookingServiceTests` run unchanged against the refactored service (create, modify, cancel, access and listing). Result: `test_result.txt`.
- **Same text as the baseline:** `BookingObserverTests.EmailObserver_Created_SendsExactlyTheBaselineSubjectAndBody` compares the CREATED e-mail with a copy of the baseline raw string literal from `BookingService.cs:172-189` (`dfc61ab8`). The comparison is exact equality of recipient, subject and body.
- **Same message reaches the queue:** `BookingServiceNotificationTests.CreateBookingAsync_QueuesOneConfirmationEmail_WithBaselineRecipientSubjectAndBody` checks that exactly one work item is enqueued. Running that item calls `IEmailSender.SendPlainAsync("jane@example.com", "Booking Confirmation - Fleeman", <baseline body>)`.

## Observable differences, stated plainly

- The warning text logged when the notification lookup fails changes:
  - before: `Failed to send booking confirmation email for {ConfirmationNo}`
  - after: `Failed to publish booking {EventKind} event for {ConfirmationNo}`
- A failing observer is now logged by `BookingEventPublisher` (`Booking observer {Observer} failed ...`), not by `BookingService`.
- No test or API response depends on either message.
- `CreateBookingAsync` now reads the car type once more (`_carTypes.GetByIdAsync(header.CarTypeId)`) when building the event. The value is the same one already loaded at line 101.
- Modify and cancel now send e-mails and write audit entries. This is the new behaviour the requirement asks for, not a refactoring side effect.

## Smell observed but deliberately not fixed

- `CustomerService.RegisterAsync` (`Service/CustomerService.cs:65`, e-mail block `:102-123`) and `StaffService.ProcessReturnAsync` (`Service/StaffService.cs:107`, e-mail block `:208-218`) have the same smell: they format and send e-mail inline. The CH04 brief forbids touching them.
- The `DateTime.Now` smell already recorded in CH03 (`BookingService.cs:126`) is still present. CH04 does not change that line.
