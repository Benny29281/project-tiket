using HelpDeskKu.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskKu.Infrastructure;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
public class RefreshToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public AppUser User { get; set; } = null!;
    public string TokenHash { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public byte[] Version { get; set; } = [];
}
public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Priority> Priorities => Set<Priority>();
    public DbSet<TicketComment> Comments => Set<TicketComment>();
    public DbSet<TicketActivity> Activities => Set<TicketActivity>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<AppUser>().Property(x => x.FullName).HasMaxLength(100);
        b.Entity<Ticket>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<Ticket>().Property(x => x.TicketNumber).HasMaxLength(32);
        b.Entity<Ticket>().Property(x => x.Title).HasMaxLength(200);
        b.Entity<Ticket>().Property(x => x.Description).HasMaxLength(10000);
        b.Entity<Ticket>().Property(x => x.Version).IsRowVersion();
        b.Entity<Ticket>().HasIndex(x => x.TicketNumber).IsUnique();
        b.Entity<Ticket>().HasIndex(x => new { x.Status, x.PriorityId });
        b.Entity<Ticket>().HasIndex(x => x.RequesterId);
        b.Entity<Ticket>().HasIndex(x => x.AssigneeId);
        b.Entity<Ticket>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.RequesterId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Ticket>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.AssigneeId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Ticket>().HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Ticket>().HasOne(x => x.Priority).WithMany().HasForeignKey(x => x.PriorityId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Category>().Property(x => x.Name).HasMaxLength(100);
        b.Entity<Category>().HasIndex(x => x.Name).IsUnique();
        b.Entity<RefreshToken>().Property(x => x.TokenHash).HasMaxLength(64);
        b.Entity<RefreshToken>().HasIndex(x => x.TokenHash).IsUnique();
        b.Entity<RefreshToken>().Property(x => x.Version).IsRowVersion();
    }
}
