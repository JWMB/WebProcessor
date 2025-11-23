using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Common
{
    public class TokenHelper
    {
        public class CreateTokenParams
        {
            public string? Issuer { get; set; }
            public string? Audience { get; set; }
            public DateTime StartTime { get; set; } = DateTime.UtcNow;
            public DateTime Expiry { get; set; } = DateTime.UtcNow.AddYears(1);

            public IEnumerable<Claim>? Claims { get; set; }
            public Dictionary<string, string> ClaimsDictionary { get; set; } = new Dictionary<string, string>();

            public List<Claim> AllClaims
            {
                get
                {
                    var keys = Claims?.Select(o => o.Type).ToList() ?? new List<string>();
                    return new List<Claim>(Claims ?? new List<Claim>())
                        .Concat(
                            ClaimsDictionary
                                .Where(o => !keys.Contains(o.Key))
                                .Select(o => new Claim(o.Key, o.Value))
                                )
                        .ToList();
                }
            }
        }

        public static (JwtSecurityToken, string) CreateToken(string signingKey, CreateTokenParams parameters)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);


            if (parameters.AllClaims.Any(o => o.Type == "iat") == false)
            {
                parameters.Claims = (parameters.Claims ?? new List<Claim>()).Concat(new[] { new Claim("iat", DateTime.UtcNow.ToEpochTime().ToString(), ClaimValueTypes.Integer64) });
            }

            // Generate the token
            var token =
                new JwtSecurityToken(parameters.Issuer, parameters.Audience, parameters.AllClaims,
                    notBefore: parameters.StartTime,
                    expires: parameters.Expiry,
                    signingCredentials: credentials);

            return (token, new JwtSecurityTokenHandler().WriteToken(token));
        }
    }

    public static class Extensions
    {
        public static long ToEpochTime(this DateTime date)
        {
            return (long)(date - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }
    }
}
