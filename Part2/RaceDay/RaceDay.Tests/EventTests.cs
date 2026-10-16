using System.Net;
using System.Net.Http.Json;
using RaceDay.Dtos;
using RaceDay.Tests.Infrastructure;
using Xunit;

namespace RaceDay.Tests;

/// <summary>
/// Event management tests: Organisers can create, view, update and delete their events.
/// </summary>
public class EventTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EventTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Organiser_CreatesEvent_Returns201AndEventIsReturned()
    {
        var (organiser, user) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", TestData.NewEventRequest("Cycle"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal("Cycle", created!.EventType);
        Assert.Equal(user.UserId, created.OrganiserId);
    }

    [Fact]
    public async Task Organiser_CreatesEventWithInvalidType_Returns400()
    {
        var (organiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", TestData.NewEventRequest("Swim"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CreatesEventWithMissingName_Returns400()
    {
        var (organiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var request = TestData.NewEventRequest();
        request.Name = string.Empty;

        var response = await organiser.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Anyone_CanViewEventsWithoutLoggingIn()
    {
        var (organiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var created = await TestData.CreateEventAsync(organiser);
        var anonymous = _factory.CreateApiClient();

        var response = await anonymous.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var events = await response.Content.ReadFromJsonAsync<List<EventResponse>>();
        Assert.Contains(events!, e => e.EventId == created.EventId);
    }

    [Fact]
    public async Task GetEvent_UnknownId_Returns404()
    {
        var anonymous = _factory.CreateApiClient();

        var response = await anonymous.GetAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_UpdatesOwnEvent_Returns200WithNewValues()
    {
        var (organiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var created = await TestData.CreateEventAsync(organiser);
        var update = TestData.NewEventRequest("Walk");
        update.Name = "Renamed Event";

        var response = await organiser.PutAsJsonAsync($"/api/events/{created.EventId}", update);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal("Renamed Event", updated!.Name);
        Assert.Equal("Walk", updated.EventType);
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent_Returns403()
    {
        var (owner, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var (otherOrganiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var created = await TestData.CreateEventAsync(owner);

        var response = await otherOrganiser.PutAsJsonAsync(
            $"/api/events/{created.EventId}", TestData.NewEventRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_UpdatesUnknownEvent_Returns404()
    {
        var (organiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");

        var response = await organiser.PutAsJsonAsync("/api/events/999999", TestData.NewEventRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_DeletesOwnEvent_Returns204AndEventIsGone()
    {
        var (organiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var created = await TestData.CreateEventAsync(organiser);

        var delete = await organiser.DeleteAsync($"/api/events/{created.EventId}");
        var afterDelete = await organiser.GetAsync($"/api/events/{created.EventId}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotDeleteAnotherOrganisersEvent_Returns403()
    {
        var (owner, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var (otherOrganiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var created = await TestData.CreateEventAsync(owner);

        var response = await otherOrganiser.DeleteAsync($"/api/events/{created.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_AddsCategoryToOwnEvent_Returns201()
    {
        var (organiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var created = await TestData.CreateEventAsync(organiser);

        var response = await organiser.PostAsJsonAsync($"/api/events/{created.EventId}/categories", new CategoryRequest
        {
            Name = "5km Fun Run",
            DistanceKm = 5,
            Price = 100,
            MaxParticipants = 50
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var category = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.Equal(created.EventId, category!.EventId);
    }
}
