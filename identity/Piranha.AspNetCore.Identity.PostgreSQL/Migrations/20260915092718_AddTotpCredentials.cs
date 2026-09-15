using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Piranha.AspNetCore.Identity.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddTotpCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Piranha_TotpCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptedSecret = table.Column<string>(type: "text", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsed = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                name: "IX_Piranha_TotpCredentials_UserId",
                table: "Piranha_TotpCredentials",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Piranha_TotpCredentials");
        }
    }
}
