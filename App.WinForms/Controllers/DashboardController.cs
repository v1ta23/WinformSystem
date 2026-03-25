using App.Core.Interfaces;
using App.WinForms.ViewModels;
using System.Drawing;

namespace App.WinForms.Controllers;

internal sealed class DashboardController
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public DashboardViewModel Load(string account)
    {
        var overview = _dashboardService.GetOverview(account);
        return new DashboardViewModel
        {
            HeaderTitle = overview.Title,
            HeaderSubtitle = overview.Subtitle,
            Cards = overview.Cards
                .Select(card => new DashboardCardViewModel
                {
                    Title = card.Title,
                    Value = card.Value,
                    Detail = card.Detail,
                    Icon = card.Icon,
                    AccentColor = MapAccent(card.Accent)
                })
                .ToList(),
            Activities = overview.Activities
                .Select(activity => new DashboardActivityViewModel
                {
                    Time = activity.Time,
                    Text = activity.Text,
                    Status = activity.Status,
                    AccentColor = MapAccent(activity.Accent)
                })
                .ToList(),
            QuickActions = overview.QuickActions
                .Select(action => new DashboardQuickActionViewModel
                {
                    Text = action.Text,
                    Icon = action.Icon,
                    PrimaryAccent = MapAccent(action.PrimaryAccent),
                    SecondaryAccent = MapAccent(action.SecondaryAccent)
                })
                .ToList()
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
