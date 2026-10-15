using TaskFlow.Api.DTOs;
using TaskFlow.Api.Models;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Tests;

public class TaskQueryUnitTests
{
    private static readonly List<TaskItem> Data =
    [
        new() { Id = 1, Title = "Bravo", Status = TaskState.Todo, Priority = TaskPriority.Low },
        new() { Id = 2, Title = "Alpha", Status = TaskState.Done, Priority = TaskPriority.High },
        new() { Id = 3, Title = "Charlie", Status = TaskState.InProgress, Priority = TaskPriority.Medium },
    ];

    [Fact]
    public void FiltersByStatus()
    {
        var r = TaskService.ApplyQuery(Data.AsQueryable(), new TaskQuery { Status = TaskState.Done }).ToList();
        Assert.Equal(2, Assert.Single(r).Id);
    }

    [Fact]
    public void SortsByPriorityDescending()
    {
        var r = TaskService.ApplyQuery(Data.AsQueryable(), new TaskQuery { SortBy = "priority", Desc = true }).Select(t => t.Id);
        Assert.Equal([2, 3, 1], r);
    }

    [Fact]
    public void SearchMatchesTitle()
    {
        var r = TaskService.ApplyQuery(Data.AsQueryable(), new TaskQuery { Search = "har" }).ToList();
        Assert.Equal("Charlie", Assert.Single(r).Title);
    }

    [Fact]
    public void DefaultsToIdOrder()
    {
        var r = TaskService.ApplyQuery(Data.AsQueryable(), new TaskQuery()).Select(t => t.Id);
        Assert.Equal([1, 2, 3], r);
    }
}
