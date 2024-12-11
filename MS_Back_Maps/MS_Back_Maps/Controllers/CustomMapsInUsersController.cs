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
    [Route("CustomMapsInUsers")]
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


        /// <summary>
        /// Save progress of user on custom the map in db.
        /// </summary>
        /// <remarks>If there is no data aobut saves in db, this method creates it</remarks>
        /// <response code="200">Progress was saved. Returns message about completion</response>
        /// <response code="400">User ID (from token) conversion in int failed, received data is null, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found. Returns message about error</response>
        [Route("ProgressCustom")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ProgressCustomPost([FromBody] MapSaveModel? mapSaveModel)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("ProgressCustomPost", "Progress was pasted");
            try
            {
                if (mapSaveModel == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Received data is null";
                    logModel.errorCode = "400";
                    await _helpfuncs.LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }

                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await _helpfuncs.UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

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
                await _helpfuncs.LogEventAsync(logModel);
                return Ok(logModel.message);
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await _helpfuncs.LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }


        /// <summary>
        /// Get progress of user on custom the map.
        /// </summary>
        /// <response code="200">Progress was sent. Returns json with progress</response>
        /// <response code="400">User ID (from token) conversion in int failed, received data is wrong, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found, map wasn't found, user save on this map wasn't found. Returns message about error</response>
        [Route("ProgressCustom/{idModel:int}")]
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ProgressCustomGet(int? idModel)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("ProgressCustomGet", "Progress was gotten");
            try
            {
                if (idModel <= 0 || idModel == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Received data is wrong";
                    logModel.errorCode = "400";
                    await _helpfuncs.LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }
                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await _helpfuncs.UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

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

                await _helpfuncs.LogEventAsync(logModel);
                return Ok(mapSaveModel);
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await _helpfuncs.LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }


        /// <summary>
        /// Save progress of user on several custom maps in db.
        /// </summary>
        /// <remarks>If there is no data aobut saves in db, this method creates it</remarks>
        /// <response code="200">Progress was saved. Returns message about completion</response>
        /// <response code="400">User ID (from token) conversion in int failed, received data is wrong, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found. Returns message about error</response>
        [Route("SaveListCustom")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SaveListCustomPost([FromBody] MapSaveListModel? mapSaveListModel)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("SaveListCustomPost", "Custom save list posted");
            try
            {
                if (mapSaveListModel.mapSaveList == null || !mapSaveListModel.mapSaveList.Any() || mapSaveListModel == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Received data is wrong";
                    logModel.errorCode = "400";
                    await _helpfuncs.LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }

                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await _helpfuncs.UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

                var existingMaps = _context.CustomMapsInUsers.Where(map => map.UserId == parsedUserId && mapSaveListModel.mapSaveList.Select(m => m.mapId).Contains(map.CustomMapId)).ToDictionary(map => map.CustomMapId);
                List<CustomMapsInUser> mapsToAdd = new List<CustomMapsInUser>();
                foreach (MapSaveModel mapSaveModel in mapSaveListModel.mapSaveList)
                {
                    if (!existingMaps.TryGetValue(mapSaveModel.mapId, out var customMapsInUser))
                    {
                        mapsToAdd.Add(new CustomMapsInUser
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
                        });
                        logModel.details += $"\n!ADD! Custom map id: {mapSaveModel.mapId}, user id: {parsedUserId}; ";
                    }
                    else
                    {
                        customMapsInUser.CustomMapId = mapSaveModel.mapId;
                        customMapsInUser.UserId = parsedUserId;
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
                        logModel.details += $"\n!CHANGE! Custom map id: {customMapsInUser.CustomMapId}, user id: {customMapsInUser.UserId}, id: {customMapsInUser.Id}; ";
                    }
                }
                if (mapsToAdd.Any())
                {
                    await _context.CustomMapsInUsers.AddRangeAsync(mapsToAdd);
                }

                await _context.SaveChangesAsync();
                await _helpfuncs.LogEventAsync(logModel);
                return Ok(logModel.message);
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await _helpfuncs.LogModelChangeForServerError(logModel, ex);
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
        [Route("SaveListCustom")]
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SaveMapsListCustomGet()
        {
            LogModel logModel = _helpfuncs.LogModelCreate("SaveMapsListCustomGet", "Custom save list gotten");
            try
            {
                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await _helpfuncs.UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

                List<CustomMapsInUser> maps = _context.CustomMapsInUsers
                                   .Where(map => map.UserId == parsedUserId)
                                   .ToList();
                if (!maps.Any())
                {
                    logModel.logLevel = "Error";
                    logModel.message = "There are no custom map saves for this user";
                    logModel.errorCode = "404";
                    await _helpfuncs.LogEventAsync(logModel);
                    return NotFound(logModel.message);
                }

                await _helpfuncs.LogEventAsync(logModel);
                return Ok(maps);
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await _helpfuncs.LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }


        /// <summary>
        /// Send new rate on a map.
        /// </summary>
        /// <response code="200">Rate was posted. Returns message about completion</response>
        /// <response code="400">User ID (from token) conversion in int failed, received data is null, the rate already exists, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found, map wasn't found, user save on this map wasn't found. Returns message about error</response>
        [Route("Rate")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RatePost([FromBody] RateMap? rateMap)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("RatePost", "Rate pasted");
            try
            {
                if (rateMap == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Received data is null";
                    logModel.errorCode = "400";
                    await _helpfuncs.LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }

                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await _helpfuncs.UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

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
                    await _helpfuncs.LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }

                customMapsInUser.Rate = rateMap.newRate;
                customMap.RatingSum += rateMap.newRate;
                customMap.RatingCount += 1;

                await _context.SaveChangesAsync();
                await _helpfuncs.LogEventAsync(logModel);
                return Ok(logModel.message);
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await _helpfuncs.LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }

        /// <summary>
        /// Change rate on a map.
        /// </summary>
        /// <response code="200">Rate was changed. Returns message about completion</response>
        /// <response code="400">User ID (from token) conversion in int failed, received data is null, the old rate doesn't exist, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found, map wasn't found, user save on this map wasn't found. Returns message about error</response>
        [Route("Rate")]
        [Authorize]
        [HttpPut]
        public async Task<IActionResult> RatePut([FromBody] RateMap? rateMap)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("RatePut", "New rate putted");
            try
            {
                if (rateMap == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Received data is null";
                    logModel.errorCode = "400";
                    await _helpfuncs.LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }

                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await _helpfuncs.UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

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
                    await _helpfuncs.LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }

                customMap.RatingSum -= customMapsInUser.Rate;
                customMapsInUser.Rate = rateMap.newRate;
                customMap.RatingSum += rateMap.newRate;

                await _context.SaveChangesAsync();
                await _helpfuncs.LogEventAsync(logModel);
                return Ok(logModel.message);
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await _helpfuncs.LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }


        /// <summary>
        /// Delete rate on a map.
        /// </summary>
        /// <response code="200">Rate was deleted. Returns message about completion</response>
        /// <response code="400">User ID (from token) conversion in int failed, received data is wrong, the old rate doesn't exist, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found, map wasn't found, user save on this map wasn't found. Returns message about error</response>
        [Route("Rate/{idModel:int}")]
        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> RateDelete(int? idModel)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("RateDelete", "Rate deleted");
            try
            {
                if (idModel <= 0 || idModel == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Recieved data is wrong";
                    logModel.errorCode = "400";
                    await _helpfuncs.LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }
                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await _helpfuncs.UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

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
                    await _helpfuncs.LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }

                customMap.RatingSum -= customMapsInUser.Rate;
                customMap.RatingCount -= 1;
                customMapsInUser.Rate = 0;

                await _context.SaveChangesAsync();
                await _helpfuncs.LogEventAsync(logModel);
                return Ok(logModel.message);
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await _helpfuncs.LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }


        /// <summary>
        /// Post about downloading of a map.
        /// </summary>
        /// <remarks>If there is no data aobut saves in db, this method creates it</remarks>
        /// <response code="200">Download posted. Returns message about completion</response>
        /// <response code="400">User ID (from token) conversion in int failed, received data is wrong, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found, map wasn't found. Returns message about error</response>
        [Route("Download/{idModel:int}")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DownLoadPost(int? idModel)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("DownLoadPost", "Map download was posted");
            try
            {
                if (idModel <= 0 || idModel == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Recieved data is wrong";
                    logModel.errorCode = "400";
                    await _helpfuncs.LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }

                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await _helpfuncs.UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => ((cmap.Id == idModel)));
                var (success2, result2) = await CustomMapNullCheck(customMap, logModel);
                if (!success2) return result2!;

                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(cmap => (cmap.CustomMapId == idModel) && (cmap.UserId == parsedUserId));
                if (customMapsInUser != null)
                {
                    if (customMapsInUser.IsAdded == false)
                    {
                        customMap.Downloads += 1;
                        customMapsInUser.IsAdded = true;
                    }
                    else
                    {
                        logModel.message = "The download already exists";
                    }
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
                await _helpfuncs.LogEventAsync(logModel);
                return Ok(logModel.message);
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await _helpfuncs.LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }


        /// <summary>
        /// Post about deletion of a map.
        /// </summary>
        /// <response code="200">Deletion posted. Returns message about completion</response>
        /// <response code="400">User ID (from token) conversion in int failed, received data is wrong, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found, map wasn't found, user save on this map wasn't found. Returns message about error</response>
        [Route("Download/{idModel:int}")]
        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> DownLoadDelete(int? idModel)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("DownLoadDelete", "Map download was deleted");
            try
            {
                if (idModel <= 0 || idModel == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Recieved data is wrong";
                    logModel.errorCode = "400";
                    await _helpfuncs.LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }
                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await _helpfuncs.UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => cmap.Id == idModel);
                var (success2, result2) = await CustomMapNullCheck(customMap, logModel);
                if (!success2) return result2!;

                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(cmap => (cmap.CustomMapId == idModel) && (cmap.UserId == parsedUserId));
                var (success3, result3) = await CustomMapsInUserNullCheck(customMapsInUser, logModel);
                if (!success3) return result3!;


                //customMap.Downloads -= 1;
                customMapsInUser.IsAdded = false;

                await _context.SaveChangesAsync();
                await _helpfuncs.LogEventAsync(logModel);
                return Ok(logModel.message);
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await _helpfuncs.LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }


        /// <summary>
        /// Post about map adding in favourites list.
        /// </summary>
        /// <response code="200">Adding map in favourites list posted. Returns message about completion</response>
        /// <response code="400">User ID (from token) conversion in int failed, received data is wrong, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found, map wasn't found, user save on this map wasn't found. Returns message about error</response>
        [Route("Favourite/{idModel:int}")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> FavouritePost(int? idModel) //можно поменять так же как у downloadpost
        {
            LogModel logModel = _helpfuncs.LogModelCreate("FavouritePost", "Map favourite mark was posted");
            try
            {
                if (idModel <= 0 || idModel == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Recieved data is wrong";
                    logModel.errorCode = "400";
                    await _helpfuncs.LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }

                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await _helpfuncs.UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => ((cmap.Id == idModel)));
                var (success2, result2) = await CustomMapNullCheck(customMap, logModel);
                if (!success2) return result2!;

                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(cmap => (cmap.CustomMapId == idModel) && (cmap.UserId == parsedUserId));
                var (success3, result3) = await CustomMapsInUserNullCheck(customMapsInUser, logModel);
                if (!success3) return result3!;


                customMapsInUser.IsFavourite = true;

                await _context.SaveChangesAsync();
                await _helpfuncs.LogEventAsync(logModel);
                return Ok(logModel.message);
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await _helpfuncs.LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }


        /// <summary>
        /// Post about map deleting from favourites list.
        /// </summary>
        /// <response code="200">Deleting map from favourites list posted. Returns message about completion</response>
        /// <response code="400">User ID (from token) conversion in int failed, received data is wrong, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found, map wasn't found, user save on this map wasn't found. Returns message about error</response>
        [Route("Favourite/{idModel:int}")]
        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> FavouriteDelete(int? idModel)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("FavouriteDelete", "Map favourite mark was deleted");
            try
            {
                if (idModel <= 0 || idModel == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Recieved data is wrong";
                    logModel.errorCode = "400";
                    await _helpfuncs.LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }
                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await _helpfuncs.UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => ((cmap.Id == idModel)));
                var (success2, result2) = await CustomMapNullCheck(customMap, logModel);
                if (!success2) return result2!;

                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(cmap => (cmap.CustomMapId == idModel) && (cmap.UserId == parsedUserId));
                var (success3, result3) = await CustomMapsInUserNullCheck(customMapsInUser, logModel);
                if (!success3) return result3!;


                customMapsInUser.IsFavourite = false;

                await _context.SaveChangesAsync();
                await _helpfuncs.LogEventAsync(logModel);
                return Ok(logModel.message);
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await _helpfuncs.LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }









        private async Task<(bool Success, IActionResult? Result, int UserId)> ValidateAndParseUserIdAsync(HttpRequest request, LogModel logModel)
        {
            string? userId = _helpfuncs.GetUserIdFromToken(request);
            if (string.IsNullOrEmpty(userId))
            {
                logModel.logLevel = "Error";
                logModel.message = "Invalid or missing token";
                logModel.errorCode = "401";
                await _helpfuncs.LogEventAsync(logModel);
                return (false, Unauthorized(logModel.message), -1);
            }

            if (!int.TryParse(userId, out int parsedUserId))
            {
                logModel.logLevel = "Error";
                logModel.message = "User ID conversion in int failed";
                logModel.errorCode = "400";
                await _helpfuncs.LogEventAsync(logModel);
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
                await _helpfuncs.LogEventAsync(logModel);
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
                await _helpfuncs.LogEventAsync(logModel);
                return (false, NotFound(logModel.message));
            }
            return (true, null);
        }
    }
}