using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WorkItems.Api.Contracts;
using WorkItems.Api.Controllers;
using WorkItems.Api.Data;
using WorkItems.Api.Models;
using WorkItems.Api.Services;

namespace WorkItems.Tests;

public sealed class WorkItemApiTests
{
    [Theory]
    [InlineData(WorkItemStatus.Todo, WorkItemStatus.Todo, false)]
    [InlineData(WorkItemStatus.Todo, WorkItemStatus.InProgress, true)]
    [InlineData(WorkItemStatus.Todo, WorkItemStatus.Done, false)]
    [InlineData(WorkItemStatus.InProgress, WorkItemStatus.Todo, false)]
    [InlineData(WorkItemStatus.InProgress, WorkItemStatus.InProgress, false)]
    [InlineData(WorkItemStatus.InProgress, WorkItemStatus.Done, true)]
    [InlineData(WorkItemStatus.Done, WorkItemStatus.Todo, false)]
    [InlineData(WorkItemStatus.Done, WorkItemStatus.InProgress, false)]
    [InlineData(WorkItemStatus.Done, WorkItemStatus.Done, false)]
    public void Domain_allows_only_the_next_status(WorkItemStatus current, WorkItemStatus next, bool expected)
    {
        var item = new WorkItem { Title = "Transition test", CreatedAt = DateTimeOffset.UtcNow };
        if (current is WorkItemStatus.InProgress or WorkItemStatus.Done)
            Assert.True(item.TryTransitionTo(WorkItemStatus.InProgress));
        if (current is WorkItemStatus.Done)
            Assert.True(item.TryTransitionTo(WorkItemStatus.Done));

        Assert.Equal(expected, item.TryTransitionTo(next));
        Assert.Equal(expected ? next : current, item.Status);
    }

    private static (SqliteConnection connection, WorkItemsDbContext db) CreateDb()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var db = new WorkItemsDbContext(new DbContextOptionsBuilder<WorkItemsDbContext>().UseSqlite(connection).Options);
        db.Database.EnsureCreated();
        return (connection, db);
    }

    [Fact]
    public async Task Status_moves_forward_and_rejects_skips_and_reversals()
    {
        var (connection, db) = CreateDb();
        try
        {
            db.WorkItems.Add(new WorkItem { Title = "Prepare release", CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
            var controller = new WorkItemsController(new WorkItemsService(db));

            var skipped = await controller.UpdateStatus(1, new UpdateStatusRequest { Status = "Done" }, default);
            Assert.IsType<ConflictObjectResult>(skipped.Result);
            var started = await controller.UpdateStatus(1, new UpdateStatusRequest { Status = "InProgress" }, default);
            Assert.IsType<OkObjectResult>(started.Result);
            var reversed = await controller.UpdateStatus(1, new UpdateStatusRequest { Status = "Todo" }, default);
            Assert.IsType<ConflictObjectResult>(reversed.Result);
            var completed = await controller.UpdateStatus(1, new UpdateStatusRequest { Status = "Done" }, default);
            Assert.IsType<OkObjectResult>(completed.Result);
        }
        finally { await db.DisposeAsync(); await connection.DisposeAsync(); }
    }

    [Fact]
    public async Task Created_work_item_survives_new_context_and_missing_item_returns_404()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<WorkItemsDbContext>().UseSqlite(connection).Options;
        try
        {
            int id;
            await using (var first = new WorkItemsDbContext(options))
            {
                await first.Database.EnsureCreatedAsync();
                var response = await new WorkItemsController(new WorkItemsService(first)).Create(new CreateWorkItemRequest { Title = "Persist me" }, default);
                var created = Assert.IsType<CreatedAtActionResult>(response.Result);
                id = Assert.IsType<WorkItemResponse>(created.Value).Id;
            }
            await using var second = new WorkItemsDbContext(options);
            var controller = new WorkItemsController(new WorkItemsService(second));
            var found = await controller.GetById(id, default);
            Assert.IsType<OkObjectResult>(found.Result);
            var missing = await controller.GetById(id + 1, default);
            Assert.IsType<NotFoundResult>(missing.Result);
        }
        finally { await connection.DisposeAsync(); }
    }

    [Fact]
    public async Task Invalid_status_returns_bad_request()
    {
        var (connection, db) = CreateDb();
        try
        {
            db.WorkItems.Add(new WorkItem { Title = "Example", CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
            var controller = new WorkItemsController(new WorkItemsService(db));
            Assert.IsType<BadRequestObjectResult>((await controller.UpdateStatus(1, new UpdateStatusRequest { Status = "Archived" }, default)).Result);
        }
        finally { await db.DisposeAsync(); await connection.DisposeAsync(); }
    }
}
