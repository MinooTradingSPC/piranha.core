using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Piranha.AspNetCore.Identity.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class PasswordLessLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Piranha_Passkeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CredentialId = table.Column<byte[]>(type: "bytea", nullable: true),
                    PublicKey = table.Column<byte[]>(type: "bytea", nullable: true),
                    SignatureCounter = table.Column<long>(type: "bigint", nullable: false),
                    DeviceName = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUsed = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Piranha_Passkeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Piranha_Passkeys_Piranha_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Piranha_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Piranha_RecoveryTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeHash = table.Column<string>(type: "text", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExpiresUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Piranha_RecoveryTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Piranha_RecoveryTokens_Piranha_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Piranha_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Piranha_TotpCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptedSecret = table.Column<string>(type: "text", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastUsed = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastAcceptedTimeStep = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Piranha_TotpCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Piranha_TotpCredentials_Piranha_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Piranha_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Piranha_Passkeys_CredentialId",
                table: "Piranha_Passkeys",
                column: "CredentialId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Piranha_Passkeys_UserId",
                table: "Piranha_Passkeys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Piranha_RecoveryTokens_UserId",
                table: "Piranha_RecoveryTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Piranha_TotpCredentials_UserId",
                table: "Piranha_TotpCredentials",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Piranha_Passkeys");

            migrationBuilder.DropTable(
                name: "Piranha_RecoveryTokens");

            migrationBuilder.DropTable(
                name: "Piranha_TotpCredentials");

        }
    }
}
