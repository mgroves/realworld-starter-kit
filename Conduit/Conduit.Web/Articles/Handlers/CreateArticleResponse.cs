﻿using Conduit.Web.Articles.ViewModels;
using Conduit.Web.DataAccess.Models;
using Conduit.Web.Users.ViewModels;
using FluentValidation.Results;

namespace Conduit.Web.Articles.Handlers;

public class CreateArticleResponse
{
    public List<ValidationFailure> ValidationErrors { get; set; }
    public ArticleViewModel Article { get; set; }
}