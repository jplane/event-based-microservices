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
                              .Where(r => r.Class == "Standard" || r.Class == "Premium")
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

        [HttpPost("update")]
        public async Task<object> SetAvailability([FromBody] dynamic events)
        {
            var db = GetDatabase();

            foreach (var evt in events)
            {
                if (evt.eventType == "Microsoft.EventGrid.SubscriptionValidationEvent")
                {
                    return new { validationResponse = (string) evt.data.validationCode };
                }

                object args = null;

                if (evt.eventType == "bookingCreated" || evt.eventType == "bookingCanceled")
                {
                    args = new { id = (int)evt.data.booking.roomId, available = evt.eventType == "bookingCreated" ? 0 : 1 };
                }
                else if (evt.eventType == "maintenanceTicketCreated" || evt.eventType == "maintenanceTicketCompleted")
                {
                    args = new { id = (int) evt.data.ticket.roomId, available = evt.eventType == "maintenanceTicketCreated" ? 0 : 1 };
                }

                await db.ExecuteAsync("UPDATE [dbo].[HotelRoom] SET [Available] = @available WHERE [Id] = @id", args);
            }

            return null;
        }
    }
}
