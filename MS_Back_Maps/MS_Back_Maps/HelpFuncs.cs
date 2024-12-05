using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MS_Back_Maps
{
    public class HelpFuncs
    {
        public string? GetUserIdFromToken(HttpRequest request)
        {
            string? authorizationHeader = request.Headers["Authorization"];
            if (authorizationHeader == null) return null;

            string token = authorizationHeader.Replace("Bearer ", "");
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token)) return null;

            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
