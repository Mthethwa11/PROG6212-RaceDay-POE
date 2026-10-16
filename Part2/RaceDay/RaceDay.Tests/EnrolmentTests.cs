using System.Net;
using System.Net.Http.Json;
using RaceDay.Dtos;
using RaceDay.Tests.Infrastructure;
using Xunit;

namespace RaceDay.Tests;

/// <summary>
/// Event enrolment tests: Participants can enrol, and the enrolment is recorded correctly.
/// </summary>
public class EnrolmentTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EnrolmentTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Participant_Enrols_Returns201AndLinksParticipantEventAndCategory()
    {
        var (organiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var (participant, participantUser) = await TestData.RegisterAndLoginAsync(_factory, "Participant");
        var created = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, created.EventId);

        var response = await participant.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrolment = await response.Content.ReadFromJsonAsync<EnrolmentResponse>();
        Assert.Equal(participantUser.UserId, enrolment!.ParticipantId);
        Assert.Equal(created.EventId, enrolment.EventId);
        Assert.Equal(category.CategoryId, enrolment.CategoryId);
        Assert.Equal("Confirmed", enrolment.Status);
    }

    [Fact]
    public async Task Participant_EnrolsTwiceInSameCategory_Returns409()
    {
        var (organiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var (participant, _) = await TestData.RegisterAndLoginAsync(_factory, "Participant");
        var created = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, created.EventId);
        await TestData.EnrolAsync(participant, category.CategoryId);

        var second = await participant.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Participant_EnrolsInFullCategory_Returns409()
    {
        var (organiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var (first, _) = await TestData.RegisterAndLoginAsync(_factory, "Participant");
        var (second, _) = await TestData.RegisterAndLoginAsync(_factory, "Participant");
        var created = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, created.EventId, maxParticipants: 1);
        await TestData.EnrolAsync(first, category.CategoryId);

        var response = await second.PostAsync($"/api/categories/{category.CategoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Participant_EnrolsInUnknownCategory_Returns404()
    {
        var (participant, _) = await TestData.RegisterAndLoginAsync(_factory, "Participant");

        var response = await participant.PostAsync("/api/categories/999999/enrolments", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Participant_MyEnrolments_ContainsTheEnrolment()
    {
        var (organiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var (participant, _) = await TestData.RegisterAndLoginAsync(_factory, "Participant");
        var created = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, created.EventId);
        var enrolment = await TestData.EnrolAsync(participant, category.CategoryId);

        var response = await participant.GetAsync("/api/users/me/enrolments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<EnrolmentResponse>>();
        Assert.Contains(list!, e => e.EnrolmentId == enrolment.EnrolmentId);
    }

    [Fact]
    public async Task Organiser_ViewsEnrolmentsForOwnEvent_ContainsTheParticipant()
    {
        var (organiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var (participant, participantUser) = await TestData.RegisterAndLoginAsync(_factory, "Participant");
        var created = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, created.EventId);
        await TestData.EnrolAsync(participant, category.CategoryId);

        var response = await organiser.GetAsync($"/api/events/{created.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<EnrolmentResponse>>();
        Assert.Contains(list!, e => e.ParticipantId == participantUser.UserId);
    }

    [Fact]
    public async Task Organiser_ViewsEnrolmentsForAnotherOrganisersEvent_Returns403()
    {
        var (owner, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var (otherOrganiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var created = await TestData.CreateEventAsync(owner);

        var response = await otherOrganiser.GetAsync($"/api/events/{created.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CancelsOwnEnrolment_Returns204AndItIsRemoved()
    {
        var (organiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var (participant, _) = await TestData.RegisterAndLoginAsync(_factory, "Participant");
        var created = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, created.EventId);
        var enrolment = await TestData.EnrolAsync(participant, category.CategoryId);

        var cancel = await participant.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}");
        var list = await (await participant.GetAsync("/api/users/me/enrolments"))
            .Content.ReadFromJsonAsync<List<EnrolmentResponse>>();

        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);
        Assert.DoesNotContain(list!, e => e.EnrolmentId == enrolment.EnrolmentId);
    }

    [Fact]
    public async Task Participant_CannotCancelAnotherParticipantsEnrolment_Returns403()
    {
        var (organiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var (owner, _) = await TestData.RegisterAndLoginAsync(_factory, "Participant");
        var (other, _) = await TestData.RegisterAndLoginAsync(_factory, "Participant");
        var created = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, created.EventId);
        var enrolment = await TestData.EnrolAsync(owner, category.CategoryId);

        var response = await other.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotDeleteCategoryThatHasEnrolments_Returns409()
    {
        var (organiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var (participant, _) = await TestData.RegisterAndLoginAsync(_factory, "Participant");
        var created = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, created.EventId);
        await TestData.EnrolAsync(participant, category.CategoryId);

        var response = await organiser.DeleteAsync($"/api/categories/{category.CategoryId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
