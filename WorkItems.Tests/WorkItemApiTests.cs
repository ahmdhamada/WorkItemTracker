using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WorkItems.Api.Contracts;
using WorkItems.Api.Controllers;
using WorkItems.Api.Data;
using WorkItems.Api.Models;

namespace WorkItems.Tests;

public sealed class WorkItemApiTests
{
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
            var controller = new WorkItemsController(db);

            var skipped = await controller.UpdateStatus(1, new UpdateStatusRequest { Status = "Done" }, default);
            Assert.IsType<ConflictObjectResult>(skipped);
            var started = await controller.UpdateStatus(1, new UpdateStatusRequest { Status = "InProgress" }, default);
            Assert.IsType<OkObjectResult>(started);
            var reversed = await controller.UpdateStatus(1, new UpdateStatusRequest { Status = "Todo" }, default);
            Assert.IsType<ConflictObjectResult>(reversed);
            var completed = await controller.UpdateStatus(1, new UpdateStatusRequest { Status = "Done" }, default);
            Assert.IsType<OkObjectResult>(completed);
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
                var response = await new WorkItemsController(first).Create(new CreateWorkItemRequest { Title = "Persist me" }, default);
                var created = Assert.IsType<CreatedAtActionResult>(response.Result);
                id = Assert.IsType<WorkItemResponse>(created.Value).Id;
            }
            await using var second = new WorkItemsDbContext(options);
            var controller = new WorkItemsController(second);
            var found = await controller.GetById(id, default);
            Assert.IsType<OkObjectResult>(found.Result);
            var missing = await controller.GetById(id + 1, default);
            Assert.IsType<NotFoundResult>(missing.Result);
        }
        finally { await connection.DisposeAsync(); }
    }

    [Fact]
    public async Task Invalid_status_and_invalid_page_are_bad_requests()
    {
        var (connection, db) = CreateDb();
        try
        {
            db.WorkItems.Add(new WorkItem { Title = "Example", CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
            var controller = new WorkItemsController(db);
            Assert.IsType<BadRequestObjectResult>(await controller.UpdateStatus(1, new UpdateStatusRequest { Status = "Archived" }, default));
            Assert.IsType<BadRequestObjectResult>((await controller.List(null, null, page: 0, ct: default)).Result);
        }
        finally { await db.DisposeAsync(); await connection.DisposeAsync(); }
    }
}
