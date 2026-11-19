namespace Api.Entities;

public class Room
{
    // These limits are shared by the database schema and the request validation.
    public const int NameMaxLength = 100;
    public const int EquipmentMaxLength = 500;

    public int Id { get; set; }

    // Unique (case-insensitive with the default MySQL collation).
    public required string Name { get; set; }

    // Number of seats, strictly positive (also enforced by a CHECK constraint).
    public int Capacity { get; set; }

    // 0 is the ground floor.
    public int Floor { get; set; }

    // Free text, e.g. "Projector, whiteboard".
    public string? Equipment { get; set; }

    // An inactive room cannot be booked anymore but keeps its booking history.
    public bool IsActive { get; set; } = true;

    public ICollection<Booking> Bookings { get; set; } = [];
}
