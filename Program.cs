using Serilog;
using VpnKeepAlive;

// 确保日志目录存在
Directory.CreateDirectory(@"E:\VpnKeepAliveLogs");

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "VpnKeepAlive";
    })
    .UseSerilog((context, config) =>
    {
        config.ReadFrom.Configuration(context.Configuration);
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<PingSettings>(
            context.Configuration.GetSection(PingSettings.SectionName));
        services.AddHostedService<PingWorker>();
    })
    .Build();

await host.RunAsync();
