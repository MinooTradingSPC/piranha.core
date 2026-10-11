/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using System.Text.RegularExpressions;
using Piranha.AspNetCore.Identity.Services;

namespace Piranha.Tests.Identity;

/// <summary>
/// Test double for <see cref="IEmailSender"/> that just records what was
/// sent, so a test can pull the recovery code out of the email body the
/// same way a real recipient would - by reading the email - rather than
/// reaching into RecoveryService internals.
/// </summary>
public sealed class FakeEmailSender : IEmailSender
{
    public List<(string ToEmail, string Subject, string HtmlBody)> SentMessages { get; } = new();

    public Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        SentMessages.Add((toEmail, subject, htmlBody));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Extracts the 6-digit code from the most recently sent message.
    /// </summary>
    public string GetLastCode()
    {
        var html = SentMessages.Last().HtmlBody;
        var match = Regex.Match(html, @"\b(\d{6})\b");

        return match.Success ? match.Groups[1].Value : null;
    }
}
