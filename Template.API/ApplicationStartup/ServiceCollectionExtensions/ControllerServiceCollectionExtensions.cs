using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Template.API.Core;

namespace Template.API.ApplicationStartup.ServiceCollectionExtensions;

public static class ControllerServiceCollectionExtensions
{
    public static IServiceCollection AddControllerServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));

        services.AddVersionedApiExplorer(
            options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = ApiVersion.Default;
                options.GroupNameFormat = "'v'VVV";
            });

        services.AddApiVersioning(
            options =>
            {
                options.ErrorResponses = new ApiVersioningErrorResponseProvider();
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
                options.DefaultApiVersion = ApiVersion.Default;
            });

        services.AddControllers(options =>
        {
            // This allows for global authorization. No need to have [Authorize] attribute on controllers with this.
            // This is what requires tokens for all endpoints. Add [AllowAnonymous] to any endpoint not requiring tokens (like login)
            var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            options.Filters.Add(new AuthorizeFilter(policy));

            options.Filters.Add(new ProducesAttribute("application/json"));
            options.Filters.Add(new ProducesResponseTypeAttribute(typeof(ProblemDetailsWithErrors), StatusCodes.Status400BadRequest));
            options.Filters.Add(new ProducesResponseTypeAttribute(typeof(ProblemDetailsWithErrors), StatusCodes.Status401Unauthorized));
            options.Filters.Add(new ProducesResponseTypeAttribute(typeof(ProblemDetailsWithErrors), StatusCodes.Status403Forbidden));
            options.Filters.Add(new ProducesResponseTypeAttribute(typeof(ProblemDetailsWithErrors), StatusCodes.Status500InternalServerError));
            options.Filters.Add(new ProducesResponseTypeAttribute(typeof(ProblemDetailsWithErrors), StatusCodes.Status504GatewayTimeout));
        })
        .ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = _ => new ValidationProblemDetailsResult();
        });

        return services;
    }
}