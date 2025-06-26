using Dapper;
using Movies.Application.Abstractions.Persistence;
using Movies.Domain.Models;
using Movies.Infrastructure.Persistence.Database;
using Npgsql;

namespace Movies.Infrastructure.Persistence.Repositories;

public class MovieImageRepository(IDbConnectionFactory dbConnectionFactory) : IMovieImageRepository
{
    public async Task<bool> CreateAsync(MovieImage image, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        
        var result = await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO movie_images (id, movie_id, public_id, original_url, thumbnail_url, medium_url, large_url, 
                                    alt_text, width, height, size, format, is_primary, created_at)
            VALUES (@Id, @MovieId, @PublicId, @OriginalUrl, @ThumbnailUrl, @MediumUrl, @LargeUrl,
                   @AltText, @Width, @Height, @Size, @Format, @IsPrimary, @CreatedAt)
            """, image, cancellationToken: cancellationToken));

        return result > 0;
    }

    public async Task<MovieImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        
        return await connection.QuerySingleOrDefaultAsync<MovieImage>(new CommandDefinition("""
            SELECT id, movie_id as MovieId, public_id as PublicId, original_url as OriginalUrl, 
                   thumbnail_url as ThumbnailUrl, medium_url as MediumUrl, large_url as LargeUrl,
                   alt_text as AltText, width, height, size, format, is_primary as IsPrimary, 
                   created_at as CreatedAt, updated_at as UpdatedAt
            FROM movie_images 
            WHERE id = @id
            """, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<MovieImage>> GetByMovieIdAsync(Guid movieId, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        
        return await connection.QueryAsync<MovieImage>(new CommandDefinition("""
            SELECT id, movie_id as MovieId, public_id as PublicId, original_url as OriginalUrl, 
                   thumbnail_url as ThumbnailUrl, medium_url as MediumUrl, large_url as LargeUrl,
                   alt_text as AltText, width, height, size, format, is_primary as IsPrimary, 
                   created_at as CreatedAt, updated_at as UpdatedAt
            FROM movie_images 
            WHERE movie_id = @movieId
            ORDER BY is_primary DESC, created_at ASC
            """, new { movieId }, cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(MovieImage image, CancellationToken cancellationToken = default)
    {
        using var rawConnection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        var connection = (NpgsqlConnection)rawConnection;

        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // Lock the row to prevent concurrent updates
            var existingId = await connection.ExecuteScalarAsync<Guid?>(
                new CommandDefinition("""
                                          SELECT id FROM movie_images
                                          WHERE id = @Id
                                          FOR UPDATE;
                                      """, new { image.Id }, transaction: transaction, cancellationToken: cancellationToken));

            if (existingId is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            // Proceed with the update
            var result = await connection.ExecuteAsync(
                new CommandDefinition("""
                                          UPDATE movie_images 
                                          SET alt_text = @AltText, is_primary = @IsPrimary, updated_at = @UpdatedAt
                                          WHERE id = @Id;
                                      """,
                    new
                    {
                        image.AltText,
                        image.IsPrimary,
                        image.Id,
                        UpdatedAt = DateTime.UtcNow
                    }, transaction: transaction, cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
            return result > 0;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var rawConnection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        var connection = (NpgsqlConnection)rawConnection;

        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // Lock the specific image row to prevent concurrent modification/deletion
            var lockedId = await connection.ExecuteScalarAsync<Guid?>(
                new CommandDefinition("""
                                          SELECT id FROM movie_images
                                          WHERE id = @id
                                          FOR UPDATE;
                                      """, new { id }, transaction: transaction, cancellationToken: cancellationToken));

            if (lockedId is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            // Proceed with deletion after locking
            var result = await connection.ExecuteAsync(
                new CommandDefinition("""
                                          DELETE FROM movie_images WHERE id = @id;
                                      """, new { id }, transaction: transaction, cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
            return result > 0;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> SetPrimaryImageAsync(Guid imageId, Guid movieId, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            // Reset all images for this movie to non-primary
            await connection.ExecuteAsync(new CommandDefinition("""
                UPDATE movie_images SET is_primary = false WHERE movie_id = @movieId
                """, new { movieId }, transaction: transaction, cancellationToken: cancellationToken));

            // Set the specified image as primary
            var result = await connection.ExecuteAsync(new CommandDefinition("""
                UPDATE movie_images SET is_primary = true, updated_at = @UpdatedAt WHERE id = @imageId
                """, new { imageId, UpdatedAt = DateTime.UtcNow }, transaction: transaction, cancellationToken: cancellationToken));

            transaction.Commit();
            return result > 0;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
    
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition("""
            SELECT COUNT(1) FROM movie_images WHERE id = @id
            """, new { id }, cancellationToken: cancellationToken));
    }

    public async Task<MovieImage?> GetPrimaryImageAsync(Guid movieId, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        
        return await connection.QuerySingleOrDefaultAsync<MovieImage>(new CommandDefinition("""
            SELECT id, movie_id as MovieId, public_id as PublicId, original_url as OriginalUrl, 
                   thumbnail_url as ThumbnailUrl, medium_url as MediumUrl, large_url as LargeUrl,
                   alt_text as AltText, width, height, size, format, is_primary as IsPrimary, 
                   created_at as CreatedAt, updated_at as UpdatedAt
            FROM movie_images 
            WHERE movie_id = @movieId AND is_primary = true
            """, new { movieId }, cancellationToken: cancellationToken));
    }
    
    public async Task<IEnumerable<MovieImage>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = """
                           SELECT id, movie_id as MovieId, public_id as PublicId, original_url as OriginalUrl, 
                                  thumbnail_url as ThumbnailUrl, medium_url as MediumUrl, large_url as LargeUrl,
                                  alt_text as AltText, width, height, size, format, is_primary as IsPrimary, 
                                  created_at as CreatedAt, updated_at as UpdatedAt
                           FROM movie_images
                           """;

        return await connection.QueryAsync<MovieImage>(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }
    
    public async Task<bool> CreateManyAsync(IEnumerable<MovieImage> images, CancellationToken cancellationToken = default)
    {
        using var connection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = """
                           INSERT INTO movie_images (id, movie_id, public_id, original_url, thumbnail_url, medium_url, large_url, 
                                                     alt_text, width, height, size, format, is_primary, created_at)
                           VALUES (@Id, @MovieId, @PublicId, @OriginalUrl, @ThumbnailUrl, @MediumUrl, @LargeUrl,
                                   @AltText, @Width, @Height, @Size, @Format, @IsPrimary, @CreatedAt)
                           """;

        var result = await connection.ExecuteAsync(new CommandDefinition(sql, images, cancellationToken: cancellationToken));
        return result == images.Count(); // لو كله نجح
    }
    
    public async Task<bool> DeleteManyAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        using var rawConnection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        var connection = (NpgsqlConnection)rawConnection;

        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // Lock all matching image rows to prevent concurrent operations
            var lockedIds = await connection.QueryAsync<Guid>(
                new CommandDefinition("""
                                          SELECT id FROM movie_images
                                          WHERE id = ANY(@Ids)
                                          FOR UPDATE;
                                      """, new { Ids = ids.ToArray() }, transaction: transaction, cancellationToken: cancellationToken));

            // If no matching IDs found, rollback
            if (!lockedIds.Any())
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            // Proceed with deletion after rows are locked
            var result = await connection.ExecuteAsync(
                new CommandDefinition("""
                                          DELETE FROM movie_images WHERE id = ANY(@Ids);
                                      """, new { Ids = ids.ToArray() }, transaction: transaction, cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);

            // We expect to delete exactly the same number of rows
            return result == ids.Count();
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
    
    public async Task<bool> UpdateManyAsync(IEnumerable<MovieImage> images, CancellationToken cancellationToken = default)
    {
        using var rawConnection = await dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        var connection = (NpgsqlConnection)rawConnection;

        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var ids = images.Select(img => img.Id).ToArray();

            // Lock all target rows before updating
            var lockedIds = await connection.QueryAsync<Guid>(
                new CommandDefinition("""
                                          SELECT id FROM movie_images
                                          WHERE id = ANY(@Ids)
                                          FOR UPDATE;
                                      """, new { Ids = ids }, transaction: transaction, cancellationToken: cancellationToken));

            if (!lockedIds.Any())
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            // Perform update per image (you can optimize this further if needed)
            foreach (var image in images)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition("""
                                              UPDATE movie_images 
                                              SET alt_text = @AltText, is_primary = @IsPrimary, updated_at = @UpdatedAt
                                              WHERE id = @Id;
                                          """,
                        new
                        {
                            image.AltText,
                            image.IsPrimary,
                            image.Id,
                            UpdatedAt = DateTime.UtcNow
                        }, transaction: transaction, cancellationToken: cancellationToken));
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}