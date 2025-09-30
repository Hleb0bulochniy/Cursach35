using Microsoft.AspNetCore.Mvc;
using MS_Back_Maps.Data;
using MS_Back_Maps.Models;
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
        public (string?, string?, string?) GetUserIdFromToken(HttpRequest request)
        {
            string? authorizationHeader = request.Headers["Authorization"];
            if (authorizationHeader == null) return (null, null, null);

            string token = authorizationHeader.Replace("Bearer ", "");
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token)) return (null, null, null);

            var jwtToken = handler.ReadJwtToken(token);
            string? userId;
            string? playerId;
            string? creatorId;
            userId = jwtToken.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
            playerId = jwtToken.Claims.FirstOrDefault(claim => claim.Type == "PlayerIdentifier")?.Value;
            creatorId = jwtToken.Claims.FirstOrDefault(claim => claim.Type == "CreatorIdentifier")?.Value;
            return (userId, playerId, creatorId);
        }

        public async Task<(LogModel, int, int?, int?)> ValidateAndParseUserIdAsync(HttpRequest request, LogModel logModel)
        {
            string? userId;
            string? playerId;
            string? creatorId;
            (userId, playerId, creatorId) = GetUserIdFromToken(request);
            if (string.IsNullOrEmpty(userId))
            {
                logModel.LogLevel = "Error";
                logModel.Message = "Invalid or missing token";
                logModel.ErrorCode = "401";
                await LogEventAsync(logModel);
                return (logModel, -1, null, null);
            }

            if (!int.TryParse(userId, out int parsedUserId))
            {
                logModel.LogLevel = "Error";
                logModel.Message = "User ID conversion in int failed";
                logModel.ErrorCode = "400";
                await LogEventAsync(logModel);
                return (logModel, -1, null, null);
            }
            int.TryParse(playerId, out int parsedPlayerId);
            int.TryParse(creatorId, out int parsedCreatorId);

            return (logModel, parsedUserId, parsedPlayerId, parsedCreatorId);
        }

        public async Task LogEventAsync(LogModel logModel)
        {
            string message = JsonSerializer.Serialize(logModel);
            await _producerService.ProduceAsync("LogUpdates", message);
        }
        public async Task UserIdCheckEventAsync(UserIdCheckModel userIdCheckModel)
        {
            var message = JsonSerializer.Serialize(userIdCheckModel);
            await _producerService.ProduceAsync("UserIdCheckRequest", message);
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


        public async Task<(UserIdCheckModel, LogModel)> UserIdCheck(string requestId, string requestMessage, LogModel logModel, int? parsedUserId, int? parsedPlayerId, int? parserCreatorId)
        {
            UserIdCheckModel requestToKafka = new UserIdCheckModel
            {
                requestId = requestId,
                requestMessage = requestMessage,
                userId = parsedUserId,
                playerId = parsedPlayerId,
                creatorId = parserCreatorId,
                isValid = false
            };
            UserIdCheckEventAsync(requestToKafka);
            var response = await _producerService.WaitForKafkaResponseAsync(requestId, "UserIdCheckResponce", TimeSpan.FromSeconds(10));
            if (response == null)
            {
                logModel.LogLevel = "Error";
                logModel.Message = "User does not exist";
                logModel.ErrorCode = "404";
                await LogEventAsync(logModel);
            }
            return (response,logModel);
        }

        public LogModel LogModelCreate(string eventType, string message, string serviceName)
        {
            return new LogModel
            {
                UserId = -1,
                DateTime = DateTime.UtcNow,
                ServiceName = serviceName,
                LogLevel = "Info",
                EventType = eventType,
                Message = message,
                Details = "",
                ErrorCode = "200"
            };
        }

        public async Task<ResponseDTO> LogModelErrorInputAndLog(LogModel logModel, string message, string errorCode)
        {
            logModel.LogLevel = "Error";
            logModel.Message = message;
            logModel.ErrorCode = errorCode;
            await LogEventAsync(logModel);
            return new ResponseDTO(message);
        }
    }
}
