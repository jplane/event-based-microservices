using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using WebApplication1.Models;
using Dapper;

namespace WebApplication1.Controllers
{
    [Route("availability")]
    public class AvailabilityController : Controller
    {
        private readonly string _dbcs;

        public AvailabilityController(IConfiguration config)
        {
            _dbcs = config["databaseConnection"];
        }

        private IDbConnection GetDatabase()
        {
            return new SqlConnection(_dbcs);
        }

        [HttpGet("{start}/{nights}/{filter=external}")]
        public async Task<IEnumerable<HotelRoom>> GetRooms(string filter, DateTime start, int nights)
        {
            var rooms = await _GetRooms(filter, start, nights);
            return rooms.Values;
        }

        [HttpGet("{id}/{start}/{nights}/{filter=external}")]
        public async Task<HotelRoom> GetRoom(int id, string filter, DateTime start, int nights)
        {
            var rooms = await _GetRooms(filter, start, nights);

            HotelRoom room;

            rooms.TryGetValue(id, out room);

            return room;
        }

        private async Task<Dictionary<int, HotelRoom>> _GetRooms(string filter, DateTime start, int nights)
        {
            var db = GetDatabase();

            var roomQuery = @"SELECT * FROM [dbo].[HotelRoom] WHERE [Id] NOT IN
                              (SELECT [RoomId] FROM [dbo].[Booking] WHERE [Start] = @start AND [Nights] = @nights
                               UNION
                               SELECT [RoomId] FROM [dbo].[MaintenanceTicket])";

            var rooms = (await db.QueryAsync<HotelRoom>(roomQuery, new { start, nights })).ToDictionary(room => room.Id, room => room);

            var rateSql = @"SELECT * FROM [dbo].[HotelRoomRate]";

            var rateGroups = (await db.QueryAsync(rateSql)).GroupBy(rate => rate.RoomId);

            Func<dynamic, bool> rateFilter = r => r.Class == "Standard" || r.Class == "Premium";

            if (filter == "internal")
            {
                rateFilter = r => true;
            }

            foreach (var group in rateGroups)
            {
                var roomId = (int)group.Key;

                var rates = group
                              .Where(rateFilter)
                              .Select(r => new HotelRoomRate { Class = r.Class, Price = r.Price })
                              .ToArray();

                HotelRoom room;

                if (rooms.TryGetValue(roomId, out room))
                {
                    room.Prices = rates;
                }
            }

            return rooms;
        }
    }
}