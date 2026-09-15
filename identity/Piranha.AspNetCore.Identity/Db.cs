/*
 * Copyright (c) .NET Foundation and Contributors
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 *
 * https://github.com/piranhacms/piranha.core
 *
 */

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Piranha.AspNetCore.Identity.Data;

namespace Piranha.AspNetCore.Identity;

public abstract class Db<T> :
    IdentityDbContext<User, Role, Guid,
        IdentityUserClaim<Guid>,
        IdentityUserRole<Guid>,
        IdentityUserLogin<Guid>,
        IdentityRoleClaim<Guid>,
        IdentityUserToken<Guid>>,
    IDb
    where T : Db<T>
{
    /// <summary>
    /// Gets/sets the registered passkeys set.
    /// </summary>
    public DbSet<Passkey> Passkeys { get; set; }

    /// <summary>
    /// Gets/sets the confirmed TOTP authenticator credentials set.
    /// </summary>
    public DbSet<TotpCredential> TotpCredentials { get; set; }

    /// <summary>
    /// Gets/sets the email recovery codes set.
    /// </summary>
    public DbSet<RecoveryToken> RecoveryTokens { get; set; }

    /// <summary>
    ///     Gets/sets whether the db context as been initialized. This
    ///     is only performed once in the application lifecycle.
    /// </summary>
    private static volatile bool IsInitialized;

    /// <summary>
    ///     The object mutex used for initializing the context.
    /// </summary>
    private static readonly object Mutex = new object();

    /// <summary>
    ///     Default constructor.
    /// </summary>
    /// <param name="options">Configuration options</param>
    protected Db(DbContextOptions<T> options) : base(options)
    {
        if (IsInitialized)
        {
            return;
        }

        lock (Mutex)
        {
            if (IsInitialized)
            {
                return;
            }

            // Migrate database
            Database.Migrate();

            Seed();

            IsInitialized = true;
        }
    }

    /// <summary>
    ///     Creates and configures the data model.
    /// </summary>
    /// <param name="mb">The current model builder</param>
    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        mb.Entity<User>().ToTable("Piranha_Users");
        mb.Entity<Role>().ToTable("Piranha_Roles");
        mb.Entity<IdentityUserClaim<Guid>>().ToTable("Piranha_UserClaims");
        mb.Entity<IdentityUserRole<Guid>>().ToTable("Piranha_UserRoles");
        mb.Entity<IdentityUserLogin<Guid>>().ToTable("Piranha_UserLogins");
        mb.Entity<IdentityRoleClaim<Guid>>().ToTable("Piranha_RoleClaims");
        mb.Entity<IdentityUserToken<Guid>>().ToTable("Piranha_UserTokens");

        mb.Entity<Passkey>(entity =>
        {
            entity.ToTable("Piranha_Passkeys");
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.CredentialId).IsUnique();
            entity.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<TotpCredential>(entity =>
        {
            entity.ToTable("Piranha_TotpCredentials");
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => t.UserId).IsUnique();
            entity.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<RecoveryToken>(entity =>
        {
            entity.ToTable("Piranha_RecoveryTokens");
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.UserId);
            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Seeds the default data.
    /// </summary>
    private void Seed()
    {
        SaveChanges();

        // Make sure we have a SysAdmin role
        var role = Roles.FirstOrDefault(r => r.NormalizedName == "SYSADMIN");
        if (role == null)
        {
            role = new Role
            {
                Id = Guid.NewGuid(),
                Name = "SysAdmin",
                NormalizedName = "SYSADMIN"
            };
            Roles.Add(role);
        }

        // Make sure our SysAdmin role has all of the available claims
        //foreach (var claim in Piranha.Security.Permission.All())
        foreach (var permission in App.Permissions.GetPermissions())
        {
            var roleClaim = RoleClaims.FirstOrDefault(c =>
                c.RoleId == role.Id && c.ClaimType == permission.Name && c.ClaimValue == permission.Name);
            if (roleClaim == null)
            {
                RoleClaims.Add(new IdentityRoleClaim<Guid>
                {
                    RoleId = role.Id,
                    ClaimType = permission.Name,
                    ClaimValue = permission.Name
                });
            }
        }

        SaveChanges();
    }
}
