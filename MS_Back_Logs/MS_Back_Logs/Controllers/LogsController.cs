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

                Log log = new Log //для каждого поставить значение в случае null
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
            catch
            {
                return BadRequest("Server error");
            }
        }
    }
}
