    using Microsoft.AspNetCore.Mvc;
    using SyncWaves.Auth.Application.Resources;
    using SyncWaves.Auth.Domain.Exceptions;
    using System.Net;
    using System.Security.Authentication;
    using System.Text.Json;

    namespace SyncWaves.Auth.Api.Middlewares;

    public sealed class AuthExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public AuthExceptionHandlingMiddleware(RequestDelegate next,
            ILogger<AuthExceptionHandlingMiddleware> logger,
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
            catch(Exception ex)
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
                InvalidCredentialException or TokenInvalidException => ((int)HttpStatusCode.Unauthorized, "Invalid credentials"),
                EmailNotConfirmedException => ((int)HttpStatusCode.BadRequest, "Email not confirmed"),
                ArgumentException => ((int)HttpStatusCode.Conflict, "Conflict"),
                UserNotFoundException => ((int)HttpStatusCode.NotFound, "User not found"),
                EmailAlreadyConfirmedException => ((int)HttpStatusCode.Forbidden, "Email already confirmed"),
                _ => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred")
            };
    }
