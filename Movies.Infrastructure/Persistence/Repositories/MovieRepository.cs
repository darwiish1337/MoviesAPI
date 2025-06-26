using Dapper;
using Movies.Application.Abstractions.Persistence;
using Movies.Domain.Enums;
using Movies.Domain.Models;
using Movies.Infrastructure.Persistence.Database;
using Npgsql;

namespace Movies.Infrastructure.Persistence.Repositories;

public class MovieRepository(IDbConnectionFactory dbConnectionFactory) : IMovieRepository
{
    public async Task<bool> CreateAsync(Movie movie, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        
        var result = await connection.ExecuteAsync(new CommandDefinition("""
            insert into movies (id, slug, title, yearofrelease) 
            values (@Id, @Slug, @Title, @YearOfRelease)
            """, movie, cancellationToken: cancellationToken));

        if (result > 0)
        {
            foreach (var genre in movie.Genres)
            {
                await connection.ExecuteAsync(new CommandDefinition("""
                    insert into genres (movieId, name) 
                    values (@MovieId, @Name)
                    """, new { MovieId = movie.Id, Name = genre }, cancellationToken: cancellationToken));
            }
        }
        transaction.Commit();

        return result > 0;
    }
    
    public async Task<bool> CreateBulkAsync(Movie movie, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            var movieResult = await connection.ExecuteAsync(new CommandDefinition("""
                    INSERT INTO movies (id, slug, title, yearofrelease) 
                    VALUES (@Id, @Slug, @Title, @YearOfRelease);
                """, movie, transaction: transaction, cancellationToken: cancellationToken));

            if (movieResult == 0)
            {
                transaction.Rollback();
                return false;
            }

            foreach (var genre in movie.Genres)
            {
                var genreResult = await connection.ExecuteAsync(new CommandDefinition("""
                        INSERT INTO genres (movieId, name) 
                        VALUES (@MovieId, @Name);
                    """, new { MovieId = movie.Id, Name = genre }, transaction: transaction, cancellationToken: cancellationToken));

                if (genreResult == 0)
                {
                    transaction.Rollback();
                    return false;
                }
            }

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
    
    public async Task<Movie?> GetByIdAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        var movie = await connection.QuerySingleOrDefaultAsync<Movie>(
            new CommandDefinition("""
                                  select m.*, round(avg(r.rating), 1) as rating, myr.rating as userrating 
                                  from movies m
                                  left join ratings r on m.id = r.movieid
                                  left join ratings myr on m.id = myr.movieid
                                      and myr.userid = @userId
                                  where id = @id
                                  group by id, userrating
                                  """, new { id, userId }, cancellationToken: cancellationToken));

        if (movie is null)
        {
            return null;
        }
    
        var genres = await connection.QueryAsync<string>(
            new CommandDefinition("""
                                  select name from genres where movieid = @id 
                                  """, new { id }, cancellationToken: cancellationToken));

        foreach (var genre in genres)
        {
            movie.Genres.Add(genre);
        }

        // Load images
        var images = await connection.QueryAsync<MovieImage>(
            new CommandDefinition("""
                                  SELECT id, movie_id as MovieId, public_id as PublicId, original_url as OriginalUrl, 
                                         thumbnail_url as ThumbnailUrl, medium_url as MediumUrl, large_url as LargeUrl,
                                         alt_text as AltText, width, height, size, format, is_primary as IsPrimary, 
                                         created_at as CreatedAt, updated_at as UpdatedAt
                                  FROM movie_images 
                                  WHERE movie_id = @id
                                  ORDER BY is_primary DESC, created_at ASC
                                  """, new { id }, cancellationToken: cancellationToken));

        foreach (var image in images)
        {
            movie.Images.Add(image);
        }

        return movie;
    }
    
    public async Task<Movie?> GetBySlugAsync(string slug, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        var movie = await connection.QuerySingleOrDefaultAsync<Movie>(
            new CommandDefinition("""
            select m.*, round(avg(r.rating), 1) as rating, myr.rating as userrating
            from movies m
            left join ratings r on m.id = r.movieid
            left join ratings myr on m.id = myr.movieid
                and myr.userid = @userId
            where slug = @slug
            group by id, userrating
            """, new { slug, userId }, cancellationToken: cancellationToken));

        if (movie is null)
        {
            return null;
        }
        
        var genres = await connection.QueryAsync<string>(
            new CommandDefinition("""
            select name from genres where movieid = @id 
            """, new { id = movie.Id }, cancellationToken: cancellationToken));

        foreach (var genre in genres)
        {
            movie.Genres.Add(genre);
        }

        return movie;
    }
    
    public async Task<IEnumerable<Movie>> GetAllAsync(GetAllMoviesOptions options, CancellationToken token = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(token);

        var sortClause = string.Empty;
        if (!string.IsNullOrWhiteSpace(options.SortField))
        {
            sortClause = $"""
                              ORDER BY m.{options.SortField} {(options.SortOrder == SortOrder.Ascending ? "ASC" : "DESC")}
                          """;
        }

        var sql = $"""
                       SELECT 
                           m.*, 
                           STRING_AGG(DISTINCT g.name, ',') AS genres, 
                           ROUND(AVG(r.rating), 1) AS rating, 
                           myr.rating AS userrating
                       FROM movies m 
                       LEFT JOIN genres g ON m.id = g.movieid
                       LEFT JOIN ratings r ON m.id = r.movieid
                       LEFT JOIN ratings myr ON m.id = myr.movieid AND myr.userid = @userId
                       WHERE (@title IS NULL OR m.title ILIKE ('%' || @title || '%'))
                       AND (@yearofrelease IS NULL OR m.yearofrelease = @yearofrelease)
                       GROUP BY m.id, myr.rating
                       {sortClause}
                       LIMIT @pageSize
                       OFFSET @pageOffset
                   """;

        var result = await connection.QueryAsync(new CommandDefinition(sql, new
        {
            userId = options.UserId,
            title = options.Title,
            yearofrelease = options.YearOfRelease,
            pageSize = options.PageSize,
            pageOffset = (options.Page - 1) * options.PageSize
        }, cancellationToken: token));

        return result.Select(x => new Movie
        {
            Id = x.id,
            Title = x.title,
            YearOfRelease = x.yearofrelease,
            Rating = (float?)x.rating,
            UserRating = (int?)x.userrating,
            Genres = Enumerable.ToList((x.genres ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries))
        });
    }
    
