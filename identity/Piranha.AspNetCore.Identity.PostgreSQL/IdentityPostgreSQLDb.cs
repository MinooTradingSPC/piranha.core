/*
 * Copyright (c) 2018 Jason Underhill
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using Microsoft.EntityFrameworkCore;

namespace Piranha.AspNetCore.Identity.PostgreSQL;

public class IdentityPostgreSQLDb : Db<IdentityPostgreSQLDb>
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="options">Configuration options</param>
    public IdentityPostgreSQLDb(DbContextOptions<IdentityPostgreSQLDb> options) : base(options)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    /// <summary>
    /// Creates and configures the data model.
    /// </summary>
    /// <param name="mb">The current model builder</param>
    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // The schema was created with serial columns. Keep them, since newer
        // Npgsql providers default to identity columns and would otherwise
        // try to convert, dropping sequences that don't exist for uuid keys.
        mb.UseSerialColumns();
    }
}
