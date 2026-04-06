using App.Core.DTOs;
using App.Core.Interfaces;

namespace WinFormsApp.Controllers;

internal sealed class RegisterController
{
    private readonly IAuthenticationService _authenticationService;

    public RegisterController(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    public OperationResult Register(string account, string password, string confirmPassword)
    {
        return _authenticationService.Register(new RegisterRequest(account, password, confirmPassword));
    }
}
