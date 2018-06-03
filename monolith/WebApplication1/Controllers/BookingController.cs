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
    [Route("booking")]
    public class BookingController : Controller
    {
        private readonly string _dbcs;

        public BookingController(IConfiguration config)
        {
            _dbcs = config["databaseConnection"];
        }

        private IDbConnection GetDatabase()
        {
            return new SqlConnection(_dbcs);
        }

        [HttpPost("create")]
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

            await db.ExecuteAsync(@"INSERT INTO [dbo].[Booking] VALUES (@id, @roomId, @start, @nights, @rateClass, @price)", args);

            return args.id;
        }

        [HttpDelete("cancel/{id}")]
        public Task CancelBooking(Guid id)
        {
            var db = GetDatabase();

            return db.ExecuteAsync(@"DELETE FROM [dbo].[Booking] WHERE [Id] = @id", new { id });
        }
    }
}
