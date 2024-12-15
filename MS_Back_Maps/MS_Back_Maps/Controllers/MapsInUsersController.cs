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
    [Route("MapsInUsers")]
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
            LogModel logModel = _helpfuncs.LogModelCreate("ProgressPost", "Progress was posted");
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

                var (result, parsedUserId, parsedPlayerId, parsedCreatorId) = await _helpfuncs.ValidateAndParseUserIdAsync(Request, logModel);
                if(result.logLevel == "Error") return BadRequest(logModel.message);
                logModel.userId = parsedUserId;

                UserIdCheckModel userIdCheckModel = new UserIdCheckModel();
                string requestId = Guid.NewGuid().ToString();
                (userIdCheckModel, logModel) = await _helpfuncs.UserIdCheck(requestId, "player", logModel, parsedUserId, parsedPlayerId, parsedCreatorId);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

                Map? map = _context.Maps.FirstOrDefault(map => ((map.Id == mapSaveModel.mapId)));
                var (success2, result2) = await MapNullCheck(map, logModel);
                if (!success2) return result2!;

                MapsInUser? mapsInUser = _context.MapsInUsers.FirstOrDefault(map => (map.MapId == mapSaveModel.mapId) && (map.PlayerId == userIdCheckModel.playerId));
                Console.WriteLine("1");
                if (mapsInUser == null)
                {
                    Console.WriteLine("2");
                    logModel.message = "Progress was added";
                    MapsInUser mapsInUserInput = new MapsInUser
                    {
                        MapId = mapSaveModel.mapId,
                        PlayerId = (int)userIdCheckModel.playerId,
                        GamesSum = mapSaveModel.gamesSum,
                        Wins = mapSaveModel.wins,
                        Loses = mapSaveModel.loses,
                        OpenedTiles = mapSaveModel.openedTiles,
                        OpenedNumberTiles = mapSaveModel.openedNumberTiles,
                        OpenedBlankTiles = mapSaveModel.openedBlankTiles,
                        FlagsSum = mapSaveModel.flagsSum,
                        FlagsOnBombs = mapSaveModel.flagsOnBombs,
                        TimeSpentSum = mapSaveModel.timeSpentSum,
                        LastGameData = mapSaveModel.lastGameData,
                        LastGameTime = mapSaveModel.lastGameTime
                    };
                    Console.WriteLine("3");
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
                    mapsInUser.LastGameData = mapSaveModel.lastGameData;
                    mapsInUser.LastGameTime = mapSaveModel.lastGameTime;
                }
                Console.WriteLine("4");
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
            LogModel logModel = _helpfuncs.LogModelCreate("ProgressGet", "Progress was gotten");
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

                var (result, parsedUserId, parsedPlayerId, parsedCreatorId) = await _helpfuncs.ValidateAndParseUserIdAsync(Request, logModel);
                if (result.logLevel == "Error") return BadRequest(logModel.message);
                logModel.userId = parsedUserId;

                UserIdCheckModel userIdCheckModel = new UserIdCheckModel();
                string requestId = Guid.NewGuid().ToString();
                (userIdCheckModel, logModel) = await _helpfuncs.UserIdCheck(requestId, "player", logModel, parsedUserId, parsedPlayerId, parsedCreatorId);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

                Map? map = _context.Maps.FirstOrDefault(map => ((map.Id == idModel)));
                var (success2, result2) = await MapNullCheck(map, logModel);
                if (!success2) return result2!;

                MapsInUser? mapsInUser = _context.MapsInUsers.FirstOrDefault(map => (map.MapId == idModel) && (map.PlayerId == userIdCheckModel.playerId));
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
                    lastGameData = mapsInUser.LastGameData,
                    lastGameTime = mapsInUser.LastGameTime,
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
            LogModel logModel = _helpfuncs.LogModelCreate("SaveListPost", "Save list was posted");
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

                var (result, parsedUserId, parsedPlayerId, parsedCreatorId) = await _helpfuncs.ValidateAndParseUserIdAsync(Request, logModel);
                if (result.logLevel == "Error") return BadRequest(logModel.message);
                logModel.userId = parsedUserId;

                UserIdCheckModel userIdCheckModel = new UserIdCheckModel();
                string requestId = Guid.NewGuid().ToString();
                (userIdCheckModel, logModel) = await _helpfuncs.UserIdCheck(requestId, "player", logModel, parsedUserId, parsedPlayerId, parsedCreatorId);
                if (logModel.errorCode == "404") return NotFound(logModel.message);


                var existingMaps = _context.MapsInUsers.Where(map => map.PlayerId == userIdCheckModel.playerId && mapSaveListModel.mapSaveList.Select(m => m.mapId).Contains(map.MapId)).ToDictionary(map => map.MapId);
                List<MapsInUser> mapsToAdd = new List<MapsInUser>();
                foreach ( MapSaveModel mapSaveModel in mapSaveListModel.mapSaveList )
                {
                    Map? map = _context.Maps.FirstOrDefault(map => ((map.Id == mapSaveModel.mapId)));
                    var (success2, result2) = await MapNullCheck(map, logModel);
                    if (success2)
                    {
                        if (!existingMaps.TryGetValue(mapSaveModel.mapId, out var mapsInUser))
                        {
                            mapsToAdd.Add(new MapsInUser
                            {
                                MapId = mapSaveModel.mapId,
                                PlayerId = (int)userIdCheckModel.playerId,
                                GamesSum = mapSaveModel.gamesSum,
                                Wins = mapSaveModel.wins,
                                Loses = mapSaveModel.loses,
                                OpenedTiles = mapSaveModel.openedTiles,
                                OpenedNumberTiles = mapSaveModel.openedNumberTiles,
                                OpenedBlankTiles = mapSaveModel.openedBlankTiles,
                                FlagsSum = mapSaveModel.flagsSum,
                                FlagsOnBombs = mapSaveModel.flagsOnBombs,
                                TimeSpentSum = mapSaveModel.timeSpentSum,
                                LastGameData = mapSaveModel.lastGameData,
                                LastGameTime = mapSaveModel.lastGameTime
                            });
                            logModel.details += $"\n!ADD! Custom map id: {mapSaveModel.mapId}, user id: {userIdCheckModel.playerId};";
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
                            mapsInUser.LastGameData = mapSaveModel.lastGameData;
                            mapsInUser.LastGameTime = mapSaveModel.lastGameTime;
                            logModel.details += $"\n!CHANGE! Custom map id: {mapsInUser.MapId}, user id: {mapsInUser.PlayerId}, id: {mapsInUser.Id};";
                        }
                    }
              
                }
                if (mapsToAdd.Any())
                {
                    await _context.MapsInUsers.AddRangeAsync(mapsToAdd);
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
        [Route("SaveList")]
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SaveListGet()
        {
            LogModel logModel = _helpfuncs.LogModelCreate("SaveListGet", "Save list gotten");
            try
            {
                var (result, parsedUserId, parsedPlayerId, parsedCreatorId) = await _helpfuncs.ValidateAndParseUserIdAsync(Request, logModel);
                if (result.logLevel == "Error") return BadRequest(logModel.message);
                logModel.userId = parsedUserId;

                UserIdCheckModel userIdCheckModel = new UserIdCheckModel();
                string requestId = Guid.NewGuid().ToString();
                (userIdCheckModel, logModel) = await _helpfuncs.UserIdCheck(requestId, "player", logModel, parsedUserId, parsedPlayerId, parsedCreatorId);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

                List<MapsInUser> maps = _context.MapsInUsers
                                   .Where(map => map.PlayerId == userIdCheckModel.playerId)
                                   .ToList();
                if (!maps.Any())
                {
                    logModel.logLevel = "Error";
                    logModel.message = "There are no map saves for this user";
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









        private async Task<(bool Success, IActionResult? Result)> MapNullCheck(Map? map, LogModel logModel)
        {
            if (map == null)
            {
                logModel.logLevel = "Error";
                logModel.message = "The map doesn't exists";
                logModel.errorCode = "404";
                await _helpfuncs.LogEventAsync(logModel);
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
                await _helpfuncs.LogEventAsync(logModel);
                return (false, NotFound(logModel.message));
            }
            return (true, null);
        }
    }
}
