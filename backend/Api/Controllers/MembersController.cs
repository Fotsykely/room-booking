using System.Linq.Expressions;
using Api.Data;
using Api.Dtos;
using Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

// No DELETE on purpose: reservations keep pointing at their member.
[ApiController]
[Route("api/members")]
public class MembersController(AppDbContext db) : ControllerBase
{
    private static readonly Expression<Func<Member, MemberDto>> ToDto =
        member => new MemberDto(member.Id, member.Name, member.Email, member.Subscription);

    [HttpGet]
    public async Task<ActionResult<List<MemberDto>>> GetAll(CancellationToken cancellationToken)
    {
        return await db.Members.OrderBy(m => m.Name).Select(ToDto).ToListAsync(cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MemberDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var member = await FindDtoAsync(id, cancellationToken);
        return member is null ? NotFound() : member;
    }

    [HttpPost]
    public async Task<ActionResult<MemberDto>> Create(MemberRequestDto request, CancellationToken cancellationToken)
    {
        if (await EmailIsTakenAsync(request.Email!, exceptId: null, cancellationToken))
            return DuplicateEmail();

        var member = new Member
        {
            Name = request.Name!,
            Email = request.Email!,
            Subscription = request.Subscription,
        };
        db.Members.Add(member);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsDuplicateKey())
        {
            return DuplicateEmail();
        }

        return CreatedAtAction(nameof(GetById), new { id = member.Id }, await FindDtoAsync(member.Id, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MemberDto>> Update(int id, MemberRequestDto request, CancellationToken cancellationToken)
    {
        var member = await db.Members.FindAsync([id], cancellationToken);
        if (member is null)
            return NotFound();

        if (await EmailIsTakenAsync(request.Email!, exceptId: id, cancellationToken))
            return DuplicateEmail();

        member.Name = request.Name!;
        member.Email = request.Email!;
        member.Subscription = request.Subscription;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsDuplicateKey())
        {
            return DuplicateEmail();
        }

        return Ok(await FindDtoAsync(id, cancellationToken));
    }

    private Task<MemberDto?> FindDtoAsync(int id, CancellationToken cancellationToken) =>
        db.Members.Where(m => m.Id == id).Select(ToDto).SingleOrDefaultAsync(cancellationToken);

    // The comparison uses the column collation (case insensitive), like the unique index.
    private Task<bool> EmailIsTakenAsync(string email, int? exceptId, CancellationToken cancellationToken) =>
        db.Members.AnyAsync(m => m.Email == email && m.Id != exceptId, cancellationToken);

    // Same 400 format as the DataAnnotations errors, so the front end shows it under the "email" field.
    private ActionResult DuplicateEmail()
    {
        ModelState.AddModelError(nameof(MemberRequestDto.Email), "Un membre utilise déjà cette adresse e-mail.");
        return ValidationProblem(ModelState);
    }
}
