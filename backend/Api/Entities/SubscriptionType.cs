using System.Text.Json.Serialization;

namespace Api.Entities;

// In JSON, the subscription travels as text ("Premium") rather than a number,
// both in request/response bodies and in the OpenAPI documentation.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SubscriptionType
{
    Standard,
    Premium,
}
