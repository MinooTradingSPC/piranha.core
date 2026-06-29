/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using Microsoft.AspNetCore.Mvc;
using Piranha.Manager.Controllers;
using Piranha.Models;
using Piranha.Services;
using Xunit;

namespace Piranha.Tests.Manager;

public class MediaApiControllerTests
{
    [Fact]
    public async Task GetUrlReturnsNotFoundWhenRequestedVersionIsUnavailable()
    {
        var controller = new MediaApiController(null, new TestApi(new TestMediaService()), null);

        var result = await controller.GetUrl(Guid.NewGuid(), 640);

        Assert.IsType<NotFoundResult>(result);
    }

    private sealed class TestApi : IApi
    {
        public TestApi(IMediaService media)
        {
            Media = media;
        }

        public IAliasService Aliases => throw new NotImplementedException();
        public IArchiveService Archives => throw new NotImplementedException();
        public IContentService Content => throw new NotImplementedException();
        public IContentGroupService ContentGroups => throw new NotImplementedException();
        public IContentTypeService ContentTypes => throw new NotImplementedException();
        public ILanguageService Languages => throw new NotImplementedException();
        public IMediaService Media { get; }
        public IPageService Pages => throw new NotImplementedException();
        public IPageTypeService PageTypes => throw new NotImplementedException();
        public IParamService Params => throw new NotImplementedException();
        public IPostService Posts => throw new NotImplementedException();
        public IPostTypeService PostTypes => throw new NotImplementedException();
        public ISiteService Sites => throw new NotImplementedException();
        public ISiteTypeService SiteTypes => throw new NotImplementedException();

        public void Dispose()
        {
        }
    }

    private sealed class TestMediaService : IMediaService
    {
        public Task<IEnumerable<Media>> GetAllByFolderIdAsync(Guid? folderId = null) => throw new NotImplementedException();
        public Task<int> CountFolderItemsAsync(Guid? folderId = null) => throw new NotImplementedException();
        public Task<IEnumerable<MediaFolder>> GetAllFoldersAsync(Guid? folderId = null) => throw new NotImplementedException();
        public Task<Media> GetByIdAsync(Guid id) => throw new NotImplementedException();
        public Task<IEnumerable<Media>> GetByIdAsync(params Guid[] ids) => throw new NotImplementedException();
        public Task<MediaFolder> GetFolderByIdAsync(Guid id) => throw new NotImplementedException();
        public Task<MediaStructure> GetStructureAsync() => throw new NotImplementedException();
        public Task SaveAsync(Media model) => throw new NotImplementedException();
        public Task SaveAsync(MediaContent content) => throw new NotImplementedException();
        public Task SaveFolderAsync(MediaFolder model) => throw new NotImplementedException();
        public Task MoveAsync(Media model, Guid? folderId) => throw new NotImplementedException();
        public string EnsureVersion(Guid id, int width, int? height = null) => throw new NotImplementedException();
        public Task<string> EnsureVersionAsync(Guid id, int width, int? height = null) => Task.FromResult<string>(null);
        public Task<string> EnsureVersionAsync(Media media, int width, int? height = null) => throw new NotImplementedException();
        public Task DeleteAsync(Guid id) => throw new NotImplementedException();
        public Task DeleteAsync(Media model) => throw new NotImplementedException();
        public Task DeleteFolderAsync(Guid id) => throw new NotImplementedException();
        public Task DeleteFolderAsync(MediaFolder model) => throw new NotImplementedException();
    }
}