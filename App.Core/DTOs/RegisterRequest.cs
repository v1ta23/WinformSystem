namespace App.Core.DTOs;

public sealed record RegisterRequest(string Account, string Password, string ConfirmPassword);
