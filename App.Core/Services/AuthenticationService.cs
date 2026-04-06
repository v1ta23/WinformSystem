using App.Core.DTOs;
using App.Core.Interfaces;
using App.Core.Models;

namespace App.Core.Services;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IRememberMeRepository _rememberMeRepository;

    public AuthenticationService(
        IUserRepository userRepository,
        IRememberMeRepository rememberMeRepository)
    {
        _userRepository = userRepository;
        _rememberMeRepository = rememberMeRepository;
    }

    public RememberedCredential? LoadRememberedCredential()
    {
        return _rememberMeRepository.Load();
    }

    public LoginResult Login(LoginRequest request)
    {
        var account = request.Account.Trim();
        if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new LoginResult(false, "请输入账号和密码。");
        }

        var isValid = _userRepository.ValidateCredentials(account, request.Password);
        if (!isValid)
        {
            return new LoginResult(false, "账号或密码错误。");
        }

        if (request.RememberPassword)
        {
            _rememberMeRepository.Save(new RememberedCredential(account, request.Password));
        }
        else
        {
            _rememberMeRepository.Clear();
        }

        return new LoginResult(true, "登录成功。", account);
    }

    public void Logout()
    {
        _rememberMeRepository.Clear();
    }

    public OperationResult Register(RegisterRequest request)
    {
        var account = request.Account.Trim();
        if (string.IsNullOrWhiteSpace(account) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.ConfirmPassword))
        {
            return new OperationResult(false, "请填写所有字段。");
        }

        if (account.Length < 4 || account.Length > 9)
        {
            return new OperationResult(false, "账号长度必须在 4 到 9 位之间。");
        }

        if (request.Password.Length < 6 || request.Password.Length > 9)
        {
            return new OperationResult(false, "密码长度必须在 6 到 9 位之间。");
        }

        if (request.Password != request.ConfirmPassword)
        {
            return new OperationResult(false, "两次输入的密码不一致。");
        }

        if (_userRepository.AccountExists(account))
        {
            return new OperationResult(false, "账号已存在，请更换一个。");
        }

        _userRepository.Create(account, request.Password);
        return new OperationResult(true, "注册成功，请登录。");
    }
}
