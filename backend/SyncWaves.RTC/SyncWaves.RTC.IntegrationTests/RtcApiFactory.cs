using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

public class RtcApiFactory : WebApplicationFactory<Program>
{
    public const string TestJwtKey = "rtc-integration-tests-key-1234567890-abcdef";
    public const string TestJwtIssuer = "rtc-integration-tests";
    public const string TestJwtAudience = "rtc-integration-tests";

    static RtcApiFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__Key", TestJwtKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", TestJwtIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", TestJwtAudience);
        Environment.SetEnvironmentVariable("App__FrontendBaseUrl", "http://frontend.test");
        Environment.SetEnvironmentVariable("App__DocumentsBaseUrl", "http://documents.test");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");

        builder.ConfigureServices(services =>
        {
            services.AddHttpClient("Frontend")
                .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://frontend.test"));
            services.AddHttpClient("DocumentsApi")
                .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://documents.test"))
                .ConfigurePrimaryHttpMessageHandler(() => new FakeDocumentsApiHandler());
        });
    }

    private sealed class FakeDocumentsApiHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (request.Method == HttpMethod.Get && path.EndsWith("/permissions/me", StringComparison.OrdinalIgnoreCase))
            {
                var json = "{\"isOwner\":true,\"canEdit\":true}";
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            }

            if (request.Method == HttpMethod.Post && path.Contains("/permissions", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("Not found")
            });
        }
    }
}
