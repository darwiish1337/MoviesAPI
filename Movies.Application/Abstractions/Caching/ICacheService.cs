﻿namespace Movies.Application.Abstractions.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class;
    
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
}