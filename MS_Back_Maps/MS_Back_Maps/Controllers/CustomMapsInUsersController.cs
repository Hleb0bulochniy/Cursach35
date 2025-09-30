using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MS_Back_Maps.Data;
using MS_Back_Maps.Models;

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
        /// <response code="400">User ID (from token) conversion in int failed, received data is wrong, the rate already exists, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found, map wasn't found, user save on this map wasn't found. Returns message about error</response>
        /// <response code="500">Server error</response>
        [Route("Rate")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RatePost([FromBody] RateMapDTO? rateMap)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("RatePost", "Rate pasted", nameof(CustomMapsInUsersController));
            try
            {
                if (rateMap == null || (rateMap.NewRate>5 ||rateMap.NewRate <0))
                {
                    ResponseDTO responseDTO = await _helpfuncs.LogModelErrorInputAndLog(logModel, "Received data is wrong", "400");
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

                await using var tx = await _context.Database.BeginTransactionAsync();

                //эти три проверки можно вынести в отдельный метод

                CustomMap? customMap = await _context.CustomMaps.FirstOrDefaultAsync(cmap => cmap.MapId == rateMap.MapId);
                if (customMap == null)
                {
                    return NotFound(await _helpfuncs.LogModelErrorInputAndLog(logModel, "The custom map doesn't exists", "404"));
                }

                MapsInUser? mapsInUser = await _context.MapsInUsers.FirstOrDefaultAsync(cmap => cmap.MapId == rateMap.MapId && (cmap.PlayerId == userIdCheckModel.playerId));
                if (mapsInUser == null)
                {
                    return NotFound(await _helpfuncs.LogModelErrorInputAndLog(logModel, "The map:user doesn't exists", "404"));
                }

                CustomMapsInUser? customMapsInUser = await _context.CustomMapsInUsers.FirstOrDefaultAsync(cmap => cmap.MapsInUserId == mapsInUser.Id);
                if (customMapsInUser == null)
                {
                    return NotFound(await _helpfuncs.LogModelErrorInputAndLog(logModel, "The customMap:user doesn't exists", "404"));
                }

                if (customMapsInUser.Rate != 0)
                {
                    ResponseDTO responseDTO = await _helpfuncs.LogModelErrorInputAndLog(logModel, "The rate already exists", "400");
                    return BadRequest(responseDTO);
                }

                customMapsInUser.Rate = rateMap.NewRate;
                customMap.RatingSum += rateMap.NewRate;
                customMap.RatingCount += Math.Max(0, customMap.RatingCount + 1);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

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
        /// Change rate on a map.
        /// </summary>
        /// <response code="200">Rate was changed. Returns message about completion</response>
        /// <response code="400">User ID (from token) conversion in int failed, received data is wrong, the old rate doesn't exist, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found, map wasn't found, user save on this map wasn't found. Returns message about error</response>
        /// <response code="500">Server error</response>
        [Route("Rate")]
        [Authorize]
        [HttpPut]
        public async Task<IActionResult> RatePut([FromBody] RateMapDTO? rateMap)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("RatePut", "New rate putted", nameof(CustomMapsInUsersController));
            try
            {
                if (rateMap == null || (rateMap.NewRate > 5 || rateMap.NewRate < 0))
                {
                    ResponseDTO responseDTO = await _helpfuncs.LogModelErrorInputAndLog(logModel, "Received data is wrong", "400");
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

                await using var tx = await _context.Database.BeginTransactionAsync();

                CustomMap? customMap = await _context.CustomMaps.FirstOrDefaultAsync(cmap => cmap.MapId == rateMap.MapId);
                if (customMap == null)
                {
                    return NotFound(await _helpfuncs.LogModelErrorInputAndLog(logModel, "The custom map doesn't exists", "404"));
                }

                MapsInUser? mapsInUser = await _context.MapsInUsers.FirstOrDefaultAsync(cmap => cmap.MapId == rateMap.MapId && (cmap.PlayerId == userIdCheckModel.playerId));
                if (mapsInUser == null)
                {
                    return NotFound(await _helpfuncs.LogModelErrorInputAndLog(logModel, "The map:user doesn't exists", "404"));
                }

                CustomMapsInUser? customMapsInUser = await _context.CustomMapsInUsers.FirstOrDefaultAsync(cmap => cmap.MapsInUserId == mapsInUser.Id);
                if (customMapsInUser == null)
                {
                    return NotFound(await _helpfuncs.LogModelErrorInputAndLog(logModel, "The customMap:user doesn't exists", "404"));
                }

                if (customMapsInUser.Rate == 0)
                {
                    ResponseDTO responseDTO = await _helpfuncs.LogModelErrorInputAndLog(logModel, "The old rate doesn't exist", "400");
                    return BadRequest(responseDTO);
                }



                customMap.RatingSum -= customMapsInUser.Rate;
                customMapsInUser.Rate = rateMap.NewRate;
                customMap.RatingSum += rateMap.NewRate;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
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
        /// Delete rate on a map.
        /// </summary>
        /// <response code="200">Rate was deleted. Returns message about completion</response>
        /// <response code="400">User ID (from token) conversion in int failed, received data is wrong, the old rate doesn't exist, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found, map wasn't found, user save on this map wasn't found. Returns message about error</response>
        /// <response code="500">Server error</response>
        [Route("Rate/{idModel:int}")]
        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> RateDelete(int? idModel)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("RateDelete", "Rate deleted", nameof(CustomMapsInUsersController));
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

                await using var tx = await _context.Database.BeginTransactionAsync();

                CustomMap? customMap = await _context.CustomMaps.FirstOrDefaultAsync(cmap => cmap.MapId == idModel);
                if (customMap == null)
                {
                    return NotFound(await _helpfuncs.LogModelErrorInputAndLog(logModel, "The custom map doesn't exists", "404"));
                }

                MapsInUser? mapsInUser = await _context.MapsInUsers.FirstOrDefaultAsync(cmap => cmap.MapId == idModel && (cmap.PlayerId == userIdCheckModel.playerId));
                if (mapsInUser == null)
                {
                    return NotFound(await _helpfuncs.LogModelErrorInputAndLog(logModel, "The map:user doesn't exists", "404"));
                }

                CustomMapsInUser? customMapsInUser = await _context.CustomMapsInUsers.FirstOrDefaultAsync(cmap => cmap.MapsInUserId == mapsInUser.Id);
                if (customMapsInUser == null)
                {
                    return NotFound(await _helpfuncs.LogModelErrorInputAndLog(logModel, "The customMap:user doesn't exists", "404"));
                }

                if (customMapsInUser.Rate == 0)
                {
                    ResponseDTO responseDTO = await _helpfuncs.LogModelErrorInputAndLog(logModel, "The old rate doesn't exist", "400");
                    return BadRequest(responseDTO);
                }

                customMap.RatingSum -= customMapsInUser.Rate;
                customMap.RatingCount -= 1;
                customMapsInUser.Rate = 0;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
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
        /// Post about downloading of a map.
        /// </summary>
        /// <remarks>If there is no data aobut saves in db, this method creates it</remarks>
        /// <response code="200">Download posted. Returns message about completion</response>
        /// <response code="400">User ID (from token) conversion in int failed, received data is wrong, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found, map wasn't found. Returns message about error</response>
        /// <response code="500">Server error</response>
        [Route("Download/{idModel:int}")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DownLoadPost(int? idModel)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("DownLoadPost", "Map download was posted", nameof(CustomMapsInUsersController));
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

                await using var tx = await _context.Database.BeginTransactionAsync();

                CustomMap? customMap = await _context.CustomMaps.FirstOrDefaultAsync(cmap => ((cmap.MapId == idModel)));
                if (customMap == null)
                {
                    return NotFound(await _helpfuncs.LogModelErrorInputAndLog(logModel, "The custom map doesn't exists", "404"));
                }

                MapsInUser? mapsInUser = await _context.MapsInUsers.FirstOrDefaultAsync(cmap => cmap.MapId == idModel && (cmap.PlayerId == userIdCheckModel.playerId));
                if (mapsInUser == null)
                {
                    return NotFound(await _helpfuncs.LogModelErrorInputAndLog(logModel, "The map:user doesn't exists", "404"));
                }

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
                }
                customMapsInUser = await _context.CustomMapsInUsers.FirstOrDefaultAsync(cmap => cmap.MapsInUserId == mapsInUser.Id);

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
                        logModel.Message = "The download already exists";
                    }
                }


                await _context.SaveChangesAsync();
                await tx.CommitAsync();
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
        /// Post about deletion of a map.
        /// </summary>
        /// <response code="200">Deletion posted. Returns message about completion</response>
        /// <response code="400">User ID (from token) conversion in int failed, received data is wrong, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found, map wasn't found, user save on this map wasn't found. Returns message about error</response>
        /// <response code="500">Server error</response>
        [Route("Download/{idModel:int}")]
        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> DownLoadDelete(int? idModel)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("DownLoadDelete", "Map download was deleted", nameof(CustomMapsInUsersController));
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

                await using var tx = await _context.Database.BeginTransactionAsync();

                CustomMap? customMap = await _context.CustomMaps.FirstOrDefaultAsync(cmap => cmap.MapId == idModel);
                if (customMap == null)
                {
                    return NotFound(await _helpfuncs.LogModelErrorInputAndLog(logModel, "The custom map doesn't exists", "404"));
                }

                MapsInUser? mapsInUser = await _context.MapsInUsers.FirstOrDefaultAsync(cmap => cmap.MapId == idModel && (cmap.PlayerId == userIdCheckModel.playerId));
                if (mapsInUser == null)
                {
                    return NotFound(await _helpfuncs.LogModelErrorInputAndLog(logModel, "The map:user doesn't exists", "404"));
                }

                CustomMapsInUser? customMapsInUser = await _context.CustomMapsInUsers.FirstOrDefaultAsync(cmap => cmap.MapsInUserId == mapsInUser.Id);
                if (customMapsInUser == null)
                {
                    return NotFound(await _helpfuncs.LogModelErrorInputAndLog(logModel, "The customMap:user doesn't exists", "404"));
                }

                if (customMapsInUser.IsAdded)
                {
                    customMap.Downloads -= 1;
                    customMapsInUser.IsAdded = false;
                }


                await _context.SaveChangesAsync();
                await tx.CommitAsync();
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
        /// Post about map adding in favourites list.
        /// </summary>
        /// <response code="200">Adding map in favourites list posted. Returns message about completion</response>
        /// <response code="400">User ID (from token) conversion in int failed, received data is wrong, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found, map wasn't found, user save on this map wasn't found. Returns message about error</response>
        /// <response code="500">Server error</response>
        [Route("Favourite/{idModel:int}")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> FavouritePost(int? idModel) //можно поменять так же как у downloadpost
        {
            LogModel logModel = _helpfuncs.LogModelCreate("FavouritePost", "Map favourite mark was posted", nameof(CustomMapsInUsersController));
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

                await using var tx = await _context.Database.BeginTransactionAsync();

                CustomMap? customMap = await _context.CustomMaps.FirstOrDefaultAsync(cmap => ((cmap.MapId == idModel)));
                if (customMap == null)
                {
                    return NotFound(await _helpfuncs.LogModelErrorInputAndLog(logModel, "The custom map doesn't exists", "404"));
                }

                MapsInUser? mapsInUser = await _context.MapsInUsers.FirstOrDefaultAsync(cmap => cmap.MapId == idModel && (cmap.PlayerId == userIdCheckModel.playerId));
                if (mapsInUser == null)
                {
                    return NotFound(await _helpfuncs.LogModelErrorInputAndLog(logModel, "The map:user doesn't exists", "404"));
                }

                CustomMapsInUser? customMapsInUser = await _context.CustomMapsInUsers.FirstOrDefaultAsync(cmap => cmap.MapsInUserId == mapsInUser.Id);
                if (customMapsInUser == null)
                {
                    return NotFound(await _helpfuncs.LogModelErrorInputAndLog(logModel, "The customMap:user doesn't exists", "404"));
                }


                customMapsInUser.IsFavourite = true;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
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
        /// Post about map deleting from favourites list.
        /// </summary>
        /// <response code="200">Deleting map from favourites list posted. Returns message about completion</response>
        /// <response code="400">User ID (from token) conversion in int failed, received data is wrong, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found, map wasn't found, user save on this map wasn't found. Returns message about error</response>
        /// <response code="500">Server error</response>
        [Route("Favourite/{idModel:int}")]
        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> FavouriteDelete(int? idModel)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("FavouriteDelete", "Map favourite mark was deleted", nameof(CustomMapsInUsersController));
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

                await using var tx = await _context.Database.BeginTransactionAsync();

                CustomMap? customMap = await _context.CustomMaps.FirstOrDefaultAsync(cmap => ((cmap.MapId == idModel)));
                if (customMap == null)
                {
                    return NotFound(await _helpfuncs.LogModelErrorInputAndLog(logModel, "The custom map doesn't exists", "404"));
                }

                MapsInUser? mapsInUser = await _context.MapsInUsers.FirstOrDefaultAsync(cmap => cmap.MapId == idModel && (cmap.PlayerId == userIdCheckModel.playerId));
                if (mapsInUser == null)
                {
                    return NotFound(await _helpfuncs.LogModelErrorInputAndLog(logModel, "The map:user doesn't exists", "404"));
                }

                CustomMapsInUser? customMapsInUser = await _context.CustomMapsInUsers.FirstOrDefaultAsync(cmap => cmap.MapsInUserId == mapsInUser.Id);
                if (customMapsInUser == null)
                {
                    return NotFound(await _helpfuncs.LogModelErrorInputAndLog(logModel, "The customMap:user doesn't exists", "404"));
                }


                customMapsInUser.IsFavourite = false;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                await _helpfuncs.LogEventAsync(logModel);
                return Ok(new ResponseDTO(logModel.Message));
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await _helpfuncs.LogModelChangeForServerError(logModel, ex);
                return BadRequest(new ResponseDTO(updatedLogModel.Message));
            }
        }


        //private async Task<(bool Success, IActionResult? Result)> CustomMapNullCheck(CustomMap? customMap, LogModel logModel)
        //{
        //    if (customMap == null)
        //    {
        //        ResponseDTO responseDTO = await _helpfuncs.LogModelErrorInputAndLog(logModel, "The custom map doesn't exists", "404");
        //        return (false, NotFound(responseDTO));
        //    }
        //    return (true, null);
        //}

        //private async Task<(bool Success, IActionResult? Result)> MapsInUserNullCheck(MapsInUser? mapsInUser, LogModel logModel)
        //{
        //    if (mapsInUser == null)
        //    {
        //        ResponseDTO responseDTO = await _helpfuncs.LogModelErrorInputAndLog(logModel, "The map:user doesn't exists", "404");
        //        return (false, NotFound(responseDTO));
        //    }
        //    return (true, null);
        //}

        //private async Task<(bool Success, IActionResult? Result)> CustomMapsInUserNullCheck(CustomMapsInUser? customMapsInUser, LogModel logModel)
        //{
        //    if (customMapsInUser == null)
        //    {
        //        ResponseDTO responseDTO = await _helpfuncs.LogModelErrorInputAndLog(logModel, "The customMap:user doesn't exists", "404");
        //        return (false, NotFound(responseDTO));
        //    }
        //    return (true, null);
        //}
    }
}