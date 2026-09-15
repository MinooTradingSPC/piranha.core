/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Piranha.Manager;

namespace Piranha.AspNetCore.Identity.Controllers;

/// <summary>
/// Manager controller for the current user's own account pages.
/// </summary>
[Area("Manager")]
[Authorize(Policy = Permission.Admin)]
public sealed class AccountController : Controller
{
    /// <summary>
    /// Gets the account security view, where the current user manages
    /// their own passkeys and authenticator app enrollment.
    /// </summary>
    [HttpGet]
    [Route("/manager/account/security")]
    public IActionResult Security()
    {
        return View();
    }
}
