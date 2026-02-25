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
        _logger.LogInformation("目标主机: {Host} | 间隔: {Min}-{Max}秒 | 超时: {Timeout}ms",
            _settings.TargetHost,
            _settings.MinIntervalSeconds,
            _settings.MaxIntervalSeconds,
            _settings.TimeoutMilliseconds);
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var pingSender = new Ping();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var reply = await pingSender.SendPingAsync(
                    _settings.TargetHost,
                    _settings.TimeoutMilliseconds);

                if (reply.Status == IPStatus.Success)
                {
                    _logger.LogInformation("Ping {Host} 成功 | RTT: {Rtt}ms",
                        _settings.TargetHost, reply.RoundtripTime);
                }
                else
                {
                    _logger.LogWarning("Ping {Host} 失败 | 状态: {Status}",
                        _settings.TargetHost, reply.Status);
                }
            }
            catch (PingException ex)
            {
                _logger.LogError("Ping 异常: {Message}", ex.InnerException?.Message ?? ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "未知错误");
            }

            var waitSeconds = _random.Next(
                _settings.MinIntervalSeconds,
                _settings.MaxIntervalSeconds + 1);

            await Task.Delay(TimeSpan.FromSeconds(waitSeconds), stoppingToken);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("===== VPN KeepAlive 服务停止 =====");
        return base.StopAsync(cancellationToken);
    }
}
