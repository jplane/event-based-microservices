using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExternalPricing.Models
{
    public class HotelRoom
    {
        public int Id { get; set; }
        public string Hotel { get; set; }
        public int Floor { get; set; }
        public int Number { get; set; }
        public int Beds { get; set; }
        public HotelRoomRate[] Prices { get; set; }
        public bool Available { get; set; }
    }

    public class HotelRoomRate
    {
        public string Class { get; set; }
        public decimal Price { get; set; }
    }
}
