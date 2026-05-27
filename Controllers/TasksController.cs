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
        public record CreateTaskRequest(Guid TabId, string Title, string? Description, int? Priority, int? Complexity, DateTime? Deadline);
        public record UpdateTaskRequest(string Title, string? Description, int? Priority, int? Complexity, DateTime? Deadline);
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
            task.IsPaused = false;
            task.FinishedAt = null;

            _context.HistoryLogs.Add(new HistoryLog { Id = Guid.NewGuid(), TabId = task.TabId, TaskId = task.Id, Action = $"Задачу '{task.Title}' взято в роботу (або відновлено)" });

            await _context.SaveChangesAsync();
            return Ok(task);
        }

        // 2. === НОВИЙ МЕТОД ПАУЗИ ===
        [HttpPost("{id}/pause")]
        public async Task<IActionResult> PauseTask(Guid id)
        {
            var task = await _context.TaskItems.FindAsync(id);
            if (task == null || task.IsPaused || task.StartedAt == null) return BadRequest();

            // Додаємо час з моменту StartedAt до загального часу
            var sessionSeconds = (DateTime.UtcNow - task.StartedAt.Value).TotalSeconds;
            task.TotalSpentSeconds += (int)sessionSeconds;

            task.IsPaused = true;
            task.StartedAt = null; // Скидаємо час початку, бо задача на паузі

            _context.HistoryLogs.Add(new HistoryLog { Id = Guid.NewGuid(), TabId = task.TabId, TaskId = task.Id, Action = $"Задачу '{task.Title}' поставлено на паузу" });

            await _context.SaveChangesAsync();
            return Ok(task);
        }

        // 3. ВІДМІНИТИ РОБОТУ (Зкинути таймер)
        [HttpPost("{id}/untake")]
        public async Task<IActionResult> UntakeTask(Guid id)
        {
            var task = await _context.TaskItems.FindAsync(id);
            if (task == null) return NotFound();

            task.StartedAt = null;
            task.FinishedAt = null;
            task.IsPaused = false;
            task.TotalSpentSeconds = 0; // Скидаємо накопичений час

            _context.HistoryLogs.Add(new HistoryLog { Id = Guid.NewGuid(), TabId = task.TabId, TaskId = task.Id, Action = $"Задачу '{task.Title}' прибрано з робочих (таймер скинуто)" });

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
                Complexity = request.Complexity ?? 0, // Зберігаємо складність (по замовчуванню 0)
                Deadline = request.Deadline
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
            task.Deadline = request.Deadline;

            LogHistory(task.TabId, task.Id, $"Задачу '{task.Title}' оновлено");

            await _context.SaveChangesAsync();
            return Ok(task);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeStatusRequest request)
        {
            var task = await _context.TaskItems.Include(t => t.Tab).FirstOrDefaultAsync(t => t.Id == id);
            if (task == null) return NotFound();

            task.Status = request.Status;

            if (request.Status == TaskItemStatus.Done && (task.StartedAt != null || task.IsPaused))
            {
                task.FinishedAt = DateTime.UtcNow;

                // РАХУЄМО ФІНАЛЬНИЙ ЧАС: Накопичений час + Час поточної сесії (якщо не на паузі)
                var currentSessionSeconds = task.StartedAt != null ? (task.FinishedAt.Value - task.StartedAt.Value).TotalSeconds : 0;
                var finalDuration = task.TotalSpentSeconds + (int)currentSessionSeconds;

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
                        StartedAt = DateTime.UtcNow.AddSeconds(-finalDuration), // Приблизний старт
                        FinishedAt = task.FinishedAt.Value,
                        DurationSeconds = finalDuration
                    });
                }
                else
                {
                    existingStat.FinishedAt = task.FinishedAt.Value;
                    existingStat.DurationSeconds = finalDuration;
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