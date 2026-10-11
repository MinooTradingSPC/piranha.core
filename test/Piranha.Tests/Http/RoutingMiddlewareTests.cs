/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Piranha.AspNetCore;
using Piranha.AspNetCore.Helpers;
using Piranha.AspNetCore.Http;
using Piranha.AspNetCore.Models;
using Piranha.AspNetCore.Services;
using Piranha.Models;
using Piranha.Services;
using Xunit;

namespace Piranha.Tests.Http;

/// <summary>
/// Regression tests for issue #143: RoutingMiddleware routed unpublished content
/// to unauthenticated visitors via the ?draft=true query parameter, and also
/// exposed hook side-effects before the early-return check.
/// </summary>
public class RoutingMiddlewareTests
{
    private static readonly Guid _siteId = Guid.NewGuid();
    private static readonly Guid _draftPageId = Guid.NewGuid();

    // ──────────────────────────────────────────────────────────────────────────
    // Page draft-access tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DraftPage_UnauthenticatedUser_WithDraftParam_IsNotRouted()
    {
        // An unauthenticated visitor appending ?draft=true must NOT be served
        // an unpublished page — this was the primary attack vector of issue #143.
        var (ctx, service) = BuildContext("/draft-page", queryString: "?draft=true", authenticated: false);
        var middleware = BuildMiddleware();

        await middleware.Invoke(ctx, BuildApi(draftPage: true), service);

        // Path must remain unchanged: no route was set.
        Assert.Equal("/draft-page", ctx.Request.Path.Value);
        // service.PageId must NOT have been set (hook side-effect guard).
        Assert.Equal(Guid.Empty, service.PageId);
    }

    [Fact]
    public async Task DraftPage_UnauthenticatedUser_WithoutDraftParam_IsNotRouted()
    {
        var (ctx, service) = BuildContext("/draft-page", queryString: "", authenticated: false);
        var middleware = BuildMiddleware();

        await middleware.Invoke(ctx, BuildApi(draftPage: true), service);

        Assert.Equal("/draft-page", ctx.Request.Path.Value);
        Assert.Equal(Guid.Empty, service.PageId);
    }

    [Fact]
    public async Task DraftPage_AuthenticatedUser_WithoutDraftParam_IsNotRouted()
    {
        // Even authenticated users must explicitly request ?draft=true; browsing
        // normally should not expose unpublished content.
        var (ctx, service) = BuildContext("/draft-page", queryString: "", authenticated: true);
        var middleware = BuildMiddleware();

        await middleware.Invoke(ctx, BuildApi(draftPage: true), service);

        Assert.Equal("/draft-page", ctx.Request.Path.Value);
        Assert.Equal(Guid.Empty, service.PageId);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static RoutingMiddleware BuildMiddleware()
    {
        var opts = Options.Create(new RoutingOptions
        {
            UseSiteRouting = false,
            UseAliasRouting = false,
            UseStartpageRouting = true,
            UsePageRouting = true,
            UsePostRouting = false,
            UseArchiveRouting = false
        });
        return new RoutingMiddleware(_ => Task.CompletedTask, opts);
    }

    private static (DefaultHttpContext ctx, StubApplicationService service) BuildContext(
        string path, string queryString, bool authenticated)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        ctx.Request.QueryString = new QueryString(queryString);
        ctx.Request.Host = new HostString("localhost");
        ctx.Request.Scheme = "http";

        if (authenticated)
        {
            var identity = new ClaimsIdentity("TestAuth");
            ctx.User = new ClaimsPrincipal(identity);
        }
        else
        {
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity()); // not authenticated
        }

