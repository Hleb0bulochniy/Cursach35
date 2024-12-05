using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using MS_Back_Auth.Models;
using MS_Back_Auth.Data;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MS_Back_Auth.Controllers
{
    public static class Cryptography
    {
        public static string ConvertPassword(string password)
        {
            string soursePass = password;
            byte[] sourcePassBytes;
            byte[] hashPassBytes;

            sourcePassBytes = ASCIIEncoding.ASCII.GetBytes(soursePass);

            hashPassBytes = new MD5CryptoServiceProvider().ComputeHash(sourcePassBytes);

            return ByteArrayToString(hashPassBytes);
        }

        static string ByteArrayToString(byte[] arrInput)
        {
            int i;
            StringBuilder sOutput = new StringBuilder(arrInput.Length);
            for (i = 0; i < arrInput.Length; i++)
            {
                sOutput.Append(arrInput[i].ToString("X2"));
            }
            return sOutput.ToString();
        }
        //передать в лог
    }

    [ApiController]
    public class AuthController : ControllerBase
    {
        [Route("UserRegistration")]
        [HttpPost]
        public IActionResult UserRegistration([FromBody] RegistrationClass model)
        {
            if (model.password1 == model.password2)
            {
                AuthContext context = new AuthContext();
                if (!context.Users.Any(u => u.Username == model.userName || u.Email == model.email))
                {
                    string cryptedPassword = Cryptography.ConvertPassword(model.password1);
                    User user = new User()
                    {
                        Username = model.userName,
                        Email = model.email,
                        Password = cryptedPassword,
                    };
                    context.Users.Add(user);
                    context.SaveChanges();
                    User newUser = context.Users.SingleOrDefault(u => ((u.Username == user.Username)))!;
                    Console.WriteLine("регистрация " + newUser.Id); //оформить в лог
                    return Ok(newUser.Id.ToString());
                }
                else return (BadRequest("The user already exists"));
            }
            else
            {
                return BadRequest("Passwords do not match");
            }
            //передать в лог
        }

        [Route("UserLogin")]
        [HttpPost]
        public IActionResult UserLogin([FromBody] LoginClass model)
        {
            AuthContext context = new AuthContext();
            string cryptedPassword = Cryptography.ConvertPassword(model.password);
            User? dbuser = context.Users.FirstOrDefault(u => ((u.Username == model.userName) & (u.Password == cryptedPassword))); //переделать если пароль не совпадает
            if (dbuser == null) return BadRequest("The user does not exists");
            if (dbuser.Password != cryptedPassword) BadRequest("The password does not match");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, dbuser.Id.ToString()),
                new Claim(ClaimTypes.Name, dbuser.Username),
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

            var response = new
            {
                access_token = encodedJwt,
                refresh_token = encodedJwtr,
                username = dbuser.Username,
            };
            Console.WriteLine("залогинился"); //переделать в лог
            return Ok(response);
        }

        [Authorize]
        [Route("RefreshToken")]
        [HttpPost]
        public IActionResult RefreshToken() //если токен пустой, добавить обработчик
        {
            string? authorizationHeader = Request.Headers["Authorization"];
            string token = authorizationHeader.Replace("Bearer ", "");
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            string userId = jwtToken.Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier).Value;
            AuthContext context = new AuthContext();
            string userName = context.Users.FirstOrDefault(u => (u.Id == int.Parse(userId))).Username; //стоит ли это оставлять так или лучше доставать значение из токена


            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier,userId),
                new Claim(ClaimTypes.Name,userName),
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

            var response = new
            {
                access_token = encodedJwt,
                refresh_token = encodedJwtr,
                username = userName, //нужен ли UserName
            };
            //передать в лог

            return Ok(response);
        }

        [Authorize]
        [Route("PasswordCheck")]
        [HttpPost]
        public IActionResult PasswordCheck([FromBody] PasswordClass password)
        {
            string? authorizationHeader = Request.Headers["Authorization"];
            string token = authorizationHeader!.Replace("Bearer ", "");
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            string userId = jwtToken.Claims.First(claim => claim.Type == ClaimTypes.NameIdentifier).Value;
            AuthContext context = new AuthContext();
            User? user = context.Users.SingleOrDefault(u => u.Id == int.Parse(userId));
            if (user is User u)
            {
                string cryptedPassword = Cryptography.ConvertPassword(password.password);
                if (cryptedPassword == u.Password)
                {
                    Console.WriteLine("совпало"); //передать в лог
                    return Ok("true");
                }
                else
                {
                    Console.WriteLine("не совпало"); //передать в лог
                    return Ok("false");
                }
            }
            else
            { //передать в лог
                return BadRequest("No user");
            }
        }
    }
}
