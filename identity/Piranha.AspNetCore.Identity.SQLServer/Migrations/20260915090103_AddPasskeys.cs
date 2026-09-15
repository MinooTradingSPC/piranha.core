using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Piranha.AspNetCore.Identity.SQLServer.Migrations
{
    /// <inheritdoc />
    public partial class AddPasskeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Piranha_Passkeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CredentialId = table.Column<byte[]>(type: "varbinary(900)", nullable: true),
                    PublicKey = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    SignatureCounter = table.Column<long>(type: "bigint", nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsed = table.Column<DateTime>(type: "datetime2", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_Piranha_Passkeys_CredentialId",
                table: "Piranha_Passkeys",
                column: "CredentialId",
                unique: true,
                filter: "[CredentialId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Piranha_Passkeys_UserId",
                table: "Piranha_Passkeys",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Piranha_Passkeys");
        }
    }
}
