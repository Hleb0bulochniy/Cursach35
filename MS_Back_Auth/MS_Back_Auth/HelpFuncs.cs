using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MS_Back_Auth.Data;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace MS_Back_Auth
{
    public class HelpFuncs
    {
        private readonly ProducerService _producerService;
        private readonly AuthContext _context;
        public HelpFuncs(ProducerService producerService, AuthContext authContext)
        {
            _producerService = producerService;
            _context = authContext;
        }

        public string? GetUserIdFromToken(HttpRequest request)
        {
            string? authorizationHeader = request.Headers["Authorization"];
            if (authorizationHeader == null) return null;

            string token = authorizationHeader.Replace("Bearer ", "");
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token)) return null;

            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
        }

        public async Task<(bool Success, int ErrorCode, int UserId)> ValidateAndParseUserIdAsync(HttpRequest request, LogModel logModel)
        {
            string? userId = GetUserIdFromToken(request);
            //var userIdClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                logModel.LogLevel = "Error";
                logModel.Message = "Token has no NameIdentifier claim";
                logModel.ErrorCode = "401";
                await LogEventAsync(logModel);
                return (false, 401, - 1);
            }

            if (!int.TryParse(userId, out int parsedUserId))
            {
                logModel.LogLevel = "Error";
                logModel.Message = "User ID conversion in int failed";
                logModel.ErrorCode = "400";
                await LogEventAsync(logModel);
                return (false, 400, - 1);
            }

            return (true, 200, parsedUserId);
        }

        public async Task LogEventAsync(LogModel logModel)
        {
            var message = JsonSerializer.Serialize(logModel);
            await _producerService.ProduceAsync("LogUpdates", message);
        }

        public LogModel LogModelCreate(string eventType, string message)
        {
            return new LogModel
            {
                UserId = -1,
                DateTime = DateTime.UtcNow,
                ServiceName = "AuthController",
                LogLevel = "Info",
                EventType = eventType,
                Message = message,
                Details = "",
                ErrorCode = "200"
            };
        }



        public async Task<LogModel> LogModelChangeForServerError(LogModel logModel, Exception ex)
        {
            logModel.LogLevel = "Error";
            logModel.Message = "Server error";
            logModel.Details = $"Error: {ex.Message} ||||| Inner error: {ex.InnerException}";
            logModel.ErrorCode = "500";
            await LogEventAsync(logModel);
            return logModel;
        }


        public bool IsPasswordValid(string password)
        {
            string signsForPassword = "abcdefghijklmnopqrstuvwxyz";
            signsForPassword += signsForPassword.ToUpper();
            signsForPassword += "1234567890";

            if (string.IsNullOrWhiteSpace(password)) return false;
            if (password.Length < 3) return false;
            if (password.Length > 50) return false;
            if (password.Contains(" ")) return false;

            foreach (char letter in password)
            {
                if (!signsForPassword.Contains(letter))
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsEmailValid(string email)
        {
            if (email.Count(c => c == '@') != 1) return false;
            if (email.Length < 7) return false;
            if (email.Contains(" ")) return false;

            return true;
        }

        public bool IsUsernameValid(string username)
        {
            string signsForUsername = "abcdefghijklmnopqrstuvwxyz";
            signsForUsername += signsForUsername.ToUpper();
            signsForUsername += "1234567890";
            signsForUsername += ".:^*()[]{}@!&$";

            if (string.IsNullOrWhiteSpace(username)) return false;
            if (username.Length < 2) return false;
            if (username.Length > 50) return false;

            foreach (char letter in username)
            {
                if (!signsForUsername.Contains(letter))
                {
                    return false;
                }
            }
            return true;
        }


        public TokenResponceDTO CreateJWT(string userName, string userId, string playerId, string creatorId)
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
            TokenResponceDTO response = new TokenResponceDTO
            {
                access_token = encodedJwt,
                refresh_token = encodedJwtr,
                username = userName,
            };
            return response;
        }
    }
}
