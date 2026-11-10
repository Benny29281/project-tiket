using HelpDeskKu.Application;
using HelpDeskKu.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskKu.Api.Controllers;

[ApiController, Route("api/v1/auth"), EnableRateLimiting("auth")]
public class AuthController(UserManager<AppUser> users, SignInManager<AppUser> signIn,
    AppDbContext db, TokenService tokens, ICurrentUser current) : ControllerBase
{
    [HttpPost("register"), AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var user = new AppUser { UserName = request.Email.Trim(), Email = request.Email.Trim(), FullName = request.FullName.Trim() };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded) return Problem(statusCode: 400, title: "Registrasi gagal.", detail: string.Join(" ", result.Errors.Select(x => x.Description)));
        var role = await users.AddToRoleAsync(user, "Requester");
        if (!role.Succeeded) return Problem(statusCode: 500, title: "Registrasi gagal.");
        var response = await tokens.Issue(user);
        await transaction.CommitAsync();
        return Ok(response);
    }
    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive) return Problem(statusCode: 401, title: "Email atau password tidak sesuai.");
        var result = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded) return Problem(statusCode: 401, title: result.IsLockedOut
            ? "Akun terkunci sementara. Coba kembali dalam 15 menit." : "Email atau password tidak sesuai.");
        return Ok(await tokens.Issue(user));
    }
    [HttpPost("refresh"), AllowAnonymous]
    public async Task<IActionResult> Refresh(RefreshRequest request)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var hash = TokenService.Hash(request.RefreshToken);
        var refresh = await db.RefreshTokens.Include(x => x.User).SingleOrDefaultAsync(x => x.TokenHash == hash);
        if (refresh is null || refresh.RevokedAt != null || refresh.ExpiresAt <= DateTimeOffset.UtcNow || !refresh.User.IsActive)
            return Problem(statusCode: 401, title: "Sesi berakhir. Silakan login kembali.");
        refresh.RevokedAt = DateTimeOffset.UtcNow;
        var response = await tokens.Issue(refresh.User);
        await transaction.CommitAsync();
        return Ok(response);
    }
    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout(RefreshRequest request)
    {
        var hash = TokenService.Hash(request.RefreshToken);
        var token = await db.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == hash && x.UserId == current.Id);
        if (token != null) { token.RevokedAt = DateTimeOffset.UtcNow; await db.SaveChangesAsync(); }
        return NoContent();
    }
    [HttpGet("me"), Authorize]
    public async Task<IActionResult> Me() => Ok(await tokens.Profile((await users.FindByIdAsync(current.Id))!));
}
