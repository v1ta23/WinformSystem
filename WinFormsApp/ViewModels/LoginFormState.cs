namespace WinFormsApp.ViewModels;

internal sealed class LoginFormState
{
    public string Account { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public bool RememberPassword { get; init; }
}
