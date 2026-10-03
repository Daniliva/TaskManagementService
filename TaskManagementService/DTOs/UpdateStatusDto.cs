namespace TaskManagementService.DTOs;

public class UpdateStatusDto
{
    /// <summary>The requested status (string name, case-insensitive). Required: a missing value is a validation error, not "Backlog".</summary>
    public Models.TaskStatus? Status { get; set; }
}
