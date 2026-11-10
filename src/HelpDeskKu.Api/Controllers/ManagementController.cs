using System.Data;
using HelpDeskKu.Application;
using HelpDeskKu.Domain;
using HelpDeskKu.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskKu.Api.Controllers;

[ApiController, Authorize, Route("api/v1")]
public class ManagementController(AppDbContext db, ICurrentUser current, UserManager<AppUser> users, TokenService tokens) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<IActionResult> Categories() => Ok(await db.Categories.AsNoTracking().Where(c => current.IsAdmin || c.IsActive).OrderBy(c => c.Name).ToListAsync());
    [HttpPost("categories"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCategory(CategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return Problem(statusCode: 400, title: "Nama kategori wajib diisi.");
        var category = new Category { Name = request.Name.Trim(), Description = request.Description.Trim(), IsActive = request.IsActive };
        db.Categories.Add(category); await db.SaveChangesAsync(); return Ok(category);
    }
    [HttpPut("categories/{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategory(int id, CategoryRequest request)
    {
        var category = await db.Categories.FindAsync(id);
        if (category is null) return NotFound();
        if (string.IsNullOrWhiteSpace(request.Name)) return Problem(statusCode: 400, title: "Nama kategori wajib diisi.");
        category.Name = request.Name.Trim(); category.Description = request.Description.Trim(); category.IsActive = request.IsActive;
        await db.SaveChangesAsync(); return NoContent();
    }
    [HttpDelete("categories/{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await db.Categories.FindAsync(id);
        if (category is null) return NotFound();
        // Keep historical tickets intact.
        category.IsActive = false; await db.SaveChangesAsync(); return NoContent();
    }
    [HttpGet("priorities")]
    public async Task<IActionResult> Priorities() => Ok(await db.Priorities.AsNoTracking().OrderBy(p => p.Level).ToListAsync());
    [HttpGet("agents"), Authorize(Roles = "Agent,Admin")]
    public async Task<IActionResult> Agents()
    {
        var staff = (await users.GetUsersInRoleAsync("Agent")).Concat(await users.GetUsersInRoleAsync("Admin"))
            .Where(u => u.IsActive).DistinctBy(u => u.Id).Select(u => new { u.Id, u.FullName });
        return Ok(staff);
    }
    [HttpGet("users"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Users(string? search, int page = 1, int pageSize = 20)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = users.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(u => u.FullName.Contains(search) || u.Email!.Contains(search));
        var total = await query.CountAsync();
        var items = new List<UserDto>();
        foreach (var user in await query.OrderBy(u => u.FullName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync()) items.Add(await tokens.Profile(user));
        return Ok(new PageResult<UserDto>(items, total, page, pageSize));
    }
    [HttpPost("users"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var user = new AppUser { UserName = request.Email.Trim(), Email = request.Email.Trim(), FullName = request.FullName.Trim() };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded) return Problem(statusCode: 400, title: "Pengguna gagal dibuat.", detail: string.Join(" ", result.Errors.Select(e => e.Description)));
        result = await users.AddToRoleAsync(user, request.Role);
        if (!result.Succeeded) return Problem(statusCode: 400, title: "Role gagal disimpan.");
        await transaction.CommitAsync(); return Ok(await tokens.Profile(user));
    }
    [HttpPut("users/{id}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUser(string id, UpdateUserRequest request)
    {
        if (id == current.Id && (!request.IsActive || request.Role != "Admin")) return Problem(statusCode: 400, title: "Anda tidak dapat mencabut akses admin sendiri.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user = await users.FindByIdAsync(id);
        if (user is null) return NotFound();
        var oldRoles = await users.GetRolesAsync(user);
        if (oldRoles.Contains("Admin") && (!request.IsActive || request.Role != "Admin") && (await users.GetUsersInRoleAsync("Admin")).Count(u => u.IsActive) <= 1)
            return Problem(statusCode: 400, title: "Minimal satu admin aktif harus tersedia.");
        user.FullName = request.FullName.Trim(); user.IsActive = request.IsActive;
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded) return Problem(statusCode: 400, title: "Pengguna gagal diperbarui.");
        result = await users.RemoveFromRolesAsync(user, oldRoles);
        if (!result.Succeeded) return Problem(statusCode: 400, title: "Role gagal diperbarui.");
        result = await users.AddToRoleAsync(user, request.Role);
        if (!result.Succeeded) return Problem(statusCode: 400, title: "Role gagal diperbarui.");
        await db.RefreshTokens.Where(t => t.UserId == id && t.RevokedAt == null).ExecuteUpdateAsync(set => set.SetProperty(t => t.RevokedAt, DateTimeOffset.UtcNow));
        await transaction.CommitAsync(); return NoContent();
    }
    [HttpGet("dashboard/summary")]
    public async Task<IActionResult> Dashboard()
    {
        var query = db.Tickets.AsNoTracking().Where(t => current.IsStaff || t.RequesterId == current.Id);
        var now = DateTimeOffset.UtcNow;
        return Ok(new { total = await query.CountAsync(),
            active = await query.CountAsync(t => t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved),
            resolved = await query.CountAsync(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed),
            overdue = await query.CountAsync(t => t.DueDate < now && t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved),
            unassigned = await query.CountAsync(t => t.AssigneeId == null && t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved),
            mine = await query.CountAsync(t => t.AssigneeId == current.Id && t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved),
            byStatus = await query.GroupBy(t => t.Status).Select(g => new { status = g.Key, count = g.Count() }).ToListAsync(),
            byCategory = await query.GroupBy(t => t.Category.Name).Select(g => new { name = g.Key, count = g.Count() }).ToListAsync() });
    }
}
