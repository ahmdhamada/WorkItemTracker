namespace WorkItems.Api.Models;

public enum WorkItemStatus { Todo, InProgress, Done }

public sealed class WorkItem
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public WorkItemStatus Status { get; set; } = WorkItemStatus.Todo;
    public DateTimeOffset CreatedAt { get; set; }
}
