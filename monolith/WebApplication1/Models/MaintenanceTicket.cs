using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication1.Models
{
    public class MaintenanceTicket
    {
        public Guid Id { get; set; }
        public int RoomId { get; set; }
        public DateTime Start { get; set; }
        public string Reason { get; set; }
    }
}
