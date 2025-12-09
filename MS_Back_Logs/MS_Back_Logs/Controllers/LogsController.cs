using Microsoft.AspNetCore.Mvc;
using MS_Back_Logs.Data;
using MS_Back_Logs.Models;
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
            try
            {
                var logData = JsonSerializer.Deserialize<LogModelDto>(kafkaMessage);

                if (logData == null)
                {
                    return BadRequest("The log data is empty");
                }

                Log log = new Log
                {
                    UserId = logData.userId,
                    DateTime = logData.dateTime,
                    ServiceName = string.IsNullOrWhiteSpace(logData.serviceName) ? "empty" : logData.serviceName,
                    LogLevel = string.IsNullOrWhiteSpace(logData.logLevel) ? "empty" : logData.logLevel,
                    EventType = string.IsNullOrWhiteSpace(logData.eventType) ? "empty" : logData.eventType,
                    Message = string.IsNullOrWhiteSpace(logData.message) ? "empty" : logData.message,
                    Details = string.IsNullOrWhiteSpace(logData.details) ? "empty" : logData.details,
                    ErrorCode = string.IsNullOrWhiteSpace(logData.errorCode) ? "empty" : logData.errorCode
                };
                _context.Logs.Add(log);

                await _context.SaveChangesAsync();
                return Ok("Log input successful");
            }
            catch (Exception ex)
            {
                Log logModel = new Log
                {
                    UserId = -1,
                    DateTime = DateTime.UtcNow,
                    ServiceName = "LogsController",
                    LogLevel = "Error",
                    EventType = "LogPost",
                    Message = "Server error",
                    Details = ex.InnerException.Message,
                    ErrorCode = "500"
                };
                Console.WriteLine("LOG ERROR" + ex.InnerException.Message);
                _context.Logs.Add(logModel);
                await _context.SaveChangesAsync();
                return BadRequest("Server error");
            }
        }
    }
}
