using System.Net;
using System.Net.Http.Json;
using RaceDay.Dtos;
using RaceDay.Tests.Infrastructure;
using Xunit;

namespace RaceDay.Tests;

/// <summary>
/// Results tests: Organisers record finish times and positions; Participants view their own.
/// </summary>
public class ResultTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public ResultTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    private static ResultRequest NewResult() => new() { FinishTimeSeconds = 3120, Position = 5 };

    [Fact]
    public async Task Organiser_RecordsResultForOwnEvent_Returns201()
    {
        var (organiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var (participant, _) = await TestData.RegisterAndLoginAsync(_factory, "Participant");
        var created = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, created.EventId);
        var enrolment = await TestData.EnrolAsync(participant, category.CategoryId);

        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", NewResult());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ResultResponse>();
        Assert.Equal(3120, result!.FinishTimeSeconds);
        Assert.Equal(5, result.Position);
        Assert.Equal("00:52:00", result.FinishTime);
    }

    [Fact]
    public async Task Organiser_RecordsResultTwice_Returns409()
    {
        var (organiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var (participant, _) = await TestData.RegisterAndLoginAsync(_factory, "Participant");
        var created = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, created.EventId);
        var enrolment = await TestData.EnrolAsync(participant, category.CategoryId);
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", NewResult());

        var second = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", NewResult());

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Organiser_RecordsInvalidPosition_Returns400()
    {
        var (organiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var (participant, _) = await TestData.RegisterAndLoginAsync(_factory, "Participant");
        var created = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, created.EventId);
        var enrolment = await TestData.EnrolAsync(participant, category.CategoryId);

        var response = await organiser.PostAsJsonAsync(
            $"/api/enrolments/{enrolment.EnrolmentId}/result",
            new ResultRequest { FinishTimeSeconds = 3000, Position = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_RecordsResultForAnotherOrganisersEvent_Returns403()
    {
        var (owner, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var (otherOrganiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var (participant, _) = await TestData.RegisterAndLoginAsync(_factory, "Participant");
        var created = await TestData.CreateEventAsync(owner);
        var category = await TestData.CreateCategoryAsync(owner, created.EventId);
        var enrolment = await TestData.EnrolAsync(participant, category.CategoryId);

        var response = await otherOrganiser.PostAsJsonAsync(
            $"/api/enrolments/{enrolment.EnrolmentId}/result", NewResult());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_ViewsOwnResult_Returns200()
    {
        var (organiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var (participant, _) = await TestData.RegisterAndLoginAsync(_factory, "Participant");
        var created = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, created.EventId);
        var enrolment = await TestData.EnrolAsync(participant, category.CategoryId);
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", NewResult());

        var single = await participant.GetAsync($"/api/enrolments/{enrolment.EnrolmentId}/result");
        var all = await participant.GetAsync("/api/users/me/results");

        Assert.Equal(HttpStatusCode.OK, single.StatusCode);
        Assert.Equal(HttpStatusCode.OK, all.StatusCode);
        var list = await all.Content.ReadFromJsonAsync<List<ResultResponse>>();
        Assert.Contains(list!, r => r.EnrolmentId == enrolment.EnrolmentId);
    }

    [Fact]
    public async Task OtherParticipant_ViewsSomeoneElsesResult_Returns403()
    {
        var (organiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var (owner, _) = await TestData.RegisterAndLoginAsync(_factory, "Participant");
        var (other, _) = await TestData.RegisterAndLoginAsync(_factory, "Participant");
        var created = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, created.EventId);
        var enrolment = await TestData.EnrolAsync(owner, category.CategoryId);
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", NewResult());

        var response = await other.GetAsync($"/api/enrolments/{enrolment.EnrolmentId}/result");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CancelsEnrolmentThatHasResult_Returns409()
    {
        var (organiser, _) = await TestData.RegisterAndLoginAsync(_factory, "Organiser");
        var (participant, _) = await TestData.RegisterAndLoginAsync(_factory, "Participant");
        var created = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, created.EventId);
        var enrolment = await TestData.EnrolAsync(participant, category.CategoryId);
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", NewResult());

        var response = await participant.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
