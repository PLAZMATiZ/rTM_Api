using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rtm.Data;
using Rtm.Models.Entities;
using Rtm.Models.Enums;

namespace Rtm.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TasksController(AppDbContext context)
        {
            _context = context;
        }

        public record CreateTaskRequest(Guid TabId, string Title, string? Description);
        public record UpdateTaskRequest(string Title, string? Description);
        public record ChangeStatusRequest(TaskItemStatus Status);

        // БЕКЕНД: TasksController.cs
        [HttpGet("by-tab/{tabId}")]
        public async Task<IActionResult> GetTasksByTab(Guid tabId)
        {
            var tasks = await _context.TaskItems
                .Include(t => t.DependentTasks)
                .Where(t => t.TabId == tabId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return Ok(tasks);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
        {
            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                TabId = request.TabId,
                Title = request.Title,
                Description = request.Description
            };

            _context.TaskItems.Add(task);
            LogHistory(task.TabId, task.Id, $"Задачу '{task.Title}' створено");

            await _context.SaveChangesAsync();
            return Ok(task);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(Guid id, [FromBody] UpdateTaskRequest request)
        {
            var task = await _context.TaskItems.FindAsync(id);
            if (task == null) return NotFound();

            task.Title = request.Title;
            task.Description = request.Description;

            LogHistory(task.TabId, task.Id, $"Задачу '{task.Title}' оновлено");

            await _context.SaveChangesAsync();
            return Ok(task);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeStatusRequest request)
        {
            var task = await _context.TaskItems.FindAsync(id);
            if (task == null) return NotFound();

            var oldStatus = task.Status;
            task.Status = request.Status;

            LogHistory(task.TabId, task.Id, $"Задача '{task.Title}' змінила статус з {oldStatus} на {task.Status}");

            await _context.SaveChangesAsync();
            return Ok(task);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(Guid id)
        {
            var task = await _context.TaskItems.FindAsync(id);
            if (task == null) return NotFound();

            LogHistory(task.TabId, null, $"Задачу '{task.Title}' видалено");
            _context.TaskItems.Remove(task);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        private void LogHistory(Guid tabId, Guid? taskId, string action)
        {
            _context.HistoryLogs.Add(new HistoryLog
            {
                Id = Guid.NewGuid(),
                TabId = tabId,
                TaskId = taskId,
                Action = action
            });
        }
    }
}