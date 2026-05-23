using CliniKey.Application.Abstractions.Tenancy;
using CliniKey.Domain.Errors;
using CliniKey.SharedKernel.Primitives;
using Npgsql;

namespace CliniKey.Infrastructure.Persistence;

internal sealed class TenantMigrationService : ITenantMigrationService
{
    public const string BaselineMigration = "202605230001_InitialTenantOperationalSchema";

    private readonly string _connectionString;
    private readonly TenancyOptions _options;

    public TenantMigrationService(string connectionString, TenancyOptions? options = null)
    {
        _connectionString = connectionString;
        _options = options ?? new TenancyOptions();
    }

    public string ExpectedMigration => BaselineMigration;

    public async Task<Result<string?>> ApplyMigrationsAsync(
        string schemaName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            var schema = PostgresIdentifier.QuoteSchema(schemaName);
            var schemaLiteral = schemaName.Replace("'", "''", StringComparison.Ordinal);

            await ExecuteAsync(connection, transaction, $"""
                CREATE SCHEMA IF NOT EXISTS {schema};

                CREATE TABLE IF NOT EXISTS {schema}.patients (
                    id uuid PRIMARY KEY,
                    first_name character varying(100) NOT NULL,
                    last_name character varying(100) NOT NULL,
                    phone character varying(11) NOT NULL,
                    date_of_birth date NOT NULL,
                    gender integer NOT NULL,
                    insurance_details character varying(500),
                    created_at_utc timestamp with time zone NOT NULL,
                    updated_at_utc timestamp with time zone,
                    is_deleted boolean NOT NULL DEFAULT false,
                    deleted_at_utc timestamp with time zone
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ix_patients_phone ON {schema}.patients (phone);

                CREATE TABLE IF NOT EXISTS {schema}.appointments (
                    id uuid PRIMARY KEY,
                    patient_id uuid NOT NULL,
                    dentist_id uuid NOT NULL,
                    start_time timestamp with time zone NOT NULL,
                    end_time timestamp with time zone NOT NULL,
                    status integer NOT NULL,
                    notes character varying(500),
                    created_at_utc timestamp with time zone NOT NULL,
                    updated_at_utc timestamp with time zone
                );
                CREATE INDEX IF NOT EXISTS ix_appointments_dentist_id_start_time ON {schema}.appointments (dentist_id, start_time);

                CREATE TABLE IF NOT EXISTS {schema}.treatment_plans (
                    id uuid PRIMARY KEY,
                    patient_id uuid NOT NULL,
                    dentist_id uuid NOT NULL,
                    status integer NOT NULL,
                    created_at_utc timestamp with time zone NOT NULL,
                    updated_at_utc timestamp with time zone
                );

                CREATE TABLE IF NOT EXISTS {schema}.treatment_items (
                    id uuid PRIMARY KEY,
                    treatment_plan_id uuid NOT NULL,
                    tooth_code integer NOT NULL,
                    procedure_name character varying(200) NOT NULL,
                    status integer NOT NULL,
                    estimated_cost_amount numeric(18,2) NOT NULL,
                    estimated_cost_currency character varying(3) NOT NULL
                );

                CREATE TABLE IF NOT EXISTS {schema}.invoices (
                    id uuid PRIMARY KEY,
                    patient_id uuid NOT NULL,
                    treatment_plan_id uuid,
                    status integer NOT NULL,
                    created_at_utc timestamp with time zone NOT NULL,
                    updated_at_utc timestamp with time zone
                );

                CREATE TABLE IF NOT EXISTS {schema}.invoice_lines (
                    id uuid PRIMARY KEY,
                    invoice_id uuid NOT NULL,
                    description character varying(300) NOT NULL,
                    vat_rate numeric(18,4) NOT NULL,
                    amount_amount numeric(18,2) NOT NULL,
                    amount_currency character varying(3) NOT NULL
                );
                CREATE INDEX IF NOT EXISTS "IX_invoice_lines_invoice_id" ON {schema}.invoice_lines (invoice_id);

                CREATE TABLE IF NOT EXISTS {schema}.payments (
                    id uuid PRIMARY KEY,
                    invoice_id uuid NOT NULL,
                    method integer NOT NULL,
                    paid_at_utc timestamp with time zone NOT NULL,
                    reference_number character varying(100),
                    amount_amount numeric(18,2) NOT NULL,
                    amount_currency character varying(3) NOT NULL
                );
                CREATE INDEX IF NOT EXISTS "IX_payments_invoice_id" ON {schema}.payments (invoice_id);

                CREATE TABLE IF NOT EXISTS {schema}."__EFMigrationsHistory" (
                    "MigrationId" character varying(150) PRIMARY KEY,
                    "ProductVersion" character varying(32) NOT NULL
                );

                INSERT INTO {schema}."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                VALUES ('{BaselineMigration}', '10.0.0')
                ON CONFLICT ("MigrationId") DO NOTHING;

                CREATE INDEX IF NOT EXISTS "IX_treatment_items_treatment_plan_id" ON {schema}.treatment_items (treatment_plan_id);

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_invoice_lines_invoices_invoice_id'
                          AND conrelid = '{schemaLiteral}.invoice_lines'::regclass
                    ) THEN
                        ALTER TABLE {schema}.invoice_lines
                        ADD CONSTRAINT "FK_invoice_lines_invoices_invoice_id"
                        FOREIGN KEY (invoice_id) REFERENCES {schema}.invoices (id) ON DELETE CASCADE;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_payments_invoices_invoice_id'
                          AND conrelid = '{schemaLiteral}.payments'::regclass
                    ) THEN
                        ALTER TABLE {schema}.payments
                        ADD CONSTRAINT "FK_payments_invoices_invoice_id"
                        FOREIGN KEY (invoice_id) REFERENCES {schema}.invoices (id) ON DELETE CASCADE;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_treatment_items_treatment_plans_treatment_plan_id'
                          AND conrelid = '{schemaLiteral}.treatment_items'::regclass
                    ) THEN
                        ALTER TABLE {schema}.treatment_items
                        ADD CONSTRAINT "FK_treatment_items_treatment_plans_treatment_plan_id"
                        FOREIGN KEY (treatment_plan_id) REFERENCES {schema}.treatment_plans (id) ON DELETE CASCADE;
                    END IF;
                END $$;
                """, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return BaselineMigration;
        }
        catch
        {
            return Result.Failure<string?>(TenantErrors.MigrationFailed);
        }
    }

