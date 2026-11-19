namespace Api.Entities;

// A reservation is never deleted: cancelling it only changes its status (history is kept).
public enum ReservationStatus
{
    Confirmed,
    Cancelled,
}
