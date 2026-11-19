namespace Api.Entities;

public class Member
{
    // These limits are shared by the database schema and the request validation.
    public const int NameMaxLength = 100;
    public const int EmailMaxLength = 254;

    // A Standard member can hold at most this many upcoming confirmed bookings. Premium has no limit.
    public const int StandardMaxUpcomingBookings = 3;

    public int Id { get; set; }
    public required string Name { get; set; }

    // Unique (case-insensitive with the default MySQL collation).
    public required string Email { get; set; }

    public SubscriptionType SubscriptionType { get; set; }

    public ICollection<Booking> Bookings { get; set; } = [];
}
