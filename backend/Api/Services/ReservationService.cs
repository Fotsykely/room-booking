using System.Data;
using System.Globalization;
using Api.Data;
using Api.Dtos;
using Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public record BookingResult(Reservation? Reservation, IReadOnlyDictionary<string, string> Errors);

// The booking rules live here. A broken rule is not an exception: BookAsync returns one message per form
// field, keyed by the field names of ReservationRequestDto, and the controller sends them as a usual 400.
public class ReservationService(AppDbContext db, TimeProvider time)
{
    public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(5);
    public static readonly TimeSpan MinDuration = TimeSpan.FromHours(2);
    public static readonly TimeOnly FirstStart = new(8, 0); // 08:00
    public static readonly TimeOnly StartDeadline = new(20, 0); // 20:00
    public const int StandardMemberUpcomingLimit = 3;

    private const string MemberField = nameof(ReservationRequestDto.MemberId);
    private const string RoomField = nameof(ReservationRequestDto.RoomId);
    private const string StartField = nameof(ReservationRequestDto.StartsAt);
    private const string EndField = nameof(ReservationRequestDto.EndsAt);

    public async Task<BookingResult> BookAsync(
        int memberId, int roomId, DateTime startsAt, DateTime endsAt, string purpose, CancellationToken cancellationToken)
    {
        var now = time.GetLocalNow().DateTime;
        var errors = new Dictionary<string, string>();

        CheckPeriod(startsAt, endsAt, now, errors);
        CheckOpeningHours(startsAt, endsAt, errors);
        
        // READ COMMITTED: every query reads the latest committed data, so the checks below always see the bookings
        // committed by the requests that held the locks before us, whatever this transaction did earlier. (MySQL's
        // default REPEATABLE READ would also work today, because the first statements are locking reads; this level
        // does not depend on that order.)
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        // Requests for the same member, or the same room, wait for each other here: the checks and the insert
        // below then run one request at a time. The order is always member then room, so two requests can never
        // block each other in a loop (deadlock).
        var member = await LockMemberAsync(memberId, cancellationToken);
        var room = await LockRoomAsync(roomId, cancellationToken);

        if (member is null)
            errors[MemberField] = "Ce membre n'existe pas.";

        if (room is null)
            errors[RoomField] = "Cette salle n'existe pas.";
        else if (!room.IsActive)
            errors[RoomField] = "Cette salle est désactivée : elle ne peut plus être réservée.";

        // Overlap and limit only make sense for a valid period, an existing member and a bookable room.
        if (errors.Count == 0 && member is not null && room is not null)
        {
            await CheckRoomIsFreeAsync(room.Id, startsAt, endsAt, errors, cancellationToken);
            await CheckUpcomingLimitAsync(member, now, errors, cancellationToken);
        }

        // Returning here leaves the transaction uncommitted: it is rolled back when disposed.
        if (errors.Count > 0)
            return new BookingResult(null, errors);

        var reservation = new Reservation
        {
            RoomId = roomId,
            MemberId = memberId,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Purpose = purpose,
            Status = ReservationStatus.Confirmed,
            CreatedAt = now,
        };
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new BookingResult(reservation, errors);
    }

    private static void CheckPeriod(DateTime startsAt, DateTime endsAt, DateTime now, Dictionary<string, string> errors)
    {
        // The current minute is accepted: form inputs have a one-minute precision, so a booking that starts
        // "now" must not be refused because a few seconds went by while the user filled in the form.
        var currentMinute = now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMinute));
        if (startsAt < currentMinute)
            errors[StartField] = "Une réservation ne peut pas commencer dans le passé.";

        if (endsAt <= startsAt)
            errors[EndField] = "La fin doit être après le début.";
        else if (endsAt - startsAt > MaxDuration)
            errors[EndField] = $"Une réservation dure au maximum {MaxDuration.TotalHours:0} heures.";
        else if (endsAt - startsAt < MinDuration)
            errors[EndField] = $"Une réservation dure au minimum {MinDuration.TotalHours:0} heures.";
    }

    private static void CheckOpeningHours(DateTime startsAt, DateTime endsAt, Dictionary<string, string> errors)
    {
        var start = TimeOnly.FromDateTime(startsAt);
        if (start < FirstStart || start > StartDeadline)
            errors.TryAdd(StartField, $"Une réservation ne peut commencer qu'entre {FirstStart:HH:mm} et {StartDeadline:HH:mm}.");

        // Midnight is the start of the next day: a date comparison, TimeOfDay would ignore the day.
        if (endsAt > startsAt.Date.AddDays(1))
            errors.TryAdd(EndField, "Une réservation ne peut se terminer qu'à minuit ou avant.");
    }

    // Periods are half-open [start, end): a reservation ending at 14:00 does not overlap one starting at 14:00.
    // Two periods overlap when each one starts before the other ends. Served by (RoomId, Status, StartsAt).
    private async Task CheckRoomIsFreeAsync(
        int roomId, DateTime startsAt, DateTime endsAt, Dictionary<string, string> errors, CancellationToken cancellationToken)
    {
        var conflict = await db.Reservations
            .Where(r => r.RoomId == roomId
                && r.Status == ReservationStatus.Confirmed
                && r.StartsAt < endsAt
                && startsAt < r.EndsAt)
            .OrderBy(r => r.StartsAt)
            .Select(r => new { r.StartsAt, r.EndsAt })
            .FirstOrDefaultAsync(cancellationToken);

        if (conflict is not null)
            errors[RoomField] = $"Cette salle est déjà réservée {DescribePeriod(conflict.StartsAt, conflict.EndsAt)}.";
    }

    // "Upcoming" means not started yet: a reservation in progress, past or cancelled does not count.
    // Premium members have no limit. Served by (MemberId, Status, StartsAt).
    private async Task CheckUpcomingLimitAsync(
        Member member, DateTime now, Dictionary<string, string> errors, CancellationToken cancellationToken)
    {
        if (member.Subscription != SubscriptionType.Standard)
            return;

        var upcoming = await db.Reservations.CountAsync(
            r => r.MemberId == member.Id && r.Status == ReservationStatus.Confirmed && r.StartsAt > now,
            cancellationToken);

        if (upcoming >= StandardMemberUpcomingLimit)
        {
            errors[MemberField] = $"Un membre Standard ne peut pas avoir plus de {StandardMemberUpcomingLimit} " +
                                  "réservations confirmées à venir (l'abonnement Premium n'a pas de limite).";
        }
    }

    // SELECT ... FOR UPDATE keeps the row locked until the end of the transaction.
    private async Task<Member?> LockMemberAsync(int id, CancellationToken cancellationToken)
    {
        var members = await db.Members
            .FromSqlInterpolated($"SELECT * FROM `Members` WHERE `Id` = {id} FOR UPDATE")
            .ToListAsync(cancellationToken);
        return members.SingleOrDefault();
    }

    private async Task<Room?> LockRoomAsync(int id, CancellationToken cancellationToken)
    {
        var rooms = await db.Rooms
            .FromSqlInterpolated($"SELECT * FROM `Rooms` WHERE `Id` = {id} FOR UPDATE")
            .ToListAsync(cancellationToken);
        return rooms.SingleOrDefault();
    }

    // Fixed formats and invariant culture: the message does not depend on the server's regional settings.
    private static string DescribePeriod(DateTime startsAt, DateTime endsAt) =>
        startsAt.Date == endsAt.Date
            ? string.Create(CultureInfo.InvariantCulture, $"le {startsAt:dd/MM/yyyy} de {startsAt:HH:mm} à {endsAt:HH:mm}")
            : string.Create(CultureInfo.InvariantCulture, $"du {startsAt:dd/MM/yyyy} à {startsAt:HH:mm} au {endsAt:dd/MM/yyyy} à {endsAt:HH:mm}");
}
