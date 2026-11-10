using System.ComponentModel.DataAnnotations;
using HelpDeskKu.Domain;

namespace HelpDeskKu.Application;

public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);
public record RegisterRequest([Required, StringLength(100, MinimumLength = 2)] string FullName,
    [Required, EmailAddress] string Email, [Required, StringLength(128, MinimumLength = 10)] string Password);
public record RefreshRequest([Required] string RefreshToken);
public record UserDto(string Id, string FullName, string Email, string Role, bool IsActive);
public record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt, UserDto User);
public record CreateTicketRequest([Required, StringLength(200, MinimumLength = 5)] string Title,
    [Required, StringLength(10000, MinimumLength = 10)] string Description,
    [Range(1, int.MaxValue)] int CategoryId, [Range(1, int.MaxValue)] int PriorityId);
public record StatusRequest(TicketStatus Status);
public record AssignRequest(string? AssigneeId);
public record CommentRequest([Required, StringLength(10000, MinimumLength = 1)] string Content, bool IsInternal = false);
public record CategoryRequest([Required, StringLength(100, MinimumLength = 2)] string Name,
    [StringLength(500)] string Description, bool IsActive = true);
public record UpdateUserRequest([Required, StringLength(100, MinimumLength = 2)] string FullName,
    [Required, RegularExpression("^(Requester|Agent|Admin)$")] string Role, bool IsActive);
public record CreateUserRequest([Required, StringLength(100, MinimumLength = 2)] string FullName,
    [Required, EmailAddress] string Email, [Required, StringLength(128, MinimumLength = 10)] string Password,
    [Required, RegularExpression("^(Requester|Agent|Admin)$")] string Role);
public record TicketRow(int Id, string TicketNumber, string Title, string Description,
    int CategoryId, string Category, int PriorityId, string Priority, TicketStatus Status,
    string RequesterId, string RequesterName, string? AssigneeId, string? AssigneeName,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset DueDate);
public record PageResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
public interface ICurrentUser
{
    string Id { get; }
    string Name { get; }
    bool IsStaff { get; }
    bool IsAdmin { get; }
}
