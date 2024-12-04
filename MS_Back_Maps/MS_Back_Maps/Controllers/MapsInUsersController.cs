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

namespace MS_Back_Maps.Controllers
{
    [ApiController]
    public class MapsInUsersController : ControllerBase
    {
        [Route("Progress")]
        [Authorize]
        [HttpPost]
        public IActionResult ProgressPost([FromBody] MapSaveModel mapSaveModel)
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

                MapsInUser? mapsInUser = context.MapsInUsers.FirstOrDefault(map => (map.Id == mapSaveModel.id) & (map.UserId.ToString() == userId));

                MapsInUser mapsInUserInput = new MapsInUser
                {
                    MapId = mapSaveModel.mapId,
                    UserId = int.Parse(userId),
                    GamesSum = mapSaveModel.gamesSum,
                    Wins = mapSaveModel.wins,
                    Loses = mapSaveModel.loses,
                    OpenedTiles = mapSaveModel.openedTiles,
                    OpenedNumberTiles = mapSaveModel.openedNumberTiles,
                    OpenedBlankTiles = mapSaveModel.openedBlankTiles,
                    FlagsSum = mapSaveModel.flagsSum,
                    FlagsOnBombs = mapSaveModel.flagsOnBombs,
                    TimeSpentSum = mapSaveModel.timeSpentSum,
                    AverageTime = mapSaveModel.wins / mapSaveModel.timeSpentSum,
                    LastGameData = mapSaveModel.lastGameData,
                    LastGameTime = mapSaveModel.lastGameTime
                };

                if (mapsInUser == null)
                {
                    context.MapsInUsers.Add(mapsInUserInput);
                }
                else
                {
                    mapsInUser = mapsInUserInput;
                }

                context.SaveChanges();
                return Ok("Данные внесены"); //логирование
            }
            catch
            {
                //залогировать ошибку
                return BadRequest("Произошла ошибка на сервере"); //дописать код ошибки чтобы найти по логам
            }
        }

        [Route("Progress")]
        [Authorize]
        [HttpGet]
        public IActionResult ProgressGet([FromBody] IdModel idmodel)
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

                MapsInUser? mapsInUser = context.MapsInUsers.FirstOrDefault(map => (map.Id == idmodel.id) & (map.UserId.ToString() == userId));

                Map map = context.Maps.FirstOrDefault(map => (map.Id == idmodel.id));
                if (map == null)
                    return NotFound("Такой карты не существует");

                if (mapsInUser == null)
                {
                    return NotFound("Соответствие пользователя и карты не найдено");
                }

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
                    averageTime = mapsInUser.AverageTime,
                    lastGameData = mapsInUser.LastGameData,
                    lastGameTime = mapsInUser.LastGameTime,
                };



                return Ok(mapSaveModel); //логирование
            }
            catch
            {
                //залогировать ошибку
                return BadRequest("Произошла ошибка на сервере"); //дописать код ошибки чтобы найти по логам
            }
        }

        [Route("SaveList")]
        [Authorize]
        [HttpPost]
        public IActionResult SaveListPost([FromBody] MapSaveListModel mapSaveListModel)
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

                if (mapSaveListModel.mapSaveList.IsNullOrEmpty())
                {
                    return BadRequest("прислан пустой список");
                }

                foreach ( MapSaveModel mapSaveModel in mapSaveListModel.mapSaveList )
                {
                    MapsInUser? mapsInUser = context.MapsInUsers.FirstOrDefault(map => (map.Id == mapSaveModel.id) & (map.UserId.ToString() == userId));

                    MapsInUser mapsInUserInput = new MapsInUser
                    {
                        MapId = mapSaveModel.mapId,
                        UserId = int.Parse(userId),
                        GamesSum = mapSaveModel.gamesSum,
                        Wins = mapSaveModel.wins,
                        Loses = mapSaveModel.loses,
                        OpenedTiles = mapSaveModel.openedTiles,
                        OpenedNumberTiles = mapSaveModel.openedNumberTiles,
                        OpenedBlankTiles = mapSaveModel.openedBlankTiles,
                        FlagsSum = mapSaveModel.flagsSum,
                        FlagsOnBombs = mapSaveModel.flagsOnBombs,
                        TimeSpentSum = mapSaveModel.timeSpentSum,
                        AverageTime = mapSaveModel.wins / mapSaveModel.timeSpentSum,
                        LastGameData = mapSaveModel.lastGameData,
                        LastGameTime = mapSaveModel.lastGameTime
                    };

                    if (mapsInUser == null)
                    {
                        context.MapsInUsers.Add(mapsInUserInput);
                    }
                    else
                    {
                        mapsInUser = mapsInUserInput;
                    }
                }

                context.SaveChanges();
                return Ok("Данные внесены"); //логирование
            }
            catch
            {
                //залогировать ошибку
                return BadRequest("Произошла ошибка на сервере"); //дописать код ошибки чтобы найти по логам
            }
        }

        [Route("SaveList")]
        [Authorize]
        [HttpGet]
        public IActionResult SaveListGet()
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

                List<MapsInUser> maps = context.MapsInUsers
                                   .Where(map => map.UserId.ToString() == userId)
                                   .ToList();
                if (maps.IsNullOrEmpty())
                    return BadRequest("нет сохранений");

                context.SaveChanges();
                return Ok("Данные внесены"); //логирование
            }
            catch
            {
                //залогировать ошибку
                return BadRequest("Произошла ошибка на сервере"); //дописать код ошибки чтобы найти по логам
            }
        }
    }
}
