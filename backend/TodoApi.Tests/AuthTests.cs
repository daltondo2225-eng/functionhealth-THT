using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace TodoApi.Tests;

public class AuthTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public AuthTests(TestWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_HappyPath_Returns200WithTokenAndUser()
    {
        var client = _factory.CreateClient();
        var email = $"register-{Guid.NewGuid():N}@example.com";

        var res = await client.PostAsJsonAsync("/api/auth/register", new { email, password = "password123" });

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<TestClientExtensions.AuthEnvelope>();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.User.Email.Should().Be(email);
        body.User.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        var email = $"dup-{Guid.NewGuid():N}@example.com";

        var first = await client.PostAsJsonAsync("/api/auth/register", new { email, password = "password123" });
        first.EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/auth/register", new { email, password = "password123" });
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var err = await second.Content.ReadFromJsonAsync<ErrorEnvelope>();
        err!.Error.Code.Should().Be("conflict");
    }

    [Theory]
    [InlineData("not-an-email", "password123")]
    [InlineData("ok@example.com", "short")]
    public async Task Register_InvalidInput_Returns400(string email, string password)
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/register", new { email, password });

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var err = await res.Content.ReadFromJsonAsync<ErrorEnvelope>();
        err!.Error.Code.Should().Be("validation_failed");
    }

    [Fact]
    public async Task Login_HappyPath_ReturnsToken()
    {
        var client = _factory.CreateClient();
        var email = $"login-{Guid.NewGuid():N}@example.com";
        await client.RegisterAsync(email, "password123");

        var res = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "password123" });
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<TestClientExtensions.AuthEnvelope>();
        body!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401WithGenericMessage()
    {
        var client = _factory.CreateClient();
        var email = $"wrongpw-{Guid.NewGuid():N}@example.com";
        await client.RegisterAsync(email, "password123");

        var res = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "wrong-password" });
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var err = await res.Content.ReadFromJsonAsync<ErrorEnvelope>();
        err!.Error.Message.Should().Be("Invalid email or password.");
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401WithSameMessageAsWrongPassword()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/login",
            new { email = $"never-registered-{Guid.NewGuid():N}@example.com", password = "anything123" });

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var err = await res.Content.ReadFromJsonAsync<ErrorEnvelope>();
        err!.Error.Message.Should().Be("Invalid email or password.");
    }

    public record ErrorEnvelope(ErrorBody Error);
    public record ErrorBody(string Code, string Message);
}
