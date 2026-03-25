using App.Core.Interfaces;
using App.Core.Models;

namespace App.Infrastructure.Repositories;

public sealed class FileRememberMeRepository : IRememberMeRepository
{
    private readonly string _filePath;

    public FileRememberMeRepository(string filePath)
    {
        _filePath = filePath;
    }

    public RememberedCredential? Load()
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        var lines = File.ReadAllLines(_filePath);
        if (lines.Length < 2)
        {
            return null;
        }

        return new RememberedCredential(lines[0], lines[1]);
    }

    public void Save(RememberedCredential credential)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(_filePath, new[] { credential.Account, credential.Password });
    }

    public void Clear()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }
}
