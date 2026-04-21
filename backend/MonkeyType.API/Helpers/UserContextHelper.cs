using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MonkeyType.API.Helpers
{
    public static class UserContextHelper
    {
        public static bool IsSelf(ClaimsPrincipal user, Guid userId)
        {
            return TryGetUserId(user, out var current) && current == userId;
        }

        public static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
        {
            var sub = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(sub, out userId);
        }
    }
}
