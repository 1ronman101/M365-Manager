using System.Text.Json;

namespace M365Manager.Helpers;

public class AppSettings
{
    private static readonly string SettingsPath = Path.Combine(
        AppContext.BaseDirectory, "appsettings.json");

    public string ClientId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = "http://localhost";
    public string Version { get; set; } = "1.0.0";
    public string LogFilePath { get; set; } = "logs/m365manager-.log";

    public static AppSettings Load()
    {
        var settings = new AppSettings();
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("AzureAd", out var ad))
                {
                    settings.ClientId = ad.TryGetProperty("ClientId", out var c) ? c.GetString() ?? "" : "";
                    settings.TenantId = ad.TryGetProperty("TenantId", out var t) ? t.GetString() ?? "" : "";
                    settings.RedirectUri = ad.TryGetProperty("RedirectUri", out var r) ? r.GetString() ?? "http://localhost" : "http://localhost";
                }

                if (root.TryGetProperty("App", out var app))
                {
                    settings.Version = app.TryGetProperty("Version", out var v) ? v.GetString() ?? "1.0.0" : "1.0.0";
                    settings.LogFilePath = app.TryGetProperty("LogFilePath", out var l) ? l.GetString() ?? "logs/m365manager-.log" : "logs/m365manager-.log";
                }
            }
        }
        catch
        {
            // Return defaults on any parse error.
        }
        return settings;
    }

    public void Save()
    {
        var obj = new
        {
            AzureAd = new
            {
                ClientId,
                TenantId,
                RedirectUri
            },
            App = new
            {
                Version,
                LogFilePath
            }
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(obj, options);
        File.WriteAllText(SettingsPath, json);
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId)
        && ClientId != "YOUR-CLIENT-ID-HERE"
        && !string.IsNullOrWhiteSpace(TenantId)
        && TenantId != "YOUR-TENANT-ID-HERE";
}
