using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Route("api")]
    public class HotelRoomController : Controller
    {
        private readonly string _dbcs;

        public HotelRoomController(IConfiguration config)
        {
            _dbcs = config["databaseConnection"];
        }

        private IDbConnection GetDatabase()
        {
            return new SqlConnection(_dbcs);
        }

        [HttpGet("room/{filter=external}")]
        public async Task<IEnumerable<HotelRoom>> GetRooms(string filter)
        {
            var db = GetDatabase();

            var rooms = (await db.QueryAsync<HotelRoom>("SELECT * FROM [dbo].[HotelRoom]")).ToDictionary(room => room.Id, room => room);

            var rateSql = @"SELECT * FROM [dbo].[HotelRoomRate]";

            var rateGroups = (await db.QueryAsync(rateSql)).GroupBy(rate => rate.RoomId);

            Func<dynamic, bool> rateFilter = r => r.Class == "Standard" || r.Class == "Premium";

            if (filter == "internal")
            {
                rateFilter = r => true;
            }

            foreach(var group in rateGroups)
            {
                var roomId = (int) group.Key;

                var rates = group
                              .Where(rateFilter)
                              .Select(r => new HotelRoomRate { Class = r.Class, Price = r.Price })
                              .ToArray();

                rooms[roomId].Prices = rates;
            }

            return rooms.Values;
        }

        [HttpGet("room/{id}/{filter=external}")]
        public async Task<HotelRoom> GetRoom(int id, string filter)
        {
            var db = GetDatabase();

            var room = (await db.QueryAsync<HotelRoom>("SELECT * FROM [dbo].[HotelRoom] WHERE [Id] = @id", new { id })).SingleOrDefault();

            if (room != null)
            {
                Func<dynamic, bool> rateFilter = r => r.Class == "Standard" || r.Class == "Premium";

                if (filter == "internal")
                {
                    rateFilter = r => true;
                }

                var rateSql = @"SELECT * FROM [dbo].[HotelRoomRate] WHERE [RoomId] = @id";

                var rates = (await db.QueryAsync(rateSql, new { id }))
                    .Where(rateFilter)
                    .Select(r => new HotelRoomRate { Class = r.Class, Price = r.Price })
                    .ToArray();

                room.Prices = rates;
            }

            return room;
        }

        [HttpPost("room/book")]
        public async Task<Guid> BookRoom([FromBody] Booking booking)
        {
            var db = GetDatabase();

            var args = new
            {
                id = Guid.NewGuid(),
                roomId = booking.RoomId,
                start = booking.Start,
                nights = booking.Nights,
                rateClass = booking.Rate.Class,
                price = booking.Rate.Price
            };

            await db.ExecuteAsync(@"INSERT INTO [dbo].[Booking] VALUES (@id, @roomId, @start, @nights, @rateClass, @price);
                                    UPDATE [dbo].[HotelRoom] SET [Available] = 0 WHERE [Id] = @roomId", args);

            return args.id;
        }

        [HttpDelete("room/book/cancel/{id}")]
        public Task CancelBooking(Guid id)
        {
            var db = GetDatabase();

            return db.ExecuteAsync(@"DECLARE @roomId INT;
                                     SELECT @roomId = [RoomId] FROM [dbo].[Booking] WHERE [Id] = @id;
                                     DELETE FROM [dbo].[Booking] WHERE [Id] = @id;
                                     UPDATE [dbo].[HotelRoom] SET [Available] = 1 WHERE [Id] = @roomId", new { id });
        }

        [HttpPost("room/maintenance")]
        public async Task<Guid> UnderMaintenance([FromBody] MaintenanceTicket ticket)
        {
            var db = GetDatabase();

            var args = new
            {
                id = Guid.NewGuid(),
                roomId = ticket.RoomId,
                start = ticket.Start,
                reason = ticket.Reason
            };

            await db.ExecuteAsync(@"INSERT INTO [dbo].[MaintenanceTicket] VALUES (@id, @roomId, @start, @reason);
                                    UPDATE [dbo].[HotelRoom] SET [Available] = 0 WHERE [Id] = @roomId", args);

            return args.id;
        }

        [HttpDelete("room/maintenancedone")]
        public Task MaintenanceDone(Guid id)
        {
            var db = GetDatabase();

            return db.ExecuteAsync(@"DECLARE @roomId INT;
                                     SELECT @roomId = [RoomId] FROM [dbo].[MaintenanceTicket] WHERE [Id] = @id;
                                     DELETE FROM [dbo].[MaintenanceTicket] WHERE [Id] = @id;
                                     UPDATE [dbo].[HotelRoom] SET [Available] = 1 WHERE [Id] = @roomId", new { id });
        }
    }
}
