using App.Core.DTOs;
using App.Core.Models;

namespace App.Core.Interfaces;

public interface IAuthenticationService
{
    RememberedCredential? LoadRememberedCredential();

    LoginResult Login(LoginRequest request);

    OperationResult Register(RegisterRequest request);
}
