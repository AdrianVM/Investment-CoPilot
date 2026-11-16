using Npgsql;

namespace InvestmentCoPilot.IntegrationTests.Infrastructure;

/// <summary>Reads the Identity module's users table directly, to check what the API stored.</summary>
public static class UserTable
{
    public sealed record Row(Guid Id, string Issuer, string Subject, string? Email, string? DisplayName);

    public static async Task<List<Row>> FindBySubjectAsync(string connectionString, string subject)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """SELECT id, issuer, subject, email, display_name FROM "identity"."users" WHERE subject = @subject""",
            connection);
        command.Parameters.AddWithValue("subject", subject);

        var rows = new List<Row>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rows.Add(new Row(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4)));
        }

        return rows;
    }
}
