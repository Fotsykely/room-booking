using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Tests;

[Collection(ApiCollection.Name)]
public class RoomsEndpointsTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Created_room_can_be_read_back_with_trimmed_values()
    {
        var name = UniqueName();

        var response = await _client.PostAsJsonAsync("/api/rooms",
            new { name = $"  {name}  ", capacity = 6, floor = 2, equipment = "  Projector  " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<RoomDto>();
        Assert.Equal($"/api/rooms/{created!.Id}", response.Headers.Location!.AbsolutePath);

        var room = await GetRoomAsync(created.Id);
        Assert.Equal(name, room.Name);
        Assert.Equal(6, room.Capacity);
        Assert.Equal(2, room.Floor);
        Assert.Equal("Projector", room.Equipment);
        Assert.True(room.IsActive);
    }

    [Fact]
    public async Task Blank_equipment_is_stored_as_no_equipment()
    {
        var created = await CreateRoomAsync(equipment: "   ");

        Assert.Null((await GetRoomAsync(created.Id)).Equipment);
    }

    [Fact]
    public async Task Listed_rooms_contain_the_created_room()
    {
        var created = await CreateRoomAsync();

        var rooms = await _client.GetFromJsonAsync<List<RoomDto>>("/api/rooms");

        Assert.Contains(rooms!, room => room.Id == created.Id);
    }

    [Fact]
    public async Task Creating_a_room_without_a_name_answers_400_on_the_Name_field()
    {
        var response = await _client.PostAsJsonAsync("/api/rooms", new { name = "  ", capacity = 4 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await response.ReadFieldErrorsAsync()).ContainsKey("Name"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task Creating_a_room_with_a_capacity_below_one_answers_400_on_the_Capacity_field(int capacity)
    {
        var response = await _client.PostAsJsonAsync("/api/rooms", new { name = UniqueName(), capacity });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await response.ReadFieldErrorsAsync();
        Assert.Equal(["La capacité doit être un entier positif."], errors["Capacity"]);
    }

    [Fact]
    public async Task Creating_a_room_with_an_existing_name_answers_400_whatever_the_case()
    {
        var existing = await CreateRoomAsync();

        var response = await _client.PostAsJsonAsync("/api/rooms",
            new { name = existing.Name.ToLowerInvariant(), capacity = 4 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await response.ReadFieldErrorsAsync();
        Assert.Equal(["Une salle porte déjà ce nom."], errors["Name"]);
    }

    [Fact]
    public async Task Two_simultaneous_creations_with_the_same_name_create_a_single_room()
    {
        var name = UniqueName();

        // Either the check or, if both requests pass it, the unique index refuses the second one: never a 500.
        var responses = await Task.WhenAll(
            _client.PostAsJsonAsync("/api/rooms", new { name, capacity = 4 }),
            _client.PostAsJsonAsync("/api/rooms", new { name, capacity = 4 }));

        var statuses = responses.Select(response => response.StatusCode).Order().ToArray();
        Assert.Equal([HttpStatusCode.Created, HttpStatusCode.BadRequest], statuses);
    }

    [Fact]
    public async Task Updated_room_keeps_its_new_values_and_can_be_deactivated()
    {
        var created = await CreateRoomAsync();
        var newName = UniqueName();

        var response = await _client.PutAsJsonAsync($"/api/rooms/{created.Id}",
            new { name = newName, capacity = 10, floor = 3, equipment = "Whiteboard", isActive = false });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var room = await GetRoomAsync(created.Id);
        Assert.Equal(newName, room.Name);
        Assert.Equal(10, room.Capacity);
        Assert.Equal(3, room.Floor);
        Assert.Equal("Whiteboard", room.Equipment);
        Assert.False(room.IsActive);
    }

    [Fact]
    public async Task Updating_a_room_with_its_own_name_is_allowed()
    {
        var created = await CreateRoomAsync();

        var response = await _client.PutAsJsonAsync($"/api/rooms/{created.Id}",
            new { name = created.Name, capacity = 9, isActive = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(9, (await GetRoomAsync(created.Id)).Capacity);
    }

    [Fact]
    public async Task Updating_a_room_with_the_name_of_another_room_answers_400_on_the_Name_field()
    {
        var first = await CreateRoomAsync();
        var second = await CreateRoomAsync();

        var response = await _client.PutAsJsonAsync($"/api/rooms/{second.Id}",
            new { name = first.Name, capacity = 4, isActive = true });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await response.ReadFieldErrorsAsync()).ContainsKey("Name"));
    }

    [Fact]
    public async Task Reading_or_updating_an_unknown_room_answers_404()
    {
        var read = await _client.GetAsync("/api/rooms/999999");
        var update = await _client.PutAsJsonAsync("/api/rooms/999999", new { name = UniqueName(), capacity = 4 });

        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
    }

    [Theory]
    [InlineData(-1, true)]  // one minute before the start: still free
    [InlineData(0, false)]  // exactly at the start: occupied
    [InlineData(30, false)]
    [InlineData(59, false)]
    [InlineData(60, true)]  // exactly at the end: free again (the period is half-open)
    public async Task Room_is_occupied_from_the_start_of_a_confirmed_reservation_until_its_end(
        int minutesAfterStart, bool expectedAvailable)
    {
        var room = await CreateRoomAsync();
        var start = new DateTime(2030, 6, 15, 14, 0, 0);
        await InsertReservationAsync(room.Id, start, start.AddHours(1), ReservationStatus.Confirmed);
        factory.Clock.SetLocalNow(start.AddMinutes(minutesAfterStart));

        var read = await GetRoomAsync(room.Id);

        Assert.Equal(expectedAvailable, read.IsAvailableNow);
    }

    [Fact]
    public async Task A_cancelled_reservation_does_not_occupy_the_room()
    {
        var room = await CreateRoomAsync();
        var start = new DateTime(2030, 6, 16, 9, 0, 0);
        await InsertReservationAsync(room.Id, start, start.AddHours(2), ReservationStatus.Cancelled);
        factory.Clock.SetLocalNow(start.AddMinutes(30));

        var rooms = await _client.GetFromJsonAsync<List<RoomDto>>("/api/rooms");

        Assert.True(rooms!.Single(r => r.Id == room.Id).IsAvailableNow);
    }

    [Fact]
    public async Task An_inactive_room_is_never_available()
    {
        var created = await CreateRoomAsync();
        await _client.PutAsJsonAsync($"/api/rooms/{created.Id}", new { name = created.Name, capacity = 4, isActive = false });

        var room = await GetRoomAsync(created.Id);

        Assert.False(room.IsActive);
        Assert.False(room.IsAvailableNow);
    }

    [Fact]
    public async Task Deactivating_a_room_keeps_its_reservations()
    {
        var created = await CreateRoomAsync();
        var start = new DateTime(2030, 6, 17, 9, 0, 0);
        await InsertReservationAsync(created.Id, start, start.AddHours(1), ReservationStatus.Confirmed);

        await _client.PutAsJsonAsync($"/api/rooms/{created.Id}", new { name = created.Name, capacity = 4, isActive = false });

        var remaining = 0;
        await factory.UseDbAsync(async db => remaining = await db.Reservations.CountAsync(r => r.RoomId == created.Id));
        Assert.Equal(1, remaining);
    }

    private static string UniqueName() => $"Room {Guid.NewGuid():N}";

    private async Task<RoomDto> CreateRoomAsync(string? equipment = null)
    {
        var response = await _client.PostAsJsonAsync("/api/rooms",
            new { name = UniqueName(), capacity = 4, floor = 1, equipment });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<RoomDto>())!;
    }

    private async Task<RoomDto> GetRoomAsync(int id) =>
        (await _client.GetFromJsonAsync<RoomDto>($"/api/rooms/{id}"))!;

    private Task InsertReservationAsync(int roomId, DateTime startsAt, DateTime endsAt, ReservationStatus status) =>
        factory.UseDbAsync(async db =>
        {
            db.Reservations.Add(new Reservation
            {
                RoomId = roomId,
                Member = new Member { Name = "Test member", Email = $"{Guid.NewGuid():N}@example.com" },
                StartsAt = startsAt,
                EndsAt = endsAt,
                Purpose = "Test",
                Status = status,
                CreatedAt = startsAt.AddDays(-1),
            });
            await db.SaveChangesAsync();
        });
}
