using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MS_Back_Maps.Models;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Cryptography;

namespace MS_Back_Maps.Controllers
{
    [ApiController]
    public class MapsInUsersController : ControllerBase
    {
        [Route("Input")]
        [Authorize]
        [HttpPost]
        public IActionResult Input([FromBody] GameData data)
        {
            string? authorizationHeader = Request.Headers["Authorization"];
            string token = authorizationHeader!.Replace("Bearer ", "");
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            string userId = jwtToken.Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier).Value;
            if (data.userID != 0) userId = data.userID.ToString();

            UserContext context = new UserContext();
            User? user = context.Users.SingleOrDefault(u => u.Id == int.Parse(userId));
            if (user == null) return BadRequest("Пользователь не обнаружен"); //логирование
            user.Username = data.username;
            user.Email = data.email;
            if (data.password != "")
            {
                string cryptedPassword = Cryptography.ConvertPassword(data.password);
                user.Password = cryptedPassword;
            }
            user.LastGameType = (int)data.lastGameType;
            user.LastGameSize = (int)data.lastGameSize;
            user.LastGameData = data.lastGameData;
            user.LastGameTime = data.lastGameTime;
            user.updateDate = data.updateDate; //вместо всего этого можно добавить галочку мол это последняя игра

            for (int i = 0; i < data.mapDataList.Count; i++)
            {
                MapsInUser? map2 = context.MapsInUsers.SingleOrDefault(u => (u.UserId == int.Parse(userId)) & (u.MapId == data.mapDataList[i].mapID));
                if (map2 == null)
                {
                    MapsInUser mapInUser = new MapsInUser();
                    Map? map = context.Maps.SingleOrDefault(m => m.Id == data.mapDataList[i].mapID);
                    if (map is Map)
                    {
                        mapInUser.MapId = map.Id;
                        mapInUser.UserId = int.Parse(userId);
                        context.MapsInUsers.Add(mapInUser);
                        context.SaveChanges();
                    }
                    else return BadRequest("No map"); //логирование
                }

                MapsInUser mapsInUser = context.MapsInUsers.SingleOrDefault(u => (u.UserId == int.Parse(userId)) & (u.MapId == data.mapDataList[i].mapID))!;
                mapsInUser.GamesSum = data.mapDataList[i].gamesSum;
                mapsInUser.Wins = data.mapDataList[i].wins;
                mapsInUser.Loses = data.mapDataList[i].loses;
                mapsInUser.OpenedTiles = data.mapDataList[i].openedTiles;
                mapsInUser.OpenedNumberTiles = data.mapDataList[i].openedNumberTiles;
                mapsInUser.OpenedBlankTiles = data.mapDataList[i].openedBlankTiles;
                mapsInUser.FlagsSum = data.mapDataList[i].flagsSum;
                mapsInUser.FlagsOnBombs = data.mapDataList[i].flagsOnBombs;
                mapsInUser.TimeSpentSum = data.mapDataList[i].timeSpentSum;
                mapsInUser.AverageTime = data.mapDataList[i].averageTime;
            }
            context.SaveChanges();
            return Ok("Данные загружены на сервер"); //логирование
        }

        [Route("Output")] //наверное стоит разделить данные учетки и данные игры
        [Authorize]
        [HttpPost]
        public IActionResult BDtoJSON()
        {
            #region user getting
            GameData gameData = new GameData();
            string? authorizationHeader = Request.Headers["Authorization"];
            string token = authorizationHeader!.Replace("Bearer ", "");
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            string userId = jwtToken.Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier).Value;
            UserContext context = new UserContext();
            User? user = context.Users.SingleOrDefault(u => u.Id == int.Parse(userId));
            if (user == null) return BadRequest("Пользователь не обнаружен"); //логирование

            gameData.username = user.Username;
            gameData.password = "";
            gameData.email = user.Email;
            gameData.updateDate = user.updateDate;
            gameData.userID = int.Parse(userId); //нужно будет обращаться к Auth


            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, user.Username),
            };

            var jwt = new JwtSecurityToken(
            issuer: AuthOptions.ISSUER,
            audience: AuthOptions.AUDIENCE,
            claims: claims,
            expires: DateTime.UtcNow.Add(TimeSpan.FromHours(24)),
            signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            var jwtr = new JwtSecurityToken(
            issuer: AuthOptions.ISSUER,
            audience: AuthOptions.AUDIENCE,
            claims: claims,
            expires: DateTime.UtcNow.Add(TimeSpan.FromHours(300)),
            signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
            var encodedJwtr = new JwtSecurityTokenHandler().WriteToken(jwt);

            gameData.aToken = encodedJwt;
            gameData.rToken = encodedJwtr;
            #endregion

            gameData.lastGameType = (MapType)user.LastGameType;
            gameData.lastGameSize = (MapSize)user.LastGameSize;
            gameData.lastGameData = user.LastGameData;
            gameData.lastGameTime = user.LastGameTime;

            var maps = context.MapsInUsers.Where(u => (u.UserId == int.Parse(userId))).ToList();
            for (int i = 1; i < maps.Count + 1; i++)
            {
                MapsInUser? mapInUser = maps.SingleOrDefault(u => u.MapId == i);
                Map? map = context.Maps.SingleOrDefault(u => u.Id == i);
                if (mapInUser != null && map != null)
                {
                    MapData mapData = new MapData();
                    mapData.mapID = i;
                    mapData.mapName = map.MapName;
                    mapData.bombCount = map.BombCount;
                    mapData.mapType = (MapType)map.MapType;
                    mapData.mapSize = (MapSize)map.MapSize;

                    mapData.gamesSum = mapInUser.GamesSum;
                    mapData.wins = mapInUser.Wins;
                    mapData.loses = mapInUser.Loses;
                    mapData.openedTiles = mapInUser.OpenedTiles;
                    mapData.openedNumberTiles = mapInUser.OpenedNumberTiles;
                    mapData.openedBlankTiles = mapInUser.OpenedBlankTiles;
                    mapData.flagsSum = mapInUser.FlagsSum;
                    mapData.flagsOnBombs = mapInUser.FlagsOnBombs;
                    mapData.timeSpentSum = mapInUser.TimeSpentSum;
                    mapData.averageTime = mapInUser.AverageTime;
                    gameData.mapDataList.Add(mapData);
                }
            }
            return Ok(gameData); //логирование
        }
    }
}
