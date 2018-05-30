using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication1.Models
{
    public class Booking
    {
        public Guid Id { get; set; }
        public int RoomId { get; set; }
        public DateTimeOffset Start { get; set; }
        public int Nights { get; set; }
        public HotelRoomRate Rate { get; set; }
    }
}
