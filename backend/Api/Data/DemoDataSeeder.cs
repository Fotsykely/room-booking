using Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public static class DemoDataSeeder
{
    // Inserts nothing when the database already has rooms: restarting the API never creates duplicates.
    // Dates are relative to "now" so the demo always has past, ongoing and upcoming reservations.
    // The data is inserted directly (not through ReservationService), so it can include past and ongoing bookings.
    public static async Task SeedAsync(AppDbContext db, TimeProvider time, CancellationToken cancellationToken = default)
    {
        if (await db.Rooms.AnyAsync(cancellationToken))
            return;

        var now = time.GetLocalNow().DateTime;
        var today = now.Date;
        DateTime At(int dayOffset, int hour, int minute = 0) => today.AddDays(dayOffset).AddHours(hour).AddMinutes(minute);

        // Last half-hour mark (14:37 -> 14:30). The ongoing reservation runs from 2 hours before it to 2 hours
        // after it (the 4 hours a booking may last): "now" is always inside, whatever the time of the day, and the
        // room stays occupied for at least 2 hours after the first launch.
        var halfHour = today.AddHours(now.Hour).AddMinutes(now.Minute < 30 ? 0 : 30);

        var baobab = new Room { Name = "Salle Baobab", Capacity = 4, Floor = 0, Equipment = "Écran, tableau blanc" };
        var lemurien = new Room { Name = "Salle Lémurien", Capacity = 8, Floor = 1, Equipment = "Vidéoprojecteur, visioconférence, tableau blanc" };
        var zebu = new Room { Name = "Salle Zébu", Capacity = 12, Floor = 2, Equipment = "Vidéoprojecteur, système audio, paperboard" };
        // Inactive on purpose: it cannot be booked any more but keeps its past reservation.
        var vanille = new Room { Name = "Salle Vanille", Capacity = 6, Floor = 0, Equipment = "Écran", IsActive = false };

        var hery = new Member { Name = "Hery Rakoto", Email = "hery.rakoto@example.com", Subscription = SubscriptionType.Standard };
        var lalaina = new Member { Name = "Lalaina Andria", Email = "lalaina.andria@example.com", Subscription = SubscriptionType.Standard };
        // Already at the limit of 3 upcoming reservations: the form refuses a 4th one.
        var mialy = new Member { Name = "Mialy Randria", Email = "mialy.randria@example.com", Subscription = SubscriptionType.Standard };
        var tiana = new Member { Name = "Tiana Rabe", Email = "tiana.rabe@example.com", Subscription = SubscriptionType.Premium };
        var faly = new Member { Name = "Faly Ramanana", Email = "faly.ramanana@example.com", Subscription = SubscriptionType.Premium };

        Reservation Book(Room room, Member member, DateTime start, DateTime end, string purpose,
            ReservationStatus status = ReservationStatus.Confirmed) =>
            new()
            {
                Room = room, Member = member, StartsAt = start, EndsAt = end, Purpose = purpose,
                Status = status, CreatedAt = start.AddDays(-3),
            };

        db.Reservations.AddRange(
            // Past
            Book(baobab, hery, At(-5, 9), At(-5, 10), "Point d'équipe"),
            Book(lemurien, tiana, At(-2, 14), At(-2, 16), "Atelier de conception"),
            Book(vanille, faly, At(-1, 10), At(-1, 11, 30), "Entretien client"),

            // Ongoing: the room shows as "occupied" on the list
            Book(zebu, faly, halfHour.AddHours(-2), halfHour.AddHours(2), "Revue de sprint"),

            // Upcoming
            Book(baobab, hery, At(1, 9), At(1, 10), "Point hebdomadaire"),
            Book(zebu, tiana, At(1, 14), At(1, 17), "Séminaire interne"),
            Book(lemurien, mialy, At(1, 14), At(1, 15), "Démo produit"),
            Book(lemurien, mialy, At(2, 10), At(2, 12), "Formation"),
            Book(zebu, mialy, At(3, 15), At(3, 16, 30), "Rétrospective"),

            // Cancelled: kept in the database, hidden from the default planning
            Book(baobab, lalaina, At(2, 11), At(2, 12), "Rendez-vous annulé", ReservationStatus.Cancelled));

        await db.SaveChangesAsync(cancellationToken);
    }
}
