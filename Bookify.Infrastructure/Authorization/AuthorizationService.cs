using Bookify.Application.Abstractions.Caching;
using Bookify.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Bookify.Infrastructure.Authorization;

internal sealed class AuthorizationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICacheService _cacheService;

    public AuthorizationService(
        ApplicationDbContext dbContext,
        ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async Task<UserRolesResponse> GetRolesForUserAsync(
        string identityId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"auth:roles-{identityId}";

        var cacheRoles = await _cacheService.GetAsync<UserRolesResponse>(cacheKey, cancellationToken);

        if (cacheRoles is not null)
        {
            return cacheRoles;
        }

        var roles = await _dbContext.Set<User>()
            .Where(user => user.IdentityId == identityId)
            .Select(user => new UserRolesResponse
            {
                Id = user.Id,
                Roles = user.Roles.ToList()
            })
            .FirstAsync(cancellationToken);

        await _cacheService.SetAsync(cacheKey, roles, cancellationToken: cancellationToken);

        return roles;
    }

    public async Task<HashSet<string>> GetPermissionsForUserAsync(
        string identityId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"auth:permissions-{identityId}";

        var cachePermissions = await _cacheService.GetAsync<HashSet<string>>(cacheKey, cancellationToken);

        if (cachePermissions is not null)
        {
            return cachePermissions;
        }

        var permissions = await _dbContext.Set<User>()
            .Where(user => user.IdentityId == identityId)
            .SelectMany(user => user.Roles.Select(role => role.Permissions))
            .FirstAsync(cancellationToken);

        var permissionsSet = permissions.Select(p => p.Name).ToHashSet();

        await _cacheService.SetAsync(cacheKey, permissionsSet, cancellationToken: cancellationToken);

        return permissionsSet;
    }
}
