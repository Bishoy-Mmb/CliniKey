using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CliniKey.Infrastructure.Persistence.Migrations.Shared
{
    /// <inheritdoc />
    public partial class AddSharedTenantRegistry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "shared");

            migrationBuilder.CreateTable(
                name: "tenants",
                schema: "shared",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    schema_name = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    provisioning_status = table.Column<string>(type: "text", nullable: false),
                    schema_health_status = table.Column<string>(type: "text", nullable: false),
                    current_migration = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    last_schema_verified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deactivated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deactivated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clinics",
                schema: "shared",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    deactivated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deactivated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clinics", x => x.id);
                    table.ForeignKey(
                        name: "FK_clinics_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "shared",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dentists",
                schema: "shared",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    specialization = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    license_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dentists", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_provisioning_audit_logs",
                schema: "shared",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    schema_name = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: true),
                    operation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    operator_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_provisioning_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_tenant_provisioning_audit_logs_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "shared",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "clinic_dentists",
                schema: "shared",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    clinic_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dentist_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clinic_dentists", x => x.id);
                    table.ForeignKey(
                        name: "FK_clinic_dentists_clinics_clinic_id",
                        column: x => x.clinic_id,
                        principalSchema: "shared",
                        principalTable: "clinics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_clinic_dentists_dentists_dentist_id",
                        column: x => x.dentist_id,
                        principalSchema: "shared",
                        principalTable: "dentists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "shared",
                table: "tenants",
                columns: new[] { "id", "created_at_utc", "current_migration", "deactivated_at_utc", "deactivated_by_user_id", "last_schema_verified_at_utc", "name", "provisioning_status", "schema_health_status", "schema_name", "status", "updated_at_utc" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SeededDevelopmentTenant", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Dev Practice", "Provisioned", "Healthy", "tenant_dev", "Active", null });

            migrationBuilder.InsertData(
                schema: "shared",
                table: "clinics",
                columns: new[] { "id", "address", "created_at_utc", "deactivated_at_utc", "deactivated_by_user_id", "name", "phone", "status", "tenant_id", "updated_at_utc" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), "Development tenant", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Dev Clinic", "01000000000", "Active", new Guid("11111111-1111-1111-1111-111111111111"), null });

            migrationBuilder.InsertData(
                schema: "shared",
                table: "dentists",
                columns: new[] { "id", "created_at_utc", "full_name", "license_number", "specialization", "updated_at_utc" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Dr. Dev", "LIC-DEV-001", "General Dentistry", null });

            migrationBuilder.InsertData(
                schema: "shared",
                table: "clinic_dentists",
                columns: new[] { "id", "clinic_id", "dentist_id" },
                values: new object[] { new Guid("33333333-3333-3333-3333-333333333333"), new Guid("11111111-1111-1111-1111-111111111111"), new Guid("22222222-2222-2222-2222-222222222222") });

            migrationBuilder.CreateIndex(
                name: "IX_clinic_dentists_clinic_id_dentist_id",
                schema: "shared",
                table: "clinic_dentists",
                columns: new[] { "clinic_id", "dentist_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_clinic_dentists_dentist_id",
                schema: "shared",
                table: "clinic_dentists",
                column: "dentist_id");

            migrationBuilder.CreateIndex(
                name: "IX_clinics_phone",
                schema: "shared",
                table: "clinics",
                column: "phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_clinics_tenant_id",
                schema: "shared",
                table: "clinics",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_dentists_license_number",
                schema: "shared",
                table: "dentists",
                column: "license_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_provisioning_audit_logs_tenant_id_operation",
                schema: "shared",
                table: "tenant_provisioning_audit_logs",
                columns: new[] { "tenant_id", "operation" });

            migrationBuilder.CreateIndex(
                name: "IX_tenants_schema_name",
                schema: "shared",
                table: "tenants",
                column: "schema_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clinic_dentists",
                schema: "shared");

            migrationBuilder.DropTable(
                name: "tenant_provisioning_audit_logs",
                schema: "shared");

            migrationBuilder.DropTable(
                name: "dentists",
                schema: "shared");

            migrationBuilder.DropTable(
                name: "clinics",
                schema: "shared");

            migrationBuilder.DropTable(
                name: "tenants",
                schema: "shared");
        }
    }
}
