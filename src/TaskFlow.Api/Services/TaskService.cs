using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Services;

public interface ITaskService
{
    Task<PagedResult<TaskDto>?> QueryAsync(int userId, int projectId, TaskQuery q, CancellationToken ct);
    Task<TaskDto?> GetAsync(int userId, int projectId, int id, CancellationToken ct);
    Task<TaskDto?> CreateAsync(int userId, int projectId, TaskRequest req, CancellationToken ct);
    Task<TaskDto?> UpdateAsync(int userId, int projectId, int id, TaskRequest req, CancellationToken ct);
    Task<bool> DeleteAsync(int userId, int projectId, int id, CancellationToken ct);
}

public class TaskService(AppDbContext db) : ITaskService
{
    private IQueryable<TaskItem> Owned(int userId, int projectId) =>
        db.Tasks.Where(t => t.ProjectId == projectId && t.Project!.OwnerId == userId);

    private Task<bool> OwnsProject(int userId, int projectId, CancellationToken ct) =>
        db.Projects.AnyAsync(p => p.Id == projectId && p.OwnerId == userId, ct);

    private static TaskDto Map(TaskItem t) => new(t.Id, t.ProjectId, t.Title, t.Description, t.Status, t.Priority, t.DueDate, t.CreatedAt);

    public static IQueryable<TaskItem> ApplyQuery(IQueryable<TaskItem> src, TaskQuery q)
    {
        if (q.Status is { } s) src = src.Where(t => t.Status == s);
        if (q.Priority is { } pr) src = src.Where(t => t.Priority == pr);
        if (!string.IsNullOrWhiteSpace(q.Search)) src = src.Where(t => t.Title.Contains(q.Search));
        return (q.SortBy?.ToLowerInvariant()) switch
        {
            "title" => q.Desc ? src.OrderByDescending(t => t.Title) : src.OrderBy(t => t.Title),
            "duedate" => q.Desc ? src.OrderByDescending(t => t.DueDate) : src.OrderBy(t => t.DueDate),
            "priority" => q.Desc ? src.OrderByDescending(t => t.Priority) : src.OrderBy(t => t.Priority),
            "status" => q.Desc ? src.OrderByDescending(t => t.Status) : src.OrderBy(t => t.Status),
            _ => q.Desc ? src.OrderByDescending(t => t.Id) : src.OrderBy(t => t.Id)
        };
    }

    public async Task<PagedResult<TaskDto>?> QueryAsync(int userId, int projectId, TaskQuery q, CancellationToken ct)
    {
        if (!await OwnsProject(userId, projectId, ct)) return null;
        var query = ApplyQuery(Owned(userId, projectId), q);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToListAsync(ct);
        return new PagedResult<TaskDto>(items.Select(Map).ToList(), q.Page, q.PageSize, total, (int)Math.Ceiling(total / (double)q.PageSize));
    }

    public async Task<TaskDto?> GetAsync(int userId, int projectId, int id, CancellationToken ct)
    {
        var t = await Owned(userId, projectId).FirstOrDefaultAsync(x => x.Id == id, ct);
        return t is null ? null : Map(t);
    }

    public async Task<TaskDto?> CreateAsync(int userId, int projectId, TaskRequest req, CancellationToken ct)
    {
        if (!await OwnsProject(userId, projectId, ct)) return null;
        var t = new TaskItem { ProjectId = projectId, Title = req.Title.Trim(), Description = req.Description, Status = req.Status, Priority = req.Priority, DueDate = req.DueDate };
        db.Tasks.Add(t);
        await db.SaveChangesAsync(ct);
        return Map(t);
    }

    public async Task<TaskDto?> UpdateAsync(int userId, int projectId, int id, TaskRequest req, CancellationToken ct)
    {
        var t = await Owned(userId, projectId).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return null;
        t.Title = req.Title.Trim(); t.Description = req.Description; t.Status = req.Status; t.Priority = req.Priority; t.DueDate = req.DueDate;
        await db.SaveChangesAsync(ct);
        return Map(t);
    }

    public async Task<bool> DeleteAsync(int userId, int projectId, int id, CancellationToken ct)
    {
        var t = await Owned(userId, projectId).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return false;
        db.Tasks.Remove(t);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
