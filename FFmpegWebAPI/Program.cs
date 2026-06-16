using MetaData.Context;
using MetaData.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;
using System.Web.Services.Description;

internal class Program
{
    static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers()
              .AddJsonOptions(options =>
              {
                  options.JsonSerializerOptions.PropertyNamingPolicy = null;
              });
       
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddDbContext<IOTContext>(options =>
                  options.UseSqlServer(builder.Configuration.GetConnectionString("IOTConnection")));
        builder.Services.AddScoped<IOTService>();
        builder.Services.AddScoped<WorkVideoService>();
        builder.Services.AddScoped<XmhtService>();
        builder.Services.AddScoped<WorkImageService>();
        builder.Services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("databases")
            .AddCheck<StorageFolderHealthCheck>("storage-folders")
            .AddCheck<FFmpegHealthCheck>("ffmpeg");


        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                var exception = exceptionFeature?.Error;
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

                if (exception != null)
                {
                    logger.LogError(exception, "Unhandled exception for {Method} {Path}.", context.Request.Method, context.Request.Path);
                }

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                await JsonSerializer.SerializeAsync(context.Response.Body, new
                {
                    ErrMsg = "Lỗi hệ thống khi xử lý yêu cầu.",
                    TraceId = context.TraceIdentifier
                });
            });
        });

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
       
        app.UseMiddleware<CustomApiKeyService>();
        app.UseStaticFiles();
        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        });

        app.MapHealthChecks("/health/detail", new HealthCheckOptions
        {
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            },
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                await JsonSerializer.SerializeAsync(context.Response.Body, new
                {
                    status = report.Status.ToString(),
                    duration = report.TotalDuration.ToString(),
                    checks = report.Entries.Select(entry => new
                    {
                        name = entry.Key,
                        status = entry.Value.Status.ToString(),
                        description = entry.Value.Description,
                        error = entry.Value.Exception?.Message,
                        duration = entry.Value.Duration.ToString()
                    })
                }, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
        });
        app.MapControllers();

        app.Run();
    }

}
