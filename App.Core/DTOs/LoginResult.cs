namespace App.Core.DTOs;

public sealed record LoginResult(bool Success, string Message, string? Account = null);
