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
        [Route("Progress")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ProgressPost([FromBody] MapSaveModel mapSaveModel)
        {
            LogModel logModel = new LogModel
            {
                userId = -1,
                dateTime = DateTime.UtcNow,
                serviceName = "MapsInUsersController",
                logLevel = "Info",
                eventType = "ProgressPost",
                message = "Progress was pasted",
                details = "",
                errorCode = "200"
            };
            try
            {
                string? userId = _helpfuncs.GetUserIdFromToken(Request);
                if (string.IsNullOrEmpty(userId))
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Invalid or missing token";
                    logModel.errorCode = "401";
                    await LogEventAsync(logModel);
                    return Unauthorized(logModel.message);
                }
                if (!int.TryParse(userId, out int parsedUserId))
                {
                    logModel.logLevel = "Error";
                    logModel.message = "User ID conversion in int failed";
                    logModel.errorCode = "500";
                    await LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }
                logModel.userId = parsedUserId;
                //Проверить, существует ли такой пользователь в Auth и залогировать
                if (mapSaveModel == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "MapSaveModel is empty";
                    logModel.errorCode = "404";
                    await LogEventAsync(logModel);
                    return NotFound(logModel.message);
                }
                MapsInUser? mapsInUser = _context.MapsInUsers.FirstOrDefault(map => (map.Id == mapSaveModel.id) && (map.UserId == parsedUserId));

                if (mapsInUser == null)
                {
                    logModel.message = "Progress was added";
                    MapsInUser mapsInUserInput = new MapsInUser
                    {
                        MapId = mapSaveModel.mapId,
                        UserId = int.Parse(userId),
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
                logModel.eventType = "Error";
                logModel.message = "Server error";
                logModel.details = ex.Message;
                logModel.errorCode = "500";
                await LogEventAsync(logModel);
                return BadRequest(logModel.message);
            }
        }

        [Route("Progress/{idModel:int}")]
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ProgressGet(int idModel)
        {
            LogModel logModel = new LogModel
            {
                userId = -1,
                dateTime = DateTime.UtcNow,
                serviceName = "MapsInUsersController",
                logLevel = "Info",
                eventType = "ProgressGet",
                message = "Progress was gotten",
                details = "",
                errorCode = "200"
            };
            try
            {
                string? userId = _helpfuncs.GetUserIdFromToken(Request);
                if (string.IsNullOrEmpty(userId))
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Invalid or missing token";
                    logModel.errorCode = "401";
                    await LogEventAsync(logModel);
                    return Unauthorized(logModel.message);
                }
                if (!int.TryParse(userId, out int parsedUserId))
                {
                    logModel.logLevel = "Error";
                    logModel.message = "User ID conversion in int failed";
                    logModel.errorCode = "500";
                    await LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }
                logModel.userId = parsedUserId;
                //Проверить, существует ли такой пользователь в Auth и залогировать


                Map map = _context.Maps.FirstOrDefault(map => (map.Id == idModel));
                if (map == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "The map doesn't exists";
                    logModel.errorCode = "404";
                    await LogEventAsync(logModel);
                    return NotFound(logModel.message);
                }

                MapsInUser? mapsInUser = _context.MapsInUsers.FirstOrDefault(map => (map.Id == idModel) && (map.UserId == parsedUserId));
                if (mapsInUser == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "The map:user doesn't exists";
                    logModel.errorCode = "404";
                    await LogEventAsync(logModel);
                    return NotFound(logModel.message);
                }

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
                logModel.eventType = "Error";
                logModel.message = "Server error";
                logModel.details = ex.Message;
                logModel.errorCode = "500";
                await LogEventAsync(logModel);
                return BadRequest(logModel.message);
            }
        }

        [Route("SaveList")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SaveListPost([FromBody] MapSaveListModel mapSaveListModel)
        {
            LogModel logModel = new LogModel
            {
                userId = -1,
                dateTime = DateTime.UtcNow,
                serviceName = "MapsInUsersController",
                logLevel = "Info",
                eventType = "SaveListPost",
                message = "Save list was posted",
                details = "",
                errorCode = "200"
            };
            try
            {
                string? userId = _helpfuncs.GetUserIdFromToken(Request);
                if (string.IsNullOrEmpty(userId))
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Invalid or missing token";
                    logModel.errorCode = "401";
                    await LogEventAsync(logModel);
                    return Unauthorized(logModel.message);
                }
                if (!int.TryParse(userId, out int parsedUserId))
                {
                    logModel.logLevel = "Error";
                    logModel.message = "User ID conversion in int failed";
                    logModel.errorCode = "500";
                    await LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }
                logModel.userId = parsedUserId;
                //Проверить, существует ли такой пользователь в Auth и залогировать

                if (mapSaveListModel.mapSaveList == null || !mapSaveListModel.mapSaveList.Any())
                {
                    logModel.logLevel = "Error";
                    logModel.message = "The list in body is empty";
                    logModel.errorCode = "404";
                    await LogEventAsync(logModel);
                    return NotFound(logModel.message);
                }

                foreach ( MapSaveModel mapSaveModel in mapSaveListModel.mapSaveList ) //извлекать данные в списке с помощью linq
                {
                MapsInUser? mapsInUser = _context.MapsInUsers.FirstOrDefault(map => (map.Id == mapSaveModel.id) && (map.UserId.ToString() == userId));

                    if (mapsInUser == null) //переделать логику логирования, чтоб для каждой карты был свой лог
                    {
                        logModel.message = "Save list was added";
                        MapsInUser mapsInUserInput = new MapsInUser
                        {
                            MapId = mapSaveModel.mapId,
                            UserId = int.Parse(userId),
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
                }

                await _context.SaveChangesAsync();
                await LogEventAsync(logModel);
                return Ok(logModel.message);
            }
            catch (Exception ex)
            {
                logModel.eventType = "Error";
                logModel.message = "Server error";
                logModel.details = ex.Message;
                logModel.errorCode = "500";
                await LogEventAsync(logModel);
                return BadRequest(logModel.message);
            }
        }

        [Route("SaveList")]
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SaveListGet()
        {
            LogModel logModel = new LogModel
            {
                userId = -1,
                dateTime = DateTime.UtcNow,
                serviceName = "MapsInUsersController",
                logLevel = "Info",
                eventType = "SaveListGet",
                message = "Save list gotten",
                details = "",
                errorCode = "200"
            };
            try
            {
                string? userId = _helpfuncs.GetUserIdFromToken(Request);
                if (string.IsNullOrEmpty(userId))
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Invalid or missing token";
                    logModel.errorCode = "401";
                    await LogEventAsync(logModel);
                    return Unauthorized(logModel.message);
                }
                if (!int.TryParse(userId, out int parsedUserId))
                {
                    logModel.logLevel = "Error";
                    logModel.message = "User ID conversion in int failed";
                    logModel.errorCode = "500";
                    await LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }
                logModel.userId = parsedUserId;
                //Проверить, существует ли такой пользователь в Auth и залогировать

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
                logModel.eventType = "Error";
                logModel.message = "Server error";
                logModel.details = ex.Message;
                logModel.errorCode = "500";
                await LogEventAsync(logModel);
                return BadRequest(logModel.message);
            }
        }


        private async Task LogEventAsync(LogModel logModel)
        {
            var message = JsonSerializer.Serialize(logModel);
            await _producerService.ProduceAsync("LogUpdates", message);
        }
    }
}
