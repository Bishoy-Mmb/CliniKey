using System.Text.RegularExpressions;

namespace CliniKey.Infrastructure.Persistence;

internal static partial class PostgresIdentifier
{
    public static string QuoteSchema(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier) || identifier.Length > 63 || !SchemaRegex().IsMatch(identifier))
        {
            throw new ArgumentException("Invalid PostgreSQL schema identifier.", nameof(identifier));
        }

        return '"' + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + '"';
    }

    [GeneratedRegex("^[a-z][a-z0-9_]*$")]
    private static partial Regex SchemaRegex();
}
