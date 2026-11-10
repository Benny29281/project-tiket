using System.Data;
using HelpDeskKu.Application;
using HelpDeskKu.Domain;
using HelpDeskKu.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskKu.Api.Controllers;

[ApiController, Authorize, Route("api/v1/tickets")]
public class TicketsController(AppDbContext db, ICurrentUser current, UserManager<AppUser> users) : ControllerBase
{
    private IQueryable<Ticket> Visible => db.Tickets.Where(x => current.IsStaff || x.RequesterId == current.Id);
    private IQueryable<TicketRow> Rows(IQueryable<Ticket> query) => query.Select(t => new TicketRow(t.Id,
        t.TicketNumber, t.Title, t.Description, t.CategoryId, t.Category.Name, t.PriorityId, t.Priority.Name,
        t.Status, t.RequesterId, db.Users.Where(u => u.Id == t.RequesterId).Select(u => u.FullName).First(),
        t.AssigneeId, db.Users.Where(u => u.Id == t.AssigneeId).Select(u => u.FullName).FirstOrDefault(),
        t.CreatedAt, t.UpdatedAt, t.DueDate));
    private void Activity(Ticket ticket, string type, string? oldValue, string? newValue) => ticket.Activities.Add(new TicketActivity
        { ActorId = current.Id, ActorName = current.Name, ActivityType = type, OldValue = oldValue, NewValue = newValue });

