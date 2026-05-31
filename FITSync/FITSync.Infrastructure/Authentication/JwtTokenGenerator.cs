using FITSync.Domain.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace FITSync.Infrastructure.Authentication
{
    public static class CustomClaimTypes
    {
        public static readonly string UserId = "userId";
    }
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly JwtSettings jwtSettings;

        public JwtTokenGenerator(IOptions<JwtSettings> jwtOptions)
        {
            jwtSettings = jwtOptions.Value;
        }

        public object GenerateToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var signingCredientials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey!)),
                SecurityAlgorithms.HmacSha256Signature);

            var claims = new List<Claim>() {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(CustomClaimTypes.UserId, user.Id.ToString()),
            };

            if (user.Roles!.Count != 0)
                claims.AddRange(user.Roles
                    .Select(role => new Claim(ClaimTypes.Role, role.Role!.Name!)));
            var signinKey = Encoding.UTF8.GetBytes(this.jwtSettings.SecretKey!);
            var securityToken = new SecurityTokenDescriptor()
            {
                Issuer = this.jwtSettings.ValidIssuer,
                Audience = this.jwtSettings.ValidAudience,
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.Add(this.jwtSettings.Expires),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(signinKey), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(securityToken);
            return new
            {
                Token = tokenHandler.WriteToken(token)
            };
        }
    }
}
