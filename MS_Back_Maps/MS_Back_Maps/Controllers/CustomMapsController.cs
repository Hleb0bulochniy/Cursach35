using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MS_Back_Maps.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MS_Back_Maps.Data;
using Azure.Core;
using System.Text.Json;

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
        /// <response code="400">User ID (from token) conversion in int failed, received data is null, this map already exists, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found. Returns message about error</response>
        [Route("CustomMap")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CustomMapPost([FromBody] CustomMapData? customMapData)//путаются maptype и mapsize
        {
            LogModel logModel = _helpfuncs.LogModelCreate("CustomMapPost", "Custom map was added");
            try
            {
                if (customMapData == null)
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
                (userIdCheckModel, logModel) = await _helpfuncs.UserIdCheck(requestId, "creator", logModel, parsedUserId, parsedPlayerId, parsedCreatorId);
                if (logModel.errorCode == "404") return NotFound(logModel.message);
                
                Map? mapCheck2 = _context.Maps.FirstOrDefault(u => u.MapName == customMapData.mapName && u.About == customMapData.about);
                
                if (mapCheck2 != null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "This map already exists";
                    logModel.errorCode = "400";
                    await _helpfuncs.LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }
                
                Map map = new Map
                {
                    MapName = customMapData.mapName,
                    BombCount = customMapData.bombCount,
                    MapSize = customMapData.mapSize,
                    MapType = (int)customMapData.mapType,
                    IsCustom = true,
                    About = customMapData.about,
                };
                
                await _context.Maps.AddAsync(map);
                await _context.SaveChangesAsync();


                CustomMap? mapCheck = _context.CustomMaps.FirstOrDefault(u => u.MapId == map.Id && u.CreatorId == userIdCheckModel.creatorId);

                if (mapCheck2 != null)
                {

                    logModel.logLevel = "Error";
                    logModel.message = "This customMap already exists";
                    logModel.errorCode = "400";
                    await _helpfuncs.LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }

                CustomMap customMap = new CustomMap
                {
                    MapId = map.Id,
                    CreatorId = (int)userIdCheckModel.creatorId,
                    CreationDate = customMapData.creationDate,
                    RatingSum = customMapData.ratingSum,
                    RatingCount = customMapData.ratingCount,
                    Downloads = customMapData.downloads,
                };

                await _context.CustomMaps.AddAsync(customMap);
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
        /// Get data of custom map from db.
        /// </summary>
        /// <response code="200">Map was found and sent. Returns json with custom map data</response>
        /// <response code="400">Recieved data is wrong, other error (watch Logs). Returns message about error</response>
        /// <response code="404">Map wasn't found, map creator wasn't found. Returns message about error</response>
        [Route("CustomMap/{idModel:int}")]
        [HttpGet]
        public async Task<IActionResult> CustomMapGet(int? idModel)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("CustomMapGet", "Custom map gotten");
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
                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => cmap.MapId == idModel);
                Map? map = _context.Maps.FirstOrDefault(map => map.Id == idModel);

                if (customMap == null || map == null)
                {
                    logModel.logLevel="Error";
                    logModel.message = "Map was not found by MapId";
                    logModel.errorCode = "404";
                    return NotFound(logModel.message);
                }

                UserIdCheckModel userIdCheckModel = new UserIdCheckModel();
                string requestId = Guid.NewGuid().ToString();
                (userIdCheckModel, logModel) = await _helpfuncs.UserIdCheck(requestId, "", logModel, null, null, customMap.CreatorId);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

                CustomMapData customMapData = new CustomMapData 
                {
                    mapID = map.Id,
                    mapName = map.MapName,
                    bombCount = map.BombCount,
                    mapSize = map.MapSize,
                    mapType = (CustomMapType)map.MapType,
                    creatorId = customMap.CreatorId,
                    creatorName = userIdCheckModel.userName,
                    creationDate = customMap.CreationDate,
                    ratingSum = customMap.RatingSum,
                    ratingCount = customMap.RatingCount,
                    downloads = customMap.Downloads,
                    about = map.About
                };
                await _helpfuncs.LogEventAsync(logModel);
                return Ok(customMapData);
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await _helpfuncs.LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }


        /// <summary>
        /// Delete data of a custom map in db.
        /// </summary>
        /// <response code="200">Map was found and deleted. Returns message about completion</response>
        /// <response code="400">User ID (from token) conversion in int failed, recieved data is wrong, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found, map wasn't found. Returns message about error</response>
        [Route("CustomMap/{idModel:int}")]
        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> CustomMapDelete(int? idModel)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("CustomMapDelete", "Custom map deleted");
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
                (userIdCheckModel, logModel) = await _helpfuncs.UserIdCheck(requestId, "creator", logModel, parsedUserId, parsedPlayerId, parsedCreatorId);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => (cmap.MapId == idModel) && (cmap.CreatorId == userIdCheckModel.creatorId));
                Map? map = _context.Maps.FirstOrDefault(map => map.Id == idModel);
                if (customMap == null || map == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Map wasn't found by mapId and userId";
                    logModel.errorCode = "404";
                    return NotFound(logModel.message);
                }
                _context.CustomMaps.Remove(customMap);
                _context.Maps.Remove(map);
                await _context.SaveChangesAsync();
                return Ok(logModel.message);
            }
            catch (Exception ex)
            {
                LogModel updatedLogModel = await _helpfuncs.LogModelChangeForServerError(logModel, ex);
                return BadRequest(updatedLogModel.message);
            }
        }


        /// <summary>
        /// Change data of a custom map in db.
        /// </summary>
        /// <response code="200">Map was found and deleted. Returns message about completion</response>
        /// <response code="400">User ID (from token) conversion in int failed, received data is null, other error (watch Logs). Returns message about error</response>
        /// <response code="401">Invalid or missing token. Returns message about error</response>
        /// <response code="404">User wasn't found, map wasn't found. Returns message about error</response>
        [Route("CustomMap")]
        [Authorize]
        [HttpPut]
        public async Task<IActionResult> CustomMapPut([FromBody] CustomMapData? customMapData)
        {
            LogModel logModel = _helpfuncs.LogModelCreate("CustomMapPut", "Custom map putted");
            try
            {
                if (customMapData == null)
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
                (userIdCheckModel, logModel) = await _helpfuncs.UserIdCheck(requestId, "creator", logModel, parsedUserId, parsedPlayerId, parsedCreatorId);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => cmap.CreatorId == userIdCheckModel.creatorId && cmap.MapId == customMapData.mapID);
                if (customMap == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Map wasn't found by mapId and userId";
                    logModel.errorCode = "404";
                    return NotFound(logModel.message);
                }
                Map map = _context.Maps.FirstOrDefault(map => map.Id == customMap.MapId && map.MapName == customMapData.mapName);

                map.MapName = customMapData.mapName;
                map.BombCount = customMapData.bombCount;
                map.MapSize = customMapData.mapSize;
                map.MapType = (int)customMapData.mapType;
                map.About = customMapData.about;

                await _context.SaveChangesAsync();
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