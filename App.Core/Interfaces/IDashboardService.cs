using App.Core.Models;

namespace App.Core.Interfaces;

public interface IDashboardService
{
    DashboardOverview GetOverview(string account);
}
