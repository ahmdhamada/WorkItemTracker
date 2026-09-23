using System.ComponentModel.DataAnnotations;
using WorkItems.Api.Models;
namespace WorkItems.Api.Contracts;
public sealed class CreateWorkItemRequest
{
    [Required, StringLength(120, MinimumLength = 1)] public string Title { get; init; } = "";
    [StringLength(4000)] public string? Description { get; init; }
}
public sealed class UpdateStatusRequest { [Required] public string Status { get; init; } = ""; }
public sealed record WorkItemResponse(int Id, string Title, string? Description, WorkItemStatus Status, DateTimeOffset CreatedAt);
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
