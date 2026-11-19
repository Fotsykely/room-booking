using Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Room>(room =>
        {
            room.Property(r => r.Name).HasMaxLength(Room.NameMaxLength);
            room.Property(r => r.Equipment).HasMaxLength(Room.EquipmentMaxLength);

            // The default MySQL collation (utf8mb4_0900_ai_ci) ignores case and accents: "Salle A" and "salle a" collide.
            room.HasIndex(r => r.Name).IsUnique();

            room.ToTable(table => table.HasCheckConstraint("CK_Rooms_Capacity_Positive", "`Capacity` > 0"));
        });

        modelBuilder.Entity<Member>(member =>
        {
            member.Property(m => m.Name).HasMaxLength(Member.NameMaxLength);
            member.Property(m => m.Email).HasMaxLength(Member.EmailMaxLength);

            // The subscription is stored as text ("Standard", "Premium"): readable directly in SQL.
            member.Property(m => m.Subscription).HasConversion<string>().HasMaxLength(20);

            member.HasIndex(m => m.Email).IsUnique();
        });

        modelBuilder.Entity<Reservation>(reservation =>
        {
            reservation.Property(r => r.Purpose).HasMaxLength(Reservation.PurposeMaxLength);
            reservation.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);

            // Nothing is ever deleted, so a room or a member that has reservations cannot be removed.
            reservation.HasOne(r => r.Room).WithMany().HasForeignKey(r => r.RoomId).OnDelete(DeleteBehavior.Restrict);
            reservation.HasOne(r => r.Member).WithMany().HasForeignKey(r => r.MemberId).OnDelete(DeleteBehavior.Restrict);

            // Last line of defense: the business rules are checked in ReservationService.
            reservation.ToTable(table => table.HasCheckConstraint("CK_Reservations_Ends_After_Start", "`EndsAt` > `StartsAt`"));

            // Each index serves one query (equality columns first, then the range or sort column).
            // The room-first and member-first indexes also cover their foreign key.

            // Overlap check and planning filtered by room: WHERE RoomId = ? AND Status = 'Confirmed' AND StartsAt < ?
            reservation.HasIndex(r => new { r.RoomId, r.Status, r.StartsAt });

            // Planning without a room filter: WHERE Status = 'Confirmed' AND StartsAt in a day ORDER BY StartsAt
            reservation.HasIndex(r => new { r.Status, r.StartsAt });

            // Limit of upcoming reservations of a member: WHERE MemberId = ? AND Status = 'Confirmed' AND StartsAt > now
            reservation.HasIndex(r => new { r.MemberId, r.Status, r.StartsAt });
        });
    }
}
