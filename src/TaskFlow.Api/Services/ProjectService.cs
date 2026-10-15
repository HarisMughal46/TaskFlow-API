using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Services;

public interface IProjectService
{
    Task<IReadOnlyList<ProjectDto>> GetAllAsync(int userId, CancellationToken ct);
    Task<ProjectDto?> GetAsync(int userId, int id, CancellationToken ct);
    Task<ProjectDto> CreateAsync(int userId, ProjectRequest req, CancellationToken ct);
    Task<ProjectDto?> UpdateAsync(int userId, int id, ProjectRequest req, CancellationToken ct);
    Task<bool> DeleteAsync(int userId, int id, CancellationToken ct);
}

public class ProjectService(AppDbContext db) : IProjectService
{
    private IQueryable<Project> Owned(int userId) => db.Projects.Where(p => p.OwnerId == userId);

    public async Task<IReadOnlyList<ProjectDto>> GetAllAsync(int userId, CancellationToken ct) =>
        await Owned(userId).OrderBy(p => p.Id)
            .Select(p => new ProjectDto(p.Id, p.Name, p.Description, p.Tasks.Count, p.CreatedAt)).ToListAsync(ct);

    public Task<ProjectDto?> GetAsync(int userId, int id, CancellationToken ct) =>
        Owned(userId).Where(p => p.Id == id)
            .Select(p => new ProjectDto(p.Id, p.Name, p.Description, p.Tasks.Count, p.CreatedAt)).FirstOrDefaultAsync(ct);

    public async Task<ProjectDto> CreateAsync(int userId, ProjectRequest req, CancellationToken ct)
    {
        var p = new Project { Name = req.Name.Trim(), Description = req.Description, OwnerId = userId };
        db.Projects.Add(p);
        await db.SaveChangesAsync(ct);
        return new ProjectDto(p.Id, p.Name, p.Description, 0, p.CreatedAt);
    }

    public async Task<ProjectDto?> UpdateAsync(int userId, int id, ProjectRequest req, CancellationToken ct)
    {
        var p = await Owned(userId).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return null;
        p.Name = req.Name.Trim();
        p.Description = req.Description;
        await db.SaveChangesAsync(ct);
        return await GetAsync(userId, id, ct);
    }

    public async Task<bool> DeleteAsync(int userId, int id, CancellationToken ct)
    {
        var p = await Owned(userId).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return false;
        db.Projects.Remove(p);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
