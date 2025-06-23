using System.Data;
using Npgsql;

namespace Movies.Infrastructure.Persistence.Database;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);   
}

public class NpgsqlDbConnectionFactory(string connectionString) : IDbConnectionFactory
{
    private readonly string _connectionString = connectionString;

    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var dbConnection = new NpgsqlConnection(_connectionString);
        await dbConnection.OpenAsync(cancellationToken);
        return dbConnection;   
    }
}