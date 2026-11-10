using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HelpDeskKu.Application;
using HelpDeskKu.Domain;
using HelpDeskKu.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace HelpDeskKu.IntegrationTests;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string database = Guid.NewGuid().ToString();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Key", "IntegrationTestKey-OnlyForTests-AtLeast32Bytes!");
        builder.UseSetting("ConnectionStrings:Default", "Server=unused;Database=unused;Trusted_Connection=True");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "IntegrationTestKey-OnlyForTests-AtLeast32Bytes!",
            ["ConnectionStrings:Default"] = "Server=unused;Database=unused;Trusted_Connection=True"
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(database)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        });
    }
    public async Task Seed()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Requester", "Agent", "Admin" }) await roles.CreateAsync(new IdentityRole(role));
        foreach (var item in new[] { ("alice", "Requester"), ("bob", "Requester"), ("agent", "Agent"), ("admin", "Admin") })
        {
            var user = new AppUser { Id = item.Item1, UserName = item.Item1 + "@test.local", Email = item.Item1 + "@test.local", FullName = item.Item1 };
            var result = await users.CreateAsync(user, "TestPassword123!");
            Assert.True(result.Succeeded); await users.AddToRoleAsync(user, item.Item2);
        }
        db.Categories.Add(new Category { Id = 1, Name = "Hardware" });
        db.Priorities.Add(new Priority { Id = 1, Name = "Medium", Level = 2, ResolutionTimeMinutes = 4320 });
        db.Tickets.Add(new Ticket { Id = 1, TicketNumber = "HD-TEST-0001", Title = "Laptop bermasalah", Description = "Laptop tidak bisa dinyalakan", RequesterId = "alice", CategoryId = 1, PriorityId = 1, DueDate = DateTimeOffset.UtcNow.AddDays(3) });
        db.Comments.AddRange(new TicketComment { TicketId = 1, AuthorId = "agent", AuthorName = "Agent", Content = "PUBLIC MESSAGE" },
            new TicketComment { TicketId = 1, AuthorId = "agent", AuthorName = "Agent", Content = "SECRET INTERNAL MESSAGE", IsInternal = true });
        await db.SaveChangesAsync();
    }
    public async Task<(HttpClient Client, AuthResponse Session)> Login(string name)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = name + "@test.local", password = "TestPassword123!" });
        response.EnsureSuccessStatusCode();
        var session = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return (client, session);
    }
}

public class ApiTests
{
    [Fact]
    public async Task AnonymousCannotReadTickets()
    {
        await using var app = new ApiFactory(); await app.Seed();
        Assert.Equal(HttpStatusCode.Unauthorized, (await app.CreateClient().GetAsync("/api/v1/tickets")).StatusCode);
    }
    [Fact]
    public async Task RequesterCannotReadOtherUsersTicketOrTimeline()
    {
        await using var app = new ApiFactory(); await app.Seed(); var (client, _) = await app.Login("bob");
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/tickets/1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/v1/tickets/1/activities")).StatusCode);
        var list = JsonDocument.Parse(await client.GetStringAsync("/api/v1/tickets"));
        Assert.Equal(0, list.RootElement.GetProperty("total").GetInt32());
    }
    [Fact]
    public async Task InternalNotesNeverAppearInRequesterDetail()
    {
        await using var app = new ApiFactory(); await app.Seed(); var (client, _) = await app.Login("alice");
        var content = await client.GetStringAsync("/api/v1/tickets/1");
        Assert.Contains("PUBLIC MESSAGE", content); Assert.DoesNotContain("SECRET INTERNAL MESSAGE", content);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/tickets/1/comments", new { content = "hidden", isInternal = true })).StatusCode);
    }
    [Fact]
    public async Task RequesterCannotUseAdminOrAssignmentEndpoints()
    {
        await using var app = new ApiFactory(); await app.Seed(); var (client, _) = await app.Login("alice");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/v1/categories", new { name = "Test", description = "" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PatchAsJsonAsync("/api/v1/tickets/1/assign", new { assigneeId = "alice" })).StatusCode);
    }
    [Fact]
    public async Task AgentCanTakeTicketAndChangeIsRecorded()
    {
        await using var app = new ApiFactory(); await app.Seed(); var (client, _) = await app.Login("agent");
        Assert.Equal(HttpStatusCode.NoContent, (await client.PatchAsJsonAsync("/api/v1/tickets/1/assign", new { assigneeId = "agent" })).StatusCode);
        var doc = JsonDocument.Parse(await client.GetStringAsync("/api/v1/tickets/1"));
        Assert.Equal("InProgress", doc.RootElement.GetProperty("ticket").GetProperty("status").GetString());
        Assert.Contains(doc.RootElement.GetProperty("activities").EnumerateArray(), a => a.GetProperty("activityType").GetString() == "Assigned");
        Assert.Contains("SECRET INTERNAL MESSAGE", doc.RootElement.ToString());
    }
    [Fact]
    public async Task RequesterCannotResolveOpenTicketOrSubmitEmptyDescription()
    {
        await using var app = new ApiFactory(); await app.Seed(); var (client, _) = await app.Login("alice");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PatchAsJsonAsync("/api/v1/tickets/1/status", new { status = "Resolved" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/tickets", new { title = "Test ticket", description = "", categoryId = 1, priorityId = 1 })).StatusCode);
    }
    [Fact]
    public async Task RefreshRotatesAndRejectsReplay()
    {
        await using var app = new ApiFactory(); await app.Seed(); var (client, session) = await app.Login("alice");
        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { session.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { session.RefreshToken })).StatusCode);
    }
    [Fact]
    public async Task LogoutRevokesRefreshToken()
    {
        await using var app = new ApiFactory(); await app.Seed(); var (client, session) = await app.Login("alice");
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/auth/logout", new { session.RefreshToken })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new { session.RefreshToken })).StatusCode);
    }
}
