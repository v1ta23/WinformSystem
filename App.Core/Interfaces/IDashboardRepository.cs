using App.Core.Models;

namespace App.Core.Interfaces;

public interface IDashboardRepository
{
    DashboardOverview GetOverview(string account);
}
