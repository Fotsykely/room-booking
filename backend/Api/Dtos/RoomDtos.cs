using System.ComponentModel.DataAnnotations;
using Api.Entities;

namespace Api.Dtos;

// Request body for POST and PUT. The validation attributes are picked up automatically
// by [ApiController]: invalid input returns a 400 with one message list per field.
public class RoomRequestDto
{
    private string? _name;
    private string? _equipment;

    [Required(ErrorMessage = "Le nom est obligatoire.")]
    [StringLength(Room.NameMaxLength, ErrorMessage = "Le nom ne peut pas dépasser {1} caractères.")]
    public string? Name
    {
        get => _name;
        set => _name = value?.Trim();
    }

    [Range(1, int.MaxValue, ErrorMessage = "La capacité doit être un entier positif.")]
    public int Capacity { get; set; }

    // 0 is the ground floor.
    public int Floor { get; set; }

    [StringLength(Room.EquipmentMaxLength, ErrorMessage = "Les équipements ne peuvent pas dépasser {1} caractères.")]
    public string? Equipment
    {
        get => _equipment;
        set => _equipment = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public bool IsActive { get; set; } = true;
}

// Response for a single room, also used for the list.
// IsAvailableNow is true when the room is active and no confirmed reservation covers the current instant.
public record RoomDto(
    int Id,
    string Name,
    int Capacity,
    int Floor,
    string? Equipment,
    bool IsActive,
    bool IsAvailableNow);
