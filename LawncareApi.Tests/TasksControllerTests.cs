using LawncareApi.Controllers;
using LawncareApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace LawncareApi.Tests;

public class TasksControllerTests
{
    private static TasksController CreateController(InMemoryLawnCareTaskService? svc = null) =>
        new(svc ?? new InMemoryLawnCareTaskService(), NullLogger<TasksController>.Instance);

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoTasksExist()
    {
        var controller = CreateController();
        var result = await controller.GetAll(CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        var tasks = Assert.IsAssignableFrom<IReadOnlyList<LawnCareTask>>(ok.Value);
        Assert.Empty(tasks);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenTitleIsEmpty()
    {
        var controller = CreateController();
        var request = new LawnCareTaskRequest { Title = "" };
        var result = await controller.Create(request, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsCreated_WithPopulatedTask()
    {
        var controller = CreateController();
        var request = new LawnCareTaskRequest
        {
            Title = "Mow the lawn",
            Category = TaskCategory.Mowing,
            DueDate = DateTime.UtcNow.AddDays(3)
        };

        var result = await controller.Create(request, CancellationToken.None);
        var created = Assert.IsType<CreatedAtActionResult>(result);
        var task = Assert.IsType<LawnCareTask>(created.Value);

        Assert.Equal("Mow the lawn", task.Title);
        Assert.Equal(TaskCategory.Mowing, task.Category);
        Assert.NotNull(task.Id);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_ForUnknownId()
    {
        var controller = CreateController();
        var result = await controller.GetById("nonexistent", CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_ReturnsTask_AfterCreate()
    {
        var svc = new InMemoryLawnCareTaskService();
        var controller = CreateController(svc);

        var created = await svc.CreateAsync(new LawnCareTaskRequest { Title = "Fertilize" });

        var result = await controller.GetById(created.Id!, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        var task = Assert.IsType<LawnCareTask>(ok.Value);
        Assert.Equal("Fertilize", task.Title);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_ForUnknownId()
    {
        var controller = CreateController();
        var result = await controller.Update(
            "none",
            new LawnCareTaskRequest { Title = "X" },
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_UpdatesTask_AndSetsCompletedAt()
    {
        var svc = new InMemoryLawnCareTaskService();
        var controller = CreateController(svc);

        var created = await svc.CreateAsync(new LawnCareTaskRequest { Title = "Weed beds" });

        var request = new LawnCareTaskRequest
        {
            Title = "Weed beds",
            IsCompleted = true
        };

        var result = await controller.Update(created.Id!, request, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        var task = Assert.IsType<LawnCareTask>(ok.Value);

        Assert.True(task.IsCompleted);
        Assert.NotNull(task.CompletedAt);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_ForExistingTask()
    {
        var svc = new InMemoryLawnCareTaskService();
        var controller = CreateController(svc);

        var created = await svc.CreateAsync(new LawnCareTaskRequest { Title = "Aerate" });

        var result = await controller.Delete(created.Id!, CancellationToken.None);
        Assert.IsType<NoContentResult>(result);

        // Confirm it's gone
        var getResult = await controller.GetById(created.Id!, CancellationToken.None);
        Assert.IsType<NotFoundResult>(getResult);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_ForUnknownId()
    {
        var controller = CreateController();
        var result = await controller.Delete("ghost", CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }
}
