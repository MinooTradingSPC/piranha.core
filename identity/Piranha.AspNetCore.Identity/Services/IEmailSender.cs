/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

namespace Piranha.AspNetCore.Identity.Services;

/// <summary>
/// Optional, host-pluggable service for sending outbound emails - only
/// used by the email-otp recovery login. If the host doesn't register an
/// implementation (e.g. via <c>AddPiranhaSmtpEmailSender</c>), recovery
/// codes are logged instead of delivered, and the flow's responses stay
/// generic either way.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends an email.
    /// </summary>
    /// <param name="toEmail">The recipient</param>
    /// <param name="subject">The subject</param>
    /// <param name="htmlBody">The HTML body</param>
    Task SendAsync(string toEmail, string subject, string htmlBody);
}
