using System.Net.Http.Json;
using RaceDay.Controllers;
using RaceDay.Dtos;

namespace RaceDay.Tests.Infrastructure;

/// <summary>
/// Helpers that set up users, events, categories and enrolments through the real API,
/// so each test starts from a known state.
/// </summary>
public static class TestData
{
    public const string Password = "Test@1234";

    public static string NewEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@test.com";

    /// <summary>Registers a new user with the given role, logs in, and returns the logged-in client.</summary>
    public static async Task<(HttpClient Client, UserProfileDto User)> RegisterAndLoginAsync(
        RaceDayFactory factory, string role)
    {
        var client = factory.CreateApiClient();
        var email = NewEmail(role.ToLowerInvariant());

        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = $"Test {role}",
            email,
            password = Password,
            role
        });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        login.EnsureSuccessStatusCode();

        var user = (await login.Content.ReadFromJsonAsync<UserProfileDto>())!;
        return (client, user);
    }

    public static EventRequest NewEventRequest(string eventType = "Run") => new()
    {
        Name = "Test Fun Run",
        Description = "A test event.",
        EventDate = DateTime.Today.AddDays(30),
        Location = "Durban",
        DistanceKm = 10,
        EventType = eventType
    };

    public static async Task<EventResponse> CreateEventAsync(HttpClient organiser, string eventType = "Run")
    {
        var response = await organiser.PostAsJsonAsync("/api/events", NewEventRequest(eventType));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventResponse>())!;
    }

    public static async Task<CategoryResponse> CreateCategoryAsync(
        HttpClient organiser, int eventId, int maxParticipants = 100)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new CategoryRequest
        {
            Name = "10km Race",
            DistanceKm = 10,
            Price = 150,
            MaxParticipants = maxParticipants
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryResponse>())!;
    }

    public static async Task<EnrolmentResponse> EnrolAsync(HttpClient participant, int categoryId)
    {
        var response = await participant.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EnrolmentResponse>())!;
    }
}
