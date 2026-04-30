using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rtm.Models.Entities
{
public class User
    {
        public Guid Id { get; set; }
        public required string Username { get; set; }

        // Навігаційна властивість
        public ICollection<Tab> Tabs { get; set; } = new List<Tab>();
    }
}