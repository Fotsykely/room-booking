using System.Linq.Expressions;
using Api.Data;
using Api.Dtos;
using Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

// No DELETE on purpose: a room that is no longer used is deactivated (IsActive = false) and keeps its history.
[ApiController]
[Route("api/rooms")]
public class RoomsController(AppDbContext db, TimeProvider time) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<RoomDto>>> GetAll(CancellationToken cancellationToken)
    {
        return await db.Rooms
            .OrderBy(r => r.Name)
            .Select(ToDto(LocalNow()))
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var room = await FindDtoAsync(id, cancellationToken);
        return room is null ? NotFound() : room;
    }

    [HttpPost]
    public async Task<ActionResult<RoomDto>> Create(RoomRequestDto request, CancellationToken cancellationToken)
    {
        if (await NameIsTakenAsync(request.Name!, exceptId: null, cancellationToken))
            return DuplicateName();

        var room = new Room
        {
            Name = request.Name!,
            Capacity = request.Capacity,
            Floor = request.Floor,
            Equipment = request.Equipment,
            IsActive = request.IsActive,
        };
        db.Rooms.Add(room);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsDuplicateKey())
        {
            return DuplicateName();
        }

        return CreatedAtAction(nameof(GetById), new { id = room.Id }, await FindDtoAsync(room.Id, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoomDto>> Update(int id, RoomRequestDto request, CancellationToken cancellationToken)
    {
        var room = await db.Rooms.FindAsync([id], cancellationToken);
        if (room is null)
            return NotFound();

        if (await NameIsTakenAsync(request.Name!, exceptId: id, cancellationToken))
            return DuplicateName();

        room.Name = request.Name!;
        room.Capacity = request.Capacity;
        room.Floor = request.Floor;
        room.Equipment = request.Equipment;
        room.IsActive = request.IsActive;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsDuplicateKey())
        {
            return DuplicateName();
        }

        return Ok(await FindDtoAsync(id, cancellationToken));
    }

    // "Now" is the local time of the coworking space, the same clock as the reservation dates.
    private DateTime LocalNow() => time.GetLocalNow().DateTime;

    // A room is available now when it can be booked (active) and no confirmed reservation covers the
    // current instant. The period is half-open: a reservation ending exactly now no longer occupies the room.
    // The subquery is served by the index (RoomId, Status, StartsAt).
    private Expression<Func<Room, RoomDto>> ToDto(DateTime now) => room => new RoomDto(
        room.Id,
        room.Name,
        room.Capacity,
        room.Floor,
        room.Equipment,
        room.IsActive,
        room.IsActive && !db.Reservations.Any(reservation =>
            reservation.RoomId == room.Id
            && reservation.Status == ReservationStatus.Confirmed
            && reservation.StartsAt <= now
            && reservation.EndsAt > now));

    private Task<RoomDto?> FindDtoAsync(int id, CancellationToken cancellationToken) =>
        db.Rooms.Where(r => r.Id == id).Select(ToDto(LocalNow())).SingleOrDefaultAsync(cancellationToken);

    // The comparison uses the column collation (case and accent insensitive), like the unique index.
    private Task<bool> NameIsTakenAsync(string name, int? exceptId, CancellationToken cancellationToken) =>
        db.Rooms.AnyAsync(r => r.Name == name && r.Id != exceptId, cancellationToken);

    // Same 400 format as the DataAnnotations errors, so the front end shows it under the "name" field.
    private ActionResult DuplicateName()
    {
        ModelState.AddModelError(nameof(RoomRequestDto.Name), "Une salle porte déjà ce nom.");
        return ValidationProblem(ModelState);
    }
}
