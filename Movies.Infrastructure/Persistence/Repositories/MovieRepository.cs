using Dapper;
using Movies.Application.Abstractions.Persistence;
using Movies.Domain.Enums;
using Movies.Domain.Models;
using Movies.Infrastructure.Persistence.Database;

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
        using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        
        await connection.ExecuteAsync(new CommandDefinition("""
            delete from genres where movieid = @id
            """, new { id = movie.Id }, cancellationToken: cancellationToken));
        
        foreach (var genre in movie.Genres)
        {
            await connection.ExecuteAsync(new CommandDefinition("""
                    insert into genres (movieId, name) 
                    values (@MovieId, @Name)
                    """, new { MovieId = movie.Id, Name = genre }, cancellationToken: cancellationToken));
        }
        
        var result = await connection.ExecuteAsync(new CommandDefinition("""
            update movies set slug = @Slug, title = @Title, yearofrelease = @YearOfRelease 
            where id = @Id
            """, movie, cancellationToken: cancellationToken));
        
        transaction.Commit();
        return result > 0;
    }

    public async Task<bool> DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        
        await connection.ExecuteAsync(new CommandDefinition("""
            delete from genres where movieid = @id
            """, new { id }, cancellationToken: cancellationToken));
        
        var result = await connection.ExecuteAsync(new CommandDefinition("""
            delete from movies where id = @id
            """, new { id }, cancellationToken: cancellationToken));
        
        transaction.Commit();
        return result > 0;
    }
    
    public async Task<bool> DeleteBulkAsync(List<Guid> ids, CancellationToken ct = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(ct);
        using var transaction = connection.BeginTransaction();

         var result = await connection.ExecuteAsync("""
                                          DELETE FROM genres WHERE movieId IN @Ids;
                                          DELETE FROM movies WHERE id IN @Ids;
                                      """, new { Ids = ids }, transaction);

        transaction.Commit();
        return result > 0;
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
