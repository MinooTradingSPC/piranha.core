/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Piranha.AspNetCore.Identity.Services;

/// <summary>
/// Options for <see cref="SmtpEmailSender"/>.
/// </summary>
public sealed class SmtpOptions
{
    /// <summary>
    /// Gets/sets the SMTP server host.
    /// </summary>
    public string Host { get; set; }

    /// <summary>
    /// Gets/sets the SMTP server port.
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// Gets/sets whether to use TLS.
    /// </summary>
    public SecureSocketOptions SecureSocketOptions { get; set; } = SecureSocketOptions.StartTls;

    /// <summary>
    /// Gets/sets the SMTP username, if authentication is required.
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// Gets/sets the SMTP password, if authentication is required.
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// Gets/sets the "from" address emails are sent as.
    /// </summary>
    public string FromAddress { get; set; }

    /// <summary>
    /// Gets/sets the "from" display name emails are sent as.
    /// </summary>
    public string FromName { get; set; } = "Piranha Manager";
}

/// <summary>
/// Default <see cref="IEmailSender"/> implementation, sending over SMTP
/// via MailKit. Registered by the host with <c>AddPiranhaSmtpEmailSender</c>.
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public SmtpEmailSender(SmtpOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(_options.Host, _options.Port, _options.SecureSocketOptions);

            if (!string.IsNullOrEmpty(_options.UserName))
            {
                await client.AuthenticateAsync(_options.UserName, _options.Password);
            }

            await client.SendAsync(message);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true);
            }
        }
    }
}
