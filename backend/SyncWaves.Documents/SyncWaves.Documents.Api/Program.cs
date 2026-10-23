using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SyncWaves.Documents.Api.Middlewares;
using SyncWaves.Documents.Infrastructure.Data;
using SyncWaves.Documents.Infrastructure.Extensions;
using System;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DocumentsDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<DocumentsExceptionHandlingMiddleware>();

var locOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>();
app.UseRequestLocalization(locOptions.Value);

app.UseRouting();
app.UseCors("AllowFrontendAndRtc");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
