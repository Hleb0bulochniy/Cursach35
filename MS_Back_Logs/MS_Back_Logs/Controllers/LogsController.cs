using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using static MS_Back_Logs.Data.LogsContext;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MS_Back_Logs.Data;
using MS_Back_Logs.Models;

namespace MS_Back_Logs.Controllers
{
    [ApiController]
    public class LogsController : ControllerBase
    {
        [Route("Log")]
        [Authorize] //сделать роль админа
        [HttpPost]
        public IActionResult LogPost([FromBody] LogModel logModel)
        {
            try
            {
                string? authorizationHeader = Request.Headers["Authorization"];
                if (authorizationHeader == null)
                    return Unauthorized("Токен авторизации отсутствует");
                string token = authorizationHeader!.Replace("Bearer ", "");
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                    return Unauthorized("Невалидный токен");
                var jwtToken = handler.ReadJwtToken(token);
                string userId = jwtToken.Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier).Value;
                //Проверить, существует ли такой пользователь в Auth и залогировать


                LogsContext context = new LogsContext();

                Log log = new Log
                {
                    UserId = logModel.userId,
                    DateTime = logModel.dateTime,
                    ServiceName = logModel.serviceName,
                    LogLevel = logModel.logLevel,
                    EventType = logModel.eventType,
                    Message = logModel.message,
                    Details = logModel.details,
                    ErrorCode = logModel.errorCode
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
    }
}
