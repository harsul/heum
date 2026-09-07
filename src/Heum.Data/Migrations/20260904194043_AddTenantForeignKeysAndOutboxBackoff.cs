using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heum.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantForeignKeysAndOutboxBackoff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_Attempts",
                table: "OutboxMessages");

            migrationBuilder.AddColumn<DateTime>(
                name: "NextAttemptAtUtc",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Plans_Name",
                table: "Plans",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_Attempts_OccurredAtUtc",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAtUtc", "Attempts", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_TenantId_Status",
                table: "Invitations",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Tenants_TenantId",
                table: "Invitations",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TenantEntitlementOverrides_Tenants_TenantId",
                table: "TenantEntitlementOverrides",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TenantSettings_Tenants_TenantId",
                table: "TenantSettings",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TenantSubscriptions_Tenants_TenantId",
                table: "TenantSubscriptions",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_Tenants_TenantId",
                table: "Invitations");

            migrationBuilder.DropForeignKey(
                name: "FK_TenantEntitlementOverrides_Tenants_TenantId",
                table: "TenantEntitlementOverrides");

            migrationBuilder.DropForeignKey(
                name: "FK_TenantSettings_Tenants_TenantId",
                table: "TenantSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_TenantSubscriptions_Tenants_TenantId",
                table: "TenantSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Plans_Name",
                table: "Plans");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_Attempts_OccurredAtUtc",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_TenantId_Status",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "NextAttemptAtUtc",
                table: "OutboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_Attempts",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAtUtc", "Attempts" });
        }
    }
}
