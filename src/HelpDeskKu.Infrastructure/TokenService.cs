using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HelpDeskKu.Application;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HelpDeskKu.Infrastructure;

public class TokenService(AppDbContext db, UserManager<AppUser> users, IConfiguration config)
{
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    public async Task<UserDto> Profile(AppUser user) => new(user.Id, user.FullName, user.Email!,
        (await users.GetRolesAsync(user)).FirstOrDefault() ?? "Requester", user.IsActive);
    public async Task<AuthResponse> Issue(AppUser user)
    {
        var profile = await Profile(user);
        var expires = DateTimeOffset.UtcNow.AddMinutes(15);
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.Id), new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, profile.Role), new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) };
        var jwt = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"], claims,
            expires: expires.UtcDateTime, signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)), SecurityAlgorithms.HmacSha256));
        var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = Hash(refresh), ExpiresAt = DateTimeOffset.UtcNow.AddDays(7) });
        await db.SaveChangesAsync();
        return new(new JwtSecurityTokenHandler().WriteToken(jwt), refresh, expires, profile);
    }
}
