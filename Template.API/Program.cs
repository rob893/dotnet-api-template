
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Template.API.ApplicationStartup.ApplicationBuilderExtensions;
using Template.API.ApplicationStartup.ServiceCollectionExtensions;
using Template.API.Middleware;

namespace Template.API;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

        builder.Services.AddControllerServices()
            .AddHealthCheckServices()
            .AddSwaggerServices(builder.Configuration)
            .AddCors()
            .AddHttpClient();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseExceptionHandler(x => x.UseMiddleware<GlobalExceptionHandlerMiddleware>())
            .UseRouting()
            .UseHsts()
            .UseHttpsRedirection()
            .UseAndConfigureCors(builder.Configuration)
            .UseAuthentication()
            .UseAuthorization()
            .UseAndConfigureSwagger(builder.Configuration)
            .UseAndConfigureEndpoints(builder.Configuration);

        app.Run();
    }
}
