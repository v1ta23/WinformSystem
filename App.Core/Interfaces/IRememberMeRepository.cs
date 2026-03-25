using App.Core.Models;

namespace App.Core.Interfaces;

public interface IRememberMeRepository
{
    RememberedCredential? Load();

    void Save(RememberedCredential credential);

    void Clear();
}
