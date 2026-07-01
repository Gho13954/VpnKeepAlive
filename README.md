# VPN KeepAlive 部署指南

这是一个 .NET Worker 服务，用于定时 Ping 目标 IP，保持 VPN 链路活跃。

当前项目已改成 Windows/macOS 都可使用的配置：

- 程序日志默认写入程序目录下的 `logs/`
- Windows 可注册为 Windows Service
- macOS 可注册为 `launchd` 后台服务

## 一、项目结构

```
VpnKeepAlive/
├── VpnKeepAlive.csproj    # 项目文件
├── Program.cs              # 入口
├── PingWorker.cs           # 核心 Ping 逻辑
├── PingSettings.cs         # 配置模型
├── appsettings.json        # 配置文件（IP、间隔、日志路径）
└── com.vpnkeepalive.plist  # macOS launchd 服务模板
```

## 二、配置说明

修改 `appsettings.json`：

```json
{
  "PingSettings": {
    "TargetHosts": [
      "192.168.1.102",
      "10.33.6.24"
    ],
    "MinIntervalSeconds": 30,
    "MaxIntervalSeconds": 60,
    "TimeoutMilliseconds": 3000
  }
}
```

说明：

| 配置项 | 说明 |
| --- | --- |
| `TargetHosts` | 要保活 Ping 的 IP 或域名列表 |
| `MinIntervalSeconds` | 随机 Ping 间隔最小值，单位秒 |
| `MaxIntervalSeconds` | 随机 Ping 间隔最大值，单位秒 |
| `TimeoutMilliseconds` | 单次 Ping 超时时间，单位毫秒 |

macOS/Windows 通用日志路径：

```json
"path": "logs/vpn-keepalive-.log"
```

注意：`appsettings.json` 是标准 JSON，不能写注释；macOS 使用说明写在本 README 和 `com.vpnkeepalive.plist` 注释中。

## 三、macOS 部署步骤

### 1. 安装 .NET SDK

如果没有安装 `dotnet`，先安装 .NET SDK：

```bash
brew install --cask dotnet-sdk
```

安装后检查：

```bash
dotnet --info
```

### 2. 发布程序

Apple Silicon Mac，例如 M1/M2/M3/M4：

```bash
dotnet publish -c Release -r osx-arm64 --self-contained true -o ./publish/macos
```

Intel Mac：

```bash
dotnet publish -c Release -r osx-x64 --self-contained true -o ./publish/macos
```

发布完成后，确认可执行文件存在：

```bash
ls -l ./publish/macos/VpnKeepAlive
```

### 3. 安装到固定目录

```bash
sudo mkdir -p /usr/local/vpnkeepalive
sudo cp -R ./publish/macos/* /usr/local/vpnkeepalive/
sudo mkdir -p /usr/local/vpnkeepalive/logs
sudo chmod +x /usr/local/vpnkeepalive/VpnKeepAlive
```

### 4. 先手动运行测试

```bash
cd /usr/local/vpnkeepalive
./VpnKeepAlive
```

看到 Ping 日志后按 `Ctrl+C` 停止。

如果 macOS 报 ICMP 权限错误，例如 `Operation not permitted`，请使用下面的 `LaunchDaemon` 方式以 root 运行。

### 5. 注册为 macOS 后台服务

项目里的 `com.vpnkeepalive.plist` 已经按 `/usr/local/vpnkeepalive` 写好。

复制到系统服务目录：

```bash
sudo cp ./com.vpnkeepalive.plist /Library/LaunchDaemons/com.vpnkeepalive.plist
sudo chown root:wheel /Library/LaunchDaemons/com.vpnkeepalive.plist
sudo chmod 644 /Library/LaunchDaemons/com.vpnkeepalive.plist
```

加载并启动：

```bash
sudo launchctl bootstrap system /Library/LaunchDaemons/com.vpnkeepalive.plist
sudo launchctl enable system/com.vpnkeepalive
sudo launchctl kickstart -k system/com.vpnkeepalive
```

### 6. 查看 macOS 服务状态和日志

查看服务：

```bash
sudo launchctl print system/com.vpnkeepalive
```

查看程序业务日志：

```bash
tail -f /usr/local/vpnkeepalive/logs/vpn-keepalive-*.log
```

查看 launchd 输出：

```bash
tail -f /usr/local/vpnkeepalive/logs/launchd.out.log
tail -f /usr/local/vpnkeepalive/logs/launchd.err.log
```

### 7. 重启 macOS 服务

```bash
sudo launchctl kickstart -k system/com.vpnkeepalive
```

### 8. 停止并卸载 macOS 服务

```bash
sudo launchctl bootout system /Library/LaunchDaemons/com.vpnkeepalive.plist
sudo rm /Library/LaunchDaemons/com.vpnkeepalive.plist
sudo rm -rf /usr/local/vpnkeepalive
```

## 四、Windows 构建与发布

在项目目录下打开终端执行：

```powershell
dotnet publish -c Release -o C:\Services\VpnKeepAlive
```

## 五、注册为 Windows 服务

**管理员 PowerShell** 中执行：

```powershell
sc.exe create VpnKeepAlive binPath="C:\Services\VpnKeepAlive\VpnKeepAlive.exe" start=auto DisplayName="VPN KeepAlive Service"
sc.exe description VpnKeepAlive "定时 Ping 保活 VPN 连接 (30-60秒随机间隔)"
sc.exe start VpnKeepAlive
```

## 六、Windows 日常管理命令

| 操作       | 命令                              |
| ---------- | --------------------------------- |
| 🚀 启动    | `sc.exe start VpnKeepAlive`       |
| 🛑 停止    | `sc.exe stop VpnKeepAlive`        |
| 🔍 查状态  | `sc.exe query VpnKeepAlive`       |
| 🔄 重启    | `sc.exe stop VpnKeepAlive && sc.exe start VpnKeepAlive` |

也可以在 `services.msc` 中右键管理。

需要新增保活 IP 时，在 `TargetHosts` 数组中追加即可。每个周期内所有主机会被并发 Ping。

修改配置后需重启服务生效：`sc.exe stop VpnKeepAlive && sc.exe start VpnKeepAlive`

## 七、Windows 日志

日志文件位于程序目录下的 `logs\`，按天滚动，自动保留最近 30 天。

查看实时日志：
```powershell
Get-Content "C:\Services\VpnKeepAlive\logs\vpn-keepalive-*.log" -Tail 20 -Wait
```

## 八、Windows 彻底卸载

```powershell
sc.exe stop VpnKeepAlive
sc.exe delete VpnKeepAlive
Remove-Item "C:\Services\VpnKeepAlive" -Recurse -Force
```
