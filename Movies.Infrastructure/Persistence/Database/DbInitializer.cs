using Dapper;

namespace Movies.Infrastructure.Persistence.Database;

public class DbInitializer(IDbConnectionFactory dbConnectionFactory)
{
    private readonly IDbConnectionFactory _dbConnectionFactory = dbConnectionFactory;

    public async Task InitializeAsync()
    {
        using var dbConnection = await _dbConnectionFactory.CreateConnectionAsync();

        await dbConnection.ExecuteAsync("""
                                          create table if not exists movies (
                                          id UUID primary key,
                                          slug TEXT not null, 
                                          title TEXT not null,
                                          yearofrelease integer not null);
                                      """);
        
        await dbConnection.ExecuteAsync("""
                                          create unique index concurrently if not exists movies_slug_idx
                                          on movies
                                          using btree(slug);
                                      """);
        
        await dbConnection.ExecuteAsync("""
                                          create table if not exists genres (
                                          movieId UUID references movies (Id),
                                          name TEXT not null);
                                      """);
        
        await dbConnection.ExecuteAsync("""
                                          create table if not exists ratings (
                                          userid uuid,
                                          movieid uuid references movies (id),
                                          rating integer not null,
                                          primary key (userid, movieid));
                                      """);   
    }
}