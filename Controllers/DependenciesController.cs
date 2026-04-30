using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rtm.Data;
using Rtm.Models.Entities;

namespace Rtm.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DependenciesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DependenciesController(AppDbContext context)
        {
            _context = context;
        }

        public record DependencyRequest(Guid ParentTaskId, Guid ChildTaskId);

        [HttpPost]
        public async Task<IActionResult> AddDependency([FromBody] DependencyRequest request)
        {
            if (request.ParentTaskId == request.ChildTaskId)
                return BadRequest("Задача не може залежати сама від себе.");

            var exists = await _context.TaskDependencies
                .AnyAsync(d => d.ParentTaskId == request.ParentTaskId && d.ChildTaskId == request.ChildTaskId);

            if (exists)
                return BadRequest("Ця залежність вже існує.");

            // Отримуємо задачі, щоб дізнатися TabId для логування
            var parentTask = await _context.TaskItems.FindAsync(request.ParentTaskId);
            var childTask = await _context.TaskItems.FindAsync(request.ChildTaskId);

            if (parentTask == null || childTask == null)
                return NotFound("Одну з задач не знайдено.");

            var dependency = new TaskDependency
            {
                Id = Guid.NewGuid(),
                ParentTaskId = request.ParentTaskId,
                ChildTaskId = request.ChildTaskId
            };

            _context.TaskDependencies.Add(dependency);

            // Логуємо у вкладку батьківської задачі
            _context.HistoryLogs.Add(new HistoryLog
            {
                Id = Guid.NewGuid(),
                TabId = parentTask.TabId,
                TaskId = parentTask.Id,
                Action = $"Додано залежність: задача '{childTask.Title}'"
            });

            await _context.SaveChangesAsync();
            return Ok(dependency);
        }

        [HttpDelete("{parentId}/{childId}")]
        public async Task<IActionResult> RemoveDependency(Guid parentId, Guid childId)
        {
            var dependency = await _context.TaskDependencies
                .FirstOrDefaultAsync(d => d.ParentTaskId == parentId && d.ChildTaskId == childId);

            if (dependency == null) return NotFound();

            var parentTask = await _context.TaskItems.FindAsync(parentId);

            _context.TaskDependencies.Remove(dependency);

            if (parentTask != null)
            {
                _context.HistoryLogs.Add(new HistoryLog
                {
                    Id = Guid.NewGuid(),
                    TabId = parentTask.TabId,
                    TaskId = parentTask.Id,
                    Action = $"Видалено залежність від задачі (Id: {childId})"
                });
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}