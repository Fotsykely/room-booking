namespace Api.Entities;

public class Room
{
    // These limits are shared by the database schema and the request validation.
    public const int NameMaxLength = 100;
    public const int EquipmentMaxLength = 500;

    public int Id { get; set; }
    public required string Name { get; set; }
    public int Capacity { get; set; }

    // 0 is the ground floor.
    public int Floor { get; set; }

    // Free text, for example "Projector, whiteboard".
    public string? Equipment { get; set; }

    // An inactive room cannot be booked any more, but keeps its reservation history.
    public bool IsActive { get; set; } = true;
}
