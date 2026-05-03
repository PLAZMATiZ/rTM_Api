using Api.Models.Entities;
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

        // ДОДАНО: Priority та Complexity
        public record CreateTaskRequest(Guid TabId, string Title, string? Description, int? Priority, int? Complexity);
        public record UpdateTaskRequest(string Title, string? Description, int? Priority, int? Complexity);
        public record ChangeStatusRequest(TaskItemStatus Status);

        // 1. Отримати всі взяті задачі користувача
        [HttpGet("taken")]
        public async Task<IActionResult> GetTakenTasks([FromQuery] Guid userId)
        {
            var tasks = await _context.TaskItems
                .Include(t => t.Tab)
                .Where(t => t.Tab.UserId == userId && t.StartedAt != null)
                .OrderByDescending(t => t.StartedAt)
                .ToListAsync();

            return Ok(tasks);
        }

        // 2. Взяти задачу в роботу
        [HttpPost("{id}/take")]
        public async Task<IActionResult> TakeTask(Guid id)
        {
            var task = await _context.TaskItems.FindAsync(id);
            if (task == null) return NotFound();

            task.StartedAt = DateTime.UtcNow;
            task.FinishedAt = null; // Скидаємо час завершення, якщо задачу взяли повторно

            // Логуємо в історію
            _context.HistoryLogs.Add(new HistoryLog { Id = Guid.NewGuid(), TabId = task.TabId, TaskId = task.Id, Action = $"Задачу '{task.Title}' взято в роботу" });

            await _context.SaveChangesAsync();
            return Ok(task);
        }

        // 3. Прибрати з узятих
        [HttpPost("{id}/untake")]
        public async Task<IActionResult> UntakeTask(Guid id)
        {
            var task = await _context.TaskItems.FindAsync(id);
            if (task == null) return NotFound();

            task.StartedAt = null;
            task.FinishedAt = null;

            _context.HistoryLogs.Add(new HistoryLog { Id = Guid.NewGuid(), TabId = task.TabId, TaskId = task.Id, Action = $"Задачу '{task.Title}' прибрано з робочих" });

            await _context.SaveChangesAsync();
            return Ok(task);
        }

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
                Description = request.Description,
                Priority = request.Priority ?? 0,    // Зберігаємо пріоритет (по замовчуванню 0)
                Complexity = request.Complexity ?? 0 // Зберігаємо складність (по замовчуванню 0)
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
            task.Priority = request.Priority ?? 0;     // Оновлюємо пріоритет
            task.Complexity = request.Complexity ?? 0; // Оновлюємо складність

            LogHistory(task.TabId, task.Id, $"Задачу '{task.Title}' оновлено");

            await _context.SaveChangesAsync();
            return Ok(task);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeStatusRequest request)
        {
            // Include(t => t.Tab) потрібен, щоб знайти UserId
            var task = await _context.TaskItems.Include(t => t.Tab).FirstOrDefaultAsync(t => t.Id == id);
            if (task == null) return NotFound();

            task.Status = request.Status;

            // Якщо задачу ВИКОНАНО і вона БУЛА В РОБОТІ
            if (request.Status == TaskItemStatus.Done && task.StartedAt != null)
            {
                task.FinishedAt = DateTime.UtcNow;
                var duration = (int)(task.FinishedAt.Value - task.StartedAt.Value).TotalSeconds;

                // Перевіряємо, чи є вже статистика для цієї задачі (щоб не дублювати, якщо юзер клікав туди-сюди)
                var existingStat = await _context.TaskStatistics.FirstOrDefaultAsync(s => s.TaskId == task.Id);

                if (existingStat == null)
                {
                    _context.TaskStatistics.Add(new TaskStatistic
                    {
                        Id = Guid.NewGuid(),
                        UserId = task.Tab.UserId,
                        TaskId = task.Id,
                        TaskTitle = task.Title,
                        TabName = task.Tab.Name,
                        StartedAt = task.StartedAt.Value,
                        FinishedAt = task.FinishedAt.Value,
                        DurationSeconds = duration
                    });
                }
                else
                {
                    existingStat.FinishedAt = task.FinishedAt.Value;
                    existingStat.DurationSeconds = duration;
                }
            }
            else if (request.Status == TaskItemStatus.Pending)
            {
                task.FinishedAt = null;
            }

            _context.HistoryLogs.Add(new HistoryLog { Id = Guid.NewGuid(), TabId = task.TabId, TaskId = task.Id, Action = $"Статус змінено на {task.Status}" });

            await _context.SaveChangesAsync();
            return Ok(task);
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics([FromQuery] Guid userId)
        {
            var stats = await _context.TaskStatistics
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.FinishedAt)
                .ToListAsync();

            return Ok(stats);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(Guid id)
        {
            var task = await _context.TaskItems.FindAsync(id);
            if (task == null) return NotFound();

            var dependenciesToRemove = await _context.TaskDependencies
                .Where(d => d.ParentTaskId == id || d.ChildTaskId == id)
                .ToListAsync();

            if (dependenciesToRemove.Any())
            {
                _context.TaskDependencies.RemoveRange(dependenciesToRemove);
            }

            _context.HistoryLogs.Add(new HistoryLog
            {
                Id = Guid.NewGuid(),
                TabId = task.TabId,
                Action = $"Задачу '{task.Title}' та її зв'язки видалено"
            });

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