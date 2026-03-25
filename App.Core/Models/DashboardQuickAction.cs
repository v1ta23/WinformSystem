namespace App.Core.Models;

public sealed record DashboardQuickAction(
    string Text,
    string Icon,
    string PrimaryAccent,
    string SecondaryAccent);
