using System.Text;
using System.Text.Json;

namespace LawncareApi.Services;

/// <summary>
/// Sends Discord webhook notifications for treatment logs and GDD alerts.
/// Replaces the Firebase Cloud Functions that previously handled this.
/// </summary>
public class DiscordNotificationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DiscordNotificationService> _logger;

    public DiscordNotificationService(IHttpClientFactory httpClientFactory, ILogger<DiscordNotificationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SendTreatmentNotificationAsync(
        string webhookUrl, string productName, string zoneName, double amount, string date)
    {
        var payload = new
        {
            content = "",
            embeds = new[]
            {
                new
                {
                    title = "🌿 Treatment Applied",
                    description = $"Applied **{productName}** to **{zoneName}**",
                    color = 0x2e7d32,
                    fields = new[]
                    {
                        new { name = "Amount", value = $"{amount} oz", inline = true },
                        new { name = "Date", value = date, inline = true },
                    }
                }
            }
        };

        await SendWebhookAsync(webhookUrl, payload);
    }

    public async Task SendGddAlertAsync(
        string webhookUrl, double currentGdd, string thresholdName, string gddRange)
    {
        var payload = new
        {
            content = "",
            embeds = new[]
            {
                new
                {
                    title = "🌡️ GDD Threshold Alert",
                    description = $"Current cumulative GDD: **{currentGdd}**",
                    color = 0xff9800,
                    fields = new[]
                    {
                        new { name = "Threshold", value = thresholdName, inline = true },
                        new { name = "GDD Range", value = gddRange, inline = true },
                    }
                }
            }
        };

        await SendWebhookAsync(webhookUrl, payload);
    }

    public async Task SendReminderNotificationAsync(string webhookUrl, string title, string date, string? time)
    {
        var dateDisplay = time is not null ? $"{date} at {time}" : date;
        var payload = new
        {
            content = "",
            embeds = new[]
            {
                new
                {
                    title = "🔔 Reminder",
                    description = $"**{title}**",
                    color = 0x1565c0,
                    fields = new[]
                    {
                        new { name = "Date", value = dateDisplay, inline = true },
                    }
                }
            }
        };

        await SendWebhookAsync(webhookUrl, payload);
    }

    private static bool IsValidDiscordWebhookUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        uri.Scheme == "https" &&
        (uri.Host == "discord.com" || uri.Host == "discordapp.com") &&
        uri.AbsolutePath.StartsWith("/api/webhooks/", StringComparison.Ordinal);

    private async Task SendWebhookAsync(string webhookUrl, object payload)
    {
        if (string.IsNullOrEmpty(webhookUrl)) return;

        if (!IsValidDiscordWebhookUrl(webhookUrl))
        {
            _logger.LogWarning("Blocked webhook request to non-Discord URL: {Url}", webhookUrl);
            return;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(webhookUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Discord webhook failed with status {Status}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send Discord webhook notification");
        }
    }
}
