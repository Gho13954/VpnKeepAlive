namespace VpnKeepAlive;

public class PingSettings
{
    public const string SectionName = "PingSettings";

    public List<string> TargetHosts { get; set; } = new() { "192.168.1.102" };
    public int MinIntervalSeconds { get; set; } = 30;
    public int MaxIntervalSeconds { get; set; } = 60;
    public int TimeoutMilliseconds { get; set; } = 3000;
}
