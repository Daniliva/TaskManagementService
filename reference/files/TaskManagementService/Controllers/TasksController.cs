using Microsoft.AspNetCore.Mvc;
using TaskManagementService.Domain;
using TaskManagementService.DTOs;
using TaskManagementService.Filters;
using TaskManagementService.Models;
using TaskManagementService.Repositories;

namespace TaskManagementService.Controllers;

/// <summary>
/// Errors are application/problem+json: 400 validation (ValidationProblemDetails), 404 unknown task, 409 refused status change
/// (extension "allowedNext" lists the statuses that would have been accepted), 500 from the global handler (no internals).
/// Status values travel as strings ("Backlog", "InWork", "Testing", "Done").
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ServiceFilter(typeof(ValidationFilter))]
public class TasksController : ControllerBase
{
    private readonly ITaskRepository repository;

    public TasksController(ITaskRepository repository)
    {
        this.repository = repository;
    }

    /// <summary>201 with Location (GetById) and the created task; the status is always Backlog, title and description are trimmed.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Models.Task), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Models.Task>> Create([FromBody] CreateTaskDto dto)
    {
        var created = await this.repository.CreateAsync(new Models.Task { Title = dto.Title.Trim(), Description = (dto.Description ?? string.Empty).Trim() });
        return this.CreatedAtAction(nameof(this.GetById), new { id = created.Id }, created);
    }

    /// <summary>200 with a PagedResult: filter by ?status=, ?search=, paging ?page= and ?pageSize= (see TaskQuery). Invalid query values give 400.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Models.Task>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<Models.Task>>> GetAll([FromQuery] TaskQuery query) =>
        this.Ok(await this.repository.QueryAsync(query));

    /// <summary>200 with the task or 404.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Models.Task>> GetById(int id)
    {
        var task = await this.repository.GetByIdAsync(id);
        return task is null ? this.NotFoundProblem(id) : this.Ok(task);
    }

    /// <summary>200 with the updated task; 404 for an unknown id; 400 for invalid input.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<Models.Task>> Update(int id, [FromBody] UpdateTaskDto dto)
    {
        try
        {
            return this.Ok(await this.repository.UpdateAsync(id, dto.Title.Trim(), (dto.Description ?? string.Empty).Trim()));
        }
        catch (KeyNotFoundException)
        {
            return this.NotFoundProblem(id);
        }
    }

    /// <summary>204, or 404 for an unknown id.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        await this.repository.DeleteAsync(id) ? this.NoContent() : this.NotFoundProblem(id);

    /// <summary>204; 404 unknown task; 400 invalid status value; 409 when TaskStatusRules refuses the move.</summary>
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        try
        {
            await this.repository.UpdateStatusAsync(id, dto.Status!.Value);
            return this.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.NotFoundProblem(id);
        }
        catch (InvalidOperationException ex)
        {
            var current = (await this.repository.GetByIdAsync(id))?.Status;
            var problem = new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "The status change is not allowed.", Detail = ex.Message };
            if (current is { } status)
            {
                problem.Extensions["allowedNext"] = TaskStatusRules.AllowedNext(status).Select(s => s.ToString()).ToArray();
            }

            return new ObjectResult(problem) { StatusCode = StatusCodes.Status409Conflict, ContentTypes = { "application/problem+json" } };
        }
    }

    /// <summary>200 with the transitions (oldest first), or 404.</summary>
    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<IReadOnlyList<TaskHistoryEntry>>> GetHistory(int id)
    {
        try
        {
            return this.Ok(await this.repository.GetHistoryAsync(id));
        }
        catch (KeyNotFoundException)
        {
            return this.NotFoundProblem(id);
        }
    }

    private ObjectResult NotFoundProblem(int id) =>
        new(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Task not found.", Detail = $"Task with ID {id} not found." })
        {
            StatusCode = StatusCodes.Status404NotFound,
            ContentTypes = { "application/problem+json" },
        };
}
