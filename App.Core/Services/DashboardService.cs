using App.Core.Interfaces;
using App.Core.Models;

namespace App.Core.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashboardRepository;

    public DashboardService(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    public DashboardOverview GetOverview(string account)
    {
        return _dashboardRepository.GetOverview(account);
    }
}
