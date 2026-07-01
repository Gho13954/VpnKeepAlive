module.exports = {
  apps: [
    // --- VPN KeepAlive: macOS PM2 守护进程 ---
    {
      name: 'vpnkeepalive',
      cwd: '/Users/gho/Projects/LocalServer/vpnkeepalive',
      script: './VpnKeepAlive',
      args: '',
      watch: false,

      // PM2 自身日志；程序业务日志仍由 appsettings.json 写入 logs/vpn-keepalive-*.log。
      error_file: '/Users/gho/Projects/LocalServer/vpnkeepalive/logs/pm2-error.log',
      out_file: '/Users/gho/Projects/LocalServer/vpnkeepalive/logs/pm2-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss',

      env: {
        DOTNET_ENVIRONMENT: 'Production'
      }
    }
  ]
};
