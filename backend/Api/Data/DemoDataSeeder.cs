using Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public static class DemoDataSeeder
{
    // Inserts nothing when the database already has rooms: restarting the API never creates duplicates.
    // Dates are relative to "now", so the demo always has past, current and upcoming bookings.
    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Rooms.AnyAsync(cancellationToken))
            return;

        var now = DateTime.Now;

        // Start of the current hour, so the demo bookings sit on clean hours.
        var thisHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);

        var rooms = new Dictionary<string, Room>
        {
            ["Atlas"] = new() { Name = "Atlas", Capacity = 4, Floor = 0, Equipment = "Écran, visioconférence" },
            ["Boréal"] = new() { Name = "Boréal", Capacity = 8, Floor = 1, Equipment = "Vidéoprojecteur, tableau blanc" },
            ["Cèdre"] = new() { Name = "Cèdre", Capacity = 12, Floor = 2, Equipment = "Vidéoprojecteur, visioconférence, sonorisation" },
            // Inactive: cannot be booked anymore, but keeps its past bookings.
            ["Delta"] = new() { Name = "Delta", Capacity = 6, Floor = 1, Equipment = "Tableau blanc", IsActive = false },
        };

        var members = new Dictionary<string, Member>
        {
            ["Alice"] = new() { Name = "Alice Martin", Email = "alice.martin@example.com", SubscriptionType = SubscriptionType.Premium },
            ["Bruno"] = new() { Name = "Bruno Lefèvre", Email = "bruno.lefevre@example.com", SubscriptionType = SubscriptionType.Standard },
            ["Chloé"] = new() { Name = "Chloé Durand", Email = "chloe.durand@example.com", SubscriptionType = SubscriptionType.Standard },
            ["David"] = new() { Name = "David Moreau", Email = "david.moreau@example.com", SubscriptionType = SubscriptionType.Premium },
            ["Emma"] = new() { Name = "Emma Rousseau", Email = "emma.rousseau@example.com", SubscriptionType = SubscriptionType.Standard },
        };

        // The creation date is set explicitly so the history looks realistic (created before the start).
        Booking NewBooking(string room, string member, DateTime start, TimeSpan duration, string purpose,
            BookingStatus status = BookingStatus.Confirmed) => new()
        {
            Room = rooms[room],
            Member = members[member],
            StartAt = start,
            EndAt = start + duration,
            Purpose = purpose,
            Status = status,
            CreatedAt = start.AddDays(-2),
        };

        var bookings = new List<Booking>
        {
            // Past bookings (the Delta ones show that an inactive room keeps its history).
            NewBooking("Delta", "Alice", thisHour.AddDays(-14), TimeSpan.FromHours(2), "Atelier de conception"),
            NewBooking("Atlas", "Bruno", thisHour.AddDays(-7), TimeSpan.FromHours(1), "Point hebdomadaire"),
            NewBooking("Boréal", "Chloé", thisHour.AddDays(-3), TimeSpan.FromHours(3), "Formation interne"),
            NewBooking("Cèdre", "David", thisHour.AddDays(-1), TimeSpan.FromHours(2), "Entretien client"),

            // In progress right now: Boréal shows as busy in the room list.
            NewBooking("Boréal", "Emma", thisHour.AddMinutes(-30), TimeSpan.FromHours(2), "Revue de sprint"),

            // Upcoming, without overlap. Bruno (Standard) already holds his 3 upcoming bookings: the quota is reached.
            NewBooking("Atlas", "Bruno", thisHour.AddDays(1), TimeSpan.FromHours(1), "Démo produit"),
            NewBooking("Atlas", "Bruno", thisHour.AddDays(2), TimeSpan.FromHours(2), "Rétrospective"),
            NewBooking("Boréal", "Bruno", thisHour.AddDays(3), TimeSpan.FromHours(1), "Point équipe"),
            NewBooking("Cèdre", "Alice", thisHour.AddDays(1), TimeSpan.FromHours(4), "Séminaire trimestriel"),

            // Cancelled: kept in the database, hidden from the default schedule.
            NewBooking("Cèdre", "Chloé", thisHour.AddDays(2), TimeSpan.FromHours(2), "Présentation annulée",
                BookingStatus.Cancelled),
        };

        db.Bookings.AddRange(bookings);
        await db.SaveChangesAsync(cancellationToken);
    }
}
