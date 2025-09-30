using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MS_Back_Maps.Models;
using MS_Back_Maps.Data;
using Microsoft.EntityFrameworkCore;

namespace MS_Back_Maps.Controllers
{
    [ApiController]
    [Route("CustomMaps")]

    public class CustomMapsController : ControllerBase
    {
        private readonly HelpFuncs _helpfuncs;
        private readonly MapsContext _context;
        private readonly ProducerService _producerService;
        public CustomMapsController(HelpFuncs helpfuncs, ProducerService producerService, MapsContext mapsContext)
        {
            _helpfuncs = helpfuncs;
            _producerService = producerService;
            _context = mapsContext;
        }


        /// <summary>
        /// Add new custom map in db.
        /// </summary>
        /// <response code="200">Map was added. Returns message about completion</response>
        /// <response code="400">User ID (from token) conversion in int failed, received data is null, map name is empty, about field is too long, this map already exists, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found. Returns message about error</response>
        /// <response code="500">Server error</response>
        [Route("CustomMap")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CustomMapPost([FromBody] CustomMapDataDTO? customMapData)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("CustomMapPost", "Custom map was added", nameof(CustomMapsController));
            try
            {
                if (customMapData == null)
                {
                    ResponseDTO responseDTO = await _helpfuncs.LogModelErrorInputAndLog(logModel, "Recieved data is wrong", "400");
                    return BadRequest(responseDTO);
                }

                if (customMapData.MapName == null || customMapData.MapName == "")
                {
                    ResponseDTO responseDTO = await _helpfuncs.LogModelErrorInputAndLog(logModel, "Name is empty", "400");
                    return BadRequest(responseDTO);
                }

                if (customMapData.About.Count() > 500)
                {
                    ResponseDTO responseDTO = await _helpfuncs.LogModelErrorInputAndLog(logModel, "About field is too long", "400");
                    return BadRequest(responseDTO);
                }

                var (result, parsedUserId, parsedPlayerId, parsedCreatorId) = await _helpfuncs.ValidateAndParseUserIdAsync(Request, logModel);
                if (result.ErrorCode == "401") return Unauthorized(new ResponseDTO(logModel.Message));
                if (result.LogLevel == "Error") return BadRequest(new ResponseDTO(logModel.Message));
                logModel.UserId = parsedUserId;

                UserIdCheckModel userIdCheckModel = new UserIdCheckModel();
                string requestId = Guid.NewGuid().ToString();
                (userIdCheckModel, logModel) = await _helpfuncs.UserIdCheck(requestId, "creator", logModel, parsedUserId, parsedPlayerId, parsedCreatorId);
                if (logModel.ErrorCode == "404") return NotFound(new ResponseDTO(logModel.Message));
                
                Map? mapCheck = await _context.Maps.FirstOrDefaultAsync(u => u.MapName == customMapData.MapName && u.About == customMapData.About);
                
                if (mapCheck != null)
                {
                    ResponseDTO responseDTO = await _helpfuncs.LogModelErrorInputAndLog(logModel, "This map already exists", "400");
                    return BadRequest(responseDTO);
                }
                
                Map map = new Map
                {
                    MapName = customMapData.MapName,
                    BombCount = customMapData.BombCount,
                    MapSize = customMapData.MapSize,
                    MapType = (int)customMapData.MapType,
                    IsCustom = true,
                    About = customMapData.About,
                };
                
                await _context.Maps.AddAsync(map);
                await _context.SaveChangesAsync();


                CustomMap? customMapCheck = await _context.CustomMaps.FirstOrDefaultAsync(u => u.MapId == map.Id && u.CreatorId == userIdCheckModel.creatorId);

                if (customMapCheck != null)
                {
                    ResponseDTO responseDTO = await _helpfuncs.LogModelErrorInputAndLog(logModel, "This map already exists", "400");
                    return BadRequest(responseDTO);
                }

                CustomMap customMap = new CustomMap
                {
                    MapId = map.Id,
                    CreatorId = (int)userIdCheckModel.creatorId,
                    CreationDate = customMapData.CreationDate,
                    RatingSum = customMapData.RatingSum,
                    RatingCount = customMapData.RatingCount,
                    Downloads = customMapData.Downloads,
                };

                await _context.CustomMaps.AddAsync(customMap);
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
        /// Get data of custom map from db.
        /// </summary>
        /// <response code="200">Map was found and sent. Returns json with custom map data</response>
        /// <response code="400">Recieved data is wrong, other error (watch Logs). Returns message about error</response>
        /// <response code="404">Map wasn't found, map creator wasn't found. Returns message about error</response>
        /// <response code="500">Server error</response>
        [Route("CustomMap/{idModel:int}")]
        [HttpGet]
        public async Task<IActionResult> CustomMapGet(int? idModel)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("CustomMapGet", "Custom map gotten", nameof(CustomMapsController));
            try
            {
                if (idModel <= 0 || idModel == null)
                {
                    ResponseDTO responseDTO = await _helpfuncs.LogModelErrorInputAndLog(logModel, "Recieved data is wrong", "400");
                    return BadRequest(responseDTO);
                }
                CustomMap? customMap = await _context.CustomMaps.AsNoTracking().FirstOrDefaultAsync(cmap => cmap.MapId == idModel);
                Map? map = await _context.Maps.AsNoTracking().FirstOrDefaultAsync(map => map.Id == idModel);

                if (customMap == null || map == null)
                {
                    ResponseDTO responseDTO = await _helpfuncs.LogModelErrorInputAndLog(logModel, "Map was not found by MapId", "404");
                    return NotFound(responseDTO);
                }

                UserIdCheckModel userIdCheckModel = new UserIdCheckModel();
                string requestId = Guid.NewGuid().ToString();
                (userIdCheckModel, logModel) = await _helpfuncs.UserIdCheck(requestId, "", logModel, null, null, customMap.CreatorId);
                if (logModel.ErrorCode == "404") return NotFound(new ResponseDTO(logModel.Message));

                CustomMapDataDTO customMapDataDTO = new CustomMapDataDTO 
                {
                    MapID = map.Id,
                    MapName = map.MapName,
                    BombCount = map.BombCount,
                    MapSize = map.MapSize,
                    MapType = (CustomMapType)map.MapType,
                    CreatorId = customMap.CreatorId,
                    CreatorName = userIdCheckModel.userName,
                    CreationDate = customMap.CreationDate,
                    RatingSum = customMap.RatingSum,
                    RatingCount = customMap.RatingCount,
                    Downloads = customMap.Downloads,
                    About = map.About
                };
                await _helpfuncs.LogEventAsync(logModel);
                return Ok(customMapDataDTO);
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await _helpfuncs.LogModelChangeForServerError(logModel, ex);
                return BadRequest(new ResponseDTO(updatedLogModel.Message));
            }
        }


        /// <summary>
        /// Delete data of a custom map in db.
        /// </summary>
        /// <response code="200">Map was found and deleted. Returns message about completion</response>
        /// <response code="400">User ID (from token) conversion in int failed, recieved data is wrong, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found, map wasn't found. Returns message about error</response>
        /// <response code="500">Server error</response>
        [Route("CustomMap/{idModel:int}")]
        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> CustomMapDelete(int? idModel)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("CustomMapDelete", "Custom map deleted", nameof(CustomMapsController));
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
                (userIdCheckModel, logModel) = await _helpfuncs.UserIdCheck(requestId, "creator", logModel, parsedUserId, parsedPlayerId, parsedCreatorId);
                if (logModel.ErrorCode == "404") return NotFound(new ResponseDTO(logModel.Message));

                CustomMap? customMap = await _context.CustomMaps.FirstOrDefaultAsync(cmap => (cmap.MapId == idModel) && (cmap.CreatorId == userIdCheckModel.creatorId));
                Map? map = await _context.Maps.FirstOrDefaultAsync(map => map.Id == idModel);
                if (customMap == null || map == null)
                {
                    ResponseDTO responseDTO = await _helpfuncs.LogModelErrorInputAndLog(logModel, "Map wasn't found by mapId and userId", "404");
                    return NotFound(responseDTO);
                }
                _context.CustomMaps.Remove(customMap);
                _context.Maps.Remove(map);
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
        /// Change data of a custom map in db.
        /// </summary>
        /// <response code="200">Map was found and deleted. Returns message about completion</response>
        /// <response code="400">User ID (from token) conversion in int failed, received data is null, map name is empty, about field is too long, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found, map wasn't found. Returns message about error</response>
        /// <response code="500">Server error</response>
        [Route("CustomMap")]
        [Authorize]
        [HttpPut]
        public async Task<IActionResult> CustomMapPut([FromBody] CustomMapDataDTO? customMapData)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("CustomMapPut", "Custom map putted", nameof(CustomMapsController));
            try
            {
                if (customMapData == null)
                {
                    ResponseDTO responseDTO = await _helpfuncs.LogModelErrorInputAndLog(logModel, "Received data is null", "400");
                    return BadRequest(responseDTO);
                }

                if (customMapData.MapName == null || customMapData.MapName == "")
                {
                    ResponseDTO responseDTO = await _helpfuncs.LogModelErrorInputAndLog(logModel, "Name is empty", "400");
                    return BadRequest(responseDTO);
                }

                if (customMapData.About.Count() > 500)
                {
                    ResponseDTO responseDTO = await _helpfuncs.LogModelErrorInputAndLog(logModel, "About field is too long", "400");
                    return BadRequest(responseDTO);
                }

                var (result, parsedUserId, parsedPlayerId, parsedCreatorId) = await _helpfuncs.ValidateAndParseUserIdAsync(Request, logModel);
                if (result.ErrorCode == "401") return Unauthorized(new ResponseDTO(logModel.Message));
                if (result.LogLevel == "Error") return BadRequest(new ResponseDTO(logModel.Message));
                logModel.UserId = parsedUserId;

                UserIdCheckModel userIdCheckModel = new UserIdCheckModel();
                string requestId = Guid.NewGuid().ToString();
                (userIdCheckModel, logModel) = await _helpfuncs.UserIdCheck(requestId, "creator", logModel, parsedUserId, parsedPlayerId, parsedCreatorId);
                if (logModel.ErrorCode == "404") return NotFound(new ResponseDTO(logModel.Message));

                CustomMap? customMap = await _context.CustomMaps.FirstOrDefaultAsync(cmap => cmap.CreatorId == userIdCheckModel.creatorId && cmap.MapId == customMapData.MapID);
                if (customMap == null)
                {
                    ResponseDTO responseDTO = await _helpfuncs.LogModelErrorInputAndLog(logModel, "Map wasn't found by mapId and userId", "404");
                    return NotFound(responseDTO);
                }
                Map map = await _context.Maps.FirstOrDefaultAsync(map => map.Id == customMap.MapId); //Раньше тут также была проверка на сходство имени

                map.MapName = customMapData.MapName;
                map.BombCount = customMapData.BombCount;
                map.MapSize = customMapData.MapSize;
                map.MapType = (int)customMapData.MapType;
                map.About = customMapData.About;

                await _context.SaveChangesAsync();
                return Ok(new ResponseDTO(logModel.Message));
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await _helpfuncs.LogModelChangeForServerError(logModel, ex);
                return BadRequest(new ResponseDTO(updatedLogModel.Message));
            }
        }



        private async Task<(bool Success, IActionResult? Result)> CustomMapNullCheck(CustomMap? customMap, LogModel logModel)
        {
            if (customMap == null)
            {
                logModel.LogLevel = "Error";
                logModel.Message = "The custom map doesn't exists";
                logModel.ErrorCode = "404";
                await _helpfuncs.LogEventAsync(logModel);
                return (false, NotFound(new ResponseDTO(logModel.Message)));
            }
            return (true, null);
        }

        private async Task<(bool Success, IActionResult? Result)> CustomMapsInUserNullCheck(CustomMapsInUser? customMapsInUser, LogModel logModel)
        {
            if (customMapsInUser == null)
            {
                logModel.LogLevel = "Error";
                logModel.Message = "The customMap:user doesn't exists";
                logModel.ErrorCode = "404";
                await _helpfuncs.LogEventAsync(logModel);
                return (false, NotFound(new ResponseDTO(logModel.Message)));
            }
            return (true, null);
        }
    }
}