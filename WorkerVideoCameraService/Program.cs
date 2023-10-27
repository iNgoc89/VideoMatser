using FFmpegWebAPI.Models;
using Microsoft.EntityFrameworkCore;
using WorkerVideoCameraService;
using WorkerVideoCameraService.Services;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;
        //service lấy video
        services.AddHostedService<ScopedVideoCameraService>();
        services.AddScoped<IScopedVideoCameraService, ScopedProcessingService>();

        //service delete video
        //services.AddHostedService<DeleteVideoCameraService>();
        //services.AddScoped<IDeleteVideoCameraService, DeleteProcessingService>();

        services.AddScoped<XmhtService>();
        services.AddScoped<WorkVideoService>();
        services.AddDbContext<IOTContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("IOTConnection")));
     
    })
    .Build();

host.Run();
