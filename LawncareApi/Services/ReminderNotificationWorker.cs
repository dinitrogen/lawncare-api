using Google.Cloud.Firestore;
using LawncareApi.Models;

namespace LawncareApi.Services;

/// <summary>
/// Hosted background service that polls Firestore for reminders whose date/time
/// has arrived and dispatches Discord notifications for them.
///
/// Runs every <c>ReminderNotifications:PollIntervalMinutes</c> minutes (default: 5).
/// Requires a Firestore composite index on the <c>reminders</c> collection group
/// for the fields <c>SendDiscordReminder</c>, <c>NotificationSent</c>, and <c>Date</c>.
/// </summary>
public class ReminderNotificationWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ReminderNotificationWorker> _logger;
    private readonly TimeSpan _pollInterval;

    public ReminderNotificationWorker(
        IServiceProvider services,
        ILogger<ReminderNotificationWorker> logger,
        IConfiguration configuration)
    {
        _services = services;
        _logger = logger;
        var intervalMinutes = configuration.GetValue<int>("ReminderNotifications:PollIntervalMinutes", 5);
        _pollInterval = TimeSpan.FromMinutes(intervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Brief startup delay so the host finishes initializing before the first poll.
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            await SendDueNotificationsAsync(stoppingToken);
            await Task.Delay(_pollInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    // Internal for testing.
    internal async Task SendDueNotificationsAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FirestoreDb>();
            var discord = scope.ServiceProvider.GetRequiredService<DiscordNotificationService>();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

            var utcNow = DateTime.UtcNow;
            var todayStr = DateOnly.FromDateTime(utcNow).ToString("yyyy-MM-dd");

            // Collection-group query across all users' reminder sub-collections.
            // Requires a composite index: SendDiscordReminder ASC, NotificationSent ASC, Date ASC.
            var snapshot = await db.CollectionGroup("reminders")
                .WhereEqualTo(nameof(Reminder.SendDiscordReminder), true)
                .WhereEqualTo(nameof(Reminder.NotificationSent), false)
                .WhereLessThanOrEqualTo(nameof(Reminder.Date), todayStr)
                .GetSnapshotAsync(ct);

            foreach (var doc in snapshot.Documents)
            {
                var reminder = doc.ConvertTo<Reminder>();

                // For reminders due today with a specific time, wait until that time has passed.
                if (DateOnly.ParseExact(reminder.Date, "yyyy-MM-dd", null) == DateOnly.FromDateTime(utcNow)
                    && reminder.Time is not null)
                {
                    if (!TimeOnly.TryParseExact(
                            reminder.Time, "HH:mm", null,
                            System.Globalization.DateTimeStyles.None,
                            out var reminderTime)
                        || TimeOnly.FromDateTime(utcNow) < reminderTime)
                    {
                        continue;
                    }
                }

                // Extract user UID from path: users/{uid}/reminders/{id}
                var uid = doc.Reference.Parent.Parent!.Id;

                try
                {
                    // Atomically claim the reminder so concurrent instances don't double-send.
                    var claimed = false;
                    Reminder? claimedReminder = null;
                    await db.RunTransactionAsync(async transaction =>
                    {
                        var fresh = await transaction.GetSnapshotAsync(doc.Reference);
                        if (!fresh.Exists) return;
                        var current = fresh.ConvertTo<Reminder>();
                        if (current.NotificationSent)
                        {
                            claimed = false;
                            return;
                        }
                        current.NotificationSent = true;
                        transaction.Set(doc.Reference, current);
                        claimed = true;
                        claimedReminder = current;
                    }, cancellationToken: ct);

                    if (!claimed || claimedReminder is null)
                        continue;

                    var user = await userService.GetAsync(uid, ct);
                    if (user?.DiscordWebhookUrl is not null)
                    {
                        await discord.SendReminderNotificationAsync(
                            user.DiscordWebhookUrl,
                            claimedReminder.Title,
                            claimedReminder.Date,
                            claimedReminder.Time);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send notification for reminder {Id}", doc.Id);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during graceful shutdown; do not log as an error.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error while processing due reminder notifications");
        }
    }
}
