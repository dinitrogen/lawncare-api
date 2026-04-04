using LawncareApi.Controllers;
using LawncareApi.Models;
using LawncareApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;

namespace LawncareApi.Tests;

public class RemindersControllerTests
{
    private const string TestUid = "test-uid";

    private static RemindersController CreateController(InMemoryReminderService? svc = null)
    {
        var controller = new RemindersController(
            svc ?? new InMemoryReminderService(),
            NullLogger<RemindersController>.Instance);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, TestUid) }))
            }
        };

        return controller;
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoRemindersExist()
    {
        var controller = CreateController();
        var result = await controller.GetAll(CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        var reminders = Assert.IsAssignableFrom<IReadOnlyList<Reminder>>(ok.Value);
        Assert.Empty(reminders);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenTitleIsEmpty()
    {
        var controller = CreateController();
        var request = new ReminderRequest { Title = "", Date = "2025-06-01" };
        var result = await controller.Create(request, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenDateIsEmpty()
    {
        var controller = CreateController();
        var request = new ReminderRequest { Title = "Fertilize", Date = "" };
        var result = await controller.Create(request, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenDateIsInvalidFormat()
    {
        var controller = CreateController();
        var request = new ReminderRequest { Title = "Fertilize", Date = "not-a-date" };
        var result = await controller.Create(request, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsCreated_WithPopulatedReminder()
    {
        var controller = CreateController();
        var request = new ReminderRequest
        {
            Title = "Apply pre-emergent",
            Date = "2025-03-15",
            Time = "08:00",
            Notes = "Use crabgrass preventer",
            SendDiscordReminder = false,
        };

        var result = await controller.Create(request, CancellationToken.None);
        var created = Assert.IsType<CreatedAtActionResult>(result);
        var reminder = Assert.IsType<Reminder>(created.Value);

        Assert.Equal("Apply pre-emergent", reminder.Title);
        Assert.Equal("2025-03-15", reminder.Date);
        Assert.Equal("08:00", reminder.Time);
        Assert.NotNull(reminder.Id);
    }

    /// <summary>
    /// Notifications must NOT be dispatched at creation time; the background worker
    /// sends them on the reminder's scheduled date/time.
    /// </summary>
    [Fact]
    public async Task Create_SetsNotificationSent_False_OnCreation()
    {
        var svc = new InMemoryReminderService();
        var controller = CreateController(svc);

        var request = new ReminderRequest
        {
            Title = "Overseed",
            Date = "2025-09-15",
            SendDiscordReminder = true,
        };

        var result = await controller.Create(request, CancellationToken.None);
        var created = Assert.IsType<CreatedAtActionResult>(result);
        var reminder = Assert.IsType<Reminder>(created.Value);

        Assert.False(reminder.NotificationSent,
            "NotificationSent must be false at creation; the worker will set it to true on the event date.");
    }

    /// <summary>
    /// Updating a reminder resets <see cref="Reminder.NotificationSent"/> so the
    /// background worker re-evaluates it on the new date/time.
    /// </summary>
    [Fact]
    public async Task Update_ResetsNotificationSent_ToFalse()
    {
        var svc = new InMemoryReminderService();
        var controller = CreateController(svc);

        var created = await svc.CreateAsync(TestUid, new ReminderRequest
        {
            Title = "Fertilize",
            Date = "2025-05-01",
            SendDiscordReminder = true,
        });

        // Simulate the worker marking the notification as sent.
        svc.MarkNotificationSentForTest(created.Id!);

        var updateRequest = new ReminderRequest
        {
            Title = "Fertilize (rescheduled)",
            Date = "2025-05-15",
            SendDiscordReminder = true,
        };

        var result = await controller.Update(created.Id!, updateRequest, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        var updated = Assert.IsType<Reminder>(ok.Value);

        Assert.False(updated.NotificationSent,
            "Updating a reminder must reset NotificationSent so the worker fires again on the new date.");
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_ForUnknownId()
    {
        var controller = CreateController();
        var result = await controller.GetById("nonexistent", CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_ReturnsReminder_AfterCreate()
    {
        var svc = new InMemoryReminderService();
        var controller = CreateController(svc);

        var created = await svc.CreateAsync("uid", new ReminderRequest { Title = "Mow", Date = "2025-05-01" });

        var result = await controller.GetById(created.Id!, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        var reminder = Assert.IsType<Reminder>(ok.Value);
        Assert.Equal("Mow", reminder.Title);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_ForUnknownId()
    {
        var controller = CreateController();
        var result = await controller.Update(
            "none",
            new ReminderRequest { Title = "X", Date = "2025-01-01" },
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_UpdatesReminder_Fields()
    {
        var svc = new InMemoryReminderService();
        var controller = CreateController(svc);

        var created = await svc.CreateAsync("uid", new ReminderRequest { Title = "Old title", Date = "2025-01-01" });

        var request = new ReminderRequest
        {
            Title = "New title",
            Date = "2025-06-15",
            Time = "10:00",
            Notes = "Updated notes",
            SendDiscordReminder = true,
        };

        var result = await controller.Update(created.Id!, request, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        var reminder = Assert.IsType<Reminder>(ok.Value);

        Assert.Equal("New title", reminder.Title);
        Assert.Equal("2025-06-15", reminder.Date);
        Assert.Equal("10:00", reminder.Time);
        Assert.True(reminder.SendDiscordReminder);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_ForExistingReminder()
    {
        var svc = new InMemoryReminderService();
        var controller = CreateController(svc);

        var created = await svc.CreateAsync("uid", new ReminderRequest { Title = "Aerate", Date = "2025-09-01" });

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
