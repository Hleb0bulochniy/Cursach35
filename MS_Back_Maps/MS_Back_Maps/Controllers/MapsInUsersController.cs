using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MS_Back_Maps.Models;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Cryptography;
using MS_Back_Maps.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MS_Back_Maps.Controllers
{
    [ApiController]
    public class MapsInUsersController : ControllerBase
    {
        private readonly HelpFuncs _helpfuncs;
        private readonly MapsContext _context;
        private readonly ProducerService _producerService;
        public MapsInUsersController(HelpFuncs helpfuncs, ProducerService producerService, MapsContext mapsContext)
        {
            _helpfuncs = helpfuncs;
            _producerService = producerService;
            _context = mapsContext;
        }


        /// <summary>
        /// Save progress of user on the map in db.
        /// </summary>
        /// <remarks>If there is no data aobut saves in db, this method creates it</remarks>
        /// <response code="200">Progress was saved. Returns message about completion</response>
        /// <response code="400">User ID (from token) conversion in int failed, received data is null, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found. Returns message about error</response>
        [Route("Progress")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ProgressPost([FromBody] MapSaveModel? mapSaveModel)
        {
            LogModel logModel = LogModelCreate("ProgressPost", "Progress was pasted");
            try
            {
                if (mapSaveModel == null)
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

                string requestId = Guid.NewGuid().ToString();
                logModel = await UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

                MapsInUser? mapsInUser = _context.MapsInUsers.FirstOrDefault(map => (map.MapId == mapSaveModel.mapId) && (map.UserId == parsedUserId));

                if (mapsInUser == null)
                {
                    logModel.message = "Progress was added";
                    MapsInUser mapsInUserInput = new MapsInUser
                    {
                        MapId = mapSaveModel.mapId,
                        UserId = parsedUserId,
                        GamesSum = mapSaveModel.gamesSum,
                        Wins = mapSaveModel.wins,
                        Loses = mapSaveModel.loses,
                        OpenedTiles = mapSaveModel.openedTiles,
                        OpenedNumberTiles = mapSaveModel.openedNumberTiles,
                        OpenedBlankTiles = mapSaveModel.openedBlankTiles,
                        FlagsSum = mapSaveModel.flagsSum,
                        FlagsOnBombs = mapSaveModel.flagsOnBombs,
                        TimeSpentSum = mapSaveModel.timeSpentSum,
                        AverageTime = mapSaveModel.timeSpentSum > 0 ? mapSaveModel.wins / mapSaveModel.timeSpentSum : 0,
                        LastGameData = mapSaveModel.lastGameData,
                        LastGameTime = mapSaveModel.lastGameTime
                    };
                    await _context.MapsInUsers.AddAsync(mapsInUserInput);
                }
                else
                {
                    mapsInUser.MapId = mapSaveModel.mapId;
                    mapsInUser.GamesSum = mapSaveModel.gamesSum;
                    mapsInUser.Wins = mapSaveModel.wins;
                    mapsInUser.Loses = mapSaveModel.loses;
                    mapsInUser.OpenedTiles = mapSaveModel.openedTiles;
                    mapsInUser.OpenedNumberTiles = mapSaveModel.openedNumberTiles;
                    mapsInUser.OpenedBlankTiles = mapSaveModel.openedBlankTiles;
                    mapsInUser.FlagsSum = mapSaveModel.flagsSum;
                    mapsInUser.FlagsOnBombs = mapSaveModel.flagsOnBombs;
                    mapsInUser.TimeSpentSum = mapSaveModel.timeSpentSum;
                    mapsInUser.AverageTime = mapSaveModel.timeSpentSum > 0 ? mapSaveModel.wins / mapSaveModel.timeSpentSum : 0;
                    mapsInUser.LastGameData = mapSaveModel.lastGameData;
                    mapsInUser.LastGameTime = mapSaveModel.lastGameTime;
                }

                await _context.SaveChangesAsync();
                await LogEventAsync(logModel);
                return Ok(logModel.message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
                LogModel updatedLogModel = await LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }


        /// <summary>
        /// Get progress of user on the map.
        /// </summary>
        /// <response code="200">Progress was sent. Returns json with progress</response>
        /// <response code="400">User ID (from token) conversion in int failed, received data is wrong, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found, map wasn't found, user save on this map wasn't found. Returns message about error</response>
        [Route("Progress/{idModel:int}")]
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ProgressGet(int? idModel)
        {
            LogModel logModel = LogModelCreate("ProgressGet", "Progress was gotten");
            try
            {
                if (idModel <= 0 || idModel == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Received data is wrong";
                    logModel.errorCode = "400";
                    await LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }
                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

                Map? map = _context.Maps.FirstOrDefault(map => ((map.Id == idModel)));
                var (success2, result2) = await MapNullCheck(map, logModel);
                if (!success2) return result2!;

                MapsInUser? mapsInUser = _context.MapsInUsers.FirstOrDefault(map => (map.MapId == idModel) && (map.UserId == parsedUserId));
                var (success3, result3) = await MapsInUserNullCheck(mapsInUser, logModel);
                if (!success3) return result3!;

                MapSaveModel mapSaveModel = new MapSaveModel
                {
                    id = mapsInUser.Id,
                    mapId = mapsInUser.MapId,
                    mapName = map.MapName,
                    gamesSum = mapsInUser.GamesSum,
                    wins = mapsInUser.Wins,
                    loses = mapsInUser.Loses,
                    openedTiles = mapsInUser.OpenedTiles,
                    openedNumberTiles = mapsInUser.OpenedNumberTiles,
                    openedBlankTiles = mapsInUser.OpenedBlankTiles,
                    flagsSum = mapsInUser.FlagsSum,
                    flagsOnBombs = mapsInUser.FlagsOnBombs,
                    averageTime = mapsInUser.AverageTime,
                    lastGameData = mapsInUser.LastGameData,
                    lastGameTime = mapsInUser.LastGameTime,
                };

                await LogEventAsync(logModel);
                return Ok(mapSaveModel);
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }


        /// <summary>
        /// Save progress of user on several maps in db.
        /// </summary>
        /// <remarks>If there is no data aobut saves in db, this method creates it</remarks>
        /// <response code="200">Progress was saved. Returns message about completion</response>
        /// <response code="400">User ID (from token) conversion in int failed, received data is wrong, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found. Returns message about error</response>
        [Route("SaveList")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SaveListPost([FromBody] MapSaveListModel mapSaveListModel)
        {
            LogModel logModel = LogModelCreate("SaveListPost", "Save list was posted");
            try
            {
                if (mapSaveListModel.mapSaveList == null || !mapSaveListModel.mapSaveList.Any() || mapSaveListModel == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Received data is wrong";
                    logModel.errorCode = "400";
                    await LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }

                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

                var existingMaps = _context.MapsInUsers.Where(map => map.UserId == parsedUserId && mapSaveListModel.mapSaveList.Select(m => m.mapId).Contains(map.MapId)).ToDictionary(map => map.MapId);
                List<MapsInUser> mapsToAdd = new List<MapsInUser>();
                foreach ( MapSaveModel mapSaveModel in mapSaveListModel.mapSaveList )
                {
                    if (!existingMaps.TryGetValue(mapSaveModel.mapId, out var mapsInUser))
                    {
                        mapsToAdd.Add(new MapsInUser
                        {
                            MapId = mapSaveModel.mapId,
                            UserId = logModel.userId = parsedUserId,
                            GamesSum = mapSaveModel.gamesSum,
                            Wins = mapSaveModel.wins,
                            Loses = mapSaveModel.loses,
                            OpenedTiles = mapSaveModel.openedTiles,
                            OpenedNumberTiles = mapSaveModel.openedNumberTiles,
                            OpenedBlankTiles = mapSaveModel.openedBlankTiles,
                            FlagsSum = mapSaveModel.flagsSum,
                            FlagsOnBombs = mapSaveModel.flagsOnBombs,
                            TimeSpentSum = mapSaveModel.timeSpentSum,
                            AverageTime = mapSaveModel.timeSpentSum > 0 ? mapSaveModel.wins / mapSaveModel.timeSpentSum : 0,
                            LastGameData = mapSaveModel.lastGameData,
                            LastGameTime = mapSaveModel.lastGameTime
                        });
                        logModel.details += $"\n!ADD! Custom map id: {mapSaveModel.mapId}, user id: {parsedUserId};";
                    }
                    else
                    {
                        mapsInUser.MapId = mapSaveModel.mapId;
                        mapsInUser.GamesSum = mapSaveModel.gamesSum;
                        mapsInUser.Wins = mapSaveModel.wins;
                        mapsInUser.Loses = mapSaveModel.loses;
                        mapsInUser.OpenedTiles = mapSaveModel.openedTiles;
                        mapsInUser.OpenedNumberTiles = mapSaveModel.openedNumberTiles;
                        mapsInUser.OpenedBlankTiles = mapSaveModel.openedBlankTiles;
                        mapsInUser.FlagsSum = mapSaveModel.flagsSum;
                        mapsInUser.FlagsOnBombs = mapSaveModel.flagsOnBombs;
                        mapsInUser.TimeSpentSum = mapSaveModel.timeSpentSum;
                        mapsInUser.AverageTime = mapSaveModel.timeSpentSum > 0 ? mapSaveModel.wins / mapSaveModel.timeSpentSum : 0;
                        mapsInUser.LastGameData = mapSaveModel.lastGameData;
                        mapsInUser.LastGameTime = mapSaveModel.lastGameTime;
                        logModel.details += $"\n!CHANGE! Custom map id: {mapsInUser.MapId}, user id: {mapsInUser.UserId}, id: {mapsInUser.Id};";
                    }
                }
                if (mapsToAdd.Any())
                {
                    await _context.MapsInUsers.AddRangeAsync(mapsToAdd);
                }

                await _context.SaveChangesAsync();
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
        /// Get progress of user on all maps.
        /// </summary>
        /// <response code="200">Progress was sent. Returns json with progress</response>
        /// <response code="400">User ID (from token) conversion in int failed, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found, user's map saves weren't found. Returns message about error</response>
        [Route("SaveList")]
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SaveListGet()
        {
            LogModel logModel = LogModelCreate("SaveListGet", "Save list gotten");
            try
            {
                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

                List<MapsInUser> maps = _context.MapsInUsers
                                   .Where(map => map.UserId == parsedUserId)
                                   .ToList();
                if (!maps.Any())
                {
                    logModel.logLevel = "Error";
                    logModel.message = "There are no map saves for this user";
                    logModel.errorCode = "404";
                    await LogEventAsync(logModel);
                    return NotFound(logModel.message);
                }

                await LogEventAsync(logModel);
                return Ok(maps);
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }


        private async Task LogEventAsync(LogModel logModel)
        {
            var message = JsonSerializer.Serialize(logModel);
            await _producerService.ProduceAsync("LogUpdates", message);
        }

        private async Task UserIdCheckEventAsync(UserIdCheckModel userIdCheckModel)
        {
            var message = JsonSerializer.Serialize(userIdCheckModel);
            await _producerService.ProduceAsync("UserIdCheckRequest", message);
        }

        private async Task<LogModel> UserIdCheck(string requestId, int parsedUserId, LogModel logModel)
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

        private LogModel LogModelCreate(string eventType, string message)
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
                logModel.errorCode = "400";
                await LogEventAsync(logModel);
                return (false, BadRequest(logModel.message), -1);
            }

            return (true, null, parsedUserId);
        }

        private async Task<(bool Success, IActionResult? Result)> MapNullCheck(Map? map, LogModel logModel)
        {
            if (map == null)
            {
                logModel.logLevel = "Error";
                logModel.message = "The map doesn't exists";
                logModel.errorCode = "404";
                await LogEventAsync(logModel);
                return (false, NotFound(logModel.message));
            }
            return (true, null);
        }

        private async Task<(bool Success, IActionResult? Result)> MapsInUserNullCheck(MapsInUser? mapsInUser, LogModel logModel)
        {
            if (mapsInUser == null)
            {
                logModel.logLevel = "Error";
                logModel.message = "The map:user doesn't exists";
                logModel.errorCode = "404";
                await LogEventAsync(logModel);
                return (false, NotFound(logModel.message));
            }
            return (true, null);
        }

        private async Task<LogModel> LogModelChangeForServerError(LogModel logModel, Exception ex)
        {
            logModel.logLevel = "Error";
            logModel.message = "Server error";
            logModel.details = $"Error: {ex.Message} ||||| Inner error: {ex.InnerException}";
            logModel.errorCode = "400";
            await LogEventAsync(logModel);
            return logModel;
        }
    }
}
