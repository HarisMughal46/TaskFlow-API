using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskFlow.Api.Data;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Tests;

public class TaskFlowFactory : WebApplicationFactory<Program>
{
    private readonly string _db = Path.Combine(Path.GetTempPath(), $"taskflow-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(s =>
        {
            s.RemoveAll<DbContextOptions<AppDbContext>>();
            s.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={_db};Pooling=False"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { File.Delete(_db); } catch (IOException) { }
    }
}

public class ApiTests(TaskFlowFactory factory) : IClassFixture<TaskFlowFactory>
{
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private async Task<HttpClient> ClientFor(string? email = null)
    {
        var client = factory.CreateClient();
        AuthResponse? auth;
        if (email is null)
        {
            var r = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(DbSeeder.DemoEmail, DbSeeder.DemoPassword));
            auth = await r.Content.ReadFromJsonAsync<AuthResponse>(Json);
        }
        else
        {
            var r = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Passw0rd!x", "Tester"));
            Assert.Equal(HttpStatusCode.Created, r.StatusCode);
            auth = await r.Content.ReadFromJsonAsync<AuthResponse>(Json);
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    private static string NewEmail() => $"u{Guid.NewGuid():N}@test.dev";

    private static async Task<ProjectDto> CreateProject(HttpClient c, string name = "P")
    {
        var r = await c.PostAsJsonAsync("/api/projects", new ProjectRequest(name, null));
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<ProjectDto>(Json))!;
    }

    private static async Task<TaskDto> CreateTask(HttpClient c, int pid, string title, TaskState s = TaskState.Todo, TaskPriority p = TaskPriority.Medium)
    {
        var r = await c.PostAsJsonAsync($"/api/projects/{pid}/tasks", new TaskRequest(title, null, s, p, null), Json);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<TaskDto>(Json))!;
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var r = await factory.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("Healthy", await r.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SwaggerJson_IsServed()
    {
        var r = await factory.CreateClient().GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("Bearer", await r.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/api/projects")]
    [InlineData("/api/projects/1/tasks")]
    public async Task ProtectedEndpoints_WithoutToken_Return401(string url)
    {
        var r = await factory.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Login_DemoUser_ReturnsToken()
    {
        var r = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(DbSeeder.DemoEmail, DbSeeder.DemoPassword));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var auth = await r.Content.ReadFromJsonAsync<AuthResponse>(Json);
        Assert.False(string.IsNullOrEmpty(auth!.Token));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var r = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(DbSeeder.DemoEmail, "nope-nope"));
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var email = NewEmail();
        await ClientFor(email);
        var r = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Passw0rd!x", "Dup"));
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidPayload_Returns400ProblemDetails()
    {
        var r = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new RegisterRequest("not-an-email", "short", ""));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("application/problem+json", r.Content.Headers.ContentType?.MediaType);
        var body = await r.Content.ReadAsStringAsync();
        Assert.Contains("Email", body);
        Assert.Contains("Password", body);
    }

    [Fact]
    public async Task CreateProject_EmptyName_Returns400()
    {
        var c = await ClientFor(NewEmail());
        var r = await c.PostAsJsonAsync("/api/projects", new ProjectRequest("", null));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task DemoUser_SeesSeededProjects()
    {
        var c = await ClientFor();
        var list = await c.GetFromJsonAsync<List<ProjectDto>>("/api/projects", Json);
        Assert.Contains(list!, p => p.Name == "Portfolio Website");
    }

    [Fact]
    public async Task Project_Crud_Works()
    {
        var c = await ClientFor(NewEmail());
        var p = await CreateProject(c, "Alpha");
        var upd = await c.PutAsJsonAsync($"/api/projects/{p.Id}", new ProjectRequest("Beta", "d"));
        Assert.Equal(HttpStatusCode.OK, upd.StatusCode);
        var got = await c.GetFromJsonAsync<ProjectDto>($"/api/projects/{p.Id}", Json);
        Assert.Equal("Beta", got!.Name);
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/projects/{p.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/projects/{p.Id}")).StatusCode);
    }

    [Fact]
    public async Task Task_Crud_Works()
    {
        var c = await ClientFor(NewEmail());
        var p = await CreateProject(c);
        var t = await CreateTask(c, p.Id, "Write tests", TaskState.Todo, TaskPriority.High);
        var upd = await c.PutAsJsonAsync($"/api/projects/{p.Id}/tasks/{t.Id}", new TaskRequest("Write tests", "now", TaskState.Done, TaskPriority.High, DateTime.UtcNow), Json);
        Assert.Equal(HttpStatusCode.OK, upd.StatusCode);
        var got = await c.GetFromJsonAsync<TaskDto>($"/api/projects/{p.Id}/tasks/{t.Id}", Json);
        Assert.Equal(TaskState.Done, got!.Status);
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/projects/{p.Id}/tasks/{t.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/projects/{p.Id}/tasks/{t.Id}")).StatusCode);
    }

    [Fact]
    public async Task Users_CannotAccessOthersData()
    {
        var alice = await ClientFor(NewEmail());
        var bob = await ClientFor(NewEmail());
        var p = await CreateProject(alice, "Alice secret");
        var t = await CreateTask(alice, p.Id, "Secret task");

        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/projects/{p.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/projects/{p.Id}/tasks")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/api/projects/{p.Id}/tasks/{t.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.DeleteAsync($"/api/projects/{p.Id}")).StatusCode);
        var bobs = await bob.GetFromJsonAsync<List<ProjectDto>>("/api/projects", Json);
        Assert.Empty(bobs!);
    }

    [Fact]
    public async Task Tasks_FilterPaginateAndSort()
    {
        var c = await ClientFor(NewEmail());
        var p = await CreateProject(c);
        await CreateTask(c, p.Id, "c-todo", TaskState.Todo);
        await CreateTask(c, p.Id, "a-done", TaskState.Done);
        await CreateTask(c, p.Id, "b-todo", TaskState.Todo);
        await CreateTask(c, p.Id, "d-todo", TaskState.Todo);

        var res = await c.GetFromJsonAsync<PagedResult<TaskDto>>($"/api/projects/{p.Id}/tasks?status=Todo&sortBy=title&page=1&pageSize=2", Json);
        Assert.Equal(3, res!.TotalCount);
        Assert.Equal(2, res.TotalPages);
        Assert.Equal(["b-todo", "c-todo"], res.Items.Select(i => i.Title));

        var page2 = await c.GetFromJsonAsync<PagedResult<TaskDto>>($"/api/projects/{p.Id}/tasks?status=Todo&sortBy=title&page=2&pageSize=2", Json);
        Assert.Equal("d-todo", Assert.Single(page2!.Items).Title);
    }

    [Fact]
    public async Task Tasks_InvalidPageSize_Returns400()
    {
        var c = await ClientFor(NewEmail());
        var p = await CreateProject(c);
        var r = await c.GetAsync($"/api/projects/{p.Id}/tasks?pageSize=500");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }
}
