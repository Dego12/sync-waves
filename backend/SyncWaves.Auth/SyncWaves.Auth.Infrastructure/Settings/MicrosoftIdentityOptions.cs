namespace SyncWaves.Auth.Infrastructure.Settings;

public static class MicrosoftIdentityOptions
{
    public static string ClientId { get; set; } = string.Empty;
    public static string TenantId { get; set; } = string.Empty;
    public static string Instance { get; set; } = string.Empty;
    public static string Authority { get; set; } = string.Empty;
}
