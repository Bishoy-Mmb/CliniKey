using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CliniKey.Infrastructure.Persistence.Migrations.Auth
{
    /// <inheritdoc />
    public partial class RemoveRolesSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "public",
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValues: new object[]
                {
                    new Guid("aaaaaaaa-0001-0001-0001-000000000001"),
                    new Guid("aaaaaaaa-0001-0001-0001-000000000002"),
                    new Guid("aaaaaaaa-0001-0001-0001-000000000003")
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "public",
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-0001-0001-0001-000000000001"), "68bfb878-b83c-49e2-bc73-68cffef81dc9", "ClinicAdmin", "CLINICADMIN" },
                    { new Guid("aaaaaaaa-0001-0001-0001-000000000002"), "56d8ae36-80bf-4b66-a56f-91415a0ab840", "Dentist", "DENTIST" },
                    { new Guid("aaaaaaaa-0001-0001-0001-000000000003"), "6c394a6d-2841-4b40-a7ac-99cf591ef700", "Receptionist", "RECEPTIONIST" }
                });
        }
    }
}
