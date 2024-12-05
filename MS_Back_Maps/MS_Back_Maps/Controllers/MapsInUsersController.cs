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
        private readonly HelpFuncs _helpfuncs;

        public MapsInUsersController(HelpFuncs helpfuncs)
        {
            _helpfuncs = helpfuncs;
        }
        [Route("Progress")]
        [Authorize]
        [HttpPost]
        public IActionResult ProgressPost([FromBody] MapSaveModel mapSaveModel)
        {
            try
            {
                string? userId = _helpfuncs.GetUserIdFromToken(Request);
                if (userId == null)
                {
                    return Unauthorized("Невалидный или отсутствующий токен");
                }
                //Проверить, существует ли такой пользователь в Auth и залогировать


                MapsContext context = new MapsContext(); //карты с одинаковыми названиями могут существовать

                MapsInUser? mapsInUser = context.MapsInUsers.FirstOrDefault(map => (map.Id == mapSaveModel.id) && (map.UserId.ToString() == userId));

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
                    AverageTime = mapSaveModel.timeSpentSum > 0 ? mapSaveModel.wins / mapSaveModel.timeSpentSum : 0,
                    LastGameData = mapSaveModel.lastGameData,
                    LastGameTime = mapSaveModel.lastGameTime
                };

                if (mapsInUser == null)
                {
                    context.MapsInUsers.Add(mapsInUserInput);
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
                    mapsInUser.AverageTime = mapSaveModel.timeSpentSum > 0 ? mapSaveModel.wins / mapSaveModel.timeSpentSum : 0;
                    mapsInUser.LastGameData = mapSaveModel.lastGameData;
                    mapsInUser.LastGameTime = mapSaveModel.lastGameTime;
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
                string? userId = _helpfuncs.GetUserIdFromToken(Request);
                if (userId == null)
                {
                    return Unauthorized("Невалидный или отсутствующий токен");
                }
                //Проверить, существует ли такой пользователь в Auth и залогировать


                MapsContext context = new MapsContext(); //карты с одинаковыми названиями могут существовать

                MapsInUser? mapsInUser = context.MapsInUsers.FirstOrDefault(map => (map.Id == idmodel.id) && (map.UserId.ToString() == userId));

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
                string? userId = _helpfuncs.GetUserIdFromToken(Request);
                if (userId == null)
                {
                    return Unauthorized("Невалидный или отсутствующий токен");
                }
                //Проверить, существует ли такой пользователь в Auth и залогировать


                MapsContext context = new MapsContext(); //карты с одинаковыми названиями могут существовать

                if (mapSaveListModel.mapSaveList == null || !mapSaveListModel.mapSaveList.Any())
                {
                    return BadRequest("Прислан пустой список");
                }

                foreach ( MapSaveModel mapSaveModel in mapSaveListModel.mapSaveList )
                {
                    MapsInUser? mapsInUser = context.MapsInUsers.FirstOrDefault(map => (map.Id == mapSaveModel.id) && (map.UserId.ToString() == userId));

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
                        AverageTime = mapSaveModel.timeSpentSum > 0 ? mapSaveModel.wins / mapSaveModel.timeSpentSum : 0,
                        LastGameData = mapSaveModel.lastGameData,
                        LastGameTime = mapSaveModel.lastGameTime
                    };

                    if (mapsInUser == null)
                    {
                        context.MapsInUsers.Add(mapsInUserInput);
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
                        mapsInUser.AverageTime = mapSaveModel.timeSpentSum > 0 ? mapSaveModel.wins / mapSaveModel.timeSpentSum : 0;
                        mapsInUser.LastGameData = mapSaveModel.lastGameData;
                        mapsInUser.LastGameTime = mapSaveModel.lastGameTime;
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
                string? userId = _helpfuncs.GetUserIdFromToken(Request);
                if (userId == null)
                {
                    return Unauthorized("Невалидный или отсутствующий токен");
                }
                //Проверить, существует ли такой пользователь в Auth и залогировать


                MapsContext context = new MapsContext(); //карты с одинаковыми названиями могут существовать

                List<MapsInUser> maps = context.MapsInUsers
                                   .Where(map => map.UserId.ToString() == userId)
                                   .ToList();
                if (maps.Count() == 0)
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
