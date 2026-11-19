using System.Text.Json.Serialization;

namespace Api.Entities;

// In JSON, the subscription type travels as text ("Premium") rather than a number.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SubscriptionType
{
    Standard,
    Premium,
}
