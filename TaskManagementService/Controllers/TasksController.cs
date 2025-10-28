using Microsoft.AspNetCore.Mvc;
using TaskManagementService.DTOs;
using TaskManagementService.Models;
using TaskManagementService.Repositories;
using TaskManagementService.Services;
using TaskManagementService.Validators;

namespace TaskManagementService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskRepository _repository;
    private readonly CreateTaskDtoValidator _createValidator = new();
    private readonly UpdateStatusDtoValidator _statusValidator = new();

    public TasksController(ITaskRepository repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<ActionResult<Task>> Create([FromBody] CreateTaskDto dto)
    {
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        var task = new Task { Title = dto.Title, Description = dto.Description };
        var created = await _repository.CreateAsync(task);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Task>>> GetAll()
    {
        var tasks = await _repository.GetAllAsync();
        return Ok(tasks);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Task>> GetById(int id)
    {
        var task = await _repository.GetByIdAsync(id);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        var validationResult = await _statusValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        try
        {
            await _repository.UpdateStatusAsync(id, dto.Status);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}