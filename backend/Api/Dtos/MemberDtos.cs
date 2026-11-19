using System.ComponentModel.DataAnnotations;
using Api.Entities;

namespace Api.Dtos;

// Request body for POST and PUT. The validation attributes are picked up automatically
// by [ApiController]: invalid input returns a 400 with one message list per field.
public class MemberRequestDto
{
    private string? _name;
    private string? _email;

    [Required(ErrorMessage = "Le nom est obligatoire.")]
    [StringLength(Member.NameMaxLength, ErrorMessage = "Le nom ne peut pas dépasser {1} caractères.")]
    public string? Name
    {
        get => _name;
        set => _name = value?.Trim();
    }

    [Required(ErrorMessage = "L'adresse e-mail est obligatoire.")]
    [StringLength(Member.EmailMaxLength, ErrorMessage = "L'adresse e-mail ne peut pas dépasser {1} caractères.")]
    [EmailAddress(ErrorMessage = "L'adresse e-mail n'est pas valide.")]
    public string? Email
    {
        get => _email;
        set => _email = value?.Trim();
    }

    // The enum is checked by the JSON deserializer: an unknown subscription is a 400 as well.
    public SubscriptionType Subscription { get; set; } = SubscriptionType.Standard;
}

// Response for a single member, also used for the list.
public record MemberDto(int Id, string Name, string Email, SubscriptionType Subscription);