    public async Task<Result<IReadOnlyList<TenantMigrationResult>>> ApplyPendingMigrationsAsync(
        IReadOnlyCollection<TenantMigrationTarget> targets,
        CancellationToken cancellationToken = default)
    {
        await using var lockConnection = new NpgsqlConnection(_connectionString);
        await lockConnection.OpenAsync(cancellationToken);
        await using var lockTransaction = await lockConnection.BeginTransactionAsync(cancellationToken);

        if (!await TryAcquireMigrationLockAsync(lockConnection, lockTransaction, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<TenantMigrationResult>>(TenantErrors.MigrationAlreadyRunning);
        }

        var results = new List<TenantMigrationResult>();

        foreach (var target in targets)
        {
            var previousMigration = await GetCurrentMigrationAsync(target.SchemaName, cancellationToken);
            var result = await ApplyMigrationsAsync(target.SchemaName, cancellationToken);
            var currentMigration = result.IsSuccess
                ? await GetCurrentMigrationAsync(target.SchemaName, cancellationToken)
                : null;
            results.Add(new TenantMigrationResult(
                target.ClinicId,
                target.SchemaName,
                result.IsSuccess ? "Succeeded" : "Failed",
                previousMigration,
                currentMigration ?? (result.IsSuccess ? result.Value : null),
                result.IsFailure ? result.Error.Description : null));
        }

        await lockTransaction.CommitAsync(cancellationToken);
        return results;
    }

    private async Task<string?> GetCurrentMigrationAsync(string schemaName, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var tableCommand = new NpgsqlCommand(
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM information_schema.tables
                    WHERE table_schema = @schema_name
                      AND table_name = '__EFMigrationsHistory'
                );
                """,
                connection);
            tableCommand.Parameters.AddWithValue("schema_name", schemaName);
            var hasHistory = (bool)(await tableCommand.ExecuteScalarAsync(cancellationToken) ?? false);
            if (!hasHistory)
            {
                return null;
            }

            await using var migrationCommand = new NpgsqlCommand(
                $"""
                SELECT "MigrationId"
                FROM {PostgresIdentifier.QuoteSchema(schemaName)}."__EFMigrationsHistory"
                ORDER BY "MigrationId" DESC
                LIMIT 1;
                """,
                connection);

            return (string?)await migrationCommand.ExecuteScalarAsync(cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private async Task<bool> TryAcquireMigrationLockAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT pg_try_advisory_xact_lock(@lock_key);",
            connection,
            transaction);
        command.Parameters.AddWithValue("lock_key", (long)_options.ProvisioningLockKey);

        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
