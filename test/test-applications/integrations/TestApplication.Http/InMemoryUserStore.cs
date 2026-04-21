// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET10_0_OR_GREATER
using Microsoft.AspNetCore.Identity;

namespace TestApplication.Http;

/// <summary>
/// In-memory user store for testing Identity metrics
/// </summary>
#pragma warning disable CA1812 // Avoid uninstantiated internal classes. This class is instantiated by Identity.
internal sealed class InMemoryUserStore : IUserPasswordStore<TestUser>
#pragma warning restore CA1812 // Avoid uninstantiated internal classes. This class is instantiated by Identity.
{
    private readonly Dictionary<string, TestUser> _users = [];
    private readonly Dictionary<string, string> _passwords = [];

    public Task<IdentityResult> CreateAsync(TestUser user, CancellationToken cancellationToken)
    {
        _users[user.UserName!] = user;
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> DeleteAsync(TestUser user, CancellationToken cancellationToken)
    {
        _users.Remove(user.UserName!);
        _passwords.Remove(user.Id);
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<TestUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        var user = _users.Values.FirstOrDefault(u => u.Id == userId);
        return Task.FromResult(user);
    }

    public Task<TestUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        _users.TryGetValue(normalizedUserName, out var user);
        return Task.FromResult(user);
    }

    public Task<string?> GetNormalizedUserNameAsync(TestUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.NormalizedUserName);
    }

    public Task<string> GetUserIdAsync(TestUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Id);
    }

    public Task<string?> GetUserNameAsync(TestUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.UserName);
    }

    public Task SetNormalizedUserNameAsync(TestUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetUserNameAsync(TestUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<IdentityResult> UpdateAsync(TestUser user, CancellationToken cancellationToken)
    {
        _users[user.UserName!] = user;
        return Task.FromResult(IdentityResult.Success);
    }

    public void Dispose()
    {
    }

    public Task SetPasswordHashAsync(TestUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        if (passwordHash != null)
        {
            _passwords[user.Id] = passwordHash;
        }

        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(TestUser user, CancellationToken cancellationToken)
    {
        _passwords.TryGetValue(user.Id, out var hash);
        return Task.FromResult(hash);
    }

    public Task<bool> HasPasswordAsync(TestUser user, CancellationToken cancellationToken)
    {
        return Task.FromResult(_passwords.ContainsKey(user.Id));
    }
}
#endif
