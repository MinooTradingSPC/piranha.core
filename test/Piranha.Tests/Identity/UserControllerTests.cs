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
using Microsoft.Extensions.Localization;
using Piranha.AspNetCore.Identity.Controllers;
using Piranha.AspNetCore.Identity.Data;
using Piranha.AspNetCore.Identity.Models;
using Piranha.Manager;
using Piranha.Manager.Models;
using Xunit;

namespace Piranha.Tests.Identity;

public class UserControllerTests
{
    [Fact]
    public async Task SavePasswordMismatchDoesNotReturnSubmittedPasswords()
    {
        var model = new UserEditModel
        {
            User = new User
            {
                Id = Guid.NewGuid(),
                UserName = "user",
                Email = "user@example.com"
            },
            Password = "Plaintext-Secret-1!",
            PasswordConfirm = "Plaintext-Secret-2!"
        };
        var controller = new UserController(null, null, CreateLocalizer());

        var result = await controller.Save(model);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var body = Assert.IsType<AsyncResult>(badRequest.Value);

        Assert.Contains("does not match", body.Status.Body);
        Assert.DoesNotContain(model.Password, body.Status.Body);
        Assert.DoesNotContain(model.PasswordConfirm, body.Status.Body);
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
}
