using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rtm.Models.Enums;

namespace Rtm.Models.Entities
{
    public class TaskItem
    {
        public Guid Id { get; set; }
        public Guid TabId { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; }
        public TaskItemStatus Status { get; set; } = TaskItemStatus.Pending;
        public int? Priority { get; set; }
        public int? Complexity { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; } 
        public DateTime? Deadline { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Навігаційні властивості
        public Tab Tab { get; set; } = null!;
        public ICollection<HistoryLog> HistoryLogs { get; set; } = new List<HistoryLog>();

        // Для зв'язку задач між собою
        public ICollection<TaskDependency> DependentTasks { get; set; } = new List<TaskDependency>(); // Задачі, для яких ця є батьківською
        public ICollection<TaskDependency> PrerequisiteTasks { get; set; } = new List<TaskDependency>(); // Задачі, від яких ця залежить (дочірня)
    }
}