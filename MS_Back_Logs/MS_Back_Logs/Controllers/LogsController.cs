using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using static MS_Back_Logs.Data.LogsContext;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MS_Back_Logs.Data;
using MS_Back_Logs.Models;
using System.Text.Json;

namespace MS_Back_Logs.Controllers
{
    [ApiController]
    public class LogsController : ControllerBase
    {
        [Route("Log")]
        //[Authorize] //сделать роль админа
        [HttpPost]
        public IActionResult LogPost(string kafkaMessage)
        {
            try
            {
                var logData = JsonSerializer.Deserialize<LogModel>(kafkaMessage);

                if (logData == null)
                {
                    return BadRequest("Присланные данные пусты");
                }

                LogsContext context = new LogsContext();

                Log log = new Log //для каждого поставить значение в случае null
                {
                    UserId = logData.userId,
                    DateTime = logData.dateTime,
                    ServiceName = logData.serviceName,
                    LogLevel = logData.logLevel,
                    EventType = logData.eventType,
                    Message = logData.message,
                    Details = logData.details,
                    ErrorCode = logData.errorCode
                };
                context.Logs.Add(log);

                context.SaveChanges();
                return Ok("Данные внесены"); //логирование
            }
            catch
            {
                //залогировать ошибку
                return BadRequest("Произошла ошибка на сервере"); //дописать код ошибки чтобы найти по логам
            }
        }

        /*[Route("Log123")]
        [HttpPost]
        public IActionResult Log123Post(string kafkaMessage)
        {
            var logData = JsonSerializer.Deserialize<LogModel>(kafkaMessage);
            Console.WriteLine(logData.serviceName);
            return(Ok());
        }*/
    }
}
