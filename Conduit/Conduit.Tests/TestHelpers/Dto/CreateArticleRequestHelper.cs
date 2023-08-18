﻿using Conduit.Web.Articles.Handlers;
using Conduit.Web.Articles.ViewModels;

namespace Conduit.Tests.TestHelpers.Dto;

public static class CreateArticleRequestHelper
{
    public static CreateArticleRequest Create(
        string? body = null,
        string? description = null,
        string? title = null,
        List<string>? tags = null,
        string? username = null,
        bool makeTagsNull = false)
    {
        var random = new Random();

        body ??= random.String(1000);
        description ??= random.String(100);
        title ??= random.String(60);
        tags ??= new List<string> { "Couchbase", "cruising" };
        username ??= Path.GetRandomFileName();
        if (makeTagsNull) tags = null;

        var article = new CreateArticleSubmitModel
        {
            Body = body,
            Description = description,
            Title = title,
            Tags = tags
        };

        return new CreateArticleRequest(article, username);
    }
}