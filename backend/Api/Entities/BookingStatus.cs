using System.Text.Json.Serialization;

namespace Api.Entities;

// A cancelled booking is never deleted: only its status changes, so the history is kept.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BookingStatus
{
    Confirmed,
    Cancelled,
}
