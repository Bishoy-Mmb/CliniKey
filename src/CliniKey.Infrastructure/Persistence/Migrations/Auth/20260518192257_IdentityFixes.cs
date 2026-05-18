using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CliniKey.Infrastructure.Persistence.Migrations.Auth
{
    /// <inheritdoc />
    public partial class IdentityFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_token_hash",
                schema: "public",
                table: "refresh_tokens");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0001-0001-0001-000000000001"),
                column: "ConcurrencyStamp",
                value: "68bfb878-b83c-49e2-bc73-68cffef81dc9");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0001-0001-0001-000000000002"),
                column: "ConcurrencyStamp",
                value: "56d8ae36-80bf-4b66-a56f-91415a0ab840");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0001-0001-0001-000000000003"),
                column: "ConcurrencyStamp",
                value: "6c394a6d-2841-4b40-a7ac-99cf591ef700");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_token_hash",
                schema: "public",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_token_hash",
                schema: "public",
                table: "refresh_tokens");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0001-0001-0001-000000000001"),
                column: "ConcurrencyStamp",
                value: "49b02ea7-3f80-4b1a-88ed-19606803560e");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0001-0001-0001-000000000002"),
                column: "ConcurrencyStamp",
                value: "b8872248-69c8-4451-ac52-efe9dc43931e");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-0001-0001-0001-000000000003"),
                column: "ConcurrencyStamp",
                value: "c054ddaf-61eb-472c-bb1a-329638b208ee");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_token_hash",
                schema: "public",
                table: "refresh_tokens",
                column: "token_hash");
        }
    }
}
