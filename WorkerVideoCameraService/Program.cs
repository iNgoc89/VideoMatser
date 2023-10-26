using FFmpegWebAPI.Models;
using Microsoft.EntityFrameworkCore;
using WorkerVideoCameraService;
using WorkerVideoCameraService.Services;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;

        services.AddHostedService<ScopedVideoCameraService>();
        services.AddScoped<IScopedVideoCameraService, ScopedProcessingService>();
        services.AddScoped<XmhtService>();
        services.AddDbContext<IOTContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("IOTConnection")));
     
    })
    .Build();

host.Run();
