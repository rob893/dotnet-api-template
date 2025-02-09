using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Template.API.Core;
using Template.API.Extensions;

using static Template.API.Utilities.UtilityFunctions;

namespace Template.API.Middleware;

public sealed class GlobalExceptionHandlerMiddleware
{
    private readonly ILogger<GlobalExceptionHandlerMiddleware> logger;

    private readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GlobalExceptionHandlerMiddleware(RequestDelegate _, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var error = context.Features.Get<IExceptionHandlerFeature>();

        if (error != null)
        {
            var sourceName = GetSourceName();
            var thrownException = error.Error;
            var correlationId = context.Request.Headers.GetOrGenerateCorrelationId();
            var statusCode = StatusCodes.Status500InternalServerError;

            switch (thrownException)
            {
                case TimeoutException:
                    statusCode = StatusCodes.Status504GatewayTimeout;
                    break;
                default:
                    break;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var problemDetails = new ProblemDetailsWithErrors(thrownException, context.Response.StatusCode, context.Request);

            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                this.logger.LogError(sourceName, correlationId, thrownException.Message);
            }
            else
            {
                this.logger.LogWarning(sourceName, correlationId, thrownException.Message);
            }

            var jsonResponse = JsonSerializer.Serialize(problemDetails, this.jsonOptions);

            await context.Response.WriteAsync(jsonResponse);
        }
    }
}