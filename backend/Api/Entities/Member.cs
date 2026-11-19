namespace Api.Entities;

public class Member
{
    // These limits are shared by the database schema and the request validation.
    public const int NameMaxLength = 100;
    public const int EmailMaxLength = 254;

    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public SubscriptionType Subscription { get; set; }
}
