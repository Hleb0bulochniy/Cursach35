using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using static MS_Back_Logs.Data.LogsContext;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MS_Back_Logs.Data;
using MS_Back_Logs.Models;
using System.Text.Json;
using System;

namespace MS_Back_Logs.Controllers
{
    [ApiController]
    public class LogsController : ControllerBase //Надо ли логировать логи
    {
        private readonly LogsContext _context;
        public LogsController(LogsContext logsContext)
        {
            _context = logsContext;
        }

        /// <summary>
        /// Logs info.
        /// </summary>
        /// <response code="200">Info was logged. Returns message about completion</response>
        /// <response code="400">Received data is null, other error (watch Logs). Returns message about error</response>
        [Route("Log")]
        //[Authorize] //сделать роль админа
        [HttpPost]
        public async Task<IActionResult> LogPost(string kafkaMessage)
        {
            try
            {
                var logData = JsonSerializer.Deserialize<LogModel>(kafkaMessage);

                if (logData == null)
                {
                    return BadRequest("The log data is empty");
                }

                Log log = new Log
                {
                    UserId = logData.userId,
                    DateTime = logData.dateTime,
                    ServiceName = logData.serviceName.IsNullOrEmpty() ? "empty" : logData.serviceName,
                    LogLevel = logData.logLevel.IsNullOrEmpty() ? "empty" : logData.logLevel,
                    EventType = logData.eventType.IsNullOrEmpty() ? "empty" : logData.eventType,
                    Message = logData.message.IsNullOrEmpty() ? "empty" : logData.message,
                    Details = logData.details.IsNullOrEmpty() ? "empty" : logData.details,
                    ErrorCode = logData.errorCode.IsNullOrEmpty() ? "empty" : logData.errorCode
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
                    Message = "Swever error",
                    Details = ex.InnerException.Message,
                    ErrorCode = "400"
                };
                _context.Logs.Add(logModel);
                return BadRequest("Server error");
            }
        }
    }
}
