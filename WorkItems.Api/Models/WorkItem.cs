namespace WorkItems.Api.Models;

public enum WorkItemStatus { Todo, InProgress, Done }

public sealed class WorkItem
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public WorkItemStatus Status { get; private set; } = WorkItemStatus.Todo;
    public DateTimeOffset CreatedAt { get; set; }

    public bool TryTransitionTo(WorkItemStatus nextStatus)
    {
        var allowed = (Status, nextStatus) is
            (WorkItemStatus.Todo, WorkItemStatus.InProgress) or
            (WorkItemStatus.InProgress, WorkItemStatus.Done);
        if (!allowed) return false;

        Status = nextStatus;
        return true;
    }
}
