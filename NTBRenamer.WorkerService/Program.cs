using NTBRenamer.WorkerService;
using NTBRenamer.WorkerService.Models;
using NTBRenamer.WorkerService.Services;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(o => o.ServiceName = "")
    .ConfigureServices((hostContext, services) =>
    {
        services.AddMemoryCache();

        services.AddOptions();
        services.Configure<AppSettings>(hostContext.Configuration.GetSection("ApiSettings"));

        services.AddHostedService<NTBRenamerWorker>();

        services.AddSingleton<IRenamerService, RenamerService>();
    }).Build();

await host.RunAsync();
