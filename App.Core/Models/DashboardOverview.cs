namespace App.Core.Models;

public sealed record DashboardOverview(
    string Title,
    string Subtitle,
    IReadOnlyList<DashboardCard> Cards,
    IReadOnlyList<DashboardActivity> Activities,
    IReadOnlyList<DashboardQuickAction> QuickActions);
