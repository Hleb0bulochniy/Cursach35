using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MS_Back_Maps.Data;
using MS_Back_Maps.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MS_Back_Maps.Controllers
{
    [ApiController]
    public class CustomMapsInUsersController : ControllerBase
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

                CustomMapsInUser? customMapsInUser = context.CustomMapsInUsers.FirstOrDefault(map => (map.Id == mapSaveModel.id) & (map.UserId.ToString() == userId));

                CustomMapsInUser CustomMapsInUserInput = new CustomMapsInUser
                {
                    CustomMapId = mapSaveModel.mapId,
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

                if (customMapsInUser == null)
                {
                    CustomMapsInUserInput.IsAdded = true;
                    CustomMapsInUserInput.IsFavourite = false;
                    CustomMapsInUserInput.Rate = 0;
                    context.CustomMapsInUsers.Add(CustomMapsInUserInput);
                }
                else
                {
                    customMapsInUser = CustomMapsInUserInput;
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

                CustomMapsInUser? customMapsInUser = context.CustomMapsInUsers.FirstOrDefault(map => (map.Id == idmodel.id) & (map.UserId.ToString() == userId));

                CustomMap customMap = context.CustomMaps.FirstOrDefault(cmap => (cmap.Id == idmodel.id));
                if (customMap == null)
                    return NotFound("Такой карты не существует");

                if (customMapsInUser == null)
                {
                    return NotFound("Соответствие пользователя и карты не найдено");
                }

                MapSaveModel mapSaveModel = new MapSaveModel
                {
                    id = customMapsInUser.Id,
                    mapId = customMapsInUser.CustomMapId,
                    mapName = customMap.MapName,
                    gamesSum = customMapsInUser.GamesSum,
                    wins = customMapsInUser.Wins,
                    loses = customMapsInUser.Loses,
                    openedTiles = customMapsInUser.OpenedTiles,
                    openedNumberTiles = customMapsInUser.OpenedNumberTiles,
                    openedBlankTiles = customMapsInUser.OpenedBlankTiles,
                    flagsSum = customMapsInUser.FlagsSum,
                    flagsOnBombs = customMapsInUser.FlagsOnBombs,
                    averageTime = customMapsInUser.AverageTime,
                    lastGameData = customMapsInUser.LastGameData,
                    lastGameTime = customMapsInUser.LastGameTime,
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

                foreach (MapSaveModel mapSaveModel in mapSaveListModel.mapSaveList)
                {
                    CustomMapsInUser? customMapsInUser = context.CustomMapsInUsers.FirstOrDefault(map => (map.Id == mapSaveModel.id) & (map.UserId.ToString() == userId));

                    CustomMapsInUser customMapsInUserInput = new CustomMapsInUser
                    {
                        CustomMapId = mapSaveModel.mapId,
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

                    if (customMapsInUser == null)
                    {
                        customMapsInUserInput.IsAdded = true;
                        customMapsInUserInput.IsFavourite = false;
                        customMapsInUserInput.Rate = 0;
                        context.CustomMapsInUsers.Add(customMapsInUserInput);
                    }
                    else
                    {
                        customMapsInUser = customMapsInUserInput;
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

                List<CustomMapsInUser> maps = context.CustomMapsInUsers
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

        [Route("Rate")]
        [Authorize]
        [HttpPost]
        public IActionResult RatePost([FromBody] RateMap rateMap)
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
                CustomMap? customMap = context.CustomMaps.FirstOrDefault(cmap => ((cmap.Id == rateMap.mapId)));
                if (customMap == null)
                    return NotFound("Карта с указанным ID не найдена");

                CustomMapsInUser? customMapsInUser = context.CustomMapsInUsers.FirstOrDefault(cmap => (cmap.Id == rateMap.mapId) & (cmap.UserId.ToString() == userId));

                if (customMapsInUser == null)
                    return NotFound("Нет соответствия пользователя и кстомной карты");
                if (customMapsInUser.Rate != 0)
                    return BadRequest("Оценка уже стоит");

                customMapsInUser.Rate = rateMap.newRate;
                customMap.RatingSum += rateMap.newRate;
                customMap.RatingCount += 1;

                context.SaveChanges();
                return Ok("Оценка добавлена"); //логирование
            }
            catch
            {
                //залогировать ошибку
                return BadRequest("Произошла ошибка на сервере"); //дописать код ошибки чтобы найти по логам
            }
        }

        [Route("Rate")]
        [Authorize]
        [HttpPut]
        public IActionResult RatePut([FromBody] RateMap rateMap)
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
                CustomMap? customMap = context.CustomMaps.FirstOrDefault(cmap => ((cmap.Id == rateMap.mapId)));
                if (customMap == null)
                    return NotFound("Карта с указанным ID не найдена");

                CustomMapsInUser? customMapsInUser = context.CustomMapsInUsers.FirstOrDefault(cmap => (cmap.Id == rateMap.mapId) & (cmap.UserId.ToString() == userId));

                if (customMapsInUser == null)
                    return NotFound("Нет соответствия пользователя и кстомной карты");
                if (customMapsInUser.Rate == 0)
                    return BadRequest("Оценка еще не стоит");

                customMap.RatingSum -= customMapsInUser.Rate;
                customMapsInUser.Rate = rateMap.newRate;
                customMap.RatingSum += rateMap.newRate;

                context.SaveChanges();
                return Ok("Оценка изменена"); //логирование
            }
            catch
            {
                //залогировать ошибку
                return BadRequest("Произошла ошибка на сервере"); //дописать код ошибки чтобы найти по логам
            }
        }

        [Route("Rate")]
        [Authorize]
        [HttpDelete]
        public IActionResult RateDelete([FromBody] IdModel idmodel)
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
                CustomMap? customMap = context.CustomMaps.FirstOrDefault(cmap => ((cmap.Id == idmodel.id)));
                if (customMap == null)
                    return NotFound("Карта с указанным ID не найдена");

                CustomMapsInUser? customMapsInUser = context.CustomMapsInUsers.FirstOrDefault(cmap => (cmap.Id == idmodel.id) & (cmap.UserId.ToString() == userId));

                if (customMapsInUser == null)
                    return NotFound("Нет соответствия пользователя и кстомной карты");
                if (customMapsInUser.Rate == 0)
                    return BadRequest("Оценка итак не стоит");

                customMap.RatingSum -= customMapsInUser.Rate;
                customMapsInUser.Rate = 0;

                context.SaveChanges();
                return Ok("Оценка изменена"); //логирование
            }
            catch
            {
                //залогировать ошибку
                return BadRequest("Произошла ошибка на сервере"); //дописать код ошибки чтобы найти по логам
            }
        }

        [Route("Download")]
        [Authorize]
        [HttpPost]
        public IActionResult DownLoadPost([FromBody] IdModel idmodel)
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
                CustomMap? customMap = context.CustomMaps.FirstOrDefault(cmap => ((cmap.Id == idmodel.id)));
                if (customMap == null)
                    return NotFound("Карта с указанным ID не найдена");

                CustomMapsInUser? customMapsInUser = context.CustomMapsInUsers.FirstOrDefault(cmap => (cmap.Id == idmodel.id) & (cmap.UserId.ToString() == userId));
                if (customMapsInUser != null)
                {
                    customMapsInUser.IsAdded = true;
                    return Ok("Соответствие пользователя и кастомной карты уже существует");
                }

                CustomMapsInUser customMapsInUserNew = new CustomMapsInUser
                {
                    UserId = int.Parse(userId),
                    CustomMapId = customMap.Id,
                    GamesSum = 0,
                    Wins = 0,
                    Loses = 0,
                    OpenedTiles = 0,
                    OpenedNumberTiles = 0,
                    OpenedBlankTiles = 0,
                    FlagsSum = 0,
                    FlagsOnBombs = 0,
                    TimeSpentSum = 0,
                    AverageTime = 0,
                    LastGameData = "",
                    LastGameTime = 0,
                    IsAdded = true,
                    IsFavourite = false,
                    Rate = 0
                };

                customMap.Downloads += 1;
                context.CustomMapsInUsers.Add(customMapsInUserNew);

                context.SaveChanges();
                return Ok("Карта скачана"); //логирование
            }
            catch
            {
                //залогировать ошибку
                return BadRequest("Произошла ошибка на сервере"); //дописать код ошибки чтобы найти по логам
            }
        }

        [Route("Download")]
        [Authorize]
        [HttpDelete]
        public IActionResult DownLoadDelete([FromBody] IdModel idmodel)
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
                CustomMap? customMap = context.CustomMaps.FirstOrDefault(cmap => ((cmap.Id == idmodel.id)));
                if (customMap == null)
                    return NotFound("Карта с указанным ID не найдена");

                CustomMapsInUser? customMapsInUser = context.CustomMapsInUsers.FirstOrDefault(cmap => (cmap.Id == idmodel.id) & (cmap.UserId.ToString() == userId));
                if (customMapsInUser == null)
                {
                    return BadRequest("Соответствия пользователя и кастомной карты не существует");
                }


                customMap.Downloads -= 1;
                customMapsInUser.IsAdded = false;

                context.SaveChanges();
                return Ok("Карта удалена"); //логирование
            }
            catch
            {
                //залогировать ошибку
                return BadRequest("Произошла ошибка на сервере"); //дописать код ошибки чтобы найти по логам
            }
        }

        

        [Route("Favourite")]
        [Authorize]
        [HttpPost]
        public IActionResult FavouritePost([FromBody] IdModel idmodel)
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
                CustomMap? customMap = context.CustomMaps.FirstOrDefault(cmap => ((cmap.Id == idmodel.id)));
                if (customMap == null)
                    return NotFound("Карта с указанным ID не найдена");

                CustomMapsInUser? customMapsInUser = context.CustomMapsInUsers.FirstOrDefault(cmap => (cmap.Id == idmodel.id) & (cmap.UserId.ToString() == userId));
                if (customMapsInUser == null)
                {
                    return BadRequest("Соответствия пользователя и кастомной карты не существует");
                }


                customMapsInUser.IsFavourite = true;

                context.SaveChanges();
                return Ok("Карта добавлена в избранное"); //логирование
            }
            catch
            {
                //залогировать ошибку
                return BadRequest("Произошла ошибка на сервере"); //дописать код ошибки чтобы найти по логам
            }
        }

        [Route("Favourite")]
        [Authorize]
        [HttpDelete]
        public IActionResult FavouriteDelete([FromBody] IdModel idmodel)
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
                CustomMap? customMap = context.CustomMaps.FirstOrDefault(cmap => ((cmap.Id == idmodel.id)));
                if (customMap == null)
                    return NotFound("Карта с указанным ID не найдена");

                CustomMapsInUser? customMapsInUser = context.CustomMapsInUsers.FirstOrDefault(cmap => (cmap.Id == idmodel.id) & (cmap.UserId.ToString() == userId));
                if (customMapsInUser == null)
                {
                    return BadRequest("Соответствия пользователя и кастомной карты не существует");
                }


                customMapsInUser.IsFavourite = false;

                context.SaveChanges();
                return Ok("Карта добавлена в избранное"); //логирование
            }
            catch
            {
                //залогировать ошибку
                return BadRequest("Произошла ошибка на сервере"); //дописать код ошибки чтобы найти по логам
            }
        }
    }
}
