
using MetaData.Context;
using MetaData.Services;
using Microsoft.EntityFrameworkCore;
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

        //service delete txt
        services.AddHostedService<DeleteTxtService>();
        services.AddScoped<IDeleteTxtService, DeleteTxtProcessingService>();

        //service delete image
        services.AddHostedService<DeleteImageCameraService>();
        services.AddScoped<IDeleteImageCameraService, DeleteImageProcessingService>();

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
       
    })
    .Build();

host.Run();
