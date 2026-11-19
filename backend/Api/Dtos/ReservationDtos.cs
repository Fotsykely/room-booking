using System.ComponentModel.DataAnnotations;
using Api.Entities;

namespace Api.Dtos;

// Request body for POST. The validation attributes only check the shape of the request (a value is present);
// the booking rules themselves are checked by ReservationService. Dates are local times without offset,
// for example "2030-06-15T14:00:00".
public class ReservationRequestDto
{
    private string? _purpose;

    [Range(1, int.MaxValue, ErrorMessage = "Choisissez un membre.")]
    public int MemberId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Choisissez une salle.")]
    public int RoomId { get; set; }

    // Nullable so that a missing date is told apart from the default date 0001-01-01.
    [Required(ErrorMessage = "La date de début est obligatoire.")]
    public DateTime? StartsAt { get; set; }

    [Required(ErrorMessage = "La date de fin est obligatoire.")]
    public DateTime? EndsAt { get; set; }

    [Required(ErrorMessage = "L'objet est obligatoire.")]
    [StringLength(Reservation.PurposeMaxLength, ErrorMessage = "L'objet ne peut pas dépasser {1} caractères.")]
    public string? Purpose
    {
        get => _purpose;
        set => _purpose = value?.Trim();
    }
}

// Response for a single reservation, also used for the planning. The names save the front end a second request.
public record ReservationDto(
    int Id,
    int RoomId,
    string RoomName,
    int MemberId,
    string MemberName,
    DateTime StartsAt,
    DateTime EndsAt,
    string Purpose,
    ReservationStatus Status,
    DateTime CreatedAt);
