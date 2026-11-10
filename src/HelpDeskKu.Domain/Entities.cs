namespace HelpDeskKu.Domain;

public enum TicketStatus { Open, InProgress, Pending, Resolved, Closed, Reopened }
public static class TicketWorkflow
{
    public static bool CanChange(TicketStatus from, TicketStatus to, bool staff) => from != to && (staff
        ? (from, to) is (TicketStatus.Open, TicketStatus.InProgress) or (TicketStatus.Reopened, TicketStatus.InProgress)
            or (TicketStatus.InProgress, TicketStatus.Pending) or (TicketStatus.Pending, TicketStatus.InProgress)
            or (TicketStatus.InProgress, TicketStatus.Resolved) or (TicketStatus.Pending, TicketStatus.Resolved)
            or (TicketStatus.Resolved, TicketStatus.Closed) or (TicketStatus.Resolved, TicketStatus.Reopened)
            or (TicketStatus.Closed, TicketStatus.Reopened)
        : (from, to) is (TicketStatus.Resolved, TicketStatus.Closed) or (TicketStatus.Resolved, TicketStatus.Reopened)
            or (TicketStatus.Closed, TicketStatus.Reopened));
}
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
public class Priority
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Level { get; set; }
    public int ResponseTimeMinutes { get; set; }
    public int ResolutionTimeMinutes { get; set; }
}
public class Ticket
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public int PriorityId { get; set; }
    public Priority Priority { get; set; } = null!;
    public TicketStatus Status { get; set; }
    public string RequesterId { get; set; } = "";
    public string? AssigneeId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset DueDate { get; set; }
    public DateTimeOffset? FirstResponseAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public bool IsDeleted { get; set; }
    public byte[] Version { get; set; } = [];
    public List<TicketComment> Comments { get; set; } = [];
    public List<TicketActivity> Activities { get; set; } = [];
}
public class TicketComment
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string AuthorId { get; set; } = "";
    public string AuthorName { get; set; } = "";
    public string Content { get; set; } = "";
    public bool IsInternal { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
public class TicketActivity
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string ActorId { get; set; } = "";
    public string ActorName { get; set; } = "";
    public string ActivityType { get; set; } = "";
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
