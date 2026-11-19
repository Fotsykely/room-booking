using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Dtos;
using Api.Entities;

namespace Api.Tests;

[Collection(ApiCollection.Name)]
public class MembersEndpointsTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Created_member_can_be_read_back_with_trimmed_values()
    {
        var email = UniqueEmail();

        var response = await _client.PostAsJsonAsync("/api/members",
            new { name = "  Hery Rakoto  ", email = $"  {email}  ", subscription = "Premium" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<MemberDto>();
        Assert.Equal($"/api/members/{created!.Id}", response.Headers.Location!.AbsolutePath);

        var member = await GetMemberAsync(created.Id);
        Assert.Equal("Hery Rakoto", member.Name);
        Assert.Equal(email, member.Email);
        Assert.Equal(SubscriptionType.Premium, member.Subscription);
    }

    [Fact]
    public async Task Subscription_travels_as_text_in_the_JSON_response()
    {
        var created = await CreateMemberAsync();

        var json = await _client.GetFromJsonAsync<JsonElement>($"/api/members/{created.Id}");

        Assert.Equal("Standard", json.GetProperty("subscription").GetString());
    }

    [Fact]
    public async Task Listed_members_contain_the_created_member()
    {
        var created = await CreateMemberAsync();

        var members = await _client.GetFromJsonAsync<List<MemberDto>>("/api/members");

        Assert.Contains(members!, member => member.Id == created.Id);
    }

    [Fact]
    public async Task Creating_a_member_without_a_name_answers_400_on_the_Name_field()
    {
        var response = await _client.PostAsJsonAsync("/api/members", new { name = " ", email = UniqueEmail() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await response.ReadFieldErrorsAsync()).ContainsKey("Name"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public async Task Creating_a_member_with_a_missing_or_invalid_email_answers_400_on_the_Email_field(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/members", new { name = "Test", email });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await response.ReadFieldErrorsAsync()).ContainsKey("Email"));
    }

    [Theory]
    [InlineData(7)]      // a number that matches no subscription
    [InlineData(-1)]
    [InlineData("Foo")]  // an unknown name
    public async Task Creating_a_member_with_an_unknown_subscription_answers_400(object subscription)
    {
        var response = await _client.PostAsJsonAsync("/api/members",
            new { name = "Test", email = UniqueEmail(), subscription });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task An_unknown_subscription_number_is_reported_on_the_Subscription_field()
    {
        var response = await _client.PostAsJsonAsync("/api/members",
            new { name = "Test", email = UniqueEmail(), subscription = 7 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await response.ReadFieldErrorsAsync();
        Assert.Equal(["L'abonnement doit être Standard ou Premium."], errors["Subscription"]);
    }

    [Fact]
    public async Task Creating_a_member_with_an_existing_email_answers_400_whatever_the_case()
    {
        var existing = await CreateMemberAsync();

        var response = await _client.PostAsJsonAsync("/api/members",
            new { name = "Someone else", email = existing.Email.ToUpperInvariant() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await response.ReadFieldErrorsAsync();
        Assert.Equal(["Un membre utilise déjà cette adresse e-mail."], errors["Email"]);
    }

    [Fact]
    public async Task Two_simultaneous_creations_with_the_same_email_create_a_single_member()
    {
        var email = UniqueEmail();

        // Either the check or, if both requests pass it, the unique index refuses the second one: never a 500.
        var responses = await Task.WhenAll(
            _client.PostAsJsonAsync("/api/members", new { name = "A", email }),
            _client.PostAsJsonAsync("/api/members", new { name = "B", email }));

        var statuses = responses.Select(response => response.StatusCode).Order().ToArray();
        Assert.Equal([HttpStatusCode.Created, HttpStatusCode.BadRequest], statuses);
    }

    [Fact]
    public async Task Updated_member_keeps_its_new_values()
    {
        var created = await CreateMemberAsync();
        var newEmail = UniqueEmail();

        var response = await _client.PutAsJsonAsync($"/api/members/{created.Id}",
            new { name = "New name", email = newEmail, subscription = "Premium" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var member = await GetMemberAsync(created.Id);
        Assert.Equal("New name", member.Name);
        Assert.Equal(newEmail, member.Email);
        Assert.Equal(SubscriptionType.Premium, member.Subscription);
    }

    [Fact]
    public async Task Updating_a_member_with_its_own_email_is_allowed()
    {
        var created = await CreateMemberAsync();

        var response = await _client.PutAsJsonAsync($"/api/members/{created.Id}",
            new { name = "Renamed", email = created.Email, subscription = "Standard" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Renamed", (await GetMemberAsync(created.Id)).Name);
    }

    [Fact]
    public async Task Updating_a_member_with_the_email_of_another_member_answers_400_on_the_Email_field()
    {
        var first = await CreateMemberAsync();
        var second = await CreateMemberAsync();

        var response = await _client.PutAsJsonAsync($"/api/members/{second.Id}",
            new { name = "Second", email = first.Email, subscription = "Standard" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await response.ReadFieldErrorsAsync()).ContainsKey("Email"));
    }

    [Fact]
    public async Task Reading_or_updating_an_unknown_member_answers_404()
    {
        var read = await _client.GetAsync("/api/members/999999");
        var update = await _client.PutAsJsonAsync("/api/members/999999", new { name = "Nobody", email = UniqueEmail() });

        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
    }

    private static string UniqueEmail() => $"{Guid.NewGuid():N}@example.com";

    private async Task<MemberDto> CreateMemberAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/members",
            new { name = "Test member", email = UniqueEmail(), subscription = "Standard" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<MemberDto>())!;
    }

    private async Task<MemberDto> GetMemberAsync(int id) =>
        (await _client.GetFromJsonAsync<MemberDto>($"/api/members/{id}"))!;
}
