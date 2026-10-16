using System.Net;
using System.Net.Http.Json;
using RaceDay.Controllers;
using RaceDay.Tests.Infrastructure;
using Xunit;

namespace RaceDay.Tests;

/// <summary>
/// Registration, login and session tests (success and failure cases).
/// </summary>
public class AuthTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public AuthTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_ValidParticipant_Returns201WithRole()
    {
        var client = _factory.CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "New Participant",
            email = TestData.NewEmail("new"),
            password = TestData.Password,
            role = "Participant"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.Equal("Participant", user!.Role);
        Assert.Equal("New Participant", user.FullName);
    }

    [Fact]
    public async Task Register_ValidOrganiser_Returns201WithRole()
    {
        var client = _factory.CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "New Organiser",
            email = TestData.NewEmail("org"),
            password = TestData.Password,
            role = "Organiser"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.Equal("Organiser", user!.Role);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = _factory.CreateApiClient();
        var body = new
        {
            fullName = "Duplicate",
            email = TestData.NewEmail("dup"),
            password = TestData.Password,
            role = "Participant"
        };

        await client.PostAsJsonAsync("/api/auth/register", body);
        var second = await client.PostAsJsonAsync("/api/auth/register", body);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var client = _factory.CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Sneaky",
            email = TestData.NewEmail("admin"),
            password = TestData.Password,
            role = "Admin"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400()
    {
        var client = _factory.CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Weak",
            email = TestData.NewEmail("weak"),
            password = "abcdef",
            role = "Participant"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithRole()
    {
        var email = TestData.NewEmail("login");
        var client = _factory.CreateApiClient();
        await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Login User",
            email,
            password = TestData.Password,
            role = "Organiser"
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = TestData.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.Equal("Organiser", user!.Role);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var email = TestData.NewEmail("wrongpw");
        var client = _factory.CreateApiClient();
        await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Wrong Password",
            email,
            password = TestData.Password,
            role = "Participant"
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Wrong@9999" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var client = _factory.CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = TestData.NewEmail("ghost"),
            password = TestData.Password
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WhenNotLoggedIn_Returns401()
    {
        var client = _factory.CreateApiClient();

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_AfterLogin_ReturnsUserAndRoleFromSession()
    {
        var (client, user) = await TestData.RegisterAndLoginAsync(_factory, "Participant");

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.Equal(user.UserId, me!.UserId);
        Assert.Equal("Participant", me.Role);
    }

    [Fact]
    public async Task Logout_ClearsSession_SoNextRequestIsUnauthorised()
    {
        var (client, _) = await TestData.RegisterAndLoginAsync(_factory, "Participant");

        var logout = await client.PostAsync("/api/auth/logout", null);
        var afterLogout = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }
}
