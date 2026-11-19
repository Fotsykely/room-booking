using System.Text.Json.Serialization;

namespace Api.Entities;

// A reservation is never deleted: cancelling it only changes its status (history is kept).
// In JSON, the status travels as text ("Confirmed") rather than a number.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReservationStatus
{
    Confirmed,
    Cancelled,
}
