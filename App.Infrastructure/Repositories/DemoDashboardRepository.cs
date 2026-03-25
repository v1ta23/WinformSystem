using App.Core.Interfaces;
using App.Core.Models;

namespace App.Infrastructure.Repositories;

public sealed class DemoDashboardRepository : IDashboardRepository
{
    public DashboardOverview GetOverview(string account)
    {
        var cards = new[]
        {
            new DashboardCard("系统运行时间", "128 天", "99.8% 正常", "blue", "◈"),
            new DashboardCard("活跃连接", "2,847", "+12.5% 较昨日", "green", "◉"),
            new DashboardCard("CPU 使用率", "67%", "中等负载", "orange", "◆"),
            new DashboardCard("内存占用", "14.2 GB", "总计 32 GB", "purple", "◇")
        };

        var activities = new[]
        {
            new DashboardActivity(DateTime.Now.ToString("HH:mm"), $"{account} 登录成功", "✓", "green"),
            new DashboardActivity("13:18", "数据库备份已创建", "✓", "green"),
            new DashboardActivity("12:05", "检测到 CPU 峰值 (89%)", "⚠", "orange"),
            new DashboardActivity("11:42", "新用户注册 +23", "●", "blue"),
            new DashboardActivity("10:30", "SSL 证书即将到期", "⚠", "pink"),
            new DashboardActivity("09:15", "服务器重启完成", "✓", "green")
        };

        var quickActions = new[]
        {
            new DashboardQuickAction("系统诊断", "🔍", "blue", "cyan"),
            new DashboardQuickAction("性能优化", "🚀", "purple", "pink"),
            new DashboardQuickAction("数据备份", "💾", "green", "cyan"),
            new DashboardQuickAction("安全扫描", "🛡️", "orange", "pink")
        };

        return new DashboardOverview(
            "仪表板",
            $"欢迎回来，{account}。这是你的系统概览。",
            cards,
            activities,
            quickActions);
    }
}