    [HttpGet]
    public async Task<IActionResult> List(string? search, TicketStatus? status, int? categoryId, int? priorityId,
        string? assigneeId, DateTimeOffset? from, DateTimeOffset? to, bool unassigned = false,
        int page = 1, int pageSize = 20, string sortBy = "createdAt", string sortDir = "desc")
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = Visible.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(t => t.Title.Contains(search) || t.TicketNumber.Contains(search));
        if (status.HasValue) query = query.Where(t => t.Status == status.Value);
        if (categoryId.HasValue) query = query.Where(t => t.CategoryId == categoryId);
        if (priorityId.HasValue) query = query.Where(t => t.PriorityId == priorityId);
        if (!string.IsNullOrEmpty(assigneeId)) query = query.Where(t => t.AssigneeId == assigneeId);
        if (unassigned) query = query.Where(t => t.AssigneeId == null && t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved);
        if (from.HasValue) query = query.Where(t => t.CreatedAt >= from);
        if (to.HasValue) query = query.Where(t => t.CreatedAt <= to);
        var total = await query.CountAsync();
        var ascending = sortDir == "asc";
        query = sortBy switch
        {
            "priority" => ascending ? query.OrderBy(t => t.Priority.Level).ThenBy(t => t.Id) : query.OrderByDescending(t => t.Priority.Level).ThenBy(t => t.Id),
            "title" => ascending ? query.OrderBy(t => t.Title).ThenBy(t => t.Id) : query.OrderByDescending(t => t.Title).ThenBy(t => t.Id),
            "dueDate" => ascending ? query.OrderBy(t => t.DueDate).ThenBy(t => t.Id) : query.OrderByDescending(t => t.DueDate).ThenBy(t => t.Id),
            _ => ascending ? query.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id) : query.OrderByDescending(t => t.CreatedAt).ThenBy(t => t.Id)
        };
        return Ok(new PageResult<TicketRow>(await Rows(query.Skip((page - 1) * pageSize).Take(pageSize)).ToListAsync(), total, page, pageSize));
    }
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var ticket = await Rows(Visible.AsNoTracking().Where(t => t.Id == id)).SingleOrDefaultAsync();
        if (ticket is null) return NotFound();
        return Ok(new { ticket,
            comments = await db.Comments.AsNoTracking().Where(c => c.TicketId == id && (current.IsStaff || !c.IsInternal)).OrderBy(c => c.CreatedAt).ToListAsync(),
            activities = await db.Activities.AsNoTracking().Where(a => a.TicketId == id).OrderBy(a => a.CreatedAt).ToListAsync() });
    }
    [HttpGet("{id:int}/activities")]
    public async Task<IActionResult> Activities(int id) => !await Visible.AnyAsync(t => t.Id == id) ? NotFound()
        : Ok(await db.Activities.Where(a => a.TicketId == id).OrderBy(a => a.CreatedAt).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(CreateTicketRequest request)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId && c.IsActive)) return Problem(statusCode: 400, title: "Kategori tidak tersedia.");
        var priority = await db.Priorities.FindAsync(request.PriorityId);
        if (priority is null) return Problem(statusCode: 400, title: "Prioritas tidak tersedia.");
        // Serialize monthly allocation across processes; unique index is a second safeguard.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var now = DateTimeOffset.UtcNow;
        var prefix = $"HD-{now:yyyyMM}-";
        if (db.Database.IsSqlServer())
            await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource={prefix}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; IF @result < 0 THROW 51000, 'Ticket number allocation is busy.', 1;");
        var last = await db.Tickets.IgnoreQueryFilters().Where(t => t.TicketNumber.StartsWith(prefix)).OrderByDescending(t => t.Id).Select(t => t.TicketNumber).FirstOrDefaultAsync();
        var next = last == null ? 1 : int.Parse(last[prefix.Length..]) + 1;
        var ticket = new Ticket { TicketNumber = $"{prefix}{next:D4}", Title = request.Title.Trim(), Description = request.Description.Trim(),
            CategoryId = request.CategoryId, PriorityId = priority.Id, RequesterId = current.Id, DueDate = now.AddMinutes(priority.ResolutionTimeMinutes) };
        Activity(ticket, "Created", null, "Tiket dibuat");
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return CreatedAtAction(nameof(Detail), new { id = ticket.Id }, new { ticket.Id, ticket.TicketNumber });
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Edit(int id, CreateTicketRequest request)
    {
        var ticket = await Visible.SingleOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return NotFound();
        if (!current.IsStaff && ticket.Status != TicketStatus.Open) return Forbid();
        var priority = await db.Priorities.FindAsync(request.PriorityId);
        if (priority is null || !await db.Categories.AnyAsync(c => c.Id == request.CategoryId && c.IsActive)) return Problem(statusCode: 400, title: "Kategori atau prioritas tidak valid.");
        if (ticket.PriorityId != request.PriorityId) { Activity(ticket, "PriorityChanged", ticket.PriorityId.ToString(), priority.Id.ToString()); ticket.DueDate = ticket.CreatedAt.AddMinutes(priority.ResolutionTimeMinutes); }
        ticket.Title = request.Title.Trim(); ticket.Description = request.Description.Trim(); ticket.CategoryId = request.CategoryId;
        ticket.PriorityId = request.PriorityId; ticket.UpdatedAt = DateTimeOffset.UtcNow;
        Activity(ticket, "Updated", null, "Informasi tiket diperbarui");
        await db.SaveChangesAsync(); return NoContent();
    }
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> ChangeStatus(int id, StatusRequest request)
    {
        var ticket = await Visible.SingleOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return NotFound();
        if (!current.IsStaff && ticket.RequesterId != current.Id) return Forbid();
        if (!TicketWorkflow.CanChange(ticket.Status, request.Status, current.IsStaff)) return Problem(statusCode: 400, title: "Perubahan status tidak diizinkan.");
        Activity(ticket, "StatusChanged", ticket.Status.ToString(), request.Status.ToString());
        ticket.Status = request.Status; ticket.UpdatedAt = DateTimeOffset.UtcNow;
        if (request.Status == TicketStatus.Resolved) ticket.ResolvedAt = ticket.UpdatedAt;
        if (request.Status == TicketStatus.Closed) ticket.ClosedAt = ticket.UpdatedAt;
        if (request.Status == TicketStatus.Reopened) { ticket.ClosedAt = null; ticket.ResolvedAt = null; }
        if (current.IsStaff) ticket.FirstResponseAt ??= ticket.UpdatedAt;
        await db.SaveChangesAsync(); return NoContent();
    }
    [HttpPatch("{id:int}/assign"), Authorize(Roles = "Agent,Admin")]
    public async Task<IActionResult> Assign(int id, AssignRequest request)
    {
        var ticket = await db.Tickets.SingleOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return NotFound();
        if (request.AssigneeId != null)
        {
            var agent = await users.FindByIdAsync(request.AssigneeId);
            if (agent is null || !agent.IsActive || !(await users.GetRolesAsync(agent)).Any(r => r is "Agent" or "Admin"))
                return Problem(statusCode: 400, title: "Agen tidak tersedia.");
        }
        Activity(ticket, "Assigned", ticket.AssigneeId, request.AssigneeId);
        ticket.AssigneeId = request.AssigneeId; ticket.UpdatedAt = DateTimeOffset.UtcNow;
        if (request.AssigneeId != null && ticket.Status is TicketStatus.Open or TicketStatus.Reopened)
        {
            Activity(ticket, "StatusChanged", ticket.Status.ToString(), "InProgress");
            ticket.Status = TicketStatus.InProgress; ticket.FirstResponseAt ??= ticket.UpdatedAt;
        }
        await db.SaveChangesAsync(); return NoContent();
    }
    [HttpPost("{id:int}/comments")]
    public async Task<IActionResult> Comment(int id, CommentRequest request)
    {
        var ticket = await Visible.SingleOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return NotFound();
        if (request.IsInternal && !current.IsStaff) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Content)) return Problem(statusCode: 400, title: "Komentar tidak boleh kosong.");
        db.Comments.Add(new TicketComment { TicketId = id, AuthorId = current.Id, AuthorName = current.Name,
            Content = request.Content.Trim(), IsInternal = request.IsInternal });
        ticket.UpdatedAt = DateTimeOffset.UtcNow;
        if (current.IsStaff && !request.IsInternal) ticket.FirstResponseAt ??= ticket.UpdatedAt;
        await db.SaveChangesAsync(); return NoContent();
    }
    [HttpDelete("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var ticket = await db.Tickets.SingleOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return NotFound();
        ticket.IsDeleted = true; Activity(ticket, "Deleted", null, "Tiket diarsipkan");
        await db.SaveChangesAsync(); return NoContent();
    }
}
