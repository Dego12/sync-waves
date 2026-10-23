using Microsoft.AspNetCore.Mvc;
using SyncWaves.Documents.Application.Resources;
using SyncWaves.Documents.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace SyncWaves.Documents.Api.Middlewares;

public sealed class DocumentsExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DocumentsExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public DocumentsExceptionHandlingMiddleware(RequestDelegate next,
        ILogger<DocumentsExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task Invoke(HttpContext context, IResourceLocalizer localizer)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing {Path}", context.Request.Path);

            if (context.Response.HasStarted)
            {
                throw;
            }

            var (statusCode, title) = MapException(ex);

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = localizer.Localize(ex.Message),
                Instance = context.Request.Path
            };

            var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }

    private static (int statusCode, string title) MapException(Exception ex) =>
        ex switch
        {
            UnauthorizedAccessException => ((int)HttpStatusCode.Forbidden, "Only owner can edit"),
            DocumentNotFoundException => ((int)HttpStatusCode.NoContent, "No documents"),
            _ => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred")
        };
}
