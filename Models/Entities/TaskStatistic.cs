using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Models.Entities
{
    public class TaskStatistic
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid TaskId { get; set; }
        
        // Зберігаємо назви як текст (Snapshot), щоб статистика не зламалася, якщо задачу видалять
        public required string TaskTitle { get; set; } 
        public required string TabName { get; set; }
        
        public DateTime StartedAt { get; set; }
        public DateTime FinishedAt { get; set; }
        public int DurationSeconds { get; set; } // Витрачений час у секундах
    }
}