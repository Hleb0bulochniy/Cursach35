using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MS_Back_Maps.Models;
using MS_Back_Maps.Data;
using Microsoft.EntityFrameworkCore;
using static Azure.Core.HttpHeader;

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
        /// <response code="500">Server error</response>
        [Route("Progress")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ProgressPost([FromBody] MapSaveModelDTO? mapSaveModel)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("ProgressPost", "Progress was posted", nameof(MapsInUsersController));
            try
            {
                if (mapSaveModel == null)
                {
                    ResponseDTO responseDTO = await _helpfuncs.LogModelErrorInputAndLog(logModel, "Recieved data is null", "400");
                    return BadRequest(responseDTO);
                }

                var (result, parsedUserId, parsedPlayerId, parsedCreatorId) = await _helpfuncs.ValidateAndParseUserIdAsync(Request, logModel);
                if (result.ErrorCode == "401") return Unauthorized(new ResponseDTO(logModel.Message));
                if (result.LogLevel == "Error") return BadRequest(new ResponseDTO(logModel.Message));
                logModel.UserId = parsedUserId;

                UserIdCheckModel userIdCheckModel = new UserIdCheckModel();
                string requestId = Guid.NewGuid().ToString();
                (userIdCheckModel, logModel) = await _helpfuncs.UserIdCheck(requestId, "player", logModel, parsedUserId, parsedPlayerId, parsedCreatorId);
                if (logModel.ErrorCode == "404") return NotFound(new ResponseDTO(logModel.Message));

                Map? map = await _context.Maps.FirstOrDefaultAsync(map => ((map.Id == mapSaveModel.MapId)));
                var (success2, result2) = await MapNullCheck(map, logModel);
                if (!success2) return result2!;

                MapsInUser? mapsInUser = await _context.MapsInUsers.FirstOrDefaultAsync(map => (map.MapId == mapSaveModel.MapId) && (map.PlayerId == userIdCheckModel.playerId));
                if (mapsInUser == null)
                {
                    logModel.Message = "Progress was added";
                    MapsInUser mapsInUserInput = new MapsInUser
                    {
                        MapId = mapSaveModel.MapId,
                        PlayerId = (int)userIdCheckModel.playerId,
                        GamesSum = mapSaveModel.GamesSum,
                        Wins = mapSaveModel.Wins,
                        Loses = mapSaveModel.Loses,
                        OpenedTiles = mapSaveModel.OpenedTiles,
                        OpenedNumberTiles = mapSaveModel.OpenedNumberTiles,
                        OpenedBlankTiles = mapSaveModel.OpenedBlankTiles,
                        FlagsSum = mapSaveModel.FlagsSum,
                        FlagsOnBombs = mapSaveModel.FlagsOnBombs,
                        TimeSpentSum = mapSaveModel.TimeSpentSum,
                        LastGameData = mapSaveModel.LastGameData,
                        LastGameTime = mapSaveModel.LastGameTime
                    };
                    await _context.MapsInUsers.AddAsync(mapsInUserInput);
                }
                else
                {
                    mapsInUser.MapId = mapSaveModel.MapId;
                    mapsInUser.GamesSum = mapSaveModel.GamesSum;
                    mapsInUser.Wins = mapSaveModel.Wins;
                    mapsInUser.Loses = mapSaveModel.Loses;
                    mapsInUser.OpenedTiles = mapSaveModel.OpenedTiles;
                    mapsInUser.OpenedNumberTiles = mapSaveModel.OpenedNumberTiles;
                    mapsInUser.OpenedBlankTiles = mapSaveModel.OpenedBlankTiles;
                    mapsInUser.FlagsSum = mapSaveModel.FlagsSum;
                    mapsInUser.FlagsOnBombs = mapSaveModel.FlagsOnBombs;
                    mapsInUser.TimeSpentSum = mapSaveModel.TimeSpentSum;
                    mapsInUser.LastGameData = mapSaveModel.LastGameData;
                    mapsInUser.LastGameTime = mapSaveModel.LastGameTime;
                }
                await _context.SaveChangesAsync();
                await _helpfuncs.LogEventAsync(logModel);
                return Ok(new ResponseDTO(logModel.Message));
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await _helpfuncs.LogModelChangeForServerError(logModel, ex);
                return BadRequest(new ResponseDTO(updatedLogModel.Message));
            }
        }


        /// <summary>
        /// Get progress of user on the map.
        /// </summary>
        /// <response code="200">Progress was sent. Returns json with progress</response>
        /// <response code="400">User ID (from token) conversion in int failed, received data is wrong, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found, map wasn't found, user save on this map wasn't found. Returns message about error</response>
        /// <response code="500">Server error</response>
        [Route("Progress/{idModel:int}")]
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ProgressGet(int? idModel)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("ProgressGet", "Progress was gotten", nameof(MapsInUsersController));
            try
            {
                if (idModel <= 0 || idModel == null)
                {
                    ResponseDTO responseDTO = await _helpfuncs.LogModelErrorInputAndLog(logModel, "Recieved data is wrong", "400");
                    return BadRequest(responseDTO);
                }

                var (result, parsedUserId, parsedPlayerId, parsedCreatorId) = await _helpfuncs.ValidateAndParseUserIdAsync(Request, logModel);
                if (result.ErrorCode == "401") return Unauthorized(new ResponseDTO(logModel.Message));
                if (result.LogLevel == "Error") return BadRequest(new ResponseDTO(logModel.Message));
                logModel.UserId = parsedUserId;

                UserIdCheckModel userIdCheckModel = new UserIdCheckModel();
                string requestId = Guid.NewGuid().ToString();
                (userIdCheckModel, logModel) = await _helpfuncs.UserIdCheck(requestId, "player", logModel, parsedUserId, parsedPlayerId, parsedCreatorId);
                if (logModel.ErrorCode == "404") return NotFound(new ResponseDTO(logModel.Message));

                Map? map = await _context.Maps.AsNoTracking().FirstOrDefaultAsync(map => ((map.Id == idModel)));
                var (success2, result2) = await MapNullCheck(map, logModel);
                if (!success2) return result2!;

                MapsInUser? mapsInUser = await _context.MapsInUsers.AsNoTracking().FirstOrDefaultAsync(map => (map.MapId == idModel) && (map.PlayerId == userIdCheckModel.playerId));
                var (success3, result3) = await MapsInUserNullCheck(mapsInUser, logModel);
                if (!success3) return result3!;

                MapSaveModelDTO mapSaveModelDTO = new MapSaveModelDTO
                {
                    Id = mapsInUser.Id,
                    MapId = mapsInUser.MapId,
                    MapName = map.MapName,
                    GamesSum = mapsInUser.GamesSum,
                    Wins = mapsInUser.Wins,
                    Loses = mapsInUser.Loses,
                    OpenedTiles = mapsInUser.OpenedTiles,
                    OpenedNumberTiles = mapsInUser.OpenedNumberTiles,
                    OpenedBlankTiles = mapsInUser.OpenedBlankTiles,
                    FlagsSum = mapsInUser.FlagsSum,
                    FlagsOnBombs = mapsInUser.FlagsOnBombs,
                    TimeSpentSum = mapsInUser.TimeSpentSum,
                    LastGameData = mapsInUser.LastGameData,
                    LastGameTime = mapsInUser.LastGameTime,
                };

                await _helpfuncs.LogEventAsync(logModel);
                return Ok(mapSaveModelDTO);
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await _helpfuncs.LogModelChangeForServerError(logModel, ex);
                return BadRequest(new ResponseDTO(updatedLogModel.Message));
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
        /// <response code="500">Server error</response>
        [Route("SaveList")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SaveListPost([FromBody] MapSaveListModelDTO mapSaveListModel)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("SaveListPost", "Save list was posted", nameof(MapsInUsersController));
            try
            {
                if (mapSaveListModel == null || mapSaveListModel.MapSaveList == null || !mapSaveListModel.MapSaveList.Any())
                {
                    ResponseDTO responseDTO = await _helpfuncs.LogModelErrorInputAndLog(logModel, "Recieved data is wrong", "400");
                    return BadRequest(responseDTO);
                }

                var (result, parsedUserId, parsedPlayerId, parsedCreatorId) = await _helpfuncs.ValidateAndParseUserIdAsync(Request, logModel);
                if (result.ErrorCode == "401") return Unauthorized(new ResponseDTO(logModel.Message));
                if (result.LogLevel == "Error") return BadRequest(new ResponseDTO(logModel.Message));
                logModel.UserId = parsedUserId;

                UserIdCheckModel userIdCheckModel = new UserIdCheckModel();
                string requestId = Guid.NewGuid().ToString();
                (userIdCheckModel, logModel) = await _helpfuncs.UserIdCheck(requestId, "player", logModel, parsedUserId, parsedPlayerId, parsedCreatorId);
                if (logModel.ErrorCode == "404") return NotFound(new ResponseDTO(logModel.Message));


                var existingMaps = _context.MapsInUsers.Where(map => map.PlayerId == userIdCheckModel.playerId && mapSaveListModel.MapSaveList.Select(m => m.MapId).Contains(map.MapId)).ToDictionary(map => map.MapId);
                List<MapsInUser> mapsToAdd = new List<MapsInUser>();
                foreach ( MapSaveModelDTO mapSaveModel in mapSaveListModel.MapSaveList )
                {
                    Map? map = await _context.Maps.FirstOrDefaultAsync(map => ((map.Id == mapSaveModel.MapId)));
                    var (success2, result2) = await MapNullCheck(map, logModel);
                    if (success2)
                    {
                        if (!existingMaps.TryGetValue(mapSaveModel.MapId, out var mapsInUser))
                        {
                            mapsToAdd.Add(new MapsInUser
                            {
                                MapId = mapSaveModel.MapId,
                                PlayerId = (int)userIdCheckModel.playerId,
                                GamesSum = mapSaveModel.GamesSum,
                                Wins = mapSaveModel.Wins,
                                Loses = mapSaveModel.Loses,
                                OpenedTiles = mapSaveModel.OpenedTiles,
                                OpenedNumberTiles = mapSaveModel.OpenedNumberTiles,
                                OpenedBlankTiles = mapSaveModel.OpenedBlankTiles,
                                FlagsSum = mapSaveModel.FlagsSum,
                                FlagsOnBombs = mapSaveModel.FlagsOnBombs,
                                TimeSpentSum = mapSaveModel.TimeSpentSum,
                                LastGameData = mapSaveModel.LastGameData,
                                LastGameTime = mapSaveModel.LastGameTime
                            });
                            logModel.Details += $"\n!ADD! Custom map id: {mapSaveModel.MapId}, user id: {userIdCheckModel.playerId};";
                        }
                        else
                        {
                            mapsInUser.MapId = mapSaveModel.MapId;
                            mapsInUser.GamesSum = mapSaveModel.GamesSum;
                            mapsInUser.Wins = mapSaveModel.Wins;
                            mapsInUser.Loses = mapSaveModel.Loses;
                            mapsInUser.OpenedTiles = mapSaveModel.OpenedTiles;
                            mapsInUser.OpenedNumberTiles = mapSaveModel.OpenedNumberTiles;
                            mapsInUser.OpenedBlankTiles = mapSaveModel.OpenedBlankTiles;
                            mapsInUser.FlagsSum = mapSaveModel.FlagsSum;
                            mapsInUser.FlagsOnBombs = mapSaveModel.FlagsOnBombs;
                            mapsInUser.TimeSpentSum = mapSaveModel.TimeSpentSum;
                            mapsInUser.LastGameData = mapSaveModel.LastGameData;
                            mapsInUser.LastGameTime = mapSaveModel.LastGameTime;
                            logModel.Details += $"\n!CHANGE! Custom map id: {mapsInUser.MapId}, user id: {mapsInUser.PlayerId}, id: {mapsInUser.Id};";
                        }
                    }
              
                }
                if (mapsToAdd.Any())
                {
                    await _context.MapsInUsers.AddRangeAsync(mapsToAdd);
                }

                await _context.SaveChangesAsync();
                await _helpfuncs.LogEventAsync(logModel);
                return Ok(new ResponseDTO(logModel.Message));
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await _helpfuncs.LogModelChangeForServerError(logModel, ex);
                return BadRequest(new ResponseDTO(updatedLogModel.Message));
            }
        }


        /// <summary>
        /// Get progress of user on all maps.
        /// </summary>
        /// <response code="200">Progress was sent. Returns json with progress</response>
        /// <response code="400">User ID (from token) conversion in int failed, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found, user's map saves weren't found. Returns message about error</response>
        /// <response code="500">Server error</response>
        [Route("SaveList")]
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SaveListGet()
        {
            LogModel logModel = _helpfuncs.LogModelCreate("SaveListGet", "Save list gotten", nameof(MapsInUsersController));
            try
            {
                var (result, parsedUserId, parsedPlayerId, parsedCreatorId) = await _helpfuncs.ValidateAndParseUserIdAsync(Request, logModel);
                if (result.ErrorCode == "401") return Unauthorized(new ResponseDTO(logModel.Message));
                if (result.LogLevel == "Error") return BadRequest(new ResponseDTO(logModel.Message));
                logModel.UserId = parsedUserId;

                UserIdCheckModel userIdCheckModel = new UserIdCheckModel();
                string requestId = Guid.NewGuid().ToString();
                (userIdCheckModel, logModel) = await _helpfuncs.UserIdCheck(requestId, "player", logModel, parsedUserId, parsedPlayerId, parsedCreatorId);
                if (logModel.ErrorCode == "404") return NotFound(new ResponseDTO(logModel.Message));

                List<MapsInUser> maps = await _context.MapsInUsers
                                   .Where(map => map.PlayerId == userIdCheckModel.playerId)
                                   .AsNoTracking()
                                   .ToListAsync();

                var mapIds = maps.Select(m => m.MapId).Distinct().ToList();
                var mapNames = await _context.Maps
                                   .Where(m => mapIds.Contains(m.Id))
                                   .ToDictionaryAsync(m => m.Id, m => m.MapName);

                MapSaveListModelDTO mapSaveListModelDTO = new MapSaveListModelDTO();

                foreach (MapsInUser map in maps)
                {
                    MapSaveModelDTO mapSaveModelDTO = new MapSaveModelDTO
                    {
                        Id = map.Id,
                        MapId = map.MapId,
                        MapName = mapNames.GetValueOrDefault(map.MapId),
                        GamesSum = map.GamesSum,
                        Wins = map.Wins,
                        Loses = map.Loses,
                        OpenedTiles = map.OpenedTiles,
                        OpenedNumberTiles = map.OpenedNumberTiles,
                        OpenedBlankTiles = map.OpenedBlankTiles,
                        FlagsSum = map.FlagsSum,
                        FlagsOnBombs = map.FlagsOnBombs,
                        TimeSpentSum = map.TimeSpentSum,
                        LastGameData = map.LastGameData,
                        LastGameTime = map.LastGameTime,
                    };
                    mapSaveListModelDTO.MapSaveList.Add(mapSaveModelDTO);
                }
                if (!maps.Any())
                {
                    ResponseDTO responseDTO = await _helpfuncs.LogModelErrorInputAndLog(logModel, "There are no map saves for this user", "404");
                    return NotFound(responseDTO);
                }

                await _helpfuncs.LogEventAsync(logModel);
                return Ok(mapSaveListModelDTO);
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await _helpfuncs.LogModelChangeForServerError(logModel, ex);
                return BadRequest(new ResponseDTO(updatedLogModel.Message));
            }
        }



        private async Task<(bool Success, IActionResult? Result)> MapNullCheck(Map? map, LogModel logModel)
        {
            if (map == null)
            {
                ResponseDTO responseDTO = await _helpfuncs.LogModelErrorInputAndLog(logModel, "The map doesn't exists", "404");
                return (false, NotFound(responseDTO));
            }
            return (true, null);
        }

        private async Task<(bool Success, IActionResult? Result)> MapsInUserNullCheck(MapsInUser? mapsInUser, LogModel logModel)
        {
            if (mapsInUser == null)
            {
                ResponseDTO responseDTO = await _helpfuncs.LogModelErrorInputAndLog(logModel, "The map:user doesn't exists", "404");
                return (false, NotFound(responseDTO));
            }
            return (true, null);
        }
    }
}
