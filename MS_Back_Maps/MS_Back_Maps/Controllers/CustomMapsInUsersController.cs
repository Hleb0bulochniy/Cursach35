using Azure.Core;
using Confluent.Kafka;
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
            LogModel logModel = LogModelCreate("ProgressCustomPost", "Progress was pasted");
            try
            {
                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "400") return BadRequest(logModel.message);

                if (mapSaveModel == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "MapSaveModel is empty";
                    logModel.errorCode = "404";
                    await LogEventAsync(logModel);
                    return NotFound(logModel.message);
                }
                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(map => (map.CustomMapId == mapSaveModel.mapId) && (map.UserId == parsedUserId));

                if (customMapsInUser == null)
                {
                    logModel.message = "Custom progress was added";
                    CustomMapsInUser customMapsInUserInput = new CustomMapsInUser
                    {
                        CustomMapId = mapSaveModel.mapId,
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
                LogModel updatedLogModel = await LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }

        [Route("ProgressCustom/{idModel:int}")]
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ProgressCustomGet(int idModel)
        {
            LogModel logModel = LogModelCreate("ProgressCustomGet", "Progress was gotten");
            try
            {
                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "400") return BadRequest(logModel.message);

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => ((cmap.Id == idModel)));
                var (success2, result2) = await CustomMapNullCheck(customMap, logModel);
                if (!success2) return result2!;

                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(cmap => (cmap.CustomMapId == idModel) && (cmap.UserId == parsedUserId));
                var (success3, result3) = await CustomMapsInUserNullCheck(customMapsInUser, logModel);
                if (!success3) return result3!;

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
                LogModel updatedLogModel = await LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }

        [Route("SaveListCustom")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SaveListCustomPost([FromBody] MapSaveListModel mapSaveListModel)
        {
            LogModel logModel = LogModelCreate("SaveListCustomPost", "Custom save list posted");
            try
            {
                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "400") return BadRequest(logModel.message);

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
                    CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(map => (map.CustomMapId == mapSaveModel.mapId) && (map.UserId == parsedUserId));

                    if (customMapsInUser == null) //переделать логику логирования, чтоб для каждой карты был свой лог
                    {
                        logModel.message = "Custom save list was added";
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
                LogModel updatedLogModel = await LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }

        [Route("SaveListCustom")]
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SaveMapsListCustomGet()
        {
            LogModel logModel = LogModelCreate("SaveMapsListCustomGet", "Custom save list gotten");
            try
            {
                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "400") return BadRequest(logModel.message);

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
                LogModel updatedLogModel = await LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }

        [Route("Rate")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RatePost([FromBody] RateMap rateMap)
        {
            LogModel logModel = LogModelCreate("RatePost", "Rate pasted");
            try
            {
                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "400") return BadRequest(logModel.message);

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => cmap.Id == rateMap.mapId);
                var (success2, result2) = await CustomMapNullCheck(customMap, logModel);
                if (!success2) return result2!;

                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(cmap => (cmap.CustomMapId == rateMap.mapId) && (cmap.UserId == parsedUserId));
                var (success3, result3) = await CustomMapsInUserNullCheck(customMapsInUser, logModel);
                if (!success3) return result3!;

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
                LogModel updatedLogModel = await LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }

        [Route("Rate")]
        [Authorize]
        [HttpPut]
        public async Task<IActionResult> RatePut([FromBody] RateMap rateMap)
        {
            LogModel logModel = LogModelCreate("RatePut", "New rate putted");
            try
            {
                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "400") return BadRequest(logModel.message);

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => cmap.Id == rateMap.mapId);
                var (success2, result2) = await CustomMapNullCheck(customMap, logModel);
                if (!success2) return result2!;

                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(cmap => (cmap.CustomMapId == rateMap.mapId) && (cmap.UserId == parsedUserId));
                var (success3, result3) = await CustomMapsInUserNullCheck(customMapsInUser, logModel);
                if (!success3) return result3!;

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
                LogModel updatedLogModel = await LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }

        [Route("Rate/{idModel:int}")]
        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> RateDelete(int idModel)
        {
            LogModel logModel = LogModelCreate("RateDelete", "Rate deleted");
            try
            {
                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "400") return BadRequest(logModel.message);

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => cmap.Id == idModel);
                var (success2, result2) = await CustomMapNullCheck(customMap, logModel);
                if (!success2) return result2!;

                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(cmap => (cmap.CustomMapId == idModel) && (cmap.UserId == parsedUserId));
                var (success3, result3) = await CustomMapsInUserNullCheck(customMapsInUser, logModel);
                if (!success3) return result3!;

                if (customMapsInUser.Rate == 0)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "The old rate doesn't exist";
                    logModel.errorCode = "400";
                    await LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }

                customMap.RatingSum -= customMapsInUser.Rate;
                customMap.RatingCount -= 1;
                customMapsInUser.Rate = 0;

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

        [Route("Download")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DownLoadPost([FromBody] IdModel idmodel) //можно заменить на переменную в запросе как у put или delete
        {
            LogModel logModel = LogModelCreate("DownLoadPost", "Map download was posted");
            try
            {
                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "400") return BadRequest(logModel.message);

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => ((cmap.Id == idmodel.id)));
                var (success2, result2) = await CustomMapNullCheck(customMap, logModel);
                if (!success2) return result2!;

                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(cmap => (cmap.CustomMapId == idmodel.id) && (cmap.UserId == parsedUserId));
                if (customMapsInUser != null)
                {
                    if (customMapsInUser.IsAdded == false)
                    {
                        customMap.Downloads += 1;
                        customMapsInUser.IsAdded = true;
                    }//сделать обработчик с ошибкой если карта уже добавлена
                    //тут шляпа какая-то
                    //return NotFound(logModel.message);
                }
                else
                {
                    CustomMapsInUser customMapsInUserNew = new CustomMapsInUser
                    {
                        UserId = parsedUserId,
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
                LogModel updatedLogModel = await LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }

        [Route("Download/{idModel:int}")]
        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> DownLoadDelete(int idModel)
        {
            LogModel logModel = LogModelCreate("DownLoadDelete", "Map download was deleted");
            try
            {
                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "400") return BadRequest(logModel.message);

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => cmap.Id == idModel);
                var (success2, result2) = await CustomMapNullCheck(customMap, logModel);
                if (!success2) return result2!;

                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(cmap => (cmap.CustomMapId == idModel) && (cmap.UserId == parsedUserId));
                var (success3, result3) = await CustomMapsInUserNullCheck(customMapsInUser, logModel);
                if (!success3) return result3!;


                customMap.Downloads -= 1;
                customMapsInUser.IsAdded = false;

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



        [Route("Favourite")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> FavouritePost([FromBody] IdModel idmodel) //можно поменять так же как у downloadpost
        {
            LogModel logModel = LogModelCreate("FavouritePost", "Map favourite mark was posted");
            try
            {
                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "400") return BadRequest(logModel.message);

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => ((cmap.Id == idmodel.id)));
                var (success2, result2) = await CustomMapNullCheck(customMap, logModel);
                if (!success2) return result2!;

                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(cmap => (cmap.CustomMapId == idmodel.id) && (cmap.UserId == parsedUserId));
                var (success3, result3) = await CustomMapsInUserNullCheck(customMapsInUser, logModel);
                if (!success3) return result3!;


                customMapsInUser.IsFavourite = true;

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

        [Route("Favourite")]
        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> FavouriteDelete([FromBody] IdModel idmodel)
        {
            LogModel logModel = LogModelCreate("FavouriteDelete", "Map favourite mark was deleted");
            try
            {
                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "400") return BadRequest(logModel.message);

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => ((cmap.Id == idmodel.id)));
                var (success2, result2) = await CustomMapNullCheck(customMap, logModel);
                if (!success2) return result2!;

                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(cmap => (cmap.CustomMapId == idmodel.id) && (cmap.UserId == parsedUserId));
                var (success3, result3) = await CustomMapsInUserNullCheck(customMapsInUser, logModel);
                if (!success3) return result3!;


                customMapsInUser.IsFavourite = false;

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
                logModel.errorCode = "400";
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
                serviceName = "CustomMapsInUsersController",
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

        private async Task<(bool Success, IActionResult? Result)> CustomMapNullCheck(CustomMap? customMap, LogModel logModel)
        {
            if (customMap == null)
            {
                logModel.logLevel = "Error";
                logModel.message = "The custom map doesn't exists";
                logModel.errorCode = "404";
                await LogEventAsync(logModel);
                return (false, NotFound(logModel.message));
            }
            return (true, null);
        }

        private async Task<(bool Success, IActionResult? Result)> CustomMapsInUserNullCheck(CustomMapsInUser? customMapsInUser, LogModel logModel)
        {
            if (customMapsInUser == null)
            {
                logModel.logLevel = "Error";
                logModel.message = "The customMap:user doesn't exists";
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
            logModel.errorCode = "500";
            await LogEventAsync(logModel);
            return logModel;
        }
    }
}