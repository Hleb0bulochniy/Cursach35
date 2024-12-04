using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MS_Back_Maps.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MS_Back_Maps.Data;

namespace MS_Back_Maps.Controllers
{
    [ApiController]
    public class CustomMapsController : ControllerBase
    {
        [Route("CustomMap")]
        [Authorize]
        [HttpPost]
        public IActionResult CustomMapPost([FromBody] CustomMapData customMapData)
        {
            try
            {
                string? authorizationHeader = Request.Headers["Authorization"];
                if (authorizationHeader == null) 
                    return Unauthorized("Токен авторизации отсутствует");
                string token = authorizationHeader!.Replace("Bearer ", "");
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                    return Unauthorized("Невалидный токен");
                var jwtToken = handler.ReadJwtToken(token);
                string userId = jwtToken.Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier).Value;
                //Проверить, существует ли такой пользователь в Auth и залогировать

                MapsContext context = new MapsContext(); //карты с одинаковыми названиями могут существовать
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
                //проверить, существует ли уже карта
                context.CustomMaps.Add(customMap);
                context.SaveChanges();
                return Ok("Данные загружены на сервер"); //логирование
            }
            catch
            {
                //залогировать ошибку
                return BadRequest("Произошла ошибка на сервере"); //дописать код ошибки чтобы найти по логам
            }
        }
        [Route("CustomMap")]
        [HttpGet]
        public IActionResult CustomMapGet([FromBody] IdModel idModel)
        {
            try
            {
                if (idModel.id <= 0)
                    return BadRequest("ID меньше нуля");
                MapsContext context = new MapsContext();
                CustomMap? customMap = context.CustomMaps.SingleOrDefault(cmap => cmap.Id == int.Parse(idModel.id.ToString()));
                if (customMap == null)
                    return NotFound("Карта с указанным ID не найдена");

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
                return Ok(customMapData); //логирование
            }
            catch (Exception ex)
            {
                //залогировать ошибку
                return BadRequest("Произошла ошибка на сервере"); //дописать код ошибки чтобы найти по логам
            }
        }
        [Route("CustomMap")]
        [Authorize]
        [HttpDelete]
        public IActionResult CustomMapDelete([FromBody] IdModel idModel)
        {
            try
            {
                string? authorizationHeader = Request.Headers["Authorization"];
                if (authorizationHeader == null) return Unauthorized("Токен авторизации отсутствует");
                string token = authorizationHeader!.Replace("Bearer ", "");
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                    return Unauthorized("Невалидный токен");
                var jwtToken = handler.ReadJwtToken(token);
                string userId = jwtToken.Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier).Value;
                //Проверить, существует ли такой пользователь в Auth и залогировать

                MapsContext context = new MapsContext(); //карты с одинаковыми названиями могут существовать
                CustomMap? customMap = context.CustomMaps.FirstOrDefault(cmap => ((cmap.Id == idModel.id) & (cmap.CreatorId.ToString() == userId)));
                if (customMap == null) 
                    return NotFound("Карта с указанным ID не найдена");
                context.CustomMaps.Remove(customMap);
                context.SaveChanges();
                return Ok("Карта удалена"); //логирование
            }
            catch
            {
                //залогировать ошибку
                return BadRequest("Произошла ошибка на сервере"); //дописать код ошибки чтобы найти по логам
            }
        }
        [Route("CustomMap")]
        [Authorize]
        [HttpPut]
        public IActionResult CustomMapPut([FromBody] CustomMapData customMapData)
        {
            try
            {
                string? authorizationHeader = Request.Headers["Authorization"];
                if (authorizationHeader == null) return Unauthorized("Токен авторизации отсутствует");
                string token = authorizationHeader!.Replace("Bearer ", "");
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                    return Unauthorized("Невалидный токен");
                var jwtToken = handler.ReadJwtToken(token);
                string userId = jwtToken.Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier).Value;
                //Проверить, существует ли такой пользователь в Auth и залогировать

                MapsContext context = new MapsContext(); //карты с одинаковыми названиями могут существовать
                CustomMap? customMap = context.CustomMaps.FirstOrDefault(cmap => ((cmap.Id == customMapData.mapID) & (cmap.CreatorId.ToString() == userId)));
                if (customMap == null)
                    return NotFound("Карта с указанным ID не найдена");
                
                customMap.MapName = customMapData.mapName;
                customMap.BombCount = customMapData.bombCount;
                customMap.MapSize = customMapData.mapSize;
                customMap.MapType = (int)customMapData.mapType;
                customMap.Downloads = customMapData.downloads;

                context.SaveChanges();
                return Ok("Информация о карте обновлена"); //логирование
            }
            catch
            {
                //залогировать ошибку
                return BadRequest("Произошла ошибка на сервере"); //дописать код ошибки чтобы найти по логам
            }
        }
        
    }
}
