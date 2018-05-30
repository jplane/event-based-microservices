using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ExternalPricing.Models;

namespace ExternalPricing.Controllers
{
    [Route("api")]
    public class PricingController : Controller
    {
        private readonly string _dbcs;

        public PricingController(IConfiguration config)
        {
            _dbcs = config["databaseConnection"];
        }

        private IDbConnection GetDatabase()
        {
            return new SqlConnection(_dbcs);
        }

        [HttpGet("room")]
        public async Task<IEnumerable<HotelRoom>> GetRooms()
        {
            var db = GetDatabase();

            var rooms = (await db.QueryAsync<HotelRoom>("SELECT * FROM [dbo].[HotelRoom]")).ToDictionary(room => room.Id, room => room);

            var rateSql = @"SELECT * FROM [dbo].[HotelRoomRate]";

            var rateGroups = (await db.QueryAsync(rateSql)).GroupBy(rate => rate.RoomId);

            Func<dynamic, bool> rateFilter = r => r.RateClass == "Standard" || r.RateClass == "Premium";

            foreach(var group in rateGroups)
            {
                var roomId = (int) group.Key;

                var rates = group
                              .Where(rateFilter)
                              .Select(r => new HotelRoomRate { Class = r.RateClass, Price = r.Price })
                              .ToArray();

                rooms[roomId].Prices = rates;
            }

            return rooms.Values;
        }

        [HttpGet("room/{id}")]
        public async Task<HotelRoom> GetRoom(int id)
        {
            var db = GetDatabase();

            var room = (await db.QueryAsync<HotelRoom>("SELECT * FROM [dbo].[HotelRoom] WHERE [Id] = @id", new { id })).SingleOrDefault();

            if (room != null)
            {
                Func<dynamic, bool> rateFilter = r => r.RateClass == "Standard" || r.RateClass == "Premium";

                var rateSql = @"SELECT * FROM [dbo].[HotelRoomRate] WHERE [RoomId] = @id";

                var rates = (await db.QueryAsync(rateSql, new { id }))
                    .Where(rateFilter)
                    .Select(r => new HotelRoomRate { Class = r.RateClass, Price = r.Price })
                    .ToArray();

                room.Prices = rates;
            }

            return room;
        }

        [HttpPatch("room/available")]
        public Task SetAvailability([FromBody] dynamic body)
        {
            var db = GetDatabase();

            var args = new { id = (int) body.id, available = body.available == "true" ? 1 : 0 };

            return db.ExecuteAsync("UPDATE [dbo].[HotelRoom] SET [Available] = @available WHERE [Id] = @id", args);
        }
    }
}
