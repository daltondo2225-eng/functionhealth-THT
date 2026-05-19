using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TodoApi.Data;
using Xunit;

namespace TodoApi.Tests;

public class OwnershipTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public OwnershipTests(TestWebAppFactory factory) => _factory = factory;

    private async Task<(HttpClient A, HttpClient B, Guid TodoOfB)> SetupTwoUsersWithTodoForB()
    {
        var clientA = _factory.CreateClient();
        var clientB = _factory.CreateClient();
        var a = await clientA.RegisterAsync($"a-{Guid.NewGuid():N}@example.com", "password123");
        var b = await clientB.RegisterAsync($"b-{Guid.NewGuid():N}@example.com", "password123");
        clientA.WithToken(a.Token);
        clientB.WithToken(b.Token);

        var create = await clientB.PostAsJsonAsync("/api/todos", new { title = "B's secret todo" });
        create.EnsureSuccessStatusCode();
        var todo = await create.Content.ReadFromJsonAsync<TodoEnvelope>();
        return (clientA, clientB, todo!.Id);
    }

    [Fact]
    public async Task UserA_ListEndpoint_DoesNotContain_UserB_Todos()
    {
        var (clientA, _, todoBId) = await SetupTwoUsersWithTodoForB();

        var res = await clientA.GetAsync("/api/todos");
        res.EnsureSuccessStatusCode();
        var list = await res.Content.ReadFromJsonAsync<List<TodoEnvelope>>();

        list.Should().NotBeNull();
        list!.Select(t => t.Id).Should().NotContain(todoBId);
    }

    [Fact]
    public async Task UserA_GetById_ForUserB_Todo_Returns404_NotForbidden()
    {
        var (clientA, _, todoBId) = await SetupTwoUsersWithTodoForB();

        var res = await clientA.GetAsync($"/api/todos/{todoBId}");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UserA_Update_UserB_Todo_Returns404_AndDbUnchanged()
    {
        var (clientA, clientB, todoBId) = await SetupTwoUsersWithTodoForB();

        var res = await clientA.PutAsJsonAsync($"/api/todos/{todoBId}",
            new { title = "HIJACKED", description = (string?)null, isCompleted = true });
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // B's todo is unchanged.
        var stillThere = await clientB.GetAsync($"/api/todos/{todoBId}");
        stillThere.EnsureSuccessStatusCode();
        var todo = await stillThere.Content.ReadFromJsonAsync<TodoEnvelope>();
        todo!.Title.Should().Be("B's secret todo");
        todo.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task UserA_Delete_UserB_Todo_Returns404_AndRowSurvives()
    {
        var (clientA, clientB, todoBId) = await SetupTwoUsersWithTodoForB();

        var res = await clientA.DeleteAsync($"/api/todos/{todoBId}");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var exists = db.Todos.Any(t => t.Id == todoBId);
        exists.Should().BeTrue();

        // And B can still see it.
        var stillThere = await clientB.GetAsync($"/api/todos/{todoBId}");
        stillThere.EnsureSuccessStatusCode();
    }

    [Theory]
    [InlineData("GET", "/api/todos")]
    [InlineData("POST", "/api/todos")]
    public async Task ProtectedEndpoint_WithoutToken_Returns401(string method, string path)
    {
        var client = _factory.CreateClient();
        var req = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "POST") req.Content = JsonContent.Create(new { title = "x" });
        var res = await client.SendAsync(req);
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithTamperedToken_Returns401()
    {
        var client = _factory.CreateClient();
        var a = await client.RegisterAsync($"tamper-{Guid.NewGuid():N}@example.com", "password123");
        // Mutate the signature segment.
        var parts = a.Token.Split('.');
        var tampered = $"{parts[0]}.{parts[1]}.{new string(parts[2].Reverse().ToArray())}";
        client.WithToken(tampered);

        var res = await client.GetAsync("/api/todos");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public record TodoEnvelope(Guid Id, string Title, string? Description, bool IsCompleted, DateTime CreatedAt, DateTime UpdatedAt);
}
