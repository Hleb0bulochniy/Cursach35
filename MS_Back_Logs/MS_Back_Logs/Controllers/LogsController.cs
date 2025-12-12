using Microsoft.AspNetCore.Mvc;
using MS_Back_Logs.Data;
using MS_Back_Logs.Models;
using System.Diagnostics;
using System.Text.Json;

namespace MS_Back_Logs.Controllers
{
    [ApiController]
    [Route("api/v1/logs")]
    public class LogsController : ControllerBase //Надо ли логировать логи
    {
        private readonly LogsContext _context;
        public LogsController(LogsContext logsContext)
        {
            _context = logsContext;
        }

        /// <summary>
        /// Log info.
        /// </summary>
        /// <response code="200">Info was logged. Returns message about completion</response>
        // <response code="400">Received data is null, other error (watch Logs). Returns message about error</response>
        /// <response code="500">Server error</response>
        //[Authorize] //можно сделать роль админа
        [HttpPost("Log")]
        public async Task<IActionResult> LogPost([FromBody] string kafkaMessage)
        {
            Console.WriteLine(kafkaMessage);
            try
            {
                var logData = JsonSerializer.Deserialize<LogModel>(kafkaMessage);

                if (logData == null)
                {
                    return BadRequest("The log data is empty");
                }

                Log log = new Log
                {
                    UserId = logData.UserId,
                    DateTime = logData.DateTime,
                    ServiceName = string.IsNullOrWhiteSpace(logData.ServiceName) ? "empty" : logData.ServiceName,
                    LogLevel = string.IsNullOrWhiteSpace(logData.LogLevel) ? "empty" : logData.LogLevel,
                    EventType = string.IsNullOrWhiteSpace(logData.EventType) ? "empty" : logData.EventType,
                    Message = string.IsNullOrWhiteSpace(logData.Message) ? "empty" : logData.Message,
                    Details = string.IsNullOrWhiteSpace(logData.Details) ? "empty" : logData.Details,
                    ErrorCode = string.IsNullOrWhiteSpace(logData.ErrorCode) ? "empty" : logData.ErrorCode
                };
                _context.Logs.Add(log);

                await _context.SaveChangesAsync();
                return Ok("Log input successful");
            }
            catch (Exception ex)
            {
                var details = ex.InnerException?.Message ?? ex.Message;

                Log logModel = new Log
                {
                    UserId = -1,
                    DateTime = DateTime.UtcNow,
                    ServiceName = "LogsController",
                    LogLevel = "Error",
                    EventType = "LogPost",
                    Message = "Server error",
                    Details = details,
                    ErrorCode = "500"
                };
                Console.WriteLine("LOG ERROR" + details);
                _context.Logs.Add(logModel);
                await _context.SaveChangesAsync();
                return BadRequest("Server error");
            }
        }
    }
}
