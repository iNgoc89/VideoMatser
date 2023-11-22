using FFmpegWebAPI.Models;
using FFmpegWebAPI.Services;
using Microsoft.EntityFrameworkCore;
using WorkerVideoCameraService;
using WorkerVideoCameraService.Services;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;
        //service get video
        services.AddHostedService<ScopedVideoCameraService>();
        services.AddScoped<IScopedVideoCameraService, ScopedProcessingService>();

        //service delete video
        services.AddHostedService<DeleteVideoCameraService>();
        services.AddScoped<IDeleteVideoCameraService, DeleteProcessingService>();

        //service concat video
        services.AddHostedService<ConcatVideoCameraService>();
        services.AddScoped<IConcatVideoCameraService, ConcatProcessingService>();

        services.AddScoped<XmhtService>();
        services.AddScoped<WorkVideoService>();
        services.AddScoped<IOTService>();

        //add windown service
        services.AddWindowsService();
        
        
        services.AddDbContext<IOTContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("IOTConnection")));
     
    })
    .ConfigureLogging((context, logging) => {
        var env = context.HostingEnvironment;
        var config = context.Configuration.GetSection("Logging");
       
        logging.AddConfiguration(config);
        logging.AddConsole();
      
        logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None);
        logging.AddFilter("System.IO.IOException", LogLevel.None);
    })
    .Build();

host.Run();
