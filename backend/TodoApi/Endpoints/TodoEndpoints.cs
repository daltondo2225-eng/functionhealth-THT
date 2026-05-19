using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TodoApi.Contracts;
using TodoApi.Data;
using TodoApi.Errors;

namespace TodoApi.Endpoints;

public static class TodoEndpoints
{
    public static void MapTodos(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/todos").RequireAuthorization();

        group.MapGet("/", async (HttpContext ctx, AppDbContext db) =>
        {
            var userId = ctx.GetUserId();
            var todos = await db.Todos
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new TodoDto(t.Id, t.Title, t.Description, t.IsCompleted, t.CreatedAt, t.UpdatedAt))
                .ToListAsync();
            return Results.Ok(todos);
        });

        group.MapGet("/{id:guid}", async (Guid id, HttpContext ctx, AppDbContext db) =>
        {
            var userId = ctx.GetUserId();
            var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (todo is null) throw AppException.NotFound();
            return Results.Ok(ToDto(todo));
        });

        group.MapPost("/", async (TodoCreateRequest req, HttpContext ctx, AppDbContext db) =>
        {
            Validation.Validate(req);
            var userId = ctx.GetUserId();
            var todo = new TodoItem
            {
                UserId = userId,
                Title = req.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
            };
            db.Todos.Add(todo);
            await db.SaveChangesAsync();
            return Results.Created($"/api/todos/{todo.Id}", ToDto(todo));
        });

        group.MapPut("/{id:guid}", async (Guid id, TodoUpdateRequest req, HttpContext ctx, AppDbContext db) =>
        {
            Validation.Validate(req);
            var userId = ctx.GetUserId();
            var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (todo is null) throw AppException.NotFound();

            todo.Title = req.Title.Trim();
            todo.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
            todo.IsCompleted = req.IsCompleted;
            todo.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(ToDto(todo));
        });

        group.MapDelete("/{id:guid}", async (Guid id, HttpContext ctx, AppDbContext db) =>
        {
            var userId = ctx.GetUserId();
            var todo = await db.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (todo is null) throw AppException.NotFound();

            db.Todos.Remove(todo);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static TodoDto ToDto(TodoItem t) =>
        new(t.Id, t.Title, t.Description, t.IsCompleted, t.CreatedAt, t.UpdatedAt);
}

internal static class HttpContextExtensions
{
    public static Guid GetUserId(this HttpContext ctx)
    {
        var sub = ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (sub is null || !Guid.TryParse(sub, out var id))
            throw AppException.Unauthorized();
        return id;
    }
}
