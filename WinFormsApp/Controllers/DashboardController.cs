using App.Core.Interfaces;
using App.Core.Models;
using WinFormsApp.ViewModels;
using System.Drawing;

namespace WinFormsApp.Controllers;

internal sealed class DashboardController
{
    private readonly IInspectionRecordService _inspectionRecordService;

    public DashboardController(IInspectionRecordService inspectionRecordService)
    {
        _inspectionRecordService = inspectionRecordService;
    }

    public DashboardViewModel Load(string account)
    {
        var result = _inspectionRecordService.Query(new InspectionQuery(
            string.Empty,
            string.Empty,
            string.Empty,
            null,
            null,
            null,
            false));

        var records = result.Records
            .Where(record => !record.IsRevoked)
            .OrderByDescending(record => record.CheckedAt)
            .ToList();

        var todayStart = DateTime.Today;
        var todayRecords = records
            .Where(record => record.CheckedAt >= todayStart)
            .ToList();
        var pendingRecords = records
            .Where(record => record.Status != InspectionStatus.Normal && !record.ClosedAt.HasValue)
            .ToList();
        var pendingAbnormalCount = pendingRecords.Count(record => record.Status == InspectionStatus.Abnormal);
        var todayNormalCount = todayRecords.Count(record => record.Status == InspectionStatus.Normal);
        var todayWarningCount = todayRecords.Count(record => record.Status == InspectionStatus.Warning);
        var todayAbnormalCount = todayRecords.Count(record => record.Status == InspectionStatus.Abnormal);
        var todayPassRate = todayRecords.Count == 0
            ? 0m
            : Math.Round(todayNormalCount * 100m / todayRecords.Count, 1);

        return new DashboardViewModel
        {
            HeaderTitle = "首页",
            HeaderSubtitle = $"今日巡检 {todayRecords.Count} 条，待闭环 {pendingRecords.Count} 条。",
            Cards =
            [
                new DashboardCardViewModel
                {
                    Title = "今日巡检",
                    Value = todayRecords.Count.ToString(),
                    Detail = $"正常 {todayNormalCount} / 预警 {todayWarningCount}",
                    Icon = "巡检",
                    AccentColor = MapAccent("blue"),
                    NavigationTarget = DashboardNavigationTarget.InspectionToday
                },
                new DashboardCardViewModel
                {
                    Title = "待闭环",
                    Value = pendingRecords.Count.ToString(),
                    Detail = $"异常 {pendingAbnormalCount} 条待处理",
                    Icon = "闭环",
                    AccentColor = MapAccent("orange"),
                    NavigationTarget = DashboardNavigationTarget.InspectionPending
                },
                new DashboardCardViewModel
                {
                    Title = "今日异常",
                    Value = todayAbnormalCount.ToString(),
                    Detail = $"预警 {todayWarningCount} 条",
                    Icon = "异常",
                    AccentColor = MapAccent("pink"),
                    NavigationTarget = DashboardNavigationTarget.InspectionAbnormal
                },
                new DashboardCardViewModel
                {
                    Title = "今日合格率",
                    Value = $"{todayPassRate:0.0}%",
                    Detail = todayRecords.Count == 0
                        ? "今天还没有巡检记录"
                        : $"总数 {todayRecords.Count}，正常 {todayNormalCount}",
                    Icon = "合格率",
                    AccentColor = MapAccent("green"),
                    NavigationTarget = DashboardNavigationTarget.Analytics
                }
            ],
            Activities = records
                .Take(6)
                .Select(record => new DashboardActivityViewModel
                {
                    Time = record.CheckedAt.ToString("HH:mm"),
                    Text = $"{record.LineName} / {record.DeviceName} / {record.InspectionItem}",
                    Status = record.Status switch
                    {
                        InspectionStatus.Normal => "正常",
                        InspectionStatus.Warning => "预警",
                        InspectionStatus.Abnormal => "异常",
                        _ => "未知"
                    },
                    AccentColor = MapAccent(record.Status switch
                    {
                        InspectionStatus.Normal => "green",
                        InspectionStatus.Warning => "orange",
                        InspectionStatus.Abnormal => "pink",
                        _ => "blue"
                    })
                })
                .ToList(),
            QuickActions =
            [
                new DashboardQuickActionViewModel
                {
                    Text = "新增点检",
                    Icon = "新增",
                    PrimaryAccent = MapAccent("blue"),
                    SecondaryAccent = MapAccent("cyan"),
                    NavigationTarget = DashboardNavigationTarget.InspectionCreate
                },
                new DashboardQuickActionViewModel
                {
                    Text = "处理待闭环",
                    Icon = "闭环",
                    PrimaryAccent = MapAccent("orange"),
                    SecondaryAccent = MapAccent("pink"),
                    NavigationTarget = DashboardNavigationTarget.InspectionPending
                },
                new DashboardQuickActionViewModel
                {
                    Text = "查看异常",
                    Icon = "异常",
                    PrimaryAccent = MapAccent("pink"),
                    SecondaryAccent = MapAccent("purple"),
                    NavigationTarget = DashboardNavigationTarget.InspectionAbnormal
                },
                new DashboardQuickActionViewModel
                {
                    Text = "统计分析",
                    Icon = "统计",
                    PrimaryAccent = MapAccent("green"),
                    SecondaryAccent = MapAccent("cyan"),
                    NavigationTarget = DashboardNavigationTarget.Analytics
                }
            ]
        };
    }

    private static Color MapAccent(string accent)
    {
        return accent.ToLowerInvariant() switch
        {
            "green" => Color.FromArgb(76, 217, 140),
            "orange" => Color.FromArgb(255, 165, 70),
            "purple" => Color.FromArgb(148, 90, 255),
            "pink" => Color.FromArgb(255, 100, 150),
            "cyan" => Color.FromArgb(50, 210, 220),
            _ => Color.FromArgb(88, 130, 255)
        };
    }
}
