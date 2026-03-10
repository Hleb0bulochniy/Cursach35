using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using MS_Back_Auth.Models;
using MS_Back_Auth.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace MS_Back_Auth.Controllers
{
    public static class Cryptography
    {

        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100000;

        public static string Hash(string password)
        {
            using var randomNumber = RandomNumberGenerator.Create();
            byte[] salt = new byte[SaltSize];
            randomNumber.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] key = pbkdf2.GetBytes(KeySize);

            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
        }

        public static bool Verify(string password, string storedHash)
        {
            var parts = storedHash.Split('.');
            if (parts.Length != 2)
                return false;

            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] expectedKey = Convert.FromBase64String(parts[1]);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] actualKey = pbkdf2.GetBytes(KeySize);

            return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
        }
    }

    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IHelpFuncs _helpfuncs;
        private readonly AuthContext _context;
        public AuthController(AuthContext authContext, IHelpFuncs helpfuncs)
        {
            _helpfuncs = helpfuncs;
            _context = authContext;
        }


        /// <summary>
        /// Registrate a user.
        /// </summary>
        /// <response code="200">Registration successful. Returns json with progress</response>
        /// <response code="400">The user already exists, received data is null, passwords don't match, other error (watch Logs). Returns message about error</response>
        /// <response code="500">Server error</response>
        [HttpPost("UserRegistration")]
        public async Task<IActionResult> UserRegistrationPost([FromBody] RegistrationDTO registrationClass)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("UserRegistrationPost", "Registration successful");
            try
            {
                if (registrationClass == null)
                {
                    logModel.LogLevel = "Error";
                    logModel.Message = "Received data is null";
                    logModel.ErrorCode = "400";
                    await _helpfuncs.LogEventAsync(logModel);
                    return BadRequest(logModel.Message);
                }
                if (registrationClass.password1 == registrationClass.password2)
                {
                    if (!_helpfuncs.IsPasswordValid(registrationClass.password1))
                    {
                        return BadRequest("Password does not meet requirements");
                    }
                    if (!_helpfuncs.IsEmailValid(registrationClass.email))
                    {
                        return BadRequest("Email does not meet requirements");
                    }
                    if (!_helpfuncs.IsUsernameValid(registrationClass.userName))
                    {
                        return BadRequest("Username does not meet requirements");
                    }
                    if (!(await _context.Users.AnyAsync(u => u.Username == registrationClass.userName || u.Email == registrationClass.email)))
                    {
                        string cryptedPassword = Cryptography.Hash(registrationClass.password1);
                        User user = new User()
                        {
                            Username = registrationClass.userName,
                            Email = registrationClass.email,
                            Password = cryptedPassword,
                        };
                        await _context.Users.AddAsync(user);
                        await _context.SaveChangesAsync();
                        logModel.UserId = user.Id;
                        await _helpfuncs.LogEventAsync(logModel);
                        return Ok(logModel.Message);
                    }
                    else
                    {
                        logModel.LogLevel = "Error";
                        logModel.Message = "The user already exists";
                        logModel.Details = $"userName: {registrationClass.userName}, EMail: {registrationClass.email}";
                        logModel.ErrorCode = "400";
                        await _helpfuncs.LogEventAsync(logModel);
                        return BadRequest(logModel.Message);
                    }
                }
                else
                {
                    logModel.LogLevel = "Error";
                    logModel.Message = "Passwords don't match";
                    logModel.Details = $"userName: {registrationClass.userName}, EMail: {registrationClass.email}";
                    logModel.ErrorCode = "400";
                    await _helpfuncs.LogEventAsync(logModel);
                    return BadRequest(logModel.Message);
                }
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await _helpfuncs.LogModelChangeForServerError(logModel, ex);
                return StatusCode(500, updatedLogModel.Message);
            }
        }


        /// <summary>
        /// Login user.
        /// </summary>
        /// <response code="200">Login succwssful. Returns json with jwt tokens and userName</response>
        /// <response code="400">The user already exists, received data is null, passwords don't match, other error (watch Logs). Returns message about error</response>
        /// <response code="401">There is no user with this login, the password doesn't match. Returns message about error</response>
        /// <response code="500">Server error</response>
        [HttpPost("UserLogin")]
        public async Task<IActionResult> UserLoginPost([FromBody] LoginDTO model)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("UserLoginPost", "Login successful");
            try
            {
                if (model == null)
                {
                    logModel.LogLevel = "Error";
                    logModel.Message = "Received data is null";
                    logModel.ErrorCode = "400";
                    await _helpfuncs.LogEventAsync(logModel);
                    return BadRequest(logModel.Message);
                }
                User? dbuser = await _context.Users.FirstOrDefaultAsync(u => u.Username == model.userName);
                logModel.UserId = dbuser == null? -1 : dbuser.Id;
                if (dbuser == null)
                {
                    logModel.LogLevel = "Error";
                    logModel.Message = "There is no user with this login";
                    logModel.ErrorCode = "401";
                    logModel.Details = $"User: {model.userName}";
                    await _helpfuncs.LogEventAsync(logModel);
                    return Unauthorized(logModel.Message);
                }
                bool arePasswordsTheSame = Cryptography.Verify(model.password, dbuser.Password);
                if (!arePasswordsTheSame)
                {
                    logModel.LogLevel = "Error";
                    logModel.Message = "The password doesn't match";
                    logModel.ErrorCode = "401";
                    await _helpfuncs.LogEventAsync(logModel);
                    return Unauthorized(logModel.Message);
                }

                TokenResponceDTO response = _helpfuncs.CreateJWT(dbuser.Username, dbuser.Id.ToString(), dbuser.PlayerId == null ? "-1" : dbuser.PlayerId.ToString(), dbuser.CreatorId == null ? "-1" : dbuser.CreatorId.ToString());

                await _helpfuncs.LogEventAsync(logModel);
                return Ok(response);
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await _helpfuncs.LogModelChangeForServerError(logModel, ex);
                return StatusCode(500, updatedLogModel.Message);
            }
        }


        /// <summary>
        /// Get new tokens.
        /// </summary>
        /// <response code="200">Token change successful. Returns json with jwt tokens and userName</response>
        /// <response code="400">User ID (from token) conversion in int failed, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">The user wasn't found. Returns message about error</response>
        /// <response code="500">Server error</response>
        [Authorize]
        [HttpGet("RefreshToken")]
        public async Task<IActionResult> RefreshTokenGet()
        {
            LogModel logModel = _helpfuncs.LogModelCreate("RefreshTokenGet", "Reftesh token gotten");
            try
            {
                var (success, errorCode, parsedUserId) = await _helpfuncs.ValidateAndParseUserIdAsync(Request, logModel);
                if (!success)
                {
                    if (errorCode == 401) return Unauthorized(logModel.Message);
                    else if (errorCode == 400) return BadRequest(logModel.Message);
                }
                logModel.UserId = parsedUserId;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == parsedUserId);
                if (user == null)
                {
                    logModel.ErrorCode = "404";
                    logModel.LogLevel = "Error";
                    logModel.Message = "The user wasn't found";
                    await _helpfuncs.LogEventAsync(logModel);
                    return NotFound(logModel.Message);
                }

                TokenResponceDTO response = _helpfuncs.CreateJWT(
                    user.Username, 
                    parsedUserId.ToString(), 
                    user.PlayerId == null ? "-1" : user.PlayerId.ToString(), 
                    user.CreatorId == null ? "-1" : user.CreatorId.ToString()
                );

                await _helpfuncs.LogEventAsync(logModel);
                return Ok(response);
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await _helpfuncs.LogModelChangeForServerError(logModel, ex);
                return StatusCode(500, updatedLogModel.Message);
            }
        }


        /// <summary>
        /// Check if password correct.
        /// </summary>
        /// <response code="200">Password correct. Returns message about completion</response>
        /// <response code="400">User ID (from token) conversion in int failed, received data is null, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token, the password doesn't match. Returns message about error</response>
        /// <response code="404">The user wasn't found. Returns message about error</response>
        /// <response code="500">Server error</response>
        [Authorize]
        [HttpPost("PasswordCheck")]
        public async Task<IActionResult> PasswordCheck([FromBody] PasswordDTO? password)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("PasswordCheck", "The password is correct");
            try
            {
                if (password == null)
                {
                    logModel.LogLevel = "Error";
                    logModel.Message = "Received data is null";
                    logModel.ErrorCode = "400";
                    await _helpfuncs.LogEventAsync(logModel);
                    return BadRequest(logModel.Message);
                }
                var (success, errorCode, parsedUserId) = await _helpfuncs.ValidateAndParseUserIdAsync(Request, logModel);
                if (!success)
                {
                    if(errorCode == 401) return Unauthorized(logModel.Message);
                    else if(errorCode == 400) return BadRequest(logModel.Message);
                }
                logModel.UserId = parsedUserId;
                User? user = await _context.Users.FirstOrDefaultAsync(u => u.Id == parsedUserId);

                if (user == null)
                {
                    logModel.LogLevel = "Error";
                    logModel.Message = "There is no such user";
                    logModel.ErrorCode = "404";
                    await _helpfuncs.LogEventAsync(logModel);
                    return NotFound(logModel.Message);
                }

                bool ok = Cryptography.Verify(password.password, user.Password);
                if (!ok)
                {
                    logModel.LogLevel = "Error";
                    logModel.Message = "The password doesn't match";
                    logModel.ErrorCode = "401";
                    await _helpfuncs.LogEventAsync(logModel);
                    return Unauthorized(logModel.Message);
                }
                await _helpfuncs.LogEventAsync(logModel);
                return Ok(logModel.Message);
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await _helpfuncs.LogModelChangeForServerError(logModel, ex);
                return StatusCode(500, updatedLogModel.Message);
            }
        }


        /// <summary>
        /// Check if user exists.
        /// </summary>
        /// <remarks>If user exists, it sends confirmation and his username. If user doesn't exists or recieved data is wrong, it sends denial</remarks>
        [NonAction]
        public async Task<UserIdCheckDTO> UserIdCheck(UserIdCheckDTO userIdCheckModel)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("UserIdCheck", "Check successful");
            try
            {
                if (userIdCheckModel == null || userIdCheckModel.userId < 0)
                {
                    logModel.LogLevel = "Error";
                    logModel.Message = "Received data is wrong";
                    logModel.ErrorCode = "400";
                    userIdCheckModel.isValid = false;
                    userIdCheckModel.userName = "";

                    await _helpfuncs.LogEventAsync(logModel);
                     
                    return userIdCheckModel;
                }
                User? user = new User();

                user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userIdCheckModel.userId);
                if (user == null)
                {
                    user = await _context.Users.FirstOrDefaultAsync(u => u.PlayerId == userIdCheckModel.playerId);
                    if (user == null)
                    {
                        user = await _context.Users.FirstOrDefaultAsync(u => u.CreatorId == userIdCheckModel.creatorId);
                    }
                }

                if (user == null)
                {
                    logModel.LogLevel = "Error";
                    logModel.Message = "There is no such user";
                    logModel.ErrorCode = "404";
                    userIdCheckModel.isValid = false;
                    userIdCheckModel.userName = "";

                    await _helpfuncs.LogEventAsync(logModel);

                    return userIdCheckModel;
                }

                userIdCheckModel.isValid = true;
                userIdCheckModel.userName = user.Username;

                if (userIdCheckModel.requestMessage == "player")
                {

                    if (user.PlayerId == null || user.PlayerId <= 0)
                    {
                        var maxPlayerId = await _context.Users
                            .Where(u => u.PlayerId != null && u.PlayerId > 0)
                            .MaxAsync(u => (int?)u.PlayerId) ?? 0;

                        user.PlayerId = maxPlayerId + 1;
                        await _context.SaveChangesAsync();
                    }
                    userIdCheckModel.playerId = user.PlayerId.Value;
                }
                if (userIdCheckModel.requestMessage == "creator")
                {

                    if (user.CreatorId == null || user.CreatorId <= 0)
                    {
                        var maxCreatorId = await _context.Users
                            .Where(u => u.CreatorId != null && u.CreatorId > 0)
                            .MaxAsync(u => (int?)u.CreatorId) ?? 0;

                        user.CreatorId = maxCreatorId + 1;
                        await _context.SaveChangesAsync();
                    }
                    userIdCheckModel.creatorId = user.CreatorId.Value;
                }

                return userIdCheckModel;
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await _helpfuncs.LogModelChangeForServerError(logModel, ex);
                return userIdCheckModel;
            }
        }

        
    }
}
