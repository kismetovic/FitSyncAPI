using FITSync.Domain.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FITSync.Infrastructure.Authentication
{
    public static class CustomClaimTypes
    {
        public static readonly string UserId = "userId";
    }

    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly JwtSettings _jwtSettings;

        public JwtTokenGenerator(IOptions<JwtSettings> jwtOptions)
        {
            _jwtSettings = jwtOptions.Value;
        }

        public string GenerateToken(User user)
        {
            if (string.IsNullOrWhiteSpace(_jwtSettings.SecretKey))
            {
                throw new InvalidOperationException(
                    "JwtSettings:SecretKey is not configured. Set the JwtSettings__SecretKey environment variable.");
            }

            var tokenHandler = new JwtSecurityTokenHandler();

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Sub, user.UserName ?? user.Id.ToString()),
                new(CustomClaimTypes.UserId, user.Id.ToString())
            };

            if (user.Roles != null)
            {
                claims.AddRange(user.Roles
                    .Where(ur => ur.Role?.Name != null)
                    .Select(ur => new Claim(ClaimTypes.Role, ur.Role!.Name!)));
            }

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _jwtSettings.ValidIssuer,
                Audience = _jwtSettings.ValidAudience,
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.Add(_jwtSettings.Expires == default ? TimeSpan.FromDays(7) : _jwtSettings.Expires),
                SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(descriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
