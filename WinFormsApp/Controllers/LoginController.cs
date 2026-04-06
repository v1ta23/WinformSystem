using App.Core.DTOs;
using App.Core.Interfaces;
using WinFormsApp.ViewModels;

namespace WinFormsApp.Controllers;

internal sealed class LoginController
{
    private readonly IAuthenticationService _authenticationService;

    public LoginController(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    public LoginFormState LoadInitialState()
    {
        var rememberedCredential = _authenticationService.LoadRememberedCredential();
        return new LoginFormState
        {
            Account = rememberedCredential?.Account ?? string.Empty,
            Password = rememberedCredential?.Password ?? string.Empty,
            RememberPassword = rememberedCredential is not null
        };
    }

    public LoginResult Login(string account, string password, bool rememberPassword)
    {
        return _authenticationService.Login(new LoginRequest(account, password, rememberPassword));
    }

    public void Logout()
    {
        _authenticationService.Logout();
    }
}
