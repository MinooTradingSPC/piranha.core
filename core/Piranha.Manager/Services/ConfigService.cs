/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using Piranha.Manager.Models;

namespace Piranha.Manager.Services;

public class ConfigService
{
    private readonly IApi _api;

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="api">The current api</param>
    public ConfigService(IApi api)
    {
        _api = api;
    }

    /// <summary>
    /// Gets the config model.
    /// </summary>
    /// <returns>The model</returns>
    public ConfigModel Get()
    {
        using (var config = new Config(_api))
        {
            return new ConfigModel
            {
                HierarchicalPageSlugs = config.HierarchicalPageSlugs,
                ExpandedSitemapLevels = config.ManagerExpandedSitemapLevels,
                ManagerPageSize = config.ManagerPageSize,
                ManagerMenuWidth = config.ManagerMenuWidth,
                LoginDesign = config.ManagerLoginDesign,
                Theme = config.ManagerTheme ?? "",
                SiteTheme = config.SiteTheme ?? "",
                DefaultCollapsedBlocks = config.ManagerDefaultCollapsedBlocks,
                DefaultCollapsedBlockGroupHeaders = config.ManagerDefaultCollapsedBlockGroupHeaders,
                ArchivePageSize = config.ArchivePageSize,
                CommentsApprove = config.CommentsApprove,
                CommentsCloseAfterDays = config.CommentsCloseAfterDays,
                CommentsEnabledForPages = config.CommentsEnabledForPages,
                CommentsEnabledForPosts = config.CommentsEnabledForPosts,
                CommentsPageSize = config.CommentsPageSize,
                PagesExpires = config.CacheExpiresPages,
                PostsExpires = config.CacheExpiresPosts,
                MediaCDN = config.MediaCDN,
                PageRevisions = config.PageRevisions,
                PostRevisions = config.PostRevisions
            };
        }
    }

    /// <summary>
    /// Saves the given config model to the database.
    /// </summary>
    /// <param name="model">The model</param>
    public void Save(ConfigModel model)
    {
        using (var config = new Config(_api))
        {
            config.HierarchicalPageSlugs = model.HierarchicalPageSlugs;
            config.ManagerExpandedSitemapLevels = model.ExpandedSitemapLevels;
            config.ManagerPageSize = model.ManagerPageSize;
            config.ManagerMenuWidth = model.ManagerMenuWidth;

            // Only store designs the host has registered.
            if (App.Modules.Get<Module>()?.LoginDesigns.Any(d => d.Id == model.LoginDesign) == true)
            {
                config.ManagerLoginDesign = model.LoginDesign;
            }

            // An empty key picks the built-in look, any other key must be a
            // registered theme.
            if (string.IsNullOrEmpty(model.Theme))
            {
                config.ManagerTheme = null;
            }
            else if (App.Modules.Get<Module>()?.GetTheme(model.Theme) != null)
            {
                config.ManagerTheme = model.Theme;
            }

            // The same rule applies to the site theme.
            if (string.IsNullOrEmpty(model.SiteTheme))
            {
                config.SiteTheme = null;
            }
            else if (App.SiteThemes.Get(model.SiteTheme) != null)
            {
                config.SiteTheme = model.SiteTheme;
            }
            config.ManagerDefaultCollapsedBlocks = model.DefaultCollapsedBlocks;
            config.ManagerDefaultCollapsedBlockGroupHeaders = model.DefaultCollapsedBlockGroupHeaders;
            config.ArchivePageSize = model.ArchivePageSize;
            config.CommentsApprove = model.CommentsApprove;
            config.CommentsCloseAfterDays = model.CommentsCloseAfterDays;
            config.CommentsEnabledForPages = model.CommentsEnabledForPages;
            config.CommentsEnabledForPosts = model.CommentsEnabledForPosts;
            config.CommentsPageSize = model.CommentsPageSize;
            config.CacheExpiresPages = model.PagesExpires;
            config.CacheExpiresPosts = model.PostsExpires;
            config.MediaCDN = model.MediaCDN;
            config.PageRevisions = model.PageRevisions;
            config.PostRevisions = model.PostRevisions;
        }
    }
}
