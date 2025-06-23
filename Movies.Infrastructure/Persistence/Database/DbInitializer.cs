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
        
        await dbConnection.ExecuteAsync("""
                                        CREATE TABLE IF NOT EXISTS movie_images (
                                            id UUID PRIMARY KEY,
                                            movie_id UUID NOT NULL,
                                            public_id VARCHAR(255) NOT NULL UNIQUE,
                                            original_url TEXT NOT NULL,
                                            thumbnail_url TEXT NOT NULL,
                                            medium_url TEXT NOT NULL,
                                            large_url TEXT NOT NULL,
                                            alt_text VARCHAR(500) NOT NULL DEFAULT '',
                                            width INTEGER NOT NULL,
                                            height INTEGER NOT NULL,
                                            size BIGINT NOT NULL,
                                            format VARCHAR(10) NOT NULL,
                                            is_primary BOOLEAN NOT NULL DEFAULT FALSE,
                                            created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
                                            updated_at TIMESTAMP WITH TIME ZONE,
                                            
                                            CONSTRAINT fk_movie_images_movie_id 
                                                FOREIGN KEY (movie_id) 
                                                REFERENCES movies(id) 
                                                ON DELETE CASCADE);
                                        """);
        
        await dbConnection.ExecuteAsync("""
                                        CREATE INDEX IF NOT EXISTS idx_movie_images_movie_id ON movie_images(movie_id);
                                        CREATE INDEX IF NOT EXISTS idx_movie_images_is_primary ON movie_images(movie_id, is_primary) WHERE is_primary = true;
                                        CREATE INDEX IF NOT EXISTS idx_movie_images_created_at ON movie_images(created_at);
                                        """);
        //Ensure only one primary image per movie
        await dbConnection.ExecuteAsync("""
                                        CREATE UNIQUE INDEX IF NOT EXISTS idx_movie_images_unique_primary 
                                        ON movie_images(movie_id) 
                                        WHERE is_primary = true;
                                        """);
    }
}