namespace App.Core.DTOs;

public sealed record LoginRequest(string Account, string Password, bool RememberPassword);
