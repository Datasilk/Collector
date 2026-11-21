using Npgsql;

namespace Collector.Chat.Cli;

/// <summary>
/// Read-only Postgres lookups for chat/entry metadata and the per-user
/// encryption key used to decrypt journal entry files. Every method fails
/// soft (returns null/empty) so the CLI still works when the DB is offline.
/// </summary>
public class Db
{
    private readonly string? _connectionString;

    public Db(string? connectionString) => _connectionString = connectionString;

    public bool Available => !string.IsNullOrWhiteSpace(_connectionString);

    private NpgsqlConnection? Open()
    {
        if (!Available) return null;
        try
        {
            var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            return conn;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"warning: database unavailable: {ex.Message}");
            return null;
        }
    }

    /// <summary>Default test user: owner of the most recent chat, else the
    /// first AppUser row. Pass --user to override.</summary>
    public Guid? ResolveDefaultUser()
    {
        using var conn = Open();
        if (conn == null) return null;
        try
        {
            using var cmd = new NpgsqlCommand(
                @"SELECT ""AppUserId"" FROM public.""Chats"" ORDER BY ""Created"" DESC LIMIT 1", conn);
            var result = cmd.ExecuteScalar();
            if (result is Guid g && g != Guid.Empty) return g;

            using var cmd2 = new NpgsqlCommand(
                @"SELECT ""Id"" FROM public.""AppUsers"" ORDER BY ""Created"" LIMIT 1", conn);
            return cmd2.ExecuteScalar() as Guid?;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"warning: user lookup failed: {ex.Message}");
            return null;
        }
    }

    public record ChatRow(Guid Id, string Title, Guid? JournalEntryId, DateTime Created, DateTime Modified);

    public ChatRow? GetChat(Guid chatId)
    {
        using var conn = Open();
        if (conn == null) return null;
        try
        {
            using var cmd = new NpgsqlCommand(
                @"SELECT ""Id"",""Title"",""JournalEntryId"",""Created"",""Modified"" FROM public.""Chats"" WHERE ""Id""=@id", conn);
            cmd.Parameters.AddWithValue("id", chatId);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            return new ChatRow(r.GetGuid(0), r.IsDBNull(1) ? "" : r.GetString(1),
                r.IsDBNull(2) ? null : r.GetGuid(2), r.GetDateTime(3), r.GetDateTime(4));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"warning: chat lookup failed: {ex.Message}");
            return null;
        }
    }

    public List<ChatRow> ListChats(int limit)
    {
        var rows = new List<ChatRow>();
        using var conn = Open();
        if (conn == null) return rows;
        try
        {
            using var cmd = new NpgsqlCommand(
                @"SELECT ""Id"",""Title"",""JournalEntryId"",""Created"",""Modified"" FROM public.""Chats"" ORDER BY ""Modified"" DESC LIMIT @n", conn);
            cmd.Parameters.AddWithValue("n", limit);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                rows.Add(new ChatRow(r.GetGuid(0), r.IsDBNull(1) ? "" : r.GetString(1),
                    r.IsDBNull(2) ? null : r.GetGuid(2), r.GetDateTime(3), r.GetDateTime(4)));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"warning: chat list failed: {ex.Message}");
        }
        return rows;
    }

    public record EntryRow(Guid Id, string Title, int JournalId, string JournalTitle,
        Guid OwnerId, bool Encrypted, DateTime Created, DateTime Modified);

    public EntryRow? GetEntry(Guid entryId)
    {
        using var conn = Open();
        if (conn == null) return null;
        try
        {
            using var cmd = new NpgsqlCommand(
                @"SELECT e.""Id"", e.""Title"", e.""JournalId"", j.""Title"", j.""AppUserId"", e.""Encrypted"", e.""Created"", e.""Modified""
                  FROM public.""JournalEntries"" e
                  JOIN public.""Journals"" j ON j.""Id"" = e.""JournalId""
                  WHERE e.""Id"" = @id", conn);
            cmd.Parameters.AddWithValue("id", entryId);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            return new EntryRow(r.GetGuid(0), r.IsDBNull(1) ? "" : r.GetString(1),
                r.GetInt32(2), r.IsDBNull(3) ? "" : r.GetString(3),
                r.IsDBNull(4) ? Guid.Empty : r.GetGuid(4),
                !r.IsDBNull(5) && r.GetBoolean(5), r.GetDateTime(6), r.GetDateTime(7));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"warning: entry lookup failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>AES key for decrypting a user's journal entry files.</summary>
    public string? GetEncryptionKey(Guid appUserId)
    {
        using var conn = Open();
        if (conn == null) return null;
        try
        {
            using var cmd = new NpgsqlCommand(
                @"SELECT ""EncryptionKey"" FROM public.""AppUsers"" WHERE ""Id""=@id", conn);
            cmd.Parameters.AddWithValue("id", appUserId);
            return cmd.ExecuteScalar() as string;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"warning: encryption-key lookup failed: {ex.Message}");
            return null;
        }
    }


    /// <summary>Connectivity probe for `status`.</summary>
    public bool Ping()
    {
        using var conn = Open();
        if (conn == null) return false;
        try
        {
            using var cmd = new NpgsqlCommand("SELECT 1", conn);
            return cmd.ExecuteScalar() as int? == 1;
        }
        catch { return false; }
    }
}
