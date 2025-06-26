using Dapper;
using Movies.Application.Abstractions.Persistence;
using Movies.Domain.Models;
using Movies.Infrastructure.Persistence.Database;
using Npgsql;

namespace Movies.Infrastructure.Persistence.Repositories;

public class RatingRepository(IDbConnectionFactory dbConnectionFactory) : IRatingRepository
{
    public async Task<bool> RateMovieAsync(Guid movieId, int rating, Guid userId, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        var result = await connection.ExecuteAsync(new CommandDefinition("""
            insert into ratings(userid, movieid, rating) 
            values (@userId, @movieId, @rating)
            on conflict (userid, movieid) do update 
                set rating = @rating
            """, new { userId, movieId, rating }, cancellationToken: cancellationToken));

        return result > 0;
    }

    public async Task<float?> GetRatingAsync(Guid movieId, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<float?>(new CommandDefinition("""
            select round(avg(r.rating), 1) from ratings r
            where movieid = @movieId
            """, new { movieId }, cancellationToken: cancellationToken));
    }

    public async Task<(float? Rating, int? UserRating)> GetRatingAsync(Guid movieId, Guid userId, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<(float?, int?)>(new CommandDefinition("""
            select round(avg(rating), 1), 
                   (select rating 
                    from ratings 
                    where movieid = @movieId 
                      and userid = @userId
                    limit 1) 
            from ratings
            where movieid = @movieId
            """, new { movieId, userId }, cancellationToken: cancellationToken));
    }
    
    public async Task<bool> DeleteRatingAsync(Guid movieId, Guid userId, CancellationToken cancellationToken = default)
{
    using var rawConnection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
    // Connection must be Npgsql
    var connection = (NpgsqlConnection)rawConnection;

    await connection.OpenAsync(cancellationToken); // Ensure the connection is open for the transaction

    // Start a new database transaction to ensure atomicity and lock safety
    await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

    try
    {
        // Try to select the rating using FOR UPDATE to lock the row
        // This prevents any concurrent transaction from modifying or deleting this row until we finish
        var existingRating = await connection.ExecuteScalarAsync<Guid?>(
            new CommandDefinition("""
                                      SELECT ratingid
                                      FROM ratings
                                      WHERE movieid = @movieId AND userid = @userId
                                      FOR UPDATE;
                                  """,
                new { movieId, userId },
                transaction: transaction,
                cancellationToken: cancellationToken));

        // If the rating does not exist, rollback and return false
        if (existingRating is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        // Proceed to delete the locked rating row
        var rowsAffected = await connection.ExecuteAsync(
            new CommandDefinition("""
                                      DELETE FROM ratings
                                      WHERE movieid = @movieId AND userid = @userId;
                                  """,
                new { movieId, userId },
                transaction: transaction,
                cancellationToken: cancellationToken));

        // Commit the transaction after successful delete
        await transaction.CommitAsync(cancellationToken);
        return rowsAffected > 0;
    }
    catch
    {
        // Roll back the transaction in case of any exception
        await transaction.RollbackAsync(cancellationToken);
        throw;
    }
}
    
    public async Task<IEnumerable<MovieRating>> GetRatingsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QueryAsync<MovieRating>(new CommandDefinition("""
            select r.rating, r.movieid, m.slug
            from ratings r
            inner join movies m on r.movieid = m.id
            where userid = @userId
            """, new { userId }, cancellationToken: cancellationToken));
    }
}



