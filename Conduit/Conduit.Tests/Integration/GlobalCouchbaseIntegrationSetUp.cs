﻿using Conduit.Migrations;
using Couchbase.Extensions.DependencyInjection;
using Couchbase;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NoSqlMigrator.Runner;
using Conduit.Web.DataAccess.Providers;

namespace Conduit.Tests.Integration;

[SetUpFixture]
public class GlobalCouchbaseIntegrationSetUp
{
    public static ServiceCollection ServiceCollection;
    private static ServiceProvider? _serviceProvider;
    public static ServiceProvider ServiceProvider
    {
        get { return _serviceProvider ??= ServiceCollection.BuildServiceProvider(); }
    }

    private IConfigurationRoot _config;
    private IBucket _bucket;
    private ICluster _cluster;

    [OneTimeSetUp]
    public async Task RunBeforeAllTests()
    {
        _config = new ConfigurationBuilder()
            .AddUserSecrets<CouchbaseIntegrationTest>()
            .AddEnvironmentVariables()
            .Build();

        _cluster = await Cluster.ConnectAsync(
            _config["Couchbase:ConnectionString"],
            _config["Couchbase:Username"],
            _config["Couchbase:Password"]);

        await _cluster.WaitUntilReadyAsync(TimeSpan.FromSeconds(30));

        var allBuckets = await _cluster.Buckets.GetAllBucketsAsync();
        var doesBucketExist = allBuckets.Any(b => b.Key == _config["Couchbase:BucketName"]);
        if (!doesBucketExist)
            throw new Exception($"There is no bucket for integration testing named '{_config["Couchbase:BucketName"]}'.");

        _bucket = await _cluster.BucketAsync(_config["Couchbase:BucketName"]);

        // *** nosql migrations
        var runner = new MigrationRunner();
        // run the migrations down just to be safe
        var downSettings = new RunSettings();
        downSettings.Direction = DirectionEnum.Down;
        downSettings.Bucket = _bucket;
        await runner.Run(typeof(CreateUserCollectionInDefaultScope).Assembly, downSettings);

        // run the migrations
        var upSettings = new RunSettings();
        upSettings.Direction = DirectionEnum.Up;
        upSettings.Bucket = _bucket;
        await runner.Run(typeof(CreateUserCollectionInDefaultScope).Assembly, upSettings);

        // create a DI setup, in order to get Couchbase DI objects 
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCouchbase(options =>
        {
            options.ConnectionString = _config["Couchbase:ConnectionString"];
            options.UserName = _config["Couchbase:Username"];
            options.Password = _config["Couchbase:Password"];
        });

        services.AddCouchbaseBucket<IConduitBucketProvider>("ConduitIntegrationTests", b =>
        {
            b
                .AddScope("_default")
                .AddCollection<IConduitTagsCollectionProvider>("Tags");
            b
                .AddScope("_default")
                .AddCollection<IConduitArticlesCollectionProvider>("Articles");
            b
                .AddScope("_default")
                .AddCollection<IConduitFavoritesCollectionProvider>("Favorites");
            b
                .AddScope("_default")
                .AddCollection<IConduitUsersCollectionProvider>("Users");
            b
                .AddScope("_default")
                .AddCollection<IConduitFollowsCollectionProvider>("Follows");
        });

        ServiceCollection = services;
    }

    [OneTimeTearDown]
    public async Task RunAfterAllTests()
    {
        // run the migrations down to clean up
        var runner = new MigrationRunner();
        var downSettings = new RunSettings();
        downSettings.Direction = DirectionEnum.Down;
        downSettings.Bucket = _bucket;
        await runner.Run(typeof(CreateUserCollectionInDefaultScope).Assembly, downSettings);

        // dispose couchbase services
        await _cluster.DisposeAsync();
        await ServiceProvider.GetRequiredService<ICouchbaseLifetimeService>().CloseAsync();
    }
}