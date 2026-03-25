namespace App.Core.Models;

public sealed record DashboardCard(
    string Title,
    string Value,
    string Detail,
    string Accent,
    string Icon);
