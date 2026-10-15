namespace TaskFlow.Api.Models;

public enum TaskState { Todo, InProgress, Done }
public enum TaskPriority { Low, Medium, High }

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Project> Projects { get; set; } = [];
}

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int OwnerId { get; set; }
    public User? Owner { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<TaskItem> Tasks { get; set; } = [];
}

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public TaskState Status { get; set; } = TaskState.Todo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? DueDate { get; set; }
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
