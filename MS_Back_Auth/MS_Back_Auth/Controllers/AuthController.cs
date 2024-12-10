using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using MS_Back_Auth.Models;
using MS_Back_Auth.Data;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MS_Back_Auth.Controllers
{
    public static class Cryptography
    {
        public static string ConvertPassword(string password)
        {
            string soursePass = password;
            byte[] sourcePassBytes;
            byte[] hashPassBytes;

            sourcePassBytes = ASCIIEncoding.ASCII.GetBytes(soursePass);

            hashPassBytes = new MD5CryptoServiceProvider().ComputeHash(sourcePassBytes);

            return ByteArrayToString(hashPassBytes);
        }

        static string ByteArrayToString(byte[] arrInput)
        {
            int i;
            StringBuilder sOutput = new StringBuilder(arrInput.Length);
            for (i = 0; i < arrInput.Length; i++)
            {
                sOutput.Append(arrInput[i].ToString("X2"));
            }
            return sOutput.ToString();
        }
    }

    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly HelpFuncs _helpfuncs;
        private readonly AuthContext _context;
        private readonly ProducerService _producerService;
        public AuthController(AuthContext authContext, ProducerService producerService, HelpFuncs helpfuncs)
        {
            _helpfuncs = helpfuncs;
            _context = authContext;
            _producerService = producerService;
        }
        [Route("UserRegistration")]
        [HttpPost]
        public async Task<IActionResult> UserRegistrationPost([FromBody] RegistrationClass registrationClass)
        {
            LogModel logModel = LogModelCreate("UserRegistrationPost", "Registration successful");
            try
            {
                if (registrationClass.password1 == registrationClass.password2)
                {
                    if (!_context.Users.Any(u => u.Username == registrationClass.userName || u.Email == registrationClass.email))
                    {
                        string cryptedPassword = Cryptography.ConvertPassword(registrationClass.password1);
                        User user = new User()
                        {
                            Username = registrationClass.userName,
                            Email = registrationClass.email,
                            Password = cryptedPassword,
                        };
                        await _context.Users.AddAsync(user);
                        await _context.SaveChangesAsync();
                        await LogEventAsync(logModel);
                        return Ok(logModel.message);
                    }
                    else
                    {
                        logModel.logLevel = "Error";
                        logModel.message = "The user already exists";
                        logModel.errorCode = "400";
                        await LogEventAsync(logModel);
                        return BadRequest(logModel.message);
                    }
                }
                else
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Passwords don't match";
                    logModel.errorCode = "400";
                    await LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }

        [Route("UserLogin")]
        [HttpPost]
        public async Task<IActionResult> UserLoginPost([FromBody] LoginClass model)
        {
            LogModel logModel = LogModelCreate("UserLoginPost", "Login successful");
            try
            {
                string cryptedPassword = Cryptography.ConvertPassword(model.password);
                User? dbuser = _context.Users.FirstOrDefault(u => u.Username == model.userName); //переделать если пароль не совпадает
                if (dbuser == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "There is no user with this login";
                    logModel.errorCode = "401";
                    await LogEventAsync(logModel);
                    return Unauthorized(logModel.message);
                }
                else if (dbuser.Password != cryptedPassword)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "The password doesn't match";
                    logModel.errorCode = "401";
                    await LogEventAsync(logModel);
                    return Unauthorized(logModel.message);
                }

                TokenResponceClass response = CreateJWT(dbuser.Username, dbuser.Id.ToString());

                await LogEventAsync(logModel);
                return Ok(response);
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }

        [Route("RefreshToken")]
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> RefreshTokenGet() //если токен пустой, добавить обработчик
        {
            LogModel logModel = LogModelCreate("RefreshTokenGet", "Reftesh token gotten");
            try
            {
                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;
                string userName = _context.Users.FirstOrDefault(u => u.Id == parsedUserId).Username; //стоит ли это оставлять так или лучше доставать значение из токена

                TokenResponceClass response = CreateJWT(userName, parsedUserId.ToString());

                await LogEventAsync(logModel);
                return Ok(response);
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }

        [Authorize]
        [Route("PasswordCheck")]
        [HttpPost]
        public async Task<IActionResult> PasswordCheck([FromBody] PasswordClass password)
        {
            LogModel logModel = LogModelCreate("PasswordCheck", "The password is correct");
            try
            {
                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;
                User? user = _context.Users.FirstOrDefault(u => u.Id == parsedUserId);
                string cryptedPassword = Cryptography.ConvertPassword(password.password);

                if (user == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "There is no such user";
                    logModel.errorCode = "401";
                    await LogEventAsync(logModel);
                    return Unauthorized(logModel.message);
                }

                else if (user.Password != cryptedPassword)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "The password doesn't match";
                    logModel.errorCode = "401";
                    await LogEventAsync(logModel);
                    return Unauthorized(logModel.message);
                }
                return Ok(logModel.message);
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }

        [Route("UserIdCheck/{idModel:int}")]
        [HttpGet]
        public async Task<(bool, string)> UserIdCheck(int idModel)
        {
            bool isValid = false;
            string userName = _context.Users.FirstOrDefault(u => u.Id == idModel).Username;
            if (!userName.IsNullOrEmpty()) isValid = true;
            return (isValid, userName);
        }

        private TokenResponceClass CreateJWT(string userName, string userId)
        {
            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier,userId),
                    new Claim(ClaimTypes.Name,userName),
                };

            var jwt = new JwtSecurityToken(
            issuer: AuthOptions.ISSUER,
            audience: AuthOptions.AUDIENCE,
            claims: claims,
            expires: DateTime.UtcNow.Add(TimeSpan.FromHours(24)),
            signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            var jwtr = new JwtSecurityToken(
            issuer: AuthOptions.ISSUER,
            audience: AuthOptions.AUDIENCE,
            claims: claims,
            expires: DateTime.UtcNow.Add(TimeSpan.FromHours(300)),
            signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
            var encodedJwtr = new JwtSecurityTokenHandler().WriteToken(jwt);

            TokenResponceClass response = new TokenResponceClass
            {
                access_token = encodedJwt,
                refresh_token = encodedJwtr,
                username = userName,
            };
            return response;
        }

        private async Task LogEventAsync(LogModel logModel)
        {
            var message = JsonSerializer.Serialize(logModel);
            await _producerService.ProduceAsync("LogUpdates", message);
        }

        private LogModel LogModelCreate(string eventType, string message)
        {
            return new LogModel
            {
                userId = -1,
                dateTime = DateTime.UtcNow,
                serviceName = "AuthController",
                logLevel = "Info",
                eventType = eventType,
                message = message,
                details = "",
                errorCode = "200"
            };
        }

        private async Task<(bool Success, IActionResult? Result, int UserId)> ValidateAndParseUserIdAsync(HttpRequest request, LogModel logModel)
        {
            string? userId = _helpfuncs.GetUserIdFromToken(request);
            if (string.IsNullOrEmpty(userId))
            {
                logModel.logLevel = "Error";
                logModel.message = "Invalid or missing token";
                logModel.errorCode = "401";
                await LogEventAsync(logModel);
                return (false, Unauthorized(logModel.message), -1);
            }

            if (!int.TryParse(userId, out int parsedUserId))
            {
                logModel.logLevel = "Error";
                logModel.message = "User ID conversion in int failed";
                logModel.errorCode = "500";
                await LogEventAsync(logModel);
                return (false, BadRequest(logModel.message), -1);
            }

            return (true, null, parsedUserId);
        }

        private async Task<LogModel> LogModelChangeForServerError(LogModel logModel, Exception ex)
        {
            logModel.eventType = "Error";
            logModel.message = "Server error";
            logModel.details = ex.Message;
            logModel.errorCode = "500";
            await LogEventAsync(logModel);
            return logModel;
        }
    }
}
