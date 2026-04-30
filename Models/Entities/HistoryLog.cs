using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rtm.Models.Entities
{
    public class HistoryLog
    {
        public Guid Id { get; set; }
        public Guid TabId { get; set; }
        public Guid? TaskId { get; set; }
        public required string Action { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Навігаційні властивості
        public Tab Tab { get; set; } = null!;
        public TaskItem? Task { get; set; }
    }
}