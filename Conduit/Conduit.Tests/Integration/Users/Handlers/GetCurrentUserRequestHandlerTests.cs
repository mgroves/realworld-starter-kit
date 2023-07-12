﻿using Conduit.Web.Models;
using Conduit.Web.Users.Handlers;
using Conduit.Web.Users.Services;
using Couchbase.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Conduit.Tests.Integration.Users.Handlers;

public class GetCurrentUserRequestHandlerTests : CouchbaseIntegrationTest
{
    [OneTimeSetUp]
    public override async Task Setup()
    {
        await base.Setup();

        ServiceCollection.AddCouchbaseBucket<IConduitBucketProvider>("ConduitIntegrationTests", b =>
        {
            b
                .AddScope("_default")
                .AddCollection<IConduitUsersCollectionProvider>("Users");
        });
    }

    [Test]
    public async Task GetCurrentUserRequestHandler_Is_Successful()
    {
        // *** arrange
        // arrange collections provider
        var usersCollectionProvider = ServiceProvider.GetRequiredService<IConduitUsersCollectionProvider>();

        // arrange the handler
        var authService = new AuthService();
        var getCurrentUserHandler = new GetCurrentUserHandler(authService, new UserDataService(usersCollectionProvider, authService));

        // arrange the request
        var email = $"valid{Path.GetRandomFileName()}@example.net";
        var username = $"valid{Path.GetRandomFileName()}";
        var fakeToken = new AuthService().GenerateJwtToken(email, username);
        var request = new GetCurrentUserRequest(fakeToken);

        // arrange the current user already in the database
        var collection = await usersCollectionProvider.GetCollectionAsync();
        var password = "ValidPassword1#";
        var salt = new AuthService().GenerateSalt();
        await collection.InsertAsync(username, new User
        {
            Email = email,
            Password = new AuthService().HashPassword(password, salt),
            PasswordSalt = salt,
            Bio = "lorem ipsum",
            Image = "http://example.net/profile.jpg"
        });

        // *** act
        var result = await getCurrentUserHandler.Handle(request, CancellationToken.None);

        // *** assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.IsInvalidToken, Is.False);
        Assert.That(result.UserView.Email, Is.EqualTo(email));
    }
}