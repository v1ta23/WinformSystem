using App.Core.Interfaces;
using App.Infrastructure.Config;
using Microsoft.Data.SqlClient;

namespace App.Infrastructure.Repositories;

public sealed class SqlUserRepository : IUserRepository
{
    private readonly string _connectionString;

    public SqlUserRepository(SqlServerOptions options)
    {
        _connectionString = options.ConnectionString;
    }

    public bool ValidateCredentials(string account, string password)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        const string sql = """
                           SELECT COUNT(1)
                           FROM Users
                           WHERE account = @account AND password = @password
                           """;

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@account", account);
        command.Parameters.AddWithValue("@password", password);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public bool AccountExists(string account)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        const string sql = """
                           SELECT COUNT(1)
                           FROM Users
                           WHERE account = @account
                           """;

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@account", account);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public void Create(string account, string password)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        const string sql = """
                           INSERT INTO Users (account, password)
                           VALUES (@account, @password)
                           """;

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@account", account);
        command.Parameters.AddWithValue("@password", password);
        command.ExecuteNonQuery();
    }
}
