using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest req, CancellationToken ct)
    {
        var res = await auth.RegisterAsync(req, ct);
        return res is null ? Problem(statusCode: 409, title: "Email already registered.") : StatusCode(201, res);
    }

    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest req, CancellationToken ct)
    {
        var res = await auth.LoginAsync(req, ct);
        return res is null ? Problem(statusCode: 401, title: "Invalid email or password.") : Ok(res);
    }
}

[ApiController]
[Authorize]
public abstract class AuthedController : ControllerBase
{
    protected int UserId => int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

[Route("api/projects")]
public class ProjectsController(IProjectService svc) : AuthedController
{
    [HttpGet] public async Task<IReadOnlyList<ProjectDto>> GetAll(CancellationToken ct) => await svc.GetAllAsync(UserId, ct);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectDto>> Get(int id, CancellationToken ct) =>
        await svc.GetAsync(UserId, id, ct) is { } p ? p : NotFound();

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create(ProjectRequest req, CancellationToken ct)
    {
        var p = await svc.CreateAsync(UserId, req, ct);
        return CreatedAtAction(nameof(Get), new { id = p.Id }, p);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProjectDto>> Update(int id, ProjectRequest req, CancellationToken ct) =>
        await svc.UpdateAsync(UserId, id, req, ct) is { } p ? p : NotFound();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        await svc.DeleteAsync(UserId, id, ct) ? NoContent() : NotFound();
}

[Route("api/projects/{projectId:int}/tasks")]
public class TasksController(ITaskService svc) : AuthedController
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<TaskDto>>> Query(int projectId, [FromQuery] TaskQuery q, CancellationToken ct) =>
        await svc.QueryAsync(UserId, projectId, q, ct) is { } r ? r : NotFound();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaskDto>> Get(int projectId, int id, CancellationToken ct) =>
        await svc.GetAsync(UserId, projectId, id, ct) is { } t ? t : NotFound();

    [HttpPost]
    public async Task<ActionResult<TaskDto>> Create(int projectId, TaskRequest req, CancellationToken ct)
    {
        var t = await svc.CreateAsync(UserId, projectId, req, ct);
        return t is null ? NotFound() : CreatedAtAction(nameof(Get), new { projectId, id = t.Id }, t);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TaskDto>> Update(int projectId, int id, TaskRequest req, CancellationToken ct) =>
        await svc.UpdateAsync(UserId, projectId, id, req, ct) is { } t ? t : NotFound();

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int projectId, int id, CancellationToken ct) =>
        await svc.DeleteAsync(UserId, projectId, id, ct) ? NoContent() : NotFound();
}
