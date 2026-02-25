# VPN KeepAlive Windows 服务 - 部署指南

## 一、项目结构

```
VpnKeepAlive/
├── VpnKeepAlive.csproj    # 项目文件
├── Program.cs              # 入口
├── PingWorker.cs           # 核心 Ping 逻辑
├── PingSettings.cs         # 配置模型
└── appsettings.json        # 配置文件（IP、间隔、日志路径）
```

## 二、构建与发布

在项目目录下打开终端执行：

```powershell
dotnet publish -c Release -o C:\Services\VpnKeepAlive
```

## 三、注册为 Windows 服务

**管理员 PowerShell** 中执行：

```powershell
sc.exe create VpnKeepAlive binPath="C:\Services\VpnKeepAlive\VpnKeepAlive.exe" start=auto DisplayName="VPN KeepAlive Service"
sc.exe description VpnKeepAlive "定时 Ping 保活 VPN 连接 (30-60秒随机间隔)"
sc.exe start VpnKeepAlive
```

## 四、日常管理命令

| 操作       | 命令                              |
| ---------- | --------------------------------- |
| 🚀 启动    | `sc.exe start VpnKeepAlive`       |
| 🛑 停止    | `sc.exe stop VpnKeepAlive`        |
| 🔍 查状态  | `sc.exe query VpnKeepAlive`       |
| 🔄 重启    | `sc.exe stop VpnKeepAlive && sc.exe start VpnKeepAlive` |

也可以在 `services.msc` 中右键管理。

## 五、配置说明 (appsettings.json)

```json
{
  "PingSettings": {
    "TargetHost": "192.168.1.102",     // 目标 IP
    "MinIntervalSeconds": 30,           // 最小间隔（秒）
    "MaxIntervalSeconds": 60,           // 最大间隔（秒）
    "TimeoutMilliseconds": 3000         // Ping 超时
  }
}
```

修改配置后需重启服务生效：`sc.exe stop VpnKeepAlive && sc.exe start VpnKeepAlive`

## 六、日志

日志文件位于 `E:\VpnKeepAliveLogs\`，按天滚动，自动保留最近 30 天。

查看实时日志：
```powershell
Get-Content "E:\VpnKeepAliveLogs\vpn-keepalive-*.log" -Tail 20 -Wait
```

## 七、彻底卸载

```powershell
sc.exe stop VpnKeepAlive
sc.exe delete VpnKeepAlive
Remove-Item "C:\Services\VpnKeepAlive" -Recurse -Force
Remove-Item "E:\VpnKeepAliveLogs" -Recurse -Force
```
