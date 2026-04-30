using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rtm.Models.Entities
{
public class Tab
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public required string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Навігаційні властивості
        public User User { get; set; } = null!;
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        public ICollection<HistoryLog> HistoryLogs { get; set; } = new List<HistoryLog>();
    }
}