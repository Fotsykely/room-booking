namespace Api.Entities;

public class Reservation
{
    public const int PurposeMaxLength = 200;

    public int Id { get; set; }

    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public int MemberId { get; set; }
    public Member Member { get; set; } = null!;

    // Local time of the coworking space (a single site, so no time zones). "StartsAt" and "EndsAt"
    // rather than "Start" and "End": END is an SQL keyword. The period is half-open: [StartsAt, EndsAt).
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }

    public required string Purpose { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Confirmed;

    // Set by the application from the injected TimeProvider, so it uses the same clock (and the same
    // local time) as StartsAt and EndsAt. A MySQL default such as CURRENT_TIMESTAMP would use the
    // server's time zone, which is UTC in Docker.
    public DateTime CreatedAt { get; set; }
}
