/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using Microsoft.Extensions.Localization;
using Piranha.Manager;
using Piranha.Manager.Controllers;
using Piranha.Manager.Models;
using Piranha.Models;
using Piranha.Services;
using Xunit;

namespace Piranha.Tests.Manager;

public class RevertControllerTests
{
    [Fact]
    public async Task PageRevertReturnsErrorWhenPageIsMissing()
    {
        var localizer = CreateLocalizer();
        var api = new TestApi(new MissingPageService(), null);
        var service = new Piranha.Manager.Services.PageService(api, null, localizer);
        var controller = new PageApiController(service, api, localizer, null, null);

        var result = await controller.Revert(Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Equal(StatusMessage.Error, result.Status.Type);
        Assert.Equal("The page could not be found", result.Status.Body);
    }

    [Fact]
    public async Task PostRevertReturnsErrorWhenPostIsMissing()
    {
        var localizer = CreateLocalizer();
        var api = new TestApi(null, new MissingPostService());
        var service = new Piranha.Manager.Services.PostService(api, null);
        var controller = new PostApiController(service, api, localizer, null);

        var result = await controller.Revert(Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Equal(StatusMessage.Error, result.Status.Type);
        Assert.Equal("The post could not be found", result.Status.Body);
    }

    private static ManagerLocalizer CreateLocalizer()
    {
        return new ManagerLocalizer(
            new TestStringLocalizer<Piranha.Manager.Localization.Alias>(),
            new TestStringLocalizer<Piranha.Manager.Localization.Comment>(),
            new TestStringLocalizer<Piranha.Manager.Localization.Content>(),
            new TestStringLocalizer<Piranha.Manager.Localization.Config>(),
            new TestStringLocalizer<Piranha.Manager.Localization.General>(),
            new TestStringLocalizer<Piranha.Manager.Localization.Security>(),
            new TestStringLocalizer<Piranha.Manager.Localization.Language>(),
            new TestStringLocalizer<Piranha.Manager.Localization.Media>(),
            new TestStringLocalizer<Piranha.Manager.Localization.Menu>(),
            new TestStringLocalizer<Piranha.Manager.Localization.Module>(),
            new TestStringLocalizer<Piranha.Manager.Localization.Page>(),
            new TestStringLocalizer<Piranha.Manager.Localization.Post>(),
            new TestStringLocalizer<Piranha.Manager.Localization.Site>());
    }

    private sealed class TestStringLocalizer<T> : IStringLocalizer<T>
    {
        public LocalizedString this[string name] => new(name, name);

        public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return Enumerable.Empty<LocalizedString>();
        }
    }

    private sealed class TestApi : IApi
    {
        public TestApi(IPageService pages, IPostService posts)
        {
            Pages = pages;
            Posts = posts;
        }

        public IAliasService Aliases => throw new NotImplementedException();
        public IArchiveService Archives => throw new NotImplementedException();
        public IContentService Content => throw new NotImplementedException();
        public IContentGroupService ContentGroups => throw new NotImplementedException();
        public IContentTypeService ContentTypes => throw new NotImplementedException();
        public ILanguageService Languages => throw new NotImplementedException();
        public IMediaService Media => throw new NotImplementedException();
        public IPageService Pages { get; }
        public IPageTypeService PageTypes => throw new NotImplementedException();
        public IParamService Params => throw new NotImplementedException();
        public IPostService Posts { get; }
        public IPostTypeService PostTypes => throw new NotImplementedException();
        public ISiteService Sites => throw new NotImplementedException();
        public ISiteTypeService SiteTypes => throw new NotImplementedException();

        public void Dispose()
        {
        }
    }

    private sealed class MissingPageService : IPageService
    {
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
        public Task<T> GetStartpageAsync<T>(Guid? siteId = null) where T : PageBase => throw new NotImplementedException();
        public Task<DynamicPage> GetByIdAsync(Guid id) => Task.FromResult<DynamicPage>(null);
        public Task<IEnumerable<T>> GetByIdsAsync<T>(params Guid[] ids) where T : PageBase => throw new NotImplementedException();
        public Task<T> GetByIdAsync<T>(Guid id) where T : PageBase => Task.FromResult<T>(null);
        public Task<DynamicPage> GetBySlugAsync(string slug, Guid? siteId = null) => throw new NotImplementedException();
        public Task<T> GetBySlugAsync<T>(string slug, Guid? siteId = null) where T : PageBase => throw new NotImplementedException();
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

    private sealed class MissingPostService : IPostService
    {
        public Task<T> CreateAsync<T>(string typeId = null) where T : PostBase => throw new NotImplementedException();
        public Task<IEnumerable<DynamicPost>> GetAllAsync(Guid blogId, int? index = null, int? pageSize = null) => throw new NotImplementedException();
        public Task<IEnumerable<T>> GetAllAsync<T>(Guid blogId, int? index = null, int? pageSize = null) where T : PostBase => throw new NotImplementedException();
        public Task<IEnumerable<DynamicPost>> GetAllBySiteIdAsync(Guid? siteId = null) => throw new NotImplementedException();
        public Task<IEnumerable<T>> GetAllBySiteIdAsync<T>(Guid? siteId = null) where T : PostBase => throw new NotImplementedException();
        public Task<IEnumerable<DynamicPost>> GetAllAsync(string slug, Guid? siteId = null) => throw new NotImplementedException();
        public Task<IEnumerable<T>> GetAllAsync<T>(string slug, Guid? siteId = null) where T : PostBase => throw new NotImplementedException();
        public Task<IEnumerable<Taxonomy>> GetAllCategoriesAsync(Guid blogId) => throw new NotImplementedException();
        public Task<IEnumerable<Taxonomy>> GetAllTagsAsync(Guid blogId) => throw new NotImplementedException();
        public Task<IEnumerable<Guid>> GetAllDraftsAsync(Guid blogId) => throw new NotImplementedException();
        public Task<IEnumerable<Comment>> GetAllCommentsAsync(Guid? postId = null, bool onlyApproved = true, int? page = null, int? pageSize = null) => throw new NotImplementedException();
        public Task<IEnumerable<Comment>> GetAllPendingCommentsAsync(Guid? postId = null, int? page = null, int? pageSize = null) => throw new NotImplementedException();
        public Task<int> GetCountAsync(Guid archiveId) => throw new NotImplementedException();
        public Task<DynamicPost> GetByIdAsync(Guid id) => Task.FromResult<DynamicPost>(null);
        public Task<T> GetByIdAsync<T>(Guid id) where T : PostBase => Task.FromResult<T>(null);
        public Task<IEnumerable<T>> GetByIdsAsync<T>(params Guid[] ids) where T : PostBase => Task.FromResult<IEnumerable<T>>(Array.Empty<T>());
        public Task<DynamicPost> GetDraftByIdAsync(Guid id) => throw new NotImplementedException();
        public Task<T> GetDraftByIdAsync<T>(Guid id) where T : PostBase => throw new NotImplementedException();
        public Task<DynamicPost> GetBySlugAsync(string blog, string slug, Guid? siteId = null) => throw new NotImplementedException();
        public Task<T> GetBySlugAsync<T>(string blog, string slug, Guid? siteId = null) where T : PostBase => throw new NotImplementedException();
        public Task<DynamicPost> GetBySlugAsync(Guid blogId, string slug) => throw new NotImplementedException();
        public Task<T> GetBySlugAsync<T>(Guid blogId, string slug) where T : PostBase => throw new NotImplementedException();
        public Task<Taxonomy> GetCategoryByIdAsync(Guid id) => throw new NotImplementedException();
        public Task<Taxonomy> GetCategoryBySlugAsync(Guid blogId, string slug) => throw new NotImplementedException();
        public Task<Taxonomy> GetTagByIdAsync(Guid id) => throw new NotImplementedException();
        public Task<Taxonomy> GetTagBySlugAsync(Guid blogId, string slug) => throw new NotImplementedException();
        public Task<Comment> GetCommentByIdAsync(Guid id) => throw new NotImplementedException();
        public Task SaveAsync<T>(T model) where T : PostBase => throw new NotImplementedException();
        public Task SaveDraftAsync<T>(T model) where T : PostBase => throw new NotImplementedException();
        public Task SaveCommentAsync(Guid postId, Comment model) => throw new NotImplementedException();
        public Task SaveCommentAndVerifyAsync(Guid postId, Comment model) => throw new NotImplementedException();
        public Task DeleteAsync(Guid id) => throw new NotImplementedException();
        public Task DeleteAsync<T>(T model) where T : PostBase => throw new NotImplementedException();
        public Task DeleteCommentAsync(Guid id) => throw new NotImplementedException();
        public Task DeleteCommentAsync(Comment model) => throw new NotImplementedException();
    }
}