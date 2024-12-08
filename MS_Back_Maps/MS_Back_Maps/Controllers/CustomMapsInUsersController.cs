using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MS_Back_Maps.Data;
using MS_Back_Maps.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace MS_Back_Maps.Controllers
{
    [ApiController]
    public class CustomMapsInUsersController : ControllerBase
    {
        private readonly HelpFuncs _helpfuncs;
        private readonly MapsContext _context;
        private readonly ProducerService _producerService;
        public CustomMapsInUsersController(HelpFuncs helpfuncs, ProducerService producerService, MapsContext mapsContext)
        {
            _helpfuncs = helpfuncs;
            _producerService = producerService;
            _context = mapsContext;
        }
        [Route("ProgressCustom")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ProgressCustomPost([FromBody] MapSaveModel mapSaveModel)
        {
            LogModel logModel = new LogModel
            {
                userId = -1,
                dateTime = DateTime.UtcNow,
                serviceName = "CustomMapsInUsersController",
                logLevel = "Info",
                eventType = "ProgressCustomPost",
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
                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(map => (map.Id == mapSaveModel.id) && (map.UserId == parsedUserId));

                if (customMapsInUser == null)
                {
                    logModel.message = "Custom progress was added";
                    CustomMapsInUser customMapsInUserInput = new CustomMapsInUser
                    {
                        CustomMapId = mapSaveModel.mapId,
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
                    await _context.CustomMapsInUsers.AddAsync(customMapsInUserInput);
                }
                else
                {
                    customMapsInUser.CustomMapId = mapSaveModel.mapId;
                    customMapsInUser.GamesSum = mapSaveModel.gamesSum;
                    customMapsInUser.Wins = mapSaveModel.wins;
                    customMapsInUser.Loses = mapSaveModel.loses;
                    customMapsInUser.OpenedTiles = mapSaveModel.openedTiles;
                    customMapsInUser.OpenedNumberTiles = mapSaveModel.openedNumberTiles;
                    customMapsInUser.OpenedBlankTiles = mapSaveModel.openedBlankTiles;
                    customMapsInUser.FlagsSum = mapSaveModel.flagsSum;
                    customMapsInUser.FlagsOnBombs = mapSaveModel.flagsOnBombs;
                    customMapsInUser.TimeSpentSum = mapSaveModel.timeSpentSum;
                    customMapsInUser.AverageTime = mapSaveModel.timeSpentSum > 0 ? mapSaveModel.wins / mapSaveModel.timeSpentSum : 0;
                    customMapsInUser.LastGameData = mapSaveModel.lastGameData;
                    customMapsInUser.LastGameTime = mapSaveModel.lastGameTime;
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
        public async Task<IActionResult> ProgressCustomGet(int idModel)
        {
            LogModel logModel = new LogModel
            {
                userId = -1,
                dateTime = DateTime.UtcNow,
                serviceName = "CustomMapsInUsersController",
                logLevel = "Info",
                eventType = "ProgressCustomGet",
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

                CustomMap customMap = _context.CustomMaps.FirstOrDefault(cmap => (cmap.Id == idModel));
                if (customMap == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "The custom map doesn't exists";
                    logModel.errorCode = "404";
                    await LogEventAsync(logModel);
                    return NotFound(logModel.message);
                }

                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(cmap => (cmap.Id == idModel) && (cmap.UserId == parsedUserId));
                if (customMapsInUser == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "The customMap:user doesn't exists";
                    logModel.errorCode = "404";
                    await LogEventAsync(logModel);
                    return NotFound(logModel.message);
                }

                MapSaveModel mapSaveModel = new MapSaveModel
                {
                    id = customMapsInUser.Id,
                    mapId = customMapsInUser.CustomMapId,
                    mapName = customMap.MapName,
                    gamesSum = customMapsInUser.GamesSum,
                    wins = customMapsInUser.Wins,
                    loses = customMapsInUser.Loses,
                    openedTiles = customMapsInUser.OpenedTiles,
                    openedNumberTiles = customMapsInUser.OpenedNumberTiles,
                    openedBlankTiles = customMapsInUser.OpenedBlankTiles,
                    flagsSum = customMapsInUser.FlagsSum,
                    flagsOnBombs = customMapsInUser.FlagsOnBombs,
                    averageTime = customMapsInUser.AverageTime,
                    lastGameData = customMapsInUser.LastGameData,
                    lastGameTime = customMapsInUser.LastGameTime,
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

        [Route("SaveListCustom")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SaveListCustomPost([FromBody] MapSaveListModel mapSaveListModel)
        {
            LogModel logModel = new LogModel
            {
                userId = -1,
                dateTime = DateTime.UtcNow,
                serviceName = "CustomMapsInUsersController",
                logLevel = "Info",
                eventType = "SaveListCustomPost",
                message = "Custom save list posted",
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

                foreach (MapSaveModel mapSaveModel in mapSaveListModel.mapSaveList) //извлекать данные в списке с помощью linq
                {
                    CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(map => (map.Id == mapSaveModel.id) && (map.UserId == parsedUserId));

                    if (customMapsInUser == null) //переделать логику логирования, чтоб для каждой карты был свой лог
                    {
                        logModel.message = "Custom save list was added";
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
                        customMapsInUser.CustomMapId = mapSaveModel.mapId;
                        customMapsInUser.GamesSum = mapSaveModel.gamesSum;
                        customMapsInUser.Wins = mapSaveModel.wins;
                        customMapsInUser.Loses = mapSaveModel.loses;
                        customMapsInUser.OpenedTiles = mapSaveModel.openedTiles;
                        customMapsInUser.OpenedNumberTiles = mapSaveModel.openedNumberTiles;
                        customMapsInUser.OpenedBlankTiles = mapSaveModel.openedBlankTiles;
                        customMapsInUser.FlagsSum = mapSaveModel.flagsSum;
                        customMapsInUser.FlagsOnBombs = mapSaveModel.flagsOnBombs;
                        customMapsInUser.TimeSpentSum = mapSaveModel.timeSpentSum;
                        customMapsInUser.AverageTime = mapSaveModel.timeSpentSum > 0 ? mapSaveModel.wins / mapSaveModel.timeSpentSum : 0;
                        customMapsInUser.LastGameData = mapSaveModel.lastGameData;
                        customMapsInUser.LastGameTime = mapSaveModel.lastGameTime;
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

        [Route("SaveListCustom")]
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SaveMapsListCustomGet()
        {
            LogModel logModel = new LogModel
            {
                userId = -1,
                dateTime = DateTime.UtcNow,
                serviceName = "CustomMapsInUsersController",
                logLevel = "Info",
                eventType = "SaveMapsListCustomGet",
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

                List<CustomMapsInUser> maps = _context.CustomMapsInUsers
                                   .Where(map => map.UserId == parsedUserId)
                                   .ToList();
                if (!maps.Any())
                {
                    logModel.logLevel = "Error";
                    logModel.message = "There are no custom map saves for this user";
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

        [Route("Rate")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RatePost([FromBody] RateMap rateMap)
        {
            LogModel logModel = new LogModel
            {
                userId = -1,
                dateTime = DateTime.UtcNow,
                serviceName = "CustomMapsInUsersController",
                logLevel = "Info",
                eventType = "RatePost",
                message = "Rate pasted",
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

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => ((cmap.Id == rateMap.mapId)));
                if (customMap == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "The custom map doesn't exists";
                    logModel.errorCode = "404";
                    await LogEventAsync(logModel);
                    return NotFound(logModel.message);
                }

                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(cmap => (cmap.Id == rateMap.mapId) && (cmap.UserId == parsedUserId));
                if (customMapsInUser == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "The customMap:user doesn't exists";
                    logModel.errorCode = "404";
                    await LogEventAsync(logModel);
                    return NotFound(logModel.message);
                }

                if (customMapsInUser.Rate != 0)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "The rate already exists";
                    logModel.errorCode = "400";
                    await LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }

                customMapsInUser.Rate = rateMap.newRate;
                customMap.RatingSum += rateMap.newRate;
                customMap.RatingCount += 1;

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

        [Route("Rate")]
        [Authorize]
        [HttpPut]
        public async Task<IActionResult> RatePut([FromBody] RateMap rateMap)
        {
            LogModel logModel = new LogModel
            {
                userId = -1,
                dateTime = DateTime.UtcNow,
                serviceName = "CustomMapsInUsersController",
                logLevel = "Info",
                eventType = "RatePut",
                message = "New rate putted",
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

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => ((cmap.Id == rateMap.mapId)));
                if (customMap == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "The custom map doesn't exists";
                    logModel.errorCode = "404";
                    await LogEventAsync(logModel);
                    return NotFound(logModel.message);
                }

                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(cmap => (cmap.Id == rateMap.mapId) && (cmap.UserId == parsedUserId));
                if (customMapsInUser == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "The customMap:user doesn't exists";
                    logModel.errorCode = "404";
                    await LogEventAsync(logModel);
                    return NotFound(logModel.message);
                }

                if (customMapsInUser.Rate == 0)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "The old rate doesn't exist";
                    logModel.errorCode = "400";
                    await LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }

                customMap.RatingSum -= customMapsInUser.Rate;
                customMapsInUser.Rate = rateMap.newRate;
                customMap.RatingSum += rateMap.newRate;

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

        [Route("Rate/{idModel:int}")]
        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> RateDelete(int idModel)
        {
            LogModel logModel = new LogModel
            {
                userId = -1,
                dateTime = DateTime.UtcNow,
                serviceName = "CustomMapsInUsersController",
                logLevel = "Info",
                eventType = "RateDelete",
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

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => ((cmap.Id == idModel)));
                if (customMap == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "The custom map doesn't exists";
                    logModel.errorCode = "404";
                    await LogEventAsync(logModel);
                    return NotFound(logModel.message);
                }

                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(cmap => (cmap.Id == idModel) && (cmap.UserId == parsedUserId));
                if (customMapsInUser == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "The customMap:user doesn't exists";
                    logModel.errorCode = "404";
                    await LogEventAsync(logModel);
                    return NotFound(logModel.message);
                }

                if (customMapsInUser.Rate == 0)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "The old rate doesn't exist";
                    logModel.errorCode = "400";
                    await LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }

                customMap.RatingSum -= customMapsInUser.Rate;
                customMapsInUser.Rate = 0;

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

        [Route("Download")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DownLoadPost([FromBody] IdModel idmodel) //можно заменить на переменную в запросе как у put или delete
        {
            LogModel logModel = new LogModel
            {
                userId = -1,
                dateTime = DateTime.UtcNow,
                serviceName = "CustomMapsInUsersController",
                logLevel = "Info",
                eventType = "DownLoadPost",
                message = "Map download was posted",
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

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => ((cmap.Id == idmodel.id)));
                if (customMap == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "The custom map doesn't exists";
                    logModel.errorCode = "404";
                    await LogEventAsync(logModel);
                    return NotFound(logModel.message);
                }

                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(cmap => (cmap.Id == idmodel.id) && (cmap.UserId == parsedUserId));
                if (customMapsInUser != null)
                {
                    //тут шляпа какая-то
                    customMap.Downloads += 1;
                    //return NotFound(logModel.message);
                }
                else
                {
                    CustomMapsInUser customMapsInUserNew = new CustomMapsInUser
                    {
                        UserId = int.Parse(userId),
                        CustomMapId = customMap.Id,
                        GamesSum = 0,
                        Wins = 0,
                        Loses = 0,
                        OpenedTiles = 0,
                        OpenedNumberTiles = 0,
                        OpenedBlankTiles = 0,
                        FlagsSum = 0,
                        FlagsOnBombs = 0,
                        TimeSpentSum = 0,
                        AverageTime = 0,
                        LastGameData = "",
                        LastGameTime = 0,
                        IsAdded = true,
                        IsFavourite = false,
                        Rate = 0
                    };
                    customMap.Downloads += 1;
                    await _context.CustomMapsInUsers.AddAsync(customMapsInUserNew);
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

        [Route("Download/{idModel:int}")]
        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> DownLoadDelete(int idModel)
        {
            LogModel logModel = new LogModel
            {
                userId = -1,
                dateTime = DateTime.UtcNow,
                serviceName = "CustomMapsInUsersController",
                logLevel = "Info",
                eventType = "HttpDelete",
                message = "Map download was deleted",
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

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => cmap.Id == idModel);
                if (customMap == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "The custom map doesn't exists";
                    logModel.errorCode = "404";
                    await LogEventAsync(logModel);
                    return NotFound(logModel.message);
                }

                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(cmap => (cmap.Id == idModel) && (cmap.UserId == parsedUserId));
                if (customMapsInUser == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "The customMap:user doesn't exists";
                    logModel.errorCode = "404";
                    await LogEventAsync(logModel);
                    return NotFound(logModel.message);
                }


                customMap.Downloads -= 1;
                customMapsInUser.IsAdded = false;

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

        

        [Route("Favourite")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> FavouritePost([FromBody] IdModel idmodel) //можно поменять так же как у downloadpost
        {
            LogModel logModel = new LogModel
            {
                userId = -1,
                dateTime = DateTime.UtcNow,
                serviceName = "CustomMapsInUsersController",
                logLevel = "Info",
                eventType = "FavouritePost",
                message = "Map favourite mark was posted",
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

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => ((cmap.Id == idmodel.id)));
                if (customMap == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "The custom map doesn't exists";
                    logModel.errorCode = "404";
                    await LogEventAsync(logModel);
                    return NotFound(logModel.message);
                }

                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(cmap => (cmap.Id == idmodel.id) && (cmap.UserId == parsedUserId));
                if (customMapsInUser == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "The customMap:user doesn't exists";
                    logModel.errorCode = "404";
                    await LogEventAsync(logModel);
                    return NotFound(logModel.message);
                }


                customMapsInUser.IsFavourite = true;

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

        [Route("Favourite")]
        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> FavouriteDelete([FromBody] IdModel idmodel)
        {
            LogModel logModel = new LogModel
            {
                userId = -1,
                dateTime = DateTime.UtcNow,
                serviceName = "CustomMapsInUsersController",
                logLevel = "Info",
                eventType = "FavouriteDelete",
                message = "Map favourite mark was deleted",
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

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => ((cmap.Id == idmodel.id)));
                if (customMap == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "The custom map doesn't exists";
                    logModel.errorCode = "404";
                    await LogEventAsync(logModel);
                    return NotFound(logModel.message);
                }

                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(cmap => (cmap.Id == idmodel.id) && (cmap.UserId == parsedUserId));
                if (customMapsInUser == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "The customMap:user doesn't exists";
                    logModel.errorCode = "404";
                    await LogEventAsync(logModel);
                    return NotFound(logModel.message);
                }


                customMapsInUser.IsFavourite = false;

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


        private async Task LogEventAsync(LogModel logModel)
        {
            var message = JsonSerializer.Serialize(logModel);
            await _producerService.ProduceAsync("LogUpdates", message);
        }
    }
}
