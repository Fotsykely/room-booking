using System.Linq.Expressions;
using Api.Data;
using Api.Dtos;
using Api.Entities;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

// No DELETE on purpose: cancelling a reservation only changes its status, so the history is kept.
[ApiController]
[Route("api/reservations")]
public class ReservationsController(AppDbContext db, ReservationService reservations) : ControllerBase
{
    private static readonly Expression<Func<Reservation, ReservationDto>> ToDto = reservation => new ReservationDto(
        reservation.Id,
        reservation.RoomId,
        reservation.Room.Name,
        reservation.MemberId,
        reservation.Member.Name,
        reservation.StartsAt,
        reservation.EndsAt,
        reservation.Purpose,
        reservation.Status,
        reservation.CreatedAt);

    // The planning. By default only confirmed reservations are returned, sorted by start. "date" keeps the
    // reservations that start on that day (a reservation running past midnight stays on its start day).
    [HttpGet]
    public async Task<ActionResult<List<ReservationDto>>> GetAll(
        [FromQuery] int? roomId,
        [FromQuery] DateOnly? date,
        [FromQuery] bool includeCancelled,
        CancellationToken cancellationToken)
    {
        var query = db.Reservations.AsQueryable();

        if (!includeCancelled)
            query = query.Where(r => r.Status == ReservationStatus.Confirmed);

        if (roomId is not null)
            query = query.Where(r => r.RoomId == roomId);

        if (date is not null)
        {
            var dayStart = date.Value.ToDateTime(TimeOnly.MinValue);
            var dayEnd = dayStart.AddDays(1);
            query = query.Where(r => r.StartsAt >= dayStart && r.StartsAt < dayEnd);
        }

        return await query
            .OrderBy(r => r.StartsAt)
            .ThenBy(r => r.Id)
            .Select(ToDto)
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReservationDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var reservation = await FindDtoAsync(id, cancellationToken);
        return reservation is null ? NotFound() : reservation;
    }

    [HttpPost]
    public async Task<ActionResult<ReservationDto>> Create(ReservationRequestDto request, CancellationToken cancellationToken)
    {
        // [ApiController] has already refused a request with a missing field, so the nullable values are set.
        var result = await reservations.BookAsync(
            request.MemberId,
            request.RoomId,
            request.StartsAt!.Value,
            request.EndsAt!.Value,
            request.Purpose!,
            cancellationToken);

        if (result.Reservation is not { } reservation)
        {
            // Same 400 format as the DataAnnotations errors, so the front end shows each message under its field.
            foreach (var (field, message) in result.Errors)
                ModelState.AddModelError(field, message);

            return ValidationProblem(ModelState);
        }

        return CreatedAtAction(nameof(GetById), new { id = reservation.Id }, await FindDtoAsync(reservation.Id, cancellationToken));
    }

    // Cancelling twice is not an error (double click, second tab): the reservation is simply still cancelled.
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<ReservationDto>> Cancel(int id, CancellationToken cancellationToken)
    {
        var reservation = await db.Reservations.FindAsync([id], cancellationToken);
        if (reservation is null)
            return NotFound();

        reservation.Status = ReservationStatus.Cancelled;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(await FindDtoAsync(id, cancellationToken));
    }

    private Task<ReservationDto?> FindDtoAsync(int id, CancellationToken cancellationToken) =>
        db.Reservations.Where(r => r.Id == id).Select(ToDto).SingleOrDefaultAsync(cancellationToken);
}
