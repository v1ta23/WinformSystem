namespace App.Core.Models;

public sealed record DashboardActivity(
    string Time,
    string Text,
    string Status,
    string Accent);
