using App.Core.DTOs;
using App.Core.Models;

namespace App.Core.Interfaces;

public interface IAuthenticationService
{
    RememberedCredential? LoadRememberedCredential();

    LoginResult Login(LoginRequest request);

    void Logout();

    OperationResult Register(RegisterRequest request);
}
