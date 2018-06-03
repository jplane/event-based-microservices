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
    [Route("maintenance")]
    public class MaintenanceController : Controller
    {
        private readonly string _dbcs;

        public MaintenanceController(IConfiguration config)
        {
            _dbcs = config["databaseConnection"];
        }

        private IDbConnection GetDatabase()
        {
            return new SqlConnection(_dbcs);
        }

        [HttpPost("create")]
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

            await db.ExecuteAsync(@"INSERT INTO [dbo].[MaintenanceTicket] VALUES (@id, @roomId, @start, @reason)", args);

            return args.id;
        }

        [HttpDelete("remove")]
        public Task MaintenanceDone(Guid id)
        {
            var db = GetDatabase();

            return db.ExecuteAsync(@"DELETE FROM [dbo].[MaintenanceTicket] WHERE [Id] = @id;", new { id });
        }
    }
}
