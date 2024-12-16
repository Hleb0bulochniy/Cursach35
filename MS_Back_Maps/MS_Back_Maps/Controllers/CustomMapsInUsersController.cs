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
                var (result, parsedUserId, parsedPlayerId, parsedCreatorId) = await _helpfuncs.ValidateAndParseUserIdAsync(Request, logModel);
                if (result.logLevel == "Error") return BadRequest(logModel.message);
                logModel.userId = parsedUserId;

                UserIdCheckModel userIdCheckModel = new UserIdCheckModel();
                string requestId = Guid.NewGuid().ToString();
                (userIdCheckModel, logModel) = await _helpfuncs.UserIdCheck(requestId, "player", logModel, parsedUserId, parsedPlayerId, parsedCreatorId);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => cmap.MapId == rateMap.mapId);
                var (success2, result2) = await CustomMapNullCheck(customMap, logModel);
                Console.WriteLine(customMap.Id);
                if (!success2) return result2!;

                MapsInUser? mapsInUser = _context.MapsInUsers.FirstOrDefault(cmap => cmap.MapId == rateMap.mapId && (cmap.PlayerId == userIdCheckModel.playerId));
                var (success3, result3) = await MapsInUserNullCheck(mapsInUser, logModel);
                if (!success3) return result3!;

                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(cmap => cmap.MapsInUserId == mapsInUser.Id);
                var (success4, result4) = await CustomMapsInUserNullCheck(customMapsInUser, logModel);
                if (!success4) return result4!;

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

                var (result, parsedUserId, parsedPlayerId, parsedCreatorId) = await _helpfuncs.ValidateAndParseUserIdAsync(Request, logModel);
                if (result.logLevel == "Error") return BadRequest(logModel.message);
                logModel.userId = parsedUserId;

                UserIdCheckModel userIdCheckModel = new UserIdCheckModel();
                string requestId = Guid.NewGuid().ToString();
                (userIdCheckModel, logModel) = await _helpfuncs.UserIdCheck(requestId, "player", logModel, parsedUserId, parsedPlayerId, parsedCreatorId);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => cmap.MapId == rateMap.mapId);
                var (success2, result2) = await CustomMapNullCheck(customMap, logModel);
                if (!success2) return result2!;

                MapsInUser? mapsInUser = _context.MapsInUsers.FirstOrDefault(cmap => cmap.MapId == rateMap.mapId && (cmap.PlayerId == userIdCheckModel.playerId));
                var (success3, result3) = await MapsInUserNullCheck(mapsInUser, logModel);
                if (!success3) return result3!;

                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(cmap => cmap.MapsInUserId == mapsInUser.Id);
                var (success4, result4) = await CustomMapsInUserNullCheck(customMapsInUser, logModel);
                if (!success4) return result4!;

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
                var (result, parsedUserId, parsedPlayerId, parsedCreatorId) = await _helpfuncs.ValidateAndParseUserIdAsync(Request, logModel);
                if (result.logLevel == "Error") return BadRequest(logModel.message);
                logModel.userId = parsedUserId;

                UserIdCheckModel userIdCheckModel = new UserIdCheckModel();
                string requestId = Guid.NewGuid().ToString();
                (userIdCheckModel, logModel) = await _helpfuncs.UserIdCheck(requestId, "player", logModel, parsedUserId, parsedPlayerId, parsedCreatorId);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => cmap.MapId == idModel);
                var (success2, result2) = await CustomMapNullCheck(customMap, logModel);
                if (!success2) return result2!;

                MapsInUser? mapsInUser = _context.MapsInUsers.FirstOrDefault(cmap => cmap.MapId == idModel && (cmap.PlayerId == userIdCheckModel.playerId));
                var (success3, result3) = await MapsInUserNullCheck(mapsInUser, logModel);
                if (!success3) return result3!;

                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(cmap => cmap.MapsInUserId == mapsInUser.Id);
                var (success4, result4) = await CustomMapsInUserNullCheck(customMapsInUser, logModel);
                if (!success4) return result4!;

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

                var (result, parsedUserId, parsedPlayerId, parsedCreatorId) = await _helpfuncs.ValidateAndParseUserIdAsync(Request, logModel);
                if (result.logLevel == "Error") return BadRequest(logModel.message);
                logModel.userId = parsedUserId;

                UserIdCheckModel userIdCheckModel = new UserIdCheckModel();
                string requestId = Guid.NewGuid().ToString();
                (userIdCheckModel, logModel) = await _helpfuncs.UserIdCheck(requestId, "player", logModel, parsedUserId, parsedPlayerId, parsedCreatorId);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => ((cmap.MapId == idModel)));
                var (success2, result2) = await CustomMapNullCheck(customMap, logModel);
                if (!success2) return result2!;

                MapsInUser? mapsInUser = _context.MapsInUsers.FirstOrDefault(cmap => cmap.MapId == idModel && (cmap.PlayerId == userIdCheckModel.playerId));
                CustomMapsInUser? customMapsInUser = new CustomMapsInUser();
                if (mapsInUser == null)
                {

                    mapsInUser = new MapsInUser
                    {
                        PlayerId = (int)userIdCheckModel.playerId,
                        MapId = customMap.MapId,
                        GamesSum = 0,
                        Wins = 0,
                        Loses = 0,
                        OpenedTiles = 0,
                        OpenedNumberTiles = 0,
                        OpenedBlankTiles = 0,
                        FlagsSum = 0,
                        FlagsOnBombs = 0,
                        TimeSpentSum = 0,
                        LastGameData = "",
                        LastGameTime = 0,
                    };

                    await _context.MapsInUsers.AddAsync(mapsInUser);

                    await _context.SaveChangesAsync(); 
                }
                customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(cmap => cmap.MapsInUserId == mapsInUser.Id);

                if (customMapsInUser == null)
                {

                    CustomMapsInUser customMapsInUserNew = new CustomMapsInUser
                    {
                        MapsInUserId = mapsInUser.Id,
                        IsAdded = true,
                        IsFavourite = false,
                        Rate = 0
                    };

                    await _context.CustomMapsInUsers.AddAsync(customMapsInUserNew);
                    customMap.Downloads += 1;
                }
                else
                {

                    if (customMapsInUser.IsAdded == false)
                    {
                        customMapsInUser.IsAdded = true;
                        customMap.Downloads += 1;
                    }
                    else
                    {
                        logModel.message = "The download already exists";
                    }
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
                var (result, parsedUserId, parsedPlayerId, parsedCreatorId) = await _helpfuncs.ValidateAndParseUserIdAsync(Request, logModel);
                if (result.logLevel == "Error") return BadRequest(logModel.message);
                logModel.userId = parsedUserId;

                UserIdCheckModel userIdCheckModel = new UserIdCheckModel();
                string requestId = Guid.NewGuid().ToString();
                (userIdCheckModel, logModel) = await _helpfuncs.UserIdCheck(requestId, "player", logModel, parsedUserId, parsedPlayerId, parsedCreatorId);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => cmap.MapId == idModel);
                var (success2, result2) = await CustomMapNullCheck(customMap, logModel);
                if (!success2) return result2!;

                MapsInUser? mapsInUser = _context.MapsInUsers.FirstOrDefault(cmap => cmap.MapId == idModel && (cmap.PlayerId == userIdCheckModel.playerId));
                var (success3, result3) = await MapsInUserNullCheck(mapsInUser, logModel);
                if (!success3) return result3!;

                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(cmap => cmap.MapsInUserId == mapsInUser.Id);
                var (success4, result4) = await CustomMapsInUserNullCheck(customMapsInUser, logModel);
                if (!success4) return result4!;

                if (customMapsInUser.IsAdded)
                {
                    customMap.Downloads -= 1;
                    customMapsInUser.IsAdded = false;
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

                var (result, parsedUserId, parsedPlayerId, parsedCreatorId) = await _helpfuncs.ValidateAndParseUserIdAsync(Request, logModel);
                if (result.logLevel == "Error") return BadRequest(logModel.message);
                logModel.userId = parsedUserId;

                UserIdCheckModel userIdCheckModel = new UserIdCheckModel();
                string requestId = Guid.NewGuid().ToString();
                (userIdCheckModel, logModel) = await _helpfuncs.UserIdCheck(requestId, "player", logModel, parsedUserId, parsedPlayerId, parsedCreatorId);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => ((cmap.MapId == idModel)));
                var (success2, result2) = await CustomMapNullCheck(customMap, logModel);
                if (!success2) return result2!;

                MapsInUser? mapsInUser = _context.MapsInUsers.FirstOrDefault(cmap => cmap.MapId == idModel && (cmap.PlayerId == userIdCheckModel.playerId));
                var (success3, result3) = await MapsInUserNullCheck(mapsInUser, logModel);
                if (!success3) return result3!;

                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(cmap => cmap.MapsInUserId == mapsInUser.Id);
                var (success4, result4) = await CustomMapsInUserNullCheck(customMapsInUser, logModel);
                if (!success4) return result4!;


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
                var (result, parsedUserId, parsedPlayerId, parsedCreatorId) = await _helpfuncs.ValidateAndParseUserIdAsync(Request, logModel);
                if (result.logLevel == "Error") return BadRequest(logModel.message);
                logModel.userId = parsedUserId;

                UserIdCheckModel userIdCheckModel = new UserIdCheckModel();
                string requestId = Guid.NewGuid().ToString();
                (userIdCheckModel, logModel) = await _helpfuncs.UserIdCheck(requestId, "player", logModel, parsedUserId, parsedPlayerId, parsedCreatorId);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => ((cmap.MapId == idModel)));
                var (success2, result2) = await CustomMapNullCheck(customMap, logModel);
                if (!success2) return result2!;

                MapsInUser? mapsInUser = _context.MapsInUsers.FirstOrDefault(cmap => cmap.MapId == idModel && (cmap.PlayerId == userIdCheckModel.playerId));
                var (success3, result3) = await MapsInUserNullCheck(mapsInUser, logModel);
                if (!success3) return result3!;

                CustomMapsInUser? customMapsInUser = _context.CustomMapsInUsers.FirstOrDefault(cmap => cmap.MapsInUserId == mapsInUser.Id);
                var (success4, result4) = await CustomMapsInUserNullCheck(customMapsInUser, logModel);
                if (!success4) return result4!;


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