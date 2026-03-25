using App.Core.Interfaces;
using App.Core.Services;
using App.Infrastructure.Config;
using App.Infrastructure.Repositories;
using App.WinForms.Controllers;
using App.WinForms.Views;

namespace App.WinForms;

internal sealed class AppCompositionRoot
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IDashboardService _dashboardService;

    public AppCompositionRoot()
    {
        var sqlOptions = new SqlServerOptions
        {
            ConnectionString = "Server=localhost;Database=TestDB;Trusted_Connection=True;TrustServerCertificate=True;"
        };

        var rememberMePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "remember.txt");
        var userRepository = new SqlUserRepository(sqlOptions);
        var rememberMeRepository = new FileRememberMeRepository(rememberMePath);
        var dashboardRepository = new DemoDashboardRepository();

        _authenticationService = new AuthenticationService(userRepository, rememberMeRepository);
        _dashboardService = new DashboardService(dashboardRepository);
    }

    public LoginForm CreateLoginForm()
    {
        return new LoginForm(new LoginController(_authenticationService), this);
    }

    public RegisterForm CreateRegisterForm()
    {
        return new RegisterForm(new RegisterController(_authenticationService));
    }

    public MainForm CreateDashboardForm(string account)
    {
        var controller = new DashboardController(_dashboardService);
        return new MainForm(controller.Load(account));
    }
}
