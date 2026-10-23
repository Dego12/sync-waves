using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SyncWaves.Documents.Api.Controllers;
using SyncWaves.Documents.Api.Middlewares;
using SyncWaves.Documents.Application.Handlers.Commands;
using SyncWaves.Documents.Application.Resources;
using SyncWaves.Documents.Domain.Abstractions;

public class DocumentsApiFixture : IAsyncLifetime
{
    private WebApplication? _app;

    public HttpClient Client { get; private set; } = default!;
    public InMemoryDocumentsRepository Documents { get; private set; } = default!;
    public InMemoryCollaboratorsRepository Collaborators { get; private set; } = default!;
    public FakePdfGenerator Pdfs { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing"
        });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MediatR:LicenseKey"] = ""
        });

        builder.WebHost.UseTestServer();

        var services = builder.Services;

        services.AddControllers()
            .AddApplicationPart(typeof(DocumentsController).Assembly);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateDocumentCommandHandler).Assembly);
        });

        Documents = new InMemoryDocumentsRepository();
        Collaborators = new InMemoryCollaboratorsRepository(Documents);
        Pdfs = new FakePdfGenerator();

        services.AddSingleton<IDocumentsRepository>(Documents);
        services.AddSingleton<IDocumentPermissionsRepository>(Collaborators);
        services.AddSingleton<IDocumentPdfGenerator>(Pdfs);
        services.AddSingleton<IResourceLocalizer>(new DictionaryResourceLocalizer());

        services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

        services.AddAuthorization();

        _app = builder.Build();

        _app.UseRouting();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.UseMiddleware<DocumentsExceptionHandlingMiddleware>();
        _app.MapControllers();

        await _app.StartAsync();
        Client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}
