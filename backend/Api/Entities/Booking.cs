namespace Api.Entities;

public class Booking
{
    // This limit is shared by the database schema and the request validation.
    public const int PurposeMaxLength = 200;

    // A booking lasts 4 hours at most.
    public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(4);

    public int Id { get; set; }

    public int RoomId { get; set; }
    public Room? Room { get; set; }

    public int MemberId { get; set; }
    public Member? Member { get; set; }

    // Local time of the coworking space (one site only). The interval is half-open: [StartAt, EndAt).
    // A booking ending at 14:00 does not overlap a booking starting at 14:00.
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }

    public required string Purpose { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;

    // Filled in by the database (CURRENT_TIMESTAMP(6)) on insert: see AppDbContext.
    public DateTime CreatedAt { get; set; }
}
