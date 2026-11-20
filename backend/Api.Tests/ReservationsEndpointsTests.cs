using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Tests;

[Collection(ApiCollection.Name)]
public class ReservationsEndpointsTests
{
    // Every test starts at this instant (xUnit creates a new instance of the class for each test).
    private static readonly DateTime Now = new(2030, 6, 15, 10, 0, 0);
    private static readonly DateTime Tomorrow = Now.Date.AddDays(1);

    // Hour 24 is midnight at the end of tomorrow: TomorrowAt(24, 30) is 00:30 the day after.
    private static DateTime TomorrowAt(int hour, int minute = 0) => Tomorrow.AddHours(hour).AddMinutes(minute);

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ReservationsEndpointsTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        factory.Clock.SetLocalNow(Now);
    }

    // ---- A valid booking

    [Fact]
    public async Task A_booking_in_a_free_slot_answers_201_and_can_be_read_back()
    {
        var room = await CreateRoomAsync();
        var member = await CreateMemberAsync();
        var start = Tomorrow.AddHours(14);

        var response = await BookAsync(room.Id, member.Id, start, start.AddHours(1), purpose: "  Point d'équipe  ");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<ReservationDto>())!;
        Assert.Equal($"/api/reservations/{created.Id}", response.Headers.Location!.AbsolutePath);

        var read = (await _client.GetFromJsonAsync<ReservationDto>($"/api/reservations/{created.Id}"))!;
        Assert.Equal(room.Id, read.RoomId);
        Assert.Equal(room.Name, read.RoomName);
        Assert.Equal(member.Id, read.MemberId);
        Assert.Equal(member.Name, read.MemberName);
        Assert.Equal(start, read.StartsAt);
        Assert.Equal(start.AddHours(1), read.EndsAt);
        Assert.Equal("Point d'équipe", read.Purpose);
        Assert.Equal(ReservationStatus.Confirmed, read.Status);
        Assert.Equal(Now, read.CreatedAt);
    }

    [Fact]
    public async Task Reading_an_unknown_reservation_answers_404()
    {
        var response = await _client.GetAsync("/api/reservations/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Rule: the end is after the start, and a booking lasts 4 hours at most

    [Theory]
    [InlineData(0)]    // the end equals the start
    [InlineData(-30)]  // the end is before the start
    public async Task A_booking_that_does_not_end_after_it_starts_is_refused_on_the_EndsAt_field(int minutesAfterStart)
    {
        var (room, member) = await CreateRoomAndMemberAsync();
        var start = Tomorrow.AddHours(9);

        var response = await BookAsync(room.Id, member.Id, start, start.AddMinutes(minutesAfterStart));

        Assert.Equal("La fin doit être après le début.", await ErrorOnAsync(response, "EndsAt"));
    }

    [Theory]
    [InlineData(240, true)]   // exactly 4 hours: accepted
    [InlineData(241, false)]  // one minute more: refused
    public async Task A_booking_lasts_4_hours_at_most(int minutes, bool accepted)
    {
        var (room, member) = await CreateRoomAndMemberAsync();
        var start = Tomorrow.AddHours(9);

        var response = await BookAsync(room.Id, member.Id, start, start.AddMinutes(minutes));

        if (accepted)
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        else
            Assert.Equal("Une réservation dure au maximum 4 heures.", await ErrorOnAsync(response, "EndsAt"));
    }

    // ---- Rule: a booking starts between 8:00 and 20:00 (both included) and ends by midnight at the latest
    // Every duration below is between 2 and 5 hours, so the duration rules never hide the one under test.

    [Theory]
    [InlineData(8, 0, 10, 0)]    // first possible start
    [InlineData(20, 0, 22, 0)]   // last possible start (20:00 included); it ends after 20:00, which is allowed
    [InlineData(19, 0, 24, 0)]   // ends exactly at midnight: allowed
    public async Task A_booking_that_starts_between_8_and_20_and_ends_by_midnight_is_accepted(
        int startHour, int startMinute, int endHour, int endMinute)
    {
        var (room, member) = await CreateRoomAndMemberAsync();

        var response = await BookAsync(room.Id, member.Id, TomorrowAt(startHour, startMinute), TomorrowAt(endHour, endMinute));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData(7, 59, 10, 0)]   // one minute before the first possible start
    [InlineData(20, 1, 22, 1)]   // one minute after the last possible start
    public async Task A_booking_that_starts_outside_8_to_20_is_refused_under_the_start_field(
        int startHour, int startMinute, int endHour, int endMinute)
    {
        var (room, member) = await CreateRoomAndMemberAsync();

        var response = await BookAsync(room.Id, member.Id, TomorrowAt(startHour, startMinute), TomorrowAt(endHour, endMinute));

        // The message tells the allowed hours (it only checks the times, not the exact wording).
        var message = await ErrorOnAsync(response, "StartsAt");
        Assert.Contains("08:00", message);
        Assert.Contains("20:00", message);
    }

    [Theory]
    [InlineData(19, 30, 24, 30)]  // 5 hours, ends 30 minutes after midnight
    [InlineData(19, 1, 24, 1)]    // ends one minute after midnight
    public async Task A_booking_that_ends_after_midnight_is_refused_under_the_end_field(
        int startHour, int startMinute, int endHour, int endMinute)
    {
        var (room, member) = await CreateRoomAndMemberAsync();

        var response = await BookAsync(room.Id, member.Id, TomorrowAt(startHour, startMinute), TomorrowAt(endHour, endMinute));

        Assert.Contains("minuit", await ErrorOnAsync(response, "EndsAt"));
    }

    // ---- Rule: a booking cannot start in the past

    [Theory]
    [InlineData(-1, false)]        // one minute ago: refused
    [InlineData(-1440, false)]     // yesterday: refused
    [InlineData(0, true)]          // this very minute: accepted
    [InlineData(1, true)]
    public async Task A_booking_cannot_start_in_the_past(int minutesFromNow, bool accepted)
    {
        var (room, member) = await CreateRoomAndMemberAsync();
        var start = Now.AddMinutes(minutesFromNow);

        var response = await BookAsync(room.Id, member.Id, start, start.AddHours(1));

        if (accepted)
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        else
            Assert.Equal("Une réservation ne peut pas commencer dans le passé.", await ErrorOnAsync(response, "StartsAt"));
    }

    [Fact]
    public async Task The_current_minute_is_accepted_even_when_seconds_have_passed()
    {
        var (room, member) = await CreateRoomAndMemberAsync();
        _factory.Clock.SetLocalNow(Now.AddSeconds(45));

        // 10:00 was 45 seconds ago, but a form only offers whole minutes: 10:00 is still "now".
        var accepted = await BookAsync(room.Id, member.Id, Now, Now.AddHours(1));
        // 09:59 is a different minute: it is in the past.
        var refused = await BookAsync(room.Id, member.Id, Now.AddMinutes(-1), Now.AddMinutes(30));

        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        await ErrorOnAsync(refused, "StartsAt");
    }

    // ---- Rule: no overlap on the same room

    [Theory]
    [InlineData(30, 90, false)]    // starts inside the existing booking, ends after it
    [InlineData(-30, 30, false)]   // starts before it, ends inside it
    [InlineData(15, 45, false)]    // fully inside it
    [InlineData(-60, 120, false)]  // contains it
    [InlineData(0, 60, false)]     // identical
    [InlineData(-60, 0, true)]     // ends exactly when it starts: no overlap
    [InlineData(60, 120, true)]    // starts exactly when it ends: no overlap
    public async Task Two_confirmed_bookings_cannot_overlap_on_the_same_room(
        int startMinutes, int endMinutes, bool accepted)
    {
        var room = await CreateRoomAsync();
        var existing = Tomorrow.AddHours(14); // 14:00 - 15:00
        await BookOkAsync(room.Id, (await CreateMemberAsync()).Id, existing, existing.AddHours(1));

        var response = await BookAsync(
            room.Id, (await CreateMemberAsync()).Id, existing.AddMinutes(startMinutes), existing.AddMinutes(endMinutes));

        if (accepted)
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        else
            Assert.Equal("Cette salle est déjà réservée le 16/06/2030 de 14:00 à 15:00.", await ErrorOnAsync(response, "RoomId"));
    }

    [Fact]
    public async Task The_same_slot_can_be_booked_in_another_room()
    {
        var start = Tomorrow.AddHours(14);
        var member = await CreateMemberAsync(subscription: "Premium");
        await BookOkAsync((await CreateRoomAsync()).Id, member.Id, start, start.AddHours(1));

        var response = await BookAsync((await CreateRoomAsync()).Id, member.Id, start, start.AddHours(1));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task A_cancelled_booking_does_not_block_its_slot()
    {
        var room = await CreateRoomAsync();
        var start = Tomorrow.AddHours(14);
        var first = await BookOkAsync(room.Id, (await CreateMemberAsync()).Id, start, start.AddHours(1));
        await _client.PostAsync($"/api/reservations/{first.Id}/cancel", null);

        var response = await BookAsync(room.Id, (await CreateMemberAsync()).Id, start, start.AddHours(1));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task The_overlap_message_gives_both_days_when_the_existing_booking_runs_past_midnight()
    {
        var room = await CreateRoomAsync();
        // A booking that runs past midnight cannot be created any more, but data from before the rule can exist:
        // it is inserted directly. 18:00 to 02:00 the next day.
        var late = Tomorrow.AddHours(18);
        await InsertReservationAsync(room.Id, (await CreateMemberAsync()).Id, late, late.AddHours(8), ReservationStatus.Confirmed);

        var response = await BookAsync(room.Id, (await CreateMemberAsync()).Id, late, late.AddHours(2));

        Assert.Equal("Cette salle est déjà réservée du 16/06/2030 à 18:00 au 17/06/2030 à 02:00.",
            await ErrorOnAsync(response, "RoomId"));
    }

    // ---- Rule: only an active room can be booked, for an existing member

    [Fact]
    public async Task An_inactive_room_cannot_be_booked()
    {
        var room = await CreateRoomAsync(active: false);
        var member = await CreateMemberAsync();
        var start = Tomorrow.AddHours(9);

        var response = await BookAsync(room.Id, member.Id, start, start.AddHours(1));

        Assert.Equal("Cette salle est désactivée : elle ne peut plus être réservée.", await ErrorOnAsync(response, "RoomId"));
    }

    [Fact]
    public async Task An_unknown_room_or_member_is_refused_on_its_own_field()
    {
        var (room, member) = await CreateRoomAndMemberAsync();
        var start = Tomorrow.AddHours(9);

        var unknownRoom = await BookAsync(999999, member.Id, start, start.AddHours(1));
        var unknownMember = await BookAsync(room.Id, 999999, start, start.AddHours(1));

        Assert.Equal("Cette salle n'existe pas.", await ErrorOnAsync(unknownRoom, "RoomId"));
        Assert.Equal("Ce membre n'existe pas.", await ErrorOnAsync(unknownMember, "MemberId"));
    }

    [Fact]
    public async Task Several_broken_rules_are_all_reported_at_once()
    {
        var start = Now.AddDays(-1);

        var response = await BookAsync(999999, 999999, start, start.AddHours(6));

        var errors = await response.ReadFieldErrorsAsync();
        Assert.Equal(["EndsAt", "MemberId", "RoomId", "StartsAt"], errors.Keys.Order().ToArray());
    }

    [Fact]
    public async Task Deactivating_a_room_does_not_remove_its_past_bookings_from_the_planning()
    {
        var (room, member) = await CreateRoomAndMemberAsync();
        var start = Tomorrow.AddHours(9);
        await BookOkAsync(room.Id, member.Id, start, start.AddHours(1));

        await _client.PutAsJsonAsync($"/api/rooms/{room.Id}", new { name = room.Name, capacity = 4, isActive = false });

        Assert.Single(await ListAsync($"?roomId={room.Id}"));
    }

    // ---- Rule: a Standard member has at most 3 upcoming confirmed bookings

    [Fact]
    public async Task A_standard_member_cannot_have_a_fourth_upcoming_booking()
    {
        var room = await CreateRoomAsync();
        var member = await CreateMemberAsync(subscription: "Standard");
        for (var day = 1; day <= 3; day++)
            await BookOkAsync(room.Id, member.Id, Now.Date.AddDays(day).AddHours(9), Now.Date.AddDays(day).AddHours(10));

        var response = await BookAsync(room.Id, member.Id, Now.Date.AddDays(4).AddHours(9), Now.Date.AddDays(4).AddHours(10));

        Assert.Equal(
            "Un membre Standard ne peut pas avoir plus de 3 réservations confirmées à venir (l'abonnement Premium n'a pas de limite).",
            await ErrorOnAsync(response, "MemberId"));
    }

    [Fact]
    public async Task A_premium_member_has_no_limit()
    {
        var room = await CreateRoomAsync();
        var member = await CreateMemberAsync(subscription: "Premium");

        for (var day = 1; day <= 6; day++)
        {
            var response = await BookAsync(room.Id, member.Id, Now.Date.AddDays(day).AddHours(9), Now.Date.AddDays(day).AddHours(10));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }

    [Fact]
    public async Task Cancelling_a_booking_frees_a_place_in_the_limit()
    {
        var room = await CreateRoomAsync();
        var member = await CreateMemberAsync(subscription: "Standard");
        var first = await BookOkAsync(room.Id, member.Id, Now.Date.AddDays(1).AddHours(9), Now.Date.AddDays(1).AddHours(10));
        await BookOkAsync(room.Id, member.Id, Now.Date.AddDays(2).AddHours(9), Now.Date.AddDays(2).AddHours(10));
        await BookOkAsync(room.Id, member.Id, Now.Date.AddDays(3).AddHours(9), Now.Date.AddDays(3).AddHours(10));
        await _client.PostAsync($"/api/reservations/{first.Id}/cancel", null);

        var response = await BookAsync(room.Id, member.Id, Now.Date.AddDays(4).AddHours(9), Now.Date.AddDays(4).AddHours(10));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Past_ongoing_and_cancelled_bookings_do_not_count_in_the_limit()
    {
        var room = await CreateRoomAsync();
        var member = await CreateMemberAsync(subscription: "Standard");
        await InsertReservationAsync(room.Id, member.Id, Now.AddDays(-3), Now.AddDays(-3).AddHours(1), ReservationStatus.Confirmed); // past
        await InsertReservationAsync(room.Id, member.Id, Now.AddMinutes(-30), Now.AddMinutes(30), ReservationStatus.Confirmed);      // ongoing
        await InsertReservationAsync(room.Id, member.Id, Now.AddDays(2), Now.AddDays(2).AddHours(1), ReservationStatus.Cancelled);   // cancelled

        for (var day = 5; day <= 7; day++)
        {
            var response = await BookAsync(room.Id, member.Id, Now.Date.AddDays(day).AddHours(9), Now.Date.AddDays(day).AddHours(10));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        var fourth = await BookAsync(room.Id, member.Id, Now.Date.AddDays(8).AddHours(9), Now.Date.AddDays(8).AddHours(10));
        await ErrorOnAsync(fourth, "MemberId");
    }

    // ---- Shape of the request

    [Fact]
    public async Task A_request_with_missing_fields_is_refused_field_by_field()
    {
        var response = await _client.PostAsJsonAsync("/api/reservations", new { purpose = "  " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await response.ReadFieldErrorsAsync();
        Assert.Equal(["Choisissez un membre."], errors["MemberId"]);
        Assert.Equal(["Choisissez une salle."], errors["RoomId"]);
        Assert.Equal(["La date de début est obligatoire."], errors["StartsAt"]);
        Assert.Equal(["La date de fin est obligatoire."], errors["EndsAt"]);
        Assert.Equal(["L'objet est obligatoire."], errors["Purpose"]);
    }

    // ---- The planning

    [Fact]
    public async Task The_planning_lists_confirmed_bookings_sorted_by_start_and_hides_cancelled_ones()
    {
        var (room, member) = await CreateRoomAndMemberAsync(subscription: "Premium");
        var late = await BookOkAsync(room.Id, member.Id, Tomorrow.AddHours(16), Tomorrow.AddHours(17));
        var early = await BookOkAsync(room.Id, member.Id, Tomorrow.AddHours(9), Tomorrow.AddHours(10));
        var cancelled = await BookOkAsync(room.Id, member.Id, Tomorrow.AddHours(12), Tomorrow.AddHours(13));
        await _client.PostAsync($"/api/reservations/{cancelled.Id}/cancel", null);

        var planning = await ListAsync($"?roomId={room.Id}");

        Assert.Equal([early.Id, late.Id], planning.Select(r => r.Id).ToArray());
    }

    [Fact]
    public async Task The_planning_can_include_cancelled_bookings()
    {
        var (room, member) = await CreateRoomAndMemberAsync();
        var booking = await BookOkAsync(room.Id, member.Id, Tomorrow.AddHours(9), Tomorrow.AddHours(10));
        await _client.PostAsync($"/api/reservations/{booking.Id}/cancel", null);

        var planning = await ListAsync($"?roomId={room.Id}&includeCancelled=true");

        var listed = Assert.Single(planning);
        Assert.Equal(ReservationStatus.Cancelled, listed.Status);
    }

    [Fact]
    public async Task The_planning_filters_by_room_and_by_day()
    {
        var member = await CreateMemberAsync(subscription: "Premium");
        var room = await CreateRoomAsync();
        var otherRoom = await CreateRoomAsync();
        var onTheDay = await BookOkAsync(room.Id, member.Id, Tomorrow.AddHours(9), Tomorrow.AddHours(10));
        await BookOkAsync(room.Id, member.Id, Tomorrow.AddDays(1).AddHours(9), Tomorrow.AddDays(1).AddHours(10));
        await BookOkAsync(otherRoom.Id, member.Id, Tomorrow.AddHours(9), Tomorrow.AddHours(10));

        var planning = await ListAsync($"?roomId={room.Id}&date={Tomorrow:yyyy-MM-dd}");

        Assert.Equal([onTheDay.Id], planning.Select(r => r.Id).ToArray());
    }

    [Fact]
    public async Task A_booking_running_past_midnight_belongs_to_its_start_day()
    {
        var (room, member) = await CreateRoomAndMemberAsync();
        // Cannot be created through the API any more (it ends after midnight), but data from before the rule can
        // exist: it is inserted directly.
        await InsertReservationAsync(room.Id, member.Id, Tomorrow.AddHours(23), Tomorrow.AddHours(25), ReservationStatus.Confirmed);

        var startDay = await ListAsync($"?roomId={room.Id}&date={Tomorrow:yyyy-MM-dd}");
        var nextDay = await ListAsync($"?roomId={room.Id}&date={Tomorrow.AddDays(1):yyyy-MM-dd}");

        Assert.Single(startDay);
        Assert.Empty(nextDay);
    }

    [Fact]
    public async Task An_invalid_date_filter_answers_400()
    {
        var response = await _client.GetAsync("/api/reservations?date=not-a-date");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- Cancelling

    [Fact]
    public async Task Cancelling_a_booking_keeps_it_in_the_database_with_the_Cancelled_status()
    {
        var (room, member) = await CreateRoomAndMemberAsync();
        var booking = await BookOkAsync(room.Id, member.Id, Tomorrow.AddHours(9), Tomorrow.AddHours(10));

        var response = await _client.PostAsync($"/api/reservations/{booking.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cancelled = (await response.Content.ReadFromJsonAsync<ReservationDto>())!;
        Assert.Equal(ReservationStatus.Cancelled, cancelled.Status);
        Assert.Empty(await ListAsync($"?roomId={room.Id}"));

        ReservationStatus? stored = null;
        await _factory.UseDbAsync(async db =>
            stored = (await db.Reservations.SingleAsync(r => r.Id == booking.Id)).Status);
        Assert.Equal(ReservationStatus.Cancelled, stored);
    }

    [Fact]
    public async Task Cancelling_twice_is_not_an_error()
    {
        var (room, member) = await CreateRoomAndMemberAsync();
        var booking = await BookOkAsync(room.Id, member.Id, Tomorrow.AddHours(9), Tomorrow.AddHours(10));

        var first = await _client.PostAsync($"/api/reservations/{booking.Id}/cancel", null);
        var second = await _client.PostAsync($"/api/reservations/{booking.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    [Fact]
    public async Task Cancelling_an_unknown_booking_answers_404()
    {
        var response = await _client.PostAsync("/api/reservations/999999/cancel", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Simultaneous requests: the locks make the checks and the insert atomic

    [Fact]
    public async Task Simultaneous_bookings_of_the_same_slot_create_a_single_reservation()
    {
        var room = await CreateRoomAsync();
        var members = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => CreateMemberAsync()));
        var start = Tomorrow.AddHours(9);

        var responses = await Task.WhenAll(members.Select(m => BookAsync(room.Id, m.Id, start, start.AddHours(1))));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(7, responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest));
        Assert.Single(await ListAsync($"?roomId={room.Id}"));
    }

    [Fact]
    public async Task Simultaneous_bookings_by_a_Standard_member_never_exceed_the_limit()
    {
        var member = await CreateMemberAsync(subscription: "Standard");
        var rooms = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => CreateRoomAsync()));
        var start = Tomorrow.AddHours(9);

        // Different rooms, so no overlap: only the limit of 3 can refuse a request.
        var responses = await Task.WhenAll(rooms.Select(r => BookAsync(r.Id, member.Id, start, start.AddHours(1))));

        Assert.Equal(3, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(5, responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest));
        var stored = 0;
        await _factory.UseDbAsync(async db => stored = await db.Reservations.CountAsync(r => r.MemberId == member.Id));
        Assert.Equal(3, stored);
    }

    // ---- Helpers

    private async Task<RoomDto> CreateRoomAsync(bool active = true)
    {
        var response = await _client.PostAsJsonAsync("/api/rooms",
            new { name = $"Room {Guid.NewGuid():N}", capacity = 6, floor = 1, isActive = active });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<RoomDto>())!;
    }

    private async Task<MemberDto> CreateMemberAsync(string subscription = "Standard")
    {
        var response = await _client.PostAsJsonAsync("/api/members",
            new { name = "Test member", email = $"{Guid.NewGuid():N}@example.com", subscription });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<MemberDto>())!;
    }

    private async Task<(RoomDto Room, MemberDto Member)> CreateRoomAndMemberAsync(string subscription = "Standard") =>
        (await CreateRoomAsync(), await CreateMemberAsync(subscription));

    private Task<HttpResponseMessage> BookAsync(
        int roomId, int memberId, DateTime startsAt, DateTime endsAt, string purpose = "Réunion") =>
        _client.PostAsJsonAsync("/api/reservations", new { roomId, memberId, startsAt, endsAt, purpose });

    private async Task<ReservationDto> BookOkAsync(int roomId, int memberId, DateTime startsAt, DateTime endsAt)
    {
        var response = await BookAsync(roomId, memberId, startsAt, endsAt);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ReservationDto>())!;
    }

    private async Task<List<ReservationDto>> ListAsync(string query) =>
        (await _client.GetFromJsonAsync<List<ReservationDto>>($"/api/reservations{query}"))!;

    // Checks that the answer is a 400 with a single message on the given field, and returns that message.
    private static async Task<string> ErrorOnAsync(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await response.ReadFieldErrorsAsync();
        Assert.True(errors.ContainsKey(field), $"Expected an error on {field}, got: {string.Join(", ", errors.Keys)}");
        return Assert.Single(errors[field]);
    }

    // Inserts straight into the database what the API refuses to create (a booking in the past or in progress).
    private Task InsertReservationAsync(int roomId, int memberId, DateTime startsAt, DateTime endsAt, ReservationStatus status) =>
        _factory.UseDbAsync(async db =>
        {
            db.Reservations.Add(new Reservation
            {
                RoomId = roomId,
                MemberId = memberId,
                StartsAt = startsAt,
                EndsAt = endsAt,
                Purpose = "Inserted by a test",
                Status = status,
                CreatedAt = startsAt.AddDays(-1),
            });
            await db.SaveChangesAsync();
        });
}
