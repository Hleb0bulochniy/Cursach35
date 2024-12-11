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
        public async Task<IActionResult> CustomMapPost([FromBody] CustomMapData? customMapData)
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
                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await _helpfuncs.UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

                CustomMap? mapCheck = _context.CustomMaps.FirstOrDefault(u => u.MapName == customMapData.mapName && u.CreatorId == customMapData.creatorId);
                if (mapCheck != null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "This map already exists";
                    logModel.errorCode = "400";
                    await _helpfuncs.LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }

                MapsContext context = new MapsContext();
                if (customMapData == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "New data in body is null";
                    logModel.errorCode = "404";
                    await _helpfuncs.LogEventAsync(logModel);
                    return NotFound(logModel.message);
                }
                CustomMap customMap = new CustomMap
                {
                    MapName = customMapData.mapName,
                    BombCount = customMapData.bombCount,
                    MapSize = customMapData.mapSize,
                    MapType = (int)customMapData.mapType,
                    CreatorId = customMapData.creatorId,
                    CreationDate = customMapData.creationDate,
                    RatingSum = customMapData.ratingSum,
                    RatingCount = customMapData.ratingCount,
                    Downloads = customMapData.downloads,
                    About = customMapData.about
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
                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => cmap.Id == idModel);
                if (customMap == null)
                {
                    logModel.logLevel="Error";
                    logModel.message = "Map was not found by MapId";
                    logModel.errorCode = "404";
                    return NotFound(logModel.message);
                }
                string requestId = Guid.NewGuid().ToString();
                UserIdCheckModel userIdCheckModel = new UserIdCheckModel
                {
                    requestId = requestId,
                    userId = customMap.CreatorId
                };

                _helpfuncs.UserIdCheckEventAsync(userIdCheckModel);
                var response = await _producerService.WaitForKafkaResponseAsync(requestId, "UserIdCheckResponce", TimeSpan.FromSeconds(10));
                if (response == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Map creator does not exist";
                    logModel.errorCode = "404";
                    await _helpfuncs.LogEventAsync(logModel);
                }

                CustomMapData customMapData = new CustomMapData
                {
                    mapID = customMap.Id,
                    mapName = customMap.MapName,
                    bombCount = customMap.BombCount,
                    mapSize = customMap.MapSize,
                    mapType = (CustomMapType)customMap.MapType,
                    creatorId = customMap.CreatorId,
                    creatorName = response.userName,
                    creationDate = customMap.CreationDate,
                    ratingSum = customMap.RatingSum,
                    ratingCount = customMap.RatingCount,
                    downloads = customMap.Downloads,
                    about = customMap.About
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
                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await _helpfuncs.UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => (cmap.Id == idModel) && (cmap.CreatorId == parsedUserId));
                if (customMap == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Map wasn't found by mapId and userId";
                    logModel.errorCode = "404";
                    return NotFound(logModel.message);
                }
                _context.CustomMaps.Remove(customMap);
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

                var (success, result, parsedUserId) = await ValidateAndParseUserIdAsync(Request, logModel);
                if (!success) return result!;
                logModel.userId = parsedUserId;

                string requestId = Guid.NewGuid().ToString();
                logModel = await _helpfuncs.UserIdCheck(requestId, parsedUserId, logModel);
                if (logModel.errorCode == "404") return NotFound(logModel.message);

                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => ((cmap.MapName == customMapData.mapName) && (cmap.CreatorId == parsedUserId))); //стоит ли тут делать поиск по имени карты или id
                if (customMap == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Map wasn't found by mapId and userId";
                    logModel.errorCode = "404";
                    return NotFound(logModel.message);
                }

                customMap.MapName = customMapData.mapName;
                customMap.BombCount = customMapData.bombCount;
                customMap.MapSize = customMapData.mapSize;
                customMap.MapType = (int)customMapData.mapType;
                customMap.About = customMapData.about;

                await _context.SaveChangesAsync();
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
