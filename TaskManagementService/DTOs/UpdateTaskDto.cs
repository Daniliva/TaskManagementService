namespace TaskManagementService.DTOs;

/// <summary>Replaces the title and the description of a task (PUT).</summary>
public class UpdateTaskDto
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
