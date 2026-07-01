using Serilog;
using VpnKeepAlive;

// macOS/Windows 通用：日志目录使用应用当前工作目录下的 logs。
// macOS 使用 launchd 部署时，请在 plist 里把 WorkingDirectory 指向程序目录。
Directory.CreateDirectory("logs");

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
