using Azure.Core;
using FFmpegWebAPI.Data;
using FFmpegWebAPI.Models;
using FFmpegWebAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Expressions;
using System;
using System.Diagnostics;
using System.Xml.Xsl;

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
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddDbContext<IOTContext>(options =>
                  options.UseSqlServer(builder.Configuration.GetConnectionString("IOTConnection")));
        builder.Services.AddScoped<IOTService>();
        builder.Services.AddScoped<WorkVideoService>();
        builder.Services.AddScoped<XmhtService>();
       
           
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
       
        app.UseMiddleware<CustomApiKeyMiddleware>();
        app.UseStaticFiles();
        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }

}