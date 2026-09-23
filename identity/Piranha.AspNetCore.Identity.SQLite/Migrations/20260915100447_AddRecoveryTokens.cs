using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Piranha.AspNetCore.Identity.SQLite.Migrations
{
    /// <inheritdoc />
    public partial class AddRecoveryTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Piranha_RecoveryTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CodeHash = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_Piranha_RecoveryTokens_UserId",
                table: "Piranha_RecoveryTokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Piranha_RecoveryTokens");
        }
    }
}
