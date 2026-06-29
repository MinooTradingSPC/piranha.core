/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using Xunit;
using Piranha.Models;
using Piranha.Repositories;
using Piranha.Services;

namespace Piranha.Tests.Services;

[Collection("Integration tests")]
public class AliasTestsMemoryCache : AliasTests
{
    public override Task InitializeAsync()
    {
        _cache = new Cache.MemoryCache((IMemoryCache)_services.GetService(typeof(IMemoryCache)));
        return base.InitializeAsync();
    }
}

[Collection("Integration tests")]
public class AliasTestsDistributedCache : AliasTests
{
    public override Task InitializeAsync()
    {
        _cache = new Cache.DistributedCache((IDistributedCache)_services.GetService(typeof(IDistributedCache)));
        return base.InitializeAsync();
    }
}

[Collection("Integration tests")]
public class AliasTests : BaseTestsAsync
{
    private const string ALIAS_1 = "/old-url";
    private const string ALIAS_2 = "/another-old-url";
    private const string ALIAS_3 = "/moved/page";
    private const string ALIAS_4 = "/another-moved-page";
    private const string ALIAS_5 = "/the-last-moved-page";

    private readonly Guid SITE_ID = Guid.NewGuid();
    private readonly Guid ALIAS_1_ID = Guid.NewGuid();

    public override async Task InitializeAsync()
    {
        using (var api = CreateApi())
        {
            // Add site
            var site = new Site
            {
                Id = SITE_ID,
                Title = "Alias Site",
                InternalId = "AliasSite",
                IsDefault = true
            };
            await api.Sites.SaveAsync(site);

            // Add aliases
            await api.Aliases.SaveAsync(new Alias
            {
                Id = ALIAS_1_ID,
                SiteId = SITE_ID,
                AliasUrl = ALIAS_1,
                RedirectUrl = "/redirect-1"
            });
            await api.Aliases.SaveAsync(new Alias
            {
                SiteId = SITE_ID,
                AliasUrl = ALIAS_4,
                RedirectUrl = "/redirect-4"
            });
            await api.Aliases.SaveAsync(new Alias
            {
                SiteId = SITE_ID,
                AliasUrl = ALIAS_5,
                RedirectUrl = "/redirect-5"
            });
        }
    }

    public override async Task DisposeAsync()
    {
        using (var api = CreateApi())
        {
            var aliases = await api.Aliases.GetAllAsync();
            foreach (var a in aliases)
            {
                await api.Aliases.DeleteAsync(a);
            }

            var sites = await api.Sites.GetAllAsync();
            foreach (var s in sites)
            {
                await api.Sites.DeleteAsync(s);
            }
        }
    }

    [Fact]
    public void IsCached() {
        using (var api = CreateApi()) {
            Assert.Equal(((Api)api).IsCached,
                this.GetType() == typeof(AliasTestsMemoryCache) ||
                this.GetType() == typeof(AliasTestsDistributedCache));
        }
    }

    [Fact]
    public async Task Add()
    {
        using (var api = CreateApi())
        {
            await api.Aliases.SaveAsync(new Alias
            {
                SiteId = SITE_ID,
                AliasUrl = ALIAS_2,
                RedirectUrl = "/redirect-2"
            });
        }
    }

    [Fact]
    public async Task AddDuplicateKey()
    {
        using (var api = CreateApi())
        {
            await Assert.ThrowsAnyAsync<Exception>(async () =>
                await api.Aliases.SaveAsync(new Alias
                {
                    SiteId = SITE_ID,
                    AliasUrl = ALIAS_1,
                    RedirectUrl = "/duplicate-alias"
                })
            );
        }
    }

    [Fact]
    public async Task GetNoneById()
    {
        using (var api = CreateApi())
        {
            var none = await api.Aliases.GetByIdAsync(Guid.NewGuid());

            Assert.Null(none);
        }
    }

    [Fact]
    public async Task GetNoneByAliasUrl()
    {
        using (var api = CreateApi())
        {
            var none = await api.Aliases.GetByAliasUrlAsync("/none-existing-alias");

            Assert.Null(none);
        }
    }

    [Fact]
    public async Task GetNoneByRedirectUrl()
    {
        using (var api = CreateApi())
        {
            var none = await api.Aliases.GetByRedirectUrlAsync("/none-existing-alias");

            Assert.Empty(none);
        }
    }

    [Fact]
    public async Task GetAll()
    {
        using (var api = CreateApi())
        {
            var models = await api.Aliases.GetAllAsync();

            Assert.NotNull(models);
            Assert.NotEmpty(models);
        }
    }

    [Fact]
    public async Task GetAllWithoutDefaultSiteReturnsEmpty()
    {
        var service = new Piranha.Services.AliasService(new ThrowingAliasRepository(), new MissingDefaultSiteService());

        var aliases = await service.GetAllAsync();

        Assert.NotNull(aliases);
        Assert.Empty(aliases);
    }

    [Fact]
    public async Task GetByRedirectUrlWithoutDefaultSiteReturnsEmpty()
    {
        var service = new Piranha.Services.AliasService(new ThrowingAliasRepository(), new MissingDefaultSiteService());

        var aliases = await service.GetByRedirectUrlAsync("/redirect-1");

        Assert.NotNull(aliases);
        Assert.Empty(aliases);
    }

    [Fact]
    public async Task GetById()
    {
        using (var api = CreateApi())
        {
            var model = await api.Aliases.GetByIdAsync(ALIAS_1_ID);

            Assert.NotNull(model);
            Assert.Equal(ALIAS_1, model.AliasUrl);
        }
    }

    [Fact]
    public async Task GetByAliasUrl()
    {
        using (var api = CreateApi())
        {
            var model = await api.Aliases.GetByAliasUrlAsync(ALIAS_1);

            Assert.NotNull(model);
            Assert.Equal(ALIAS_1, model.AliasUrl);
        }
    }

    [Fact]
    public async Task GetByAliasUrlWithDifferentCase()
    {
        using (var api = CreateApi())
        {
            var model = await api.Aliases.GetByAliasUrlAsync("/Old-URL");

            Assert.NotNull(model);
            Assert.Equal(ALIAS_1, model.AliasUrl);
        }
    }

    [Fact]
    public async Task GetByRedirectUrl()
    {
        using (var api = CreateApi())
        {
            var models = await api.Aliases.GetByRedirectUrlAsync("/redirect-1");

            Assert.Single(models);
            Assert.Equal(ALIAS_1, models.First().AliasUrl);
        }
    }

    [Fact]
    public async Task GetByRedirectUrlWithDifferentCase()
    {
        using (var api = CreateApi())
        {
            var models = await api.Aliases.GetByRedirectUrlAsync("/ReDiRect-1");

            Assert.Single(models);
            Assert.Equal(ALIAS_1, models.First().AliasUrl);
        }
    }

    [Fact]
    public async Task Update()
    {
        using (var api = CreateApi())
        {
            var model = await api.Aliases.GetByIdAsync(ALIAS_1_ID);

            Assert.Equal("/redirect-1", model.RedirectUrl);

            model.RedirectUrl = "/redirect-updated";

            await api.Aliases.SaveAsync(model);
        }
    }

    [Fact]
    public async Task FixAliasUrl()
    {
        using (var api = CreateApi())
        {
            var model = new Alias
            {
                SiteId = SITE_ID,
                AliasUrl = "the-alias-url-1",
                RedirectUrl = "/the-redirect-1"
            };

            await api.Aliases.SaveAsync(model);

            Assert.Equal("/the-alias-url-1", model.AliasUrl);
        }
    }

    [Fact]
    public async Task FixRedirectUrl()
    {
        using (var api = CreateApi())
        {
            var model = new Alias
            {
                SiteId = SITE_ID,
                AliasUrl = "/the-alias-url-2",
                RedirectUrl = "the-redirect-2"
            };

            await api.Aliases.SaveAsync(model);

            Assert.Equal("/the-redirect-2", model.RedirectUrl);
        }
    }

    [Fact]
    public async Task AllowHttpUrl()
    {
        using (var api = CreateApi())
        {
            var model = new Alias
            {
                SiteId = SITE_ID,
                AliasUrl = "/the-alias-url-3",
                RedirectUrl = "http://redirect.com"
            };

            await api.Aliases.SaveAsync(model);

            Assert.Equal("http://redirect.com", model.RedirectUrl);
        }
    }

    [Fact]
    public async Task AllowHttpsUrl()
    {
        using (var api = CreateApi())
        {
            var model = new Alias
            {
                SiteId = SITE_ID,
                AliasUrl = "/the-alias-url-4",
                RedirectUrl = "https://redirect.com"
            };

            await api.Aliases.SaveAsync(model);

            Assert.Equal("https://redirect.com", model.RedirectUrl);
        }
    }

    [Fact]
    public async Task Delete()
    {
        using (var api = CreateApi())
        {
            var model = await api.Aliases.GetByAliasUrlAsync(ALIAS_4);

            Assert.NotNull(model);

            model = await api.Aliases.GetByAliasUrlAsync(ALIAS_4);

            await api.Aliases.DeleteAsync(model);
        }
    }

    [Fact]
    public async Task DeleteById()
    {
        using (var api = CreateApi())
        {
            var model = await api.Aliases.GetByAliasUrlAsync(ALIAS_5);

            Assert.NotNull(model);

            await api.Aliases.DeleteAsync(model.Id);
        }
    }
}

internal sealed class ThrowingAliasRepository : IAliasRepository
{
    public Task<IEnumerable<Alias>> GetAll(Guid siteId)
    {
        throw new InvalidOperationException("The alias repository should not be called when no default site exists.");
    }

    public Task<Alias> GetById(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<Alias> GetByAliasUrl(string url, Guid siteId)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Alias>> GetByRedirectUrl(string url, Guid siteId)
    {
        throw new InvalidOperationException("The alias repository should not be called when no default site exists.");
    }

    public Task Save(Alias model)
    {
        throw new NotImplementedException();
    }

    public Task Delete(Guid id)
    {
        throw new NotImplementedException();
    }
}

internal sealed class MissingDefaultSiteService : ISiteService
{
    public Task<IEnumerable<Site>> GetAllAsync()
    {
        throw new NotImplementedException();
    }

    public Task<Site> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<Site> GetByInternalIdAsync(string internalId)
    {
        throw new NotImplementedException();
    }

    public Task<Site> GetByHostnameAsync(string hostname)
    {
        throw new NotImplementedException();
    }

    public Task<Site> GetDefaultAsync()
    {
        return Task.FromResult<Site>(null);
    }

    public Task<DynamicSiteContent> GetContentByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<T> GetContentByIdAsync<T>(Guid id) where T : SiteContent<T>
    {
        throw new NotImplementedException();
    }

    public Task<Sitemap> GetSitemapAsync(Guid? id = null, bool onlyPublished = true)
    {
        throw new NotImplementedException();
    }

    public Task SaveAsync(Site model)
    {
        throw new NotImplementedException();
    }

    public Task SaveContentAsync<T>(Guid siteId, T model) where T : SiteContent<T>
    {
        throw new NotImplementedException();
    }

    public Task<T> CreateContentAsync<T>(string typeId = null) where T : SiteContentBase
    {
        throw new NotImplementedException();
    }

    public Task InvalidateSitemapAsync(Guid id, bool updateLastModified = true)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Site model)
    {
        throw new NotImplementedException();
    }

    public Task RemoveSitemapFromCacheAsync(Guid id)
    {
        throw new NotImplementedException();
    }
}
