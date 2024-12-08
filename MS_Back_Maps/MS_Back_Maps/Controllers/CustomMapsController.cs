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
        [Route("CustomMap")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CustomMapPost([FromBody] CustomMapData customMapData)
        {
            LogModel logModel = new LogModel
            {
                userId = -1,
                dateTime = DateTime.UtcNow,
                serviceName = "CustomMapsController",
                logLevel = "Info",
                eventType = "CustomMapPost",
                message = "Custom map was added",
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

                MapsContext context = new MapsContext();
                if (customMapData == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "New data in body is null";
                    logModel.errorCode = "404";
                    await LogEventAsync(logModel);
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
        [Route("CustomMap/{idModel:int}")]
        [HttpGet]
        public async Task<IActionResult> CustomMapGet(int idModel)
        {
            LogModel logModel = new LogModel
            {
                userId = -1,
                dateTime = DateTime.UtcNow,
                serviceName = "CustomMapsController",
                logLevel = "Info",
                eventType = "CustomMapGet",
                message = "Custom map gotten",
                details = "",
                errorCode = "200"
            };
            try
            {
                if (idModel <= 0)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "Requested mapId is less than 0";
                    logModel.errorCode = "400";
                    await LogEventAsync(logModel);
                    return BadRequest(logModel.message);
                }
                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => cmap.Id == idModel);
                if (customMap == null)
                {
                    logModel.logLevel="Error";
                    logModel.message = "Map was not found by MapId";
                    logModel.errorCode = "400";
                    return NotFound(logModel.message);
                }

                CustomMapData customMapData = new CustomMapData
                {
                    mapID = customMap.Id,
                    mapName = customMap.MapName,
                    bombCount = customMap.BombCount,
                    mapSize = customMap.MapSize,
                    mapType = (CustomMapType)customMap.MapType,
                    creatorId = customMap.CreatorId,
                    //creatorName = customMap.//тут будет межсервисное взаимодействие
                    creationDate = customMap.CreationDate,
                    ratingSum = customMap.RatingSum,
                    ratingCount = customMap.RatingCount,
                    downloads = customMap.Downloads,
                    about = customMap.About
                };
                await LogEventAsync(logModel);
                return Ok(customMapData);
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
        [Route("CustomMap/{idModel:int}")]
        [Authorize]
        [HttpDelete]
        public async Task<IActionResult> CustomMapDelete(int idModel)
        {
            LogModel logModel = new LogModel
            {
                userId = -1,
                dateTime = DateTime.UtcNow,
                serviceName = "CustomMapsController",
                logLevel = "Info",
                eventType = "CustomMapDelete",
                message = "Custom map deleted",
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
                logModel.eventType = "Error";
                logModel.message = "Server error";
                logModel.details = ex.Message;
                logModel.errorCode = "500";
                await LogEventAsync(logModel);
                return BadRequest(logModel.message);
            }
        }
        [Route("CustomMap")]
        [Authorize]
        [HttpPut]
        public async Task<IActionResult> CustomMapPut([FromBody] CustomMapData customMapData)
        {
            LogModel logModel = new LogModel
            {
                userId = -1,
                dateTime = DateTime.UtcNow,
                serviceName = "CustomMapsController",
                logLevel = "Info",
                eventType = "CustomMapPut",
                message = "Custom map putted",
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

                if (customMapData == null)
                {
                    logModel.logLevel = "Error";
                    logModel.message = "New data in body is null";
                    logModel.errorCode = "404";
                    await LogEventAsync(logModel);
                    return NotFound(logModel.message);
                }
                CustomMap? customMap = _context.CustomMaps.FirstOrDefault(cmap => ((cmap.Id == customMapData.mapID) && (cmap.CreatorId == parsedUserId)));
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
                customMap.Downloads = customMapData.downloads;

                await _context.SaveChangesAsync();
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
