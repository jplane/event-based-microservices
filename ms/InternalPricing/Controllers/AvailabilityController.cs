using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using InternalPricing.Models;

namespace InternalPricing.Controllers
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

        [HttpGet("{start}/{nights}")]
        public async Task<IEnumerable<HotelRoom>> GetRooms(DateTime start, int nights)
        {
            var rooms = await _GetRooms(start, nights);
            return rooms.Values;
        }

        [HttpGet("{id}/{start}/{nights}")]
        public async Task<HotelRoom> GetRoom(int id, DateTime start, int nights)
        {
            var rooms = await _GetRooms(start, nights);

            HotelRoom room;

            rooms.TryGetValue(id, out room);

            return room;
        }

        private async Task<Dictionary<int, HotelRoom>> _GetRooms(DateTime start, int nights)
        {
            var db = GetDatabase();

            var roomQuery = @"SELECT * FROM [dbo].[HotelRoom] WHERE [Available] = 1";

            var rooms = (await db.QueryAsync<HotelRoom>(roomQuery, new { start, nights })).ToDictionary(room => room.Id, room => room);

            var rateSql = @"SELECT * FROM [dbo].[HotelRoomRate]";

            var rateGroups = (await db.QueryAsync(rateSql)).GroupBy(rate => rate.RoomId);

            foreach (var group in rateGroups)
            {
                var roomId = (int)group.Key;

                var rates = group
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

        [HttpPatch("update")]
        public Task SetAvailability([FromBody] dynamic body)
        {
            var db = GetDatabase();

            var args = new { id = (int)body.id, available = body.available == "true" ? 1 : 0 };

            return db.ExecuteAsync("UPDATE [dbo].[HotelRoom] SET [Available] = @available WHERE [Id] = @id", args);
        }
    }
}
