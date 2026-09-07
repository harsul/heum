using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heum.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxDeadLetter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FailedAtUtc",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_Attempts",
                table: "OutboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_FailedAtUtc",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAtUtc", "FailedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_FailedAtUtc",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "FailedAtUtc",
                table: "OutboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_Attempts",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAtUtc", "Attempts" });
        }
    }
}
