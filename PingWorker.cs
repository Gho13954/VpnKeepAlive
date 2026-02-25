using System.Net.NetworkInformation;
using Microsoft.Extensions.Options;

namespace VpnKeepAlive;

public class PingWorker : BackgroundService
{
    private readonly ILogger<PingWorker> _logger;
    private readonly PingSettings _settings;
    private readonly Random _random = new();

    public PingWorker(ILogger<PingWorker> logger, IOptions<PingSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("===== VPN KeepAlive 服务启动 =====");
        _logger.LogInformation("目标主机: {Hosts} | 间隔: {Min}-{Max}秒 | 超时: {Timeout}ms",
            string.Join(", ", _settings.TargetHosts),
            _settings.MinIntervalSeconds,
            _settings.MaxIntervalSeconds,
            _settings.TimeoutMilliseconds);
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var pingTasks = _settings.TargetHosts.Select(host => PingHostAsync(host));
            await Task.WhenAll(pingTasks);

            var waitSeconds = _random.Next(
                _settings.MinIntervalSeconds,
                _settings.MaxIntervalSeconds + 1);

            await Task.Delay(TimeSpan.FromSeconds(waitSeconds), stoppingToken);
        }
    }

    private async Task PingHostAsync(string host)
    {
        try
        {
            using var pingSender = new Ping();
            var reply = await pingSender.SendPingAsync(host, _settings.TimeoutMilliseconds);

            if (reply.Status == IPStatus.Success)
            {
                _logger.LogInformation("Ping {Host} 成功 | RTT: {Rtt}ms", host, reply.RoundtripTime);
            }
            else
            {
                _logger.LogWarning("Ping {Host} 失败 | 状态: {Status}", host, reply.Status);
            }
        }
        catch (PingException ex)
        {
            _logger.LogError("Ping {Host} 异常: {Message}", host, ex.InnerException?.Message ?? ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ping {Host} 未知错误", host);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("===== VPN KeepAlive 服务停止 =====");
        return base.StopAsync(cancellationToken);
    }
}
