namespace App.Core.Interfaces;

public interface IUserRepository
{
    bool ValidateCredentials(string account, string password);

    bool AccountExists(string account);

    void Create(string account, string password);
}
