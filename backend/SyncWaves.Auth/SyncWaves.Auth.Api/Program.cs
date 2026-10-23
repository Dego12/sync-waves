using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SyncWaves.Auth.Infrastructure.Data;
using SyncWaves.Auth.Infrastructure.Extensions;
using SyncWaves.Auth.Api.Middlewares;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

var locOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>();
app.UseRequestLocalization(locOptions.Value);
app.UseMiddleware<AuthExceptionHandlingMiddleware>();

app.UseRouting();
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
