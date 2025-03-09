using Microsoft.AspNetCore.Mvc;
using Pinpoint_Quiz.Services;

namespace Pinpoint_Quiz.Controllers
{
    [Route("debug/db")]
    public class DebugController : Controller
    {
        private readonly MySqlDatabase _db;
        private readonly ILogger<DebugController> _logger;

        public DebugController(MySqlDatabase db, ILogger<DebugController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet("")]
        public IActionResult TestDb()
        {
            try
            {
                using var conn = _db.GetConnection();
                conn.Open();
                _logger.LogInformation("Database connection succeeded!");
                return Ok("Database connection succeeded!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection failed");
                return StatusCode(500, ex.ToString());
            }
        }
    }

}
