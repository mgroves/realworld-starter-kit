﻿using Conduit.Web.Extensions;
using Newtonsoft.Json;

namespace Conduit.Web.DataAccess.Models;

public class Article
{
    [JsonIgnore] // ignoring this because it's the document ID (which is duplicated as part of the slug)
    public string ArticleKey => Slug.GetArticleKey();
    public string Slug { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Body { get; set; }
    public List<string> TagList { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    [JsonIgnore] // this is always calculated, depending on the logged in user, don't store
    public bool Favorited { get; set; }
    public int FavoritesCount { get; set; }
    public string AuthorUsername { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}