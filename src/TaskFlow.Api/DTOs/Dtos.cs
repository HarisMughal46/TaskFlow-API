using System.ComponentModel.DataAnnotations;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.DTOs;

public record RegisterRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(8), MaxLength(100)] string Password,
    [Required, MaxLength(100)] string DisplayName);

public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);

public record AuthResponse(string Token, DateTime ExpiresAt, string Email, string DisplayName);

public record ProjectRequest(
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    [MaxLength(1000)] string? Description);

public record ProjectDto(int Id, string Name, string? Description, int TaskCount, DateTime CreatedAt);

public record TaskRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Title,
    [MaxLength(2000)] string? Description,
    [EnumDataType(typeof(TaskState))] TaskState Status,
    [EnumDataType(typeof(TaskPriority))] TaskPriority Priority,
    DateTime? DueDate);

public record TaskDto(int Id, int ProjectId, string Title, string? Description, TaskState Status, TaskPriority Priority, DateTime? DueDate, DateTime CreatedAt);

public class TaskQuery
{
    public TaskState? Status { get; set; }
    public TaskPriority? Priority { get; set; }
    public string? Search { get; set; }
    [RegularExpression("^(title|dueDate|priority|createdAt|status)$", ErrorMessage = "sortBy must be one of title, dueDate, priority, createdAt, status.")]
    public string? SortBy { get; set; }
    public bool Desc { get; set; }
    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 10;
}

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

