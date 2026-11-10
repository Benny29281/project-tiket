using HelpDeskKu.Domain;
using Xunit;

namespace HelpDeskKu.UnitTests;

public class TicketWorkflowTests
{
    [Theory]
    [InlineData(TicketStatus.Open, TicketStatus.InProgress)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Pending)]
    [InlineData(TicketStatus.Pending, TicketStatus.InProgress)]
    [InlineData(TicketStatus.Pending, TicketStatus.Resolved)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Resolved)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Closed)]
    [InlineData(TicketStatus.Closed, TicketStatus.Reopened)]
    [InlineData(TicketStatus.Reopened, TicketStatus.InProgress)]
    public void StaffCanFollowSupportedTransitions(TicketStatus from, TicketStatus to) => Assert.True(TicketWorkflow.CanChange(from, to, true));

    [Theory]
    [InlineData(TicketStatus.Open, TicketStatus.Resolved)]
    [InlineData(TicketStatus.Closed, TicketStatus.InProgress)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Open)]
    [InlineData(TicketStatus.Pending, TicketStatus.Closed)]
    public void StaffCannotSkipWorkflow(TicketStatus from, TicketStatus to) => Assert.False(TicketWorkflow.CanChange(from, to, true));

    [Fact]
    public void RequesterCanOnlyCloseResolvedOrReopenFinishedTicket()
    {
        foreach (var from in Enum.GetValues<TicketStatus>())
        foreach (var to in Enum.GetValues<TicketStatus>())
        {
            var allowed = (from, to) is (TicketStatus.Resolved, TicketStatus.Closed)
                or (TicketStatus.Resolved, TicketStatus.Reopened) or (TicketStatus.Closed, TicketStatus.Reopened);
            Assert.Equal(allowed, TicketWorkflow.CanChange(from, to, false));
        }
    }
    [Fact]
    public void StaffCannotTransitionToSameState()
    {
        foreach (var status in Enum.GetValues<TicketStatus>()) Assert.False(TicketWorkflow.CanChange(status, status, true));
    }
}
