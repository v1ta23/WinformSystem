using App.Core.Interfaces;
using App.Core.Models;
using App.Infrastructure.Config;
using Microsoft.Data.SqlClient;

namespace App.Infrastructure.Repositories;

public sealed class SqlManagedDeviceRepository : IManagedDeviceRepository
{
    private readonly string _connectionString;

    public SqlManagedDeviceRepository(SqlServerOptions options)
    {
        _connectionString = options.ConnectionString;
        SqlServerSchemaInitializer.EnsureInitialized(_connectionString);
    }

    public IReadOnlyList<ManagedDevice> GetAll()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        const string sql = """
                           SELECT Id,
                                  LineName,
                                  DeviceName,
                                  DeviceCode,
                                  Location,
                                  Owner,
                                  CommunicationAddress,
                                  Status,
                                  UpdatedAt,
                                  Remark
                           FROM dbo.ManagedDevices
                           """;

        using var command = new SqlCommand(sql, connection);
        using var reader = command.ExecuteReader();
        var devices = new List<ManagedDevice>();

        while (reader.Read())
        {
            devices.Add(new ManagedDevice(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                (ManagedDeviceStatus)reader.GetInt32(7),
                reader.GetDateTime(8),
                reader.GetString(9)));
        }

        return devices;
    }

    public void SaveAll(IReadOnlyList<ManagedDevice> devices)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        using (var deleteCommand = new SqlCommand("DELETE FROM dbo.ManagedDevices;", connection, transaction))
        {
            deleteCommand.ExecuteNonQuery();
        }

        const string insertSql = """
                                 INSERT INTO dbo.ManagedDevices
                                 (
                                     Id,
                                     LineName,
                                     DeviceName,
                                     DeviceCode,
                                     Location,
                                     Owner,
                                     CommunicationAddress,
                                     Status,
                                     UpdatedAt,
                                     Remark
                                 )
                                 VALUES
                                 (
                                     @Id,
                                     @LineName,
                                     @DeviceName,
                                     @DeviceCode,
                                     @Location,
                                     @Owner,
                                     @CommunicationAddress,
                                     @Status,
                                     @UpdatedAt,
                                     @Remark
                                 );
                                 """;

        using var insertCommand = new SqlCommand(insertSql, connection, transaction);
        insertCommand.Parameters.Add("@Id", System.Data.SqlDbType.UniqueIdentifier);
        insertCommand.Parameters.Add("@LineName", System.Data.SqlDbType.NVarChar, 100);
        insertCommand.Parameters.Add("@DeviceName", System.Data.SqlDbType.NVarChar, 100);
        insertCommand.Parameters.Add("@DeviceCode", System.Data.SqlDbType.NVarChar, 100);
        insertCommand.Parameters.Add("@Location", System.Data.SqlDbType.NVarChar, 200);
        insertCommand.Parameters.Add("@Owner", System.Data.SqlDbType.NVarChar, 100);
        insertCommand.Parameters.Add("@CommunicationAddress", System.Data.SqlDbType.NVarChar, 300);
        insertCommand.Parameters.Add("@Status", System.Data.SqlDbType.Int);
        insertCommand.Parameters.Add("@UpdatedAt", System.Data.SqlDbType.DateTime2);
        insertCommand.Parameters.Add("@Remark", System.Data.SqlDbType.NVarChar, -1);

        foreach (var device in devices)
        {
            insertCommand.Parameters["@Id"].Value = device.Id;
            insertCommand.Parameters["@LineName"].Value = device.LineName;
            insertCommand.Parameters["@DeviceName"].Value = device.DeviceName;
            insertCommand.Parameters["@DeviceCode"].Value = device.DeviceCode;
            insertCommand.Parameters["@Location"].Value = device.Location;
            insertCommand.Parameters["@Owner"].Value = device.Owner;
            insertCommand.Parameters["@CommunicationAddress"].Value = device.CommunicationAddress;
            insertCommand.Parameters["@Status"].Value = (int)device.Status;
            insertCommand.Parameters["@UpdatedAt"].Value = device.UpdatedAt;
            insertCommand.Parameters["@Remark"].Value = device.Remark;
            insertCommand.ExecuteNonQuery();
        }

        transaction.Commit();
    }
}
