using Microsoft.EntityFrameworkCore;
using TodoApi.Auth;
using TodoApi.Contracts;
using TodoApi.Data;
using TodoApi.Errors;

namespace TodoApi.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuth(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").AllowAnonymous();

        group.MapPost("/register", async (RegisterRequest req, AppDbContext db, JwtTokenService jwt) =>
        {
            Validation.Validate(req);
            var email = req.Email.Trim().ToLowerInvariant();

            if (await db.Users.AnyAsync(u => u.Email == email))
                throw AppException.Conflict("An account with that email already exists.");

            var user = new User
            {
                Email = email,
                PasswordHash = PasswordHasher.Hash(req.Password),
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();

            var token = jwt.Create(user);
            return Results.Ok(new AuthResponse(token, new UserDto(user.Id, user.Email)));
        });

        group.MapPost("/login", async (LoginRequest req, AppDbContext db, JwtTokenService jwt) =>
        {
            Validation.Validate(req);
            var email = req.Email.Trim().ToLowerInvariant();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            // Identical message + status for unknown-email and wrong-password to avoid user enumeration.
            if (user is null || !PasswordHasher.Verify(req.Password, user.PasswordHash))
                throw AppException.Unauthorized("Invalid email or password.");

            var token = jwt.Create(user);
            return Results.Ok(new AuthResponse(token, new UserDto(user.Id, user.Email)));
        });
    }
}