    public async Task<bool> UpdateAsync(Movie movie, CancellationToken cancellationToken = default)
    {
    using var rawConnection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
    var connection = (NpgsqlConnection)rawConnection;

    await connection.OpenAsync(cancellationToken);
    await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

    try
    {
        // Lock the movie row to prevent concurrent updates
        var lockedId = await connection.ExecuteScalarAsync<Guid?>(
            new CommandDefinition("""
                SELECT id FROM movies
                WHERE id = @Id
                FOR UPDATE;
            """, new { movie.Id }, transaction: transaction, cancellationToken: cancellationToken));

        // If the movie doesn't exist, rollback early
        if (lockedId is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        // Delete existing genres for this movie to replace with the new ones
        await connection.ExecuteAsync(
            new CommandDefinition("""
                DELETE FROM genres WHERE movieid = @Id;
            """, new { movie.Id }, transaction: transaction, cancellationToken: cancellationToken));

        // Insert the updated genres
        foreach (var genre in movie.Genres)
        {
            await connection.ExecuteAsync(
                new CommandDefinition("""
                    INSERT INTO genres (movieid, name)
                    VALUES (@MovieId, @Name);
                """, new { MovieId = movie.Id, Name = genre }, transaction: transaction, cancellationToken: cancellationToken));
        }

        // Update the main movie data
        var result = await connection.ExecuteAsync(
            new CommandDefinition("""
                UPDATE movies
                SET slug = @Slug,
                    title = @Title,
                    yearofrelease = @YearOfRelease
                WHERE id = @Id;
            """, movie, transaction: transaction, cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
        return result > 0;
    }
    catch
    {
        await transaction.RollbackAsync(cancellationToken);
        throw;
    }
    }

    public async Task<bool> DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var rawConnection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        var connection = (NpgsqlConnection)rawConnection;

        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // Lock the target movie row to prevent concurrent modifications
            var existingId = await connection.ExecuteScalarAsync<Guid?>(
                new CommandDefinition("""
                                          SELECT id FROM movies
                                          WHERE id = @id
                                          FOR UPDATE;
                                      """, new { id }, transaction: transaction, cancellationToken: cancellationToken));

            // If the movie doesn't exist, rollback and return
            if (existingId is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            // First delete related genre entries (child rows)
            await connection.ExecuteAsync(
                new CommandDefinition("""
                                          DELETE FROM genres WHERE movieid = @id;
                                      """, new { id }, transaction: transaction, cancellationToken: cancellationToken));

            // Then delete the movie itself (parent row)
            var result = await connection.ExecuteAsync(
                new CommandDefinition("""
                                          DELETE FROM movies WHERE id = @id;
                                      """, new { id }, transaction: transaction, cancellationToken: cancellationToken));

            // Commit the transaction if all operations succeeded
            await transaction.CommitAsync(cancellationToken);
            return result > 0;
        }
        catch
        {
            // Roll back if any exception occurs
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
    
    public async Task<bool> DeleteBulkAsync(List<Guid> ids, CancellationToken ct = default)
    {
        using var rawConnection = await dbConnectionFactory.CreateConnectionAsync(ct);
        var connection = (NpgsqlConnection)rawConnection;

        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        try
        {
            // Lock all movie rows to be deleted to prevent concurrent operations
            var lockedIds = await connection.QueryAsync<Guid>(
                new CommandDefinition("""
                                          SELECT id FROM movies
                                          WHERE id IN @Ids
                                          FOR UPDATE;
                                      """, new { Ids = ids }, transaction: transaction, cancellationToken: ct));

            // If no matching movies found, rollback early
            if (!lockedIds.Any())
            {
                await transaction.RollbackAsync(ct);
                return false;
            }

            // Delete all genres associated with the locked movie IDs
            await connection.ExecuteAsync(
                new CommandDefinition("""
                                          DELETE FROM genres WHERE movieId IN @Ids;
                                      """, new { Ids = ids }, transaction: transaction, cancellationToken: ct));

            // Delete the movies themselves
            var result = await connection.ExecuteAsync(
                new CommandDefinition("""
                                          DELETE FROM movies WHERE id IN @Ids;
                                      """, new { Ids = ids }, transaction: transaction, cancellationToken: ct));

            // Commit the transaction after successful delete
            await transaction.CommitAsync(ct);
            return result > 0;
        }
        catch
        {
            // Roll back the transaction on any error
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition("""
            select count(1) from movies where id = @id
            """, new { id }, cancellationToken: cancellationToken));
    }
    
    public async Task<bool> ExistsAsync(string title, int yearOfRelease, CancellationToken ct = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(ct);

        const string sql = """
                               SELECT 1
                               FROM   movies
                               WHERE  title = @Title
                               AND    yearofrelease = @YearOfRelease
                               LIMIT  1;
                           """;

        var result = await connection.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(sql, new { Title = title, YearOfRelease = yearOfRelease }, cancellationToken: ct));

        return result.HasValue;
    }

    public async Task<int> GetCountAsync(string? title, int? yearOfRelease, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<int>(new CommandDefinition("""
                                                                            select count(id) from movies
                                                                            where (@title is null or title like ('%' || @title || '%'))
                                                                            and  (@yearOfRelease is null or yearofrelease = @yearOfRelease)
                                                                            """, new
        {
            title,
            yearOfRelease
        }, cancellationToken: cancellationToken));
    }
}
