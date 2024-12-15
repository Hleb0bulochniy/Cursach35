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
    [Route("Auth")]
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


        /// <summary>
        /// Registrate a user.
        /// </summary>
        /// <response code="200">Registration successful. Returns json with progress</response>
        /// <response code="400">The user already exists, received data is null, passwords don't match, other error (watch Logs). Returns message about error</response>
        [Route("UserRegistration")]
        [HttpPost]
        public async Task<IActionResult> UserRegistrationPost([FromBody] RegistrationClass registrationClass)
        {
            LogModel logModel = LogModelCreate("UserRegistrationPost", "Registration successful");
            try
            {
                if (registrationClass == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Received data is null";
                    logModel.errorCode = "400";
                    await LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }
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
                        logModel.userId = user.Id;
                        await LogEventAsync(logModel);
                        return Ok(logModel.message);
                    }
                    else
                    {
                        logModel.logLevel = "Error";
                        logModel.message = "The user already exists";
                        logModel.details = $"userName: {registrationClass.userName}, EMail: {registrationClass.email}";
                        logModel.errorCode = "400";
                        await LogEventAsync(logModel);
                        return BadRequest(logModel.message);
                    }
                }
                else
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Passwords don't match";
                    logModel.details = $"userName: {registrationClass.userName}, EMail: {registrationClass.email}";
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


        /// <summary>
        /// Login user.
        /// </summary>
        /// <response code="200">Login succwssful. Returns json with jwt tokens and userName</response>
        /// <response code="400">The user already exists, received data is null, passwords don't match, other error (watch Logs). Returns message about error</response>
        /// <response code="401">There is no user with this login, the password doesn't match. Returns message about error</response>
        [Route("UserLogin")]
        [HttpPost]
        public async Task<IActionResult> UserLoginPost([FromBody] LoginClass model)
        {
            LogModel logModel = LogModelCreate("UserLoginPost", "Login successful");
            try
            {
                if (model == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Received data is null";
                    logModel.errorCode = "400";
                    await LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }
                string cryptedPassword = Cryptography.ConvertPassword(model.password);
                User? dbuser = _context.Users.FirstOrDefault(u => u.Username == model.userName);
                logModel.userId = dbuser == null? -1 : dbuser.Id;
                if (dbuser == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "There is no user with this login";
                    logModel.errorCode = "401";
                    logModel.details = $"User: {model.userName}";
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

                TokenResponceClass response = CreateJWT(dbuser.Username, dbuser.Id.ToString(), dbuser.PlayerId == null ? "-1" : dbuser.PlayerId.ToString(), dbuser.CreatorId == null ? "-1" : dbuser.CreatorId.ToString());

                await LogEventAsync(logModel);
                return Ok(response);
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }


        /// <summary>
        /// Get new tokens.
        /// </summary>
        /// <response code="200">Token change successful. Returns json with jwt tokens and userName</response>
        /// <response code="400">User ID (from token) conversion in int failed, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">The user wasn't found. Returns message about error</response>
        [Route("RefreshToken")]
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> RefreshTokenGet()
        {
            LogModel logModel = LogModelCreate("RefreshTokenGet", "Reftesh token gotten");
            try
            {
                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;
                User user = _context.Users.FirstOrDefault(u => u.Id == parsedUserId); //стоит ли это оставлять так или лучше доставать значение из токена
                if (user == null)
                {
                    logModel.errorCode = "404";
                    logModel.logLevel = "Error";
                    logModel.message = "The user wasn't found";
                }
                TokenResponceClass response = CreateJWT(user.Username, parsedUserId.ToString(), user.PlayerId == null ? "-1" : user.PlayerId.ToString(), user.CreatorId == null ? "-1" : user.CreatorId.ToString());

                await LogEventAsync(logModel);
                return Ok(response);
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }


        /// <summary>
        /// Check if password correct.
        /// </summary>
        /// <response code="200">Password correct. Returns message about completion</response>
        /// <response code="400">User ID (from token) conversion in int failed, received data is null, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token, the password doesn't match. Returns message about error</response>
        /// <response code="404">The user wasn't found. Returns message about error</response>
        [Route("PasswordCheck")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> PasswordCheck([FromBody] PasswordClass? password)
        {
            LogModel logModel = LogModelCreate("PasswordCheck", "The password is correct");
            try
            {
                if (password == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Received data is null";
                    logModel.errorCode = "400";
                    await LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }
                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;
                User? user = _context.Users.FirstOrDefault(u => u.Id == parsedUserId);
                string cryptedPassword = Cryptography.ConvertPassword(password.password);

                if (user == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "There is no such user";
                    logModel.errorCode = "404";
                    await LogEventAsync(logModel);
                    return NotFound(logModel.message);
                }

                else if (user.Password != cryptedPassword)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "The password doesn't match";
                    logModel.errorCode = "401";
                    await LogEventAsync(logModel);
                    return Unauthorized(logModel.message);
                }
                await LogEventAsync(logModel);
                return Ok(logModel.message);
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }


        /// <summary>
        /// Check if user exists.
        /// </summary>
        /// <remarks>If user exists, it sends confirmation and his username. If user doesn't exists or recieved data is wrong, it sends denial</remarks>
        [Route("UserIdCheck/{idModel:int}")]
        [HttpGet]
        public async Task<UserIdCheckModel> UserIdCheck(UserIdCheckModel userIdCheckModel) //стоит ли это логировать
        {
            LogModel logModel = LogModelCreate("UserIdCheck", "Check successful");
            try
            {
                Console.WriteLine("1");
                /*if (userIdCheckModel == null || userIdCheckModel.userId < 0)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Received data is wrong";
                    logModel.errorCode = "400";
                    userIdCheckModel.isValid = false;
                    userIdCheckModel.userName = "";

                    await LogEventAsync(logModel);
                     
                    return userIdCheckModel;
                }*/
                User? user = new User();

                user = _context.Users.FirstOrDefault(u => u.Id == userIdCheckModel.userId);
                if (user == null)
                {
                    user = _context.Users.FirstOrDefault(u => u.PlayerId == userIdCheckModel.playerId);
                    if (user == null)
                    {
                        user = _context.Users.FirstOrDefault(u => u.CreatorId == userIdCheckModel.creatorId);
                    }
                }

                if (user == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "There is no such user";
                    logModel.errorCode = "404";
                    userIdCheckModel.isValid = false;
                    userIdCheckModel.userName = "";

                    await LogEventAsync(logModel);

                    return userIdCheckModel;
                }

                userIdCheckModel.isValid = true;
                userIdCheckModel.userName = user.Username;

                if (userIdCheckModel.requestMessage == "player")
                {

                    if (user.PlayerId == -1 || user.PlayerId == null)
                    {
                        int? max = _context.Users.Max(u => u.PlayerId) + 1;
                        user.PlayerId = (max == 0 || max == null) ? 1 : max;
                        _context.SaveChangesAsync();
                    }

                    userIdCheckModel.playerId = (int)user.PlayerId;
                }
                if (userIdCheckModel.requestMessage == "creator")
                {

                    if (user.CreatorId == -1 || user.CreatorId == null)
                    {
                        int? max = _context.Users.Max(u => u.CreatorId) + 1;
                        user.CreatorId = (max == 0 || max == null) ? 1 : max;
                        _context.SaveChangesAsync();
                    }

                    userIdCheckModel.creatorId = user.CreatorId;
                }

                return userIdCheckModel;
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await LogModelChangeForServerError(logModel, ex);
                return userIdCheckModel;
            }
        }

        private TokenResponceClass CreateJWT(string userName, string userId, string playerId, string creatorId)
        {
            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier,userId),
                    new Claim("PlayerIdentifier",playerId),
                    new Claim("CreatorIdentifier",creatorId),
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
            var encodedJwtr = new JwtSecurityTokenHandler().WriteToken(jwtr);
            if (encodedJwt == encodedJwtr) Console.WriteLine("Токены равны");
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
            logModel.logLevel = "Error";
            logModel.message = "Server error";
            logModel.details = $"Error: {ex.Message} ||||| Inner error: {ex.InnerException}";
            logModel.errorCode = "500";
            await LogEventAsync(logModel);
            return logModel;
        }
    }
}
