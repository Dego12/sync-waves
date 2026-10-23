using Microsoft.Extensions.Configuration;

namespace SyncWaves.Auth.UnitTests;

internal static class TestHelpers
{
    public static Dictionary<string, string> ResxHelper =  new Dictionary<string, string>
    {
        ["Register_Email_Confirmation_Html"] =
        "<p>Hello,</p>" +
        "<p>Welcome to SyncWaves!</p>" +
        "<p>Please confirm your email by clicking <a href=\"{0}\">here</a>.</p>" +
        "<p>If you did not create this account, you can ignore this email.</p>",
        ["Register_Email_Confirmation_Subject"] = "SyncWaves - confirm your email",
        ["Register_Email_Already_Registered"] = "Register_Email_Already_Registered",
        ["Reset_Password_Html"] = "<p>Hello,</p> <p>You can reset your password by clicking <a href=\"{0}\">here</a>.</p>",
        ["Reset_Password_Subject"] = "SyncWaves - reset password",
        ["User_Does_Not_Exist"] = "User_Does_Not_Exist"
    };

    public static IConfiguration BuildConfiguration(string frontendBaseUrl = "https://frontend.local")
    {
        var data = new Dictionary<string, string?>
        {
            ["App:FrontendBaseUrl"] = frontendBaseUrl
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();
    }
}
