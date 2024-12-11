using Microsoft.EntityFrameworkCore;
using MS_Back_Maps.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace MS_Back_Maps
{
    public class HelpFuncs
    {
        private readonly ProducerService _producerService;
        private readonly MapsContext _context;
        public HelpFuncs(ProducerService producerService, MapsContext mapsContext)
        {
            _producerService = producerService;
            _context = mapsContext;
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

        public async Task LogEventAsync(LogModel logModel)
        {
            string message = JsonSerializer.Serialize(logModel);
            await _producerService.ProduceAsync("LogUpdates", message);
        }

        public async Task<LogModel> LogModelChangeForServerError(LogModel logModel, Exception ex)
        {
            logModel.logLevel = "Error";
            logModel.message = "Server error";
            logModel.details = $"Error: {ex.Message} ||||| Inner error: {ex.InnerException}";
            logModel.errorCode = "400";
            await LogEventAsync(logModel);
            return logModel;
        }

        public async Task UserIdCheckEventAsync(UserIdCheckModel userIdCheckModel)
        {
            var message = JsonSerializer.Serialize(userIdCheckModel);
            await _producerService.ProduceAsync("UserIdCheckRequest", message);
        }

        public async Task<LogModel> UserIdCheck(string requestId, int parsedUserId, LogModel logModel)
        {
            UserIdCheckModel requestMessage = new UserIdCheckModel
            {
                requestId = requestId,
                userId = parsedUserId,
                isValid = false
            };
            UserIdCheckEventAsync(requestMessage);
            var response = await _producerService.WaitForKafkaResponseAsync(requestId, "UserIdCheckResponce", TimeSpan.FromSeconds(10));
            if (response == null)
            {
                logModel.logLevel = "Error";
                logModel.message = "User does not exist";
                logModel.errorCode = "404";
                await LogEventAsync(logModel);
            }
            return logModel;
        }

        public LogModel LogModelCreate(string eventType, string message)
        {
            return new LogModel
            {
                userId = -1,
                dateTime = DateTime.UtcNow,
                serviceName = "MapsInUsersController",
                logLevel = "Info",
                eventType = eventType,
                message = message,
                details = "",
                errorCode = "200"
            };
        }
    }
}