        return (ctx, new StubApplicationService());
    }

    private static StubApi BuildApi(bool draftPage)
    {
        var site = new Site
        {
            Id = _siteId,
            Title = "Test",
            LanguageId = Guid.NewGuid()
        };

        // A minimal page: either published or draft
        var page = new StubPage
        {
            Id = _draftPageId,
            TypeId = "StandardPage",
            Slug = draftPage ? "draft-page" : "published-page",
            SiteId = _siteId,
            Published = draftPage ? (DateTime?)null : DateTime.UtcNow,
            Route = "/page"
        };

        return new StubApi(site, page);
    }

    // ── Stubs ────────────────────────────────────────────────────────────────

    private sealed class StubPage : PageBase { }

    private sealed class StubApplicationService : IApplicationService
    {
        public IApi Api => null;
        public ISiteHelper Site { get; } = new StubSiteHelper();
        public IMediaHelper Media => null;
        public IRequestHelper Request { get; } = new StubRequestHelper();
        public Guid PageId { get; set; }
        public PageBase CurrentPage { get; set; }
        public PostBase CurrentPost { get; set; }
        public Task InitAsync(HttpContext context) => Task.CompletedTask;
        public string GetGravatarUrl(string email, int size = 0) => null;
    }

    private sealed class StubSiteHelper : ISiteHelper
    {
        public Guid Id { get; set; }
        public Guid LanguageId { get; set; }
        public string Culture { get; set; }
        public string Host { get; set; }
        public string SitePrefix { get; set; }
        public Sitemap Sitemap { get; set; }
        public SiteDescription Description { get; set; } = new();
        public Task<T> GetContentAsync<T>() where T : SiteContent<T> => Task.FromResult<T>(null);
    }

    private sealed class StubRequestHelper : IRequestHelper
    {
        public string Host { get; set; }
        public int? Port { get; set; }
        public string Scheme { get; set; }
        public string Url { get; set; }
    }

    private sealed class StubApi : IApi
    {
        private readonly Site _site;
        private readonly PageBase _page;

        public StubApi(Site site, PageBase page)
        {
            _site = site;
            _page = page;
            Sites = new StubSiteService(site);
            Languages = new StubLanguageService();
            Aliases = new StubAliasService();
            Pages = new StubPageService(page);
        }

        public IAliasService Aliases { get; }
        public IArchiveService Archives => throw new NotImplementedException();
        public IContentService Content => throw new NotImplementedException();
        public IContentGroupService ContentGroups => throw new NotImplementedException();
        public IContentTypeService ContentTypes => throw new NotImplementedException();
        public ILanguageService Languages { get; }
        public IMediaService Media => throw new NotImplementedException();
        public IPageService Pages { get; }
        public IPageTypeService PageTypes => throw new NotImplementedException();
        public IParamService Params { get; } = new StubParamService();
        public IPostService Posts => throw new NotImplementedException();
        public IPostTypeService PostTypes => throw new NotImplementedException();
        public ISiteService Sites { get; }
        public ISiteTypeService SiteTypes => throw new NotImplementedException();
        public void Dispose() { }
    }

    private sealed class StubSiteService : ISiteService
    {
        private readonly Site _site;
        public StubSiteService(Site site) => _site = site;

        public Task<Site> GetDefaultAsync() => Task.FromResult(_site);
        public Task<IEnumerable<Site>> GetAllAsync() => Task.FromResult<IEnumerable<Site>>(new[] { _site });
        public Task<Site> GetByIdAsync(Guid id) => Task.FromResult(_site);
        public Task<Site> GetByInternalIdAsync(string internalId) => Task.FromResult<Site>(null);
        public Task<Site> GetByHostnameAsync(string hostname) => Task.FromResult<Site>(null);
        public Task<Sitemap> GetSitemapAsync(Guid? id = null, bool onlyPublished = true) => Task.FromResult<Sitemap>(null);
        public Task<DynamicSiteContent> GetContentByIdAsync(Guid id) => Task.FromResult<DynamicSiteContent>(null);
        public Task<T> GetContentByIdAsync<T>(Guid id) where T : SiteContent<T> => Task.FromResult<T>(null);
        public Task<T> CreateContentAsync<T>(string typeId = null) where T : SiteContentBase => Task.FromResult<T>(null);
        public Task SaveAsync(Site model) => Task.CompletedTask;
        public Task SaveContentAsync<T>(Guid siteId, T model) where T : SiteContent<T> => Task.CompletedTask;
        public Task InvalidateSitemapAsync(Guid id, bool updateLastModified = true) => Task.CompletedTask;
        public Task RemoveSitemapFromCacheAsync(Guid id) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
        public Task DeleteAsync(Site model) => Task.CompletedTask;
    }

    private sealed class StubLanguageService : ILanguageService
    {
        public Task<IEnumerable<Language>> GetAllAsync() => Task.FromResult<IEnumerable<Language>>(Array.Empty<Language>());
        public Task<Language> GetByIdAsync(Guid id) => Task.FromResult<Language>(null);
        public Task<Language> GetDefaultAsync() => Task.FromResult<Language>(null);
        public Task SaveAsync(Language model) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
        public Task DeleteAsync(Language model) => Task.CompletedTask;
    }

    private sealed class StubAliasService : IAliasService
    {
        public Task<IEnumerable<Alias>> GetAllAsync(Guid? siteId = null) => Task.FromResult<IEnumerable<Alias>>(Array.Empty<Alias>());
        public Task<Alias> GetByIdAsync(Guid id) => Task.FromResult<Alias>(null);
        public Task<Alias> GetByAliasUrlAsync(string url, Guid? siteId = null) => Task.FromResult<Alias>(null);
        public Task<IEnumerable<Alias>> GetByRedirectUrlAsync(string url, Guid? siteId = null) => Task.FromResult<IEnumerable<Alias>>(Array.Empty<Alias>());
        public Task SaveAsync(Alias model) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
        public Task DeleteAsync(Alias model) => Task.CompletedTask;
    }

    private sealed class StubParamService : IParamService
    {
        public Task<IEnumerable<Param>> GetAllAsync() => Task.FromResult<IEnumerable<Param>>(Array.Empty<Param>());
        public Task<Param> GetByIdAsync(Guid id) => Task.FromResult<Param>(null);
        public Task<Param> GetByKeyAsync(string key) => Task.FromResult<Param>(null);
        public Task SaveAsync(Param model) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
        public Task DeleteAsync(Param model) => Task.CompletedTask;
    }

    private sealed class StubPageService : IPageService
    {
        private readonly PageBase _page;
        public StubPageService(PageBase page) => _page = page;

        public Task<T> GetBySlugAsync<T>(string slug, Guid? siteId = null) where T : PageBase
            => Task.FromResult(_page as T);

        public Task<T> GetStartpageAsync<T>(Guid? siteId = null) where T : PageBase => Task.FromResult<T>(null);
        public Task<T> CreateAsync<T>(string typeId = null) where T : PageBase => throw new NotImplementedException();
        public Task<T> CopyAsync<T>(T originalPage) where T : PageBase => throw new NotImplementedException();
        public Task DetachAsync<T>(T model) where T : PageBase => throw new NotImplementedException();
        public Task<IEnumerable<DynamicPage>> GetAllAsync(Guid? siteId = null) => throw new NotImplementedException();
        public Task<IEnumerable<T>> GetAllAsync<T>(Guid? siteId = null) where T : PageBase => throw new NotImplementedException();
        public Task<IEnumerable<DynamicPage>> GetAllBlogsAsync(Guid? siteId = null) => throw new NotImplementedException();
        public Task<IEnumerable<T>> GetAllBlogsAsync<T>(Guid? siteId = null) where T : PageBase => throw new NotImplementedException();
        public Task<IEnumerable<Guid>> GetAllDraftsAsync(Guid? siteId = null) => throw new NotImplementedException();
        public Task<IEnumerable<Comment>> GetAllCommentsAsync(Guid? pageId = null, bool onlyApproved = true, int? page = null, int? pageSize = null) => throw new NotImplementedException();
        public Task<IEnumerable<Comment>> GetAllPendingCommentsAsync(Guid? pageId = null, int? page = null, int? pageSize = null) => throw new NotImplementedException();
        public Task<DynamicPage> GetStartpageAsync(Guid? siteId = null) => throw new NotImplementedException();
        public Task<DynamicPage> GetByIdAsync(Guid id) => throw new NotImplementedException();
        public Task<IEnumerable<T>> GetByIdsAsync<T>(params Guid[] ids) where T : PageBase => throw new NotImplementedException();
        public Task<T> GetByIdAsync<T>(Guid id) where T : PageBase => throw new NotImplementedException();
        public Task<DynamicPage> GetBySlugAsync(string slug, Guid? siteId = null) => throw new NotImplementedException();
        public Task<Guid?> GetIdBySlugAsync(string slug, Guid? siteId = null) => throw new NotImplementedException();
        public Task<DynamicPage> GetDraftByIdAsync(Guid id) => throw new NotImplementedException();
        public Task<T> GetDraftByIdAsync<T>(Guid id) where T : PageBase => throw new NotImplementedException();
        public Task MoveAsync<T>(T model, Guid? parentId, int sortOrder) where T : PageBase => throw new NotImplementedException();
        public Task<Comment> GetCommentByIdAsync(Guid id) => throw new NotImplementedException();
        public Task SaveAsync<T>(T model) where T : PageBase => throw new NotImplementedException();
        public Task SaveDraftAsync<T>(T model) where T : PageBase => throw new NotImplementedException();
        public Task SaveCommentAsync(Guid pageId, PageComment model) => throw new NotImplementedException();
        public Task SaveCommentAndVerifyAsync(Guid pageId, PageComment model) => throw new NotImplementedException();
        public Task DeleteAsync(Guid id) => throw new NotImplementedException();
        public Task DeleteAsync<T>(T model) where T : PageBase => throw new NotImplementedException();
        public Task DeleteCommentAsync(Guid id) => throw new NotImplementedException();
        public Task DeleteCommentAsync(Comment model) => throw new NotImplementedException();
    }
}
