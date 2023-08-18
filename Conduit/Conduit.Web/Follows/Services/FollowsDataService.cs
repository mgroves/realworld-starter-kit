﻿using Conduit.Web.DataAccess.Providers;
using Conduit.Web.Users.Services;
using Couchbase.KeyValue;

namespace Conduit.Web.Follows.Services;

public interface IFollowDataService
{
    Task FollowUser(string userToFollow, string followerUsername);
    Task UnfollowUser(string userToUnfollow, string followerUsername);
    Task<bool> IsCurrentUserFollowing(string currentUserBearerToken, string username);
}

public class FollowsDataService : IFollowDataService
{
    private readonly IConduitFollowsCollectionProvider _followsCollectionProvider;
    private readonly IAuthService _authService;

    public FollowsDataService(IConduitFollowsCollectionProvider followsCollectionProvider, IAuthService authService)
    {
        _followsCollectionProvider = followsCollectionProvider;
        _authService = authService;
    }

    public async Task FollowUser(string userToFollow, string followerUsername)
    {
        var collection = await _followsCollectionProvider.GetCollectionAsync();

        var followKey = $"{followerUsername}::follows";

        var set = collection.Set<string>(followKey);

        await set.AddAsync(userToFollow);
    }

    public async Task UnfollowUser(string userToUnfollow, string followerUsername)
    {
        var collection = await _followsCollectionProvider.GetCollectionAsync();

        var followKey = $"{followerUsername}::follows";

        var set = collection.Set<string>(followKey);

        await set.RemoveAsync(userToUnfollow);
    }

    public async Task<bool> IsCurrentUserFollowing(string currentUserBearerToken, string username)
    {
        // TODO: can't follow yourself

        var currentUserUsername = _authService.GetUsernameClaim(currentUserBearerToken);

        var collection = await _followsCollectionProvider.GetCollectionAsync();

        var followKey = $"{currentUserUsername.Value}::follows";

        var set = collection.Set<string>(followKey);

        return await set.ContainsAsync(username);
    }
}