using Microsoft.AspNetCore.Mvc;
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
    public Task<ActionResult<Models.Task>> Create([FromBody] CreateTaskDto dto) => throw new NotImplementedException("TODO");

    /// <summary>200 with a PagedResult: filter by ?status=, ?search=, paging ?page= and ?pageSize= (see TaskQuery). Invalid query values give 400.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Models.Task>), StatusCodes.Status200OK)]
    public Task<ActionResult<PagedResult<Models.Task>>> GetAll([FromQuery] TaskQuery query) => throw new NotImplementedException("TODO");

    /// <summary>200 with the task or 404 (problem+json, detail "Task with ID {id} not found.").</summary>
    [HttpGet("{id:int}")]
    public Task<ActionResult<Models.Task>> GetById(int id) => throw new NotImplementedException("TODO");

    /// <summary>200 with the updated task (title and description trimmed); 404 for an unknown id; 400 for invalid input.</summary>
    [HttpPut("{id:int}")]
    public Task<ActionResult<Models.Task>> Update(int id, [FromBody] UpdateTaskDto dto) => throw new NotImplementedException("TODO");

    /// <summary>204, or 404 for an unknown id.</summary>
    [HttpDelete("{id:int}")]
    public Task<IActionResult> Delete(int id) => throw new NotImplementedException("TODO");

    /// <summary>
    /// 204; 404 unknown task; 400 invalid status value; 409 (problem+json) when TaskStatusRules refuses the move:
    /// Detail = the refusal message, extension "allowedNext" = the statuses (as strings) that would have been accepted.
    /// </summary>
    [HttpPatch("{id:int}/status")]
    public Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto) => throw new NotImplementedException("TODO");

    /// <summary>200 with the transitions (oldest first), or 404.</summary>
    [HttpGet("{id:int}/history")]
    public Task<ActionResult<IReadOnlyList<TaskHistoryEntry>>> GetHistory(int id) => throw new NotImplementedException("TODO");
}
