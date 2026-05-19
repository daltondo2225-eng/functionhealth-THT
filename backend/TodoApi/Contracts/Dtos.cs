using System.ComponentModel.DataAnnotations;

namespace TodoApi.Contracts;

public record RegisterRequest(
    [property: Required, EmailAddress, MaxLength(256)] string Email,
    [property: Required, MinLength(8), MaxLength(200)] string Password);

public record LoginRequest(
    [property: Required, EmailAddress, MaxLength(256)] string Email,
    [property: Required, MaxLength(200)] string Password);

public record UserDto(Guid Id, string Email);

public record AuthResponse(string Token, UserDto User);

public record TodoCreateRequest(
    [property: Required, MaxLength(200)] string Title,
    [property: MaxLength(2000)] string? Description);

public record TodoUpdateRequest(
    [property: Required, MaxLength(200)] string Title,
    [property: MaxLength(2000)] string? Description,
    bool IsCompleted);

public record TodoDto(
    Guid Id,
    string Title,
    string? Description,
    bool IsCompleted,
    DateTime CreatedAt,
    DateTime UpdatedAt);
