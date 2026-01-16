using Bookify.Application.Abstractions.Caching;
using Bookify.Domain.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bookify.Application.Abstractions.Behaviors;

internal sealed class QueryCachingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICachedQuery
    where TResponse : Result
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<QueryCachingBehavior<TRequest, TResponse>> _logger;

    public QueryCachingBehavior(
        ILogger<QueryCachingBehavior<TRequest, TResponse>> logger,
        ICacheService cacheService)
    {
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var cachedResult = await _cacheService.GetAsync<TResponse>(
            request.CacheKey,
            cancellationToken);
        
        var name = typeof(TRequest).Name;
        if (cachedResult is not null)
        {
            _logger.LogInformation("Cache hit for {Query}", name);
            
            return cachedResult;
        }
        
        _logger.LogInformation("Cache miss for {Query}", name);
        
        var result = await next(cancellationToken);

        if (result.IsSuccess)
        {
            await _cacheService.SetAsync(request.CacheKey, result, request.Expiration, cancellationToken);
        }
        
        return result;
    }
}