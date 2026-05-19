using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApi.Data;

namespace TodoApi.Tests;

public class TestWebAppFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public TestWebAppFactory()
    {
        // Program.cs reads Jwt:Secret during host build; env vars are merged in by the default
        // configuration sources, so setting these here is observed by Program before any
        // ConfigureAppConfiguration overrides run.
        Environment.SetEnvironmentVariable("Jwt__Secret", "test-only-secret-do-not-use-in-production-please-replace-min-32");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "todoapi");
        Environment.SetEnvironmentVariable("Jwt__Audience", "todoapi");
        Environment.SetEnvironmentVariable("Jwt__ExpiresHours", "1");

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbContextDescriptor is not null) services.Remove(dbContextDescriptor);

            services.AddDbContext<AppDbContext>(opts => opts.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
        }
        base.Dispose(disposing);
    }
}

public static class TestClientExtensions
{
    public record AuthResult(string Token, Guid UserId, string Email);

    public static async Task<AuthResult> RegisterAsync(this HttpClient client, string email, string password)
    {
        var res = await client.PostAsJsonAsync("/api/auth/register", new { email, password });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<AuthEnvelope>();
        return new AuthResult(body!.Token, body.User.Id, body.User.Email);
    }

    public static HttpClient WithToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public record AuthEnvelope(string Token, UserEnvelope User);
    public record UserEnvelope(Guid Id, string Email);
}
