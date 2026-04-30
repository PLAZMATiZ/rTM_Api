using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rtm.Models.Entities
{
    public class TaskDependency
    {
        public Guid Id { get; set; }
        public Guid ParentTaskId { get; set; }
        public Guid ChildTaskId { get; set; }

        // Навігаційні властивості
        public TaskItem ParentTask { get; set; } = null!;
        public TaskItem ChildTask { get; set; } = null!;
    }
}