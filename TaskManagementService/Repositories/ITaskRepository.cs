

namespace TaskManagementService.Repositories
{
    public interface ITaskRepository
    {
        Task<TaskManagementService.Models.Task> CreateAsync(Models.Task task);
        Task<IEnumerable<Models.Task>> GetAllAsync();
        Task<Models.Task?> GetByIdAsync(int id);
        Task UpdateStatusAsync(int id, Models.TaskStatus newStatus);
    }
}
