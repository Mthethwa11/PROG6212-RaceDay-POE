using System.Net;
using System.Net.Http.Json;
using RaceDay.Dtos;
using RaceDay.Tests.Infrastructure;
using Xunit;

namespace RaceDay.Tests;

/// <summary>
/// Role-based access tests: the wrong role (403) and no login at all (401) are rejected.
/// </summary>
public class RoleEnforcementTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public RoleEnforcementTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NotLoggedIn_CreateEvent_Returns401()
    {
        var anonymous = _factory.CreateApiClient();

        var response = await anonymous.PostAsJsonAsync("/api/events", TestData.NewEventRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CreateEvent_Returns403()
    {
        var (participant, _) = await TestData.RegisterAndLoginAsync(_factory, "Participant");

        var response = await participant.PostAsJsonAsync("/api/events", TestData.NewEventRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_AddCategory_Returns403()
    {
        var (organiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var (participant, _) = await TestData.RegisterAndLoginAsync(_factory, "Participant");
        var created = await TestData.CreateEventAsync(organiser);

        var response = await participant.PostAsJsonAsync($"/api/events/{created.EventId}/categories", new CategoryRequest
        {
            Name = "Sneaky",
            DistanceKm = 5,
            Price = 0,
            MaxParticipants = 10
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_Enrol_Returns403()
    {
        var (organiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var created = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, created.EventId);

        var response = await organiser.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task NotLoggedIn_Enrol_Returns401()
    {
        var (organiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var created = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, created.EventId);
        var anonymous = _factory.CreateApiClient();

        var response = await anonymous.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Participant_ViewEventEnrolments_Returns403()
    {
        var (organiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var (participant, _) = await TestData.RegisterAndLoginAsync(_factory, "Participant");
        var created = await TestData.CreateEventAsync(organiser);

        var response = await participant.GetAsync($"/api/events/{created.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_ViewOwnEnrolmentsList_Returns403()
    {
        var (organiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");

        var response = await organiser.GetAsync("/api/users/me/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_RecordResult_Returns403()
    {
        var (organiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var (participant, _) = await TestData.RegisterAndLoginAsync(_factory, "Participant");
        var created = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, created.EventId);
        var enrolment = await TestData.EnrolAsync(participant, category.CategoryId);

        var response = await participant.PostAsJsonAsync(
            $"/api/enrolments/{enrolment.EnrolmentId}/result",
            new ResultRequest { FinishTimeSeconds = 3000, Position = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
