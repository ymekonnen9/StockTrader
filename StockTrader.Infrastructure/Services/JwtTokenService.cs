using Microsoft.AspNetCore.Identity;
using StockTrader.Application.Configuration;
using StockTrader.Application.Services;
using StockTrader.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace StockTrader.Infrastructure.Services
{
    public class JwtTokenService : ITokenService
    {
        private readonly UserManager<ApplicationUser> _usermanager;
        private readonly JwtSettings _jwtsettings;

        public JwtTokenService(UserManager<ApplicationUser> usermanager, JwtSettings jwtsettings)
        {
            _usermanager = usermanager;
            _jwtsettings = jwtsettings;
        }

        public async Task<String> GenerateTokenAsync(ApplicationUser user)
        {
            var roles = await _usermanager.GetRolesAsync(user);

            var authClaims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName!),
                new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            };

            foreach(var role in roles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var authSignInKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtsettings.Key));

            var token = new JwtSecurityToken
            (
                issuer: _jwtsettings.Issuer,
                audience: _jwtsettings.Audience,
                expires : DateTime.UtcNow.AddMinutes(_jwtsettings.DuartionInMinutes),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSignInKey, SecurityAlgorithms.HmacSha256Signature)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
