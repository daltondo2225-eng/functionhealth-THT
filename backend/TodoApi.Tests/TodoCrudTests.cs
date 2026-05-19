using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace TodoApi.Tests;

public class TodoCrudTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public TodoCrudTests(TestWebAppFactory factory) => _factory = factory;

    private async Task<HttpClient> NewUserClientAsync()
    {
        var client = _factory.CreateClient();
        var a = await client.RegisterAsync($"crud-{Guid.NewGuid():N}@example.com", "password123");
        client.WithToken(a.Token);
        return client;
    }

    [Fact]
    public async Task Create_Returns201_AndAppearsInList()
    {
        var client = await NewUserClientAsync();

        var res = await client.PostAsJsonAsync("/api/todos", new { title = "Buy milk", description = "2%" });
        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await res.Content.ReadFromJsonAsync<OwnershipTests.TodoEnvelope>();
        created!.Title.Should().Be("Buy milk");
        created.Description.Should().Be("2%");
        created.IsCompleted.Should().BeFalse();

        var list = await (await client.GetAsync("/api/todos")).Content.ReadFromJsonAsync<List<OwnershipTests.TodoEnvelope>>();
        list!.Select(t => t.Id).Should().Contain(created.Id);
    }

    [Fact]
    public async Task Update_ChangesAllowedFields_AndIgnoresOthers()
    {
        var client = await NewUserClientAsync();
        var created = await (await client.PostAsJsonAsync("/api/todos", new { title = "Initial" }))
            .Content.ReadFromJsonAsync<OwnershipTests.TodoEnvelope>();

        var res = await client.PutAsJsonAsync($"/api/todos/{created!.Id}",
            new { title = "Renamed", description = "added", isCompleted = true });
        res.EnsureSuccessStatusCode();
        var updated = await res.Content.ReadFromJsonAsync<OwnershipTests.TodoEnvelope>();

        updated!.Title.Should().Be("Renamed");
        updated.Description.Should().Be("added");
        updated.IsCompleted.Should().BeTrue();
        updated.Id.Should().Be(created.Id);
        updated.CreatedAt.Should().Be(created.CreatedAt);
        updated.UpdatedAt.Should().BeAfter(created.UpdatedAt.AddMilliseconds(-1));
    }

    [Fact]
    public async Task Update_WithEmptyTitle_Returns400_AndDbUnchanged()
    {
        var client = await NewUserClientAsync();
        var created = await (await client.PostAsJsonAsync("/api/todos", new { title = "Original" }))
            .Content.ReadFromJsonAsync<OwnershipTests.TodoEnvelope>();

        var res = await client.PutAsJsonAsync($"/api/todos/{created!.Id}",
            new { title = "", description = (string?)null, isCompleted = false });
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var fetch = await client.GetAsync($"/api/todos/{created.Id}");
        var still = await fetch.Content.ReadFromJsonAsync<OwnershipTests.TodoEnvelope>();
        still!.Title.Should().Be("Original");
    }

    [Fact]
    public async Task Toggle_ViaPut_FlipsIsCompleted_AndBumpsUpdatedAt()
    {
        var client = await NewUserClientAsync();
        var created = await (await client.PostAsJsonAsync("/api/todos", new { title = "Toggle me" }))
            .Content.ReadFromJsonAsync<OwnershipTests.TodoEnvelope>();

        await Task.Delay(20); // ensure UpdatedAt visibly bumps

        var res = await client.PutAsJsonAsync($"/api/todos/{created!.Id}",
            new { title = created.Title, description = created.Description, isCompleted = true });
        res.EnsureSuccessStatusCode();
        var toggled = await res.Content.ReadFromJsonAsync<OwnershipTests.TodoEnvelope>();
        toggled!.IsCompleted.Should().BeTrue();
        toggled.UpdatedAt.Should().BeAfter(created.UpdatedAt);
    }

    [Fact]
    public async Task Delete_Returns204_AndSubsequentGetReturns404()
    {
        var client = await NewUserClientAsync();
        var created = await (await client.PostAsJsonAsync("/api/todos", new { title = "Doomed" }))
            .Content.ReadFromJsonAsync<OwnershipTests.TodoEnvelope>();

        var del = await client.DeleteAsync($"/api/todos/{created!.Id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var fetch = await client.GetAsync($"/api/todos/{created.Id}");
        fetch.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
