using System.Globalization;
using System.Text.RegularExpressions;
using Atlas.Core.Interfaces;
using Atlas.Core.Models;
using Microsoft.Data.Sqlite;

namespace Atlas.Memory;

/// <summary>
/// Atlas's long-term memory, stored in a local SQLite database with full-text
/// search (FTS5). Everything stays on this machine.
/// </summary>
public sealed class SqliteMemoryService : IMemoryService
{
    private const int SchemaVersion = 1;

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a","an","the","and","or","but","is","are","was","were","be","been","am","do","does","did",
        "i","me","my","mine","you","your","we","our","it","its","he","she","they","them","his","her",
        "to","of","in","on","at","for","with","about","from","by","as","that","this","these","those",
        "what","which","who","whom","when","where","why","how","can","could","would","should","will",
        "tell","know","remember","please","atlas","have","has","had","so","if","then","than","just"
    };

    private readonly string _connectionString;

    public SqliteMemoryService(string? databasePath = null)
    {
        string path = databasePath ?? MemoryPaths.DefaultDatabasePath();

        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false
        }.ToString();

        Migrate();
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public MemoryEntry Remember(
        string content,
        MemoryKind kind = MemoryKind.Fact,
        IEnumerable<string>? tags = null,
        int importance = 3,
        string source = "user")
    {
        content = content.Trim();
        if (content.Length == 0)
            throw new ArgumentException("Memory content cannot be empty.", nameof(content));

        importance = Math.Clamp(importance, 1, 5);
        string tagText = string.Join(",", (tags ?? Array.Empty<string>())
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length > 0)
            .Distinct());
        string now = ToDb(DateTime.UtcNow);

        using SqliteConnection connection = Open();

        // Same thing said again: refresh it instead of storing a duplicate.
        using (SqliteCommand find = connection.CreateCommand())
        {
            find.CommandText = "SELECT id FROM memories WHERE content = $c COLLATE NOCASE LIMIT 1";
            find.Parameters.AddWithValue("$c", content);

            if (find.ExecuteScalar() is long existingId)
            {
                using SqliteCommand update = connection.CreateCommand();
                update.CommandText = """
                    UPDATE memories
                    SET updated_at = $now,
                        importance = MAX(importance, $imp),
                        tags = CASE WHEN $tags = '' THEN tags ELSE $tags END
                    WHERE id = $id
                    """;
                update.Parameters.AddWithValue("$now", now);
                update.Parameters.AddWithValue("$imp", importance);
                update.Parameters.AddWithValue("$tags", tagText);
                update.Parameters.AddWithValue("$id", existingId);
                update.ExecuteNonQuery();

                return GetOn(connection, existingId)!;
            }
        }

        using SqliteCommand insert = connection.CreateCommand();
        insert.CommandText = """
            INSERT INTO memories (kind, content, tags, importance, source, created_at, updated_at, access_count)
            VALUES ($kind, $content, $tags, $imp, $source, $now, $now, 0);
            SELECT last_insert_rowid();
            """;
        insert.Parameters.AddWithValue("$kind", kind.ToString());
        insert.Parameters.AddWithValue("$content", content);
        insert.Parameters.AddWithValue("$tags", tagText);
        insert.Parameters.AddWithValue("$imp", importance);
        insert.Parameters.AddWithValue("$source", source);
        insert.Parameters.AddWithValue("$now", now);

        long id = (long)insert.ExecuteScalar()!;
        return GetOn(connection, id)!;
    }

    public IReadOnlyList<MemoryEntry> Search(string query, int limit = 5)
    {
        string? match = BuildMatchQuery(query);
        if (match == null || limit <= 0)
            return Array.Empty<MemoryEntry>();

        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT m.id, m.kind, m.content, m.tags, m.importance, m.source,
                   m.created_at, m.updated_at, m.last_accessed_at, m.access_count
            FROM memories_fts
            JOIN memories m ON m.id = memories_fts.rowid
            WHERE memories_fts MATCH $match
            ORDER BY bm25(memories_fts), m.importance DESC, m.updated_at DESC
            LIMIT $limit
            """;
        command.Parameters.AddWithValue("$match", match);
        command.Parameters.AddWithValue("$limit", limit);

        List<MemoryEntry> results = ReadAll(command);
        TouchAll(connection, results);
        return results;
    }

    public IReadOnlyList<MemoryEntry> List(MemoryKind? kind = null, int limit = 50)
    {
        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT id, kind, content, tags, importance, source,
                   created_at, updated_at, last_accessed_at, access_count
            FROM memories
            {(kind.HasValue ? "WHERE kind = $kind" : "")}
            ORDER BY updated_at DESC, id DESC
            LIMIT $limit
            """;
        if (kind.HasValue)
            command.Parameters.AddWithValue("$kind", kind.Value.ToString());
        command.Parameters.AddWithValue("$limit", limit);

        return ReadAll(command);
    }

    public IReadOnlyList<MemoryEntry> Pinned(int limit = 3)
    {
        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, kind, content, tags, importance, source,
                   created_at, updated_at, last_accessed_at, access_count
            FROM memories
            WHERE importance >= 5
            ORDER BY updated_at DESC
            LIMIT $limit
            """;
        command.Parameters.AddWithValue("$limit", limit);

        return ReadAll(command);
    }

    public MemoryEntry? Get(long id)
    {
        using SqliteConnection connection = Open();
        return GetOn(connection, id);
    }

    public bool Forget(long id)
    {
        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM memories WHERE id = $id";
        command.Parameters.AddWithValue("$id", id);
        return command.ExecuteNonQuery() > 0;
    }

    public int Count()
    {
        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM memories";
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    // ── Schema ─────────────────────────────────────────────────────────────

    private void Migrate()
    {
        using SqliteConnection connection = Open();

        long version;
        using (SqliteCommand read = connection.CreateCommand())
        {
            read.CommandText = "PRAGMA user_version";
            version = (long)read.ExecuteScalar()!;
        }

        if (version >= SchemaVersion)
            return;

        using SqliteTransaction transaction = connection.BeginTransaction();
        using SqliteCommand create = connection.CreateCommand();
        create.Transaction = transaction;
        create.CommandText = $"""
            CREATE TABLE IF NOT EXISTS memories (
                id               INTEGER PRIMARY KEY AUTOINCREMENT,
                kind             TEXT    NOT NULL DEFAULT 'Fact',
                content          TEXT    NOT NULL,
                tags             TEXT    NOT NULL DEFAULT '',
                importance       INTEGER NOT NULL DEFAULT 3,
                source           TEXT    NOT NULL DEFAULT 'user',
                created_at       TEXT    NOT NULL,
                updated_at       TEXT    NOT NULL,
                last_accessed_at TEXT,
                access_count     INTEGER NOT NULL DEFAULT 0
            );

            CREATE INDEX IF NOT EXISTS ix_memories_kind ON memories(kind);
            CREATE INDEX IF NOT EXISTS ix_memories_importance ON memories(importance);

            CREATE VIRTUAL TABLE IF NOT EXISTS memories_fts USING fts5(
                content, tags,
                content='memories', content_rowid='id',
                tokenize='porter unicode61'
            );

            CREATE TRIGGER IF NOT EXISTS memories_ai AFTER INSERT ON memories BEGIN
                INSERT INTO memories_fts(rowid, content, tags) VALUES (new.id, new.content, new.tags);
            END;

            CREATE TRIGGER IF NOT EXISTS memories_ad AFTER DELETE ON memories BEGIN
                INSERT INTO memories_fts(memories_fts, rowid, content, tags)
                VALUES ('delete', old.id, old.content, old.tags);
            END;

            CREATE TRIGGER IF NOT EXISTS memories_au AFTER UPDATE ON memories BEGIN
                INSERT INTO memories_fts(memories_fts, rowid, content, tags)
                VALUES ('delete', old.id, old.content, old.tags);
                INSERT INTO memories_fts(rowid, content, tags) VALUES (new.id, new.content, new.tags);
            END;

            PRAGMA user_version = {SchemaVersion};
            """;
        create.ExecuteNonQuery();
        transaction.Commit();
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private SqliteConnection Open()
    {
        SqliteConnection connection = new(_connectionString);
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Turns free text into a safe FTS5 query: meaningful words only, each
    /// matched as a prefix, joined with OR. Returns null when nothing is left.
    /// </summary>
    internal static string? BuildMatchQuery(string query)
    {
        List<string> words = Regex.Matches(query.ToLowerInvariant(), @"[\p{L}\p{N}]+")
            .Select(m => m.Value)
            .Where(w => w.Length > 1 && !StopWords.Contains(w))
            .Distinct()
            .Take(12)
            .ToList();

        return words.Count == 0
            ? null
            : string.Join(" OR ", words.Select(w => $"\"{w}\"*"));
    }

    private static MemoryEntry? GetOn(SqliteConnection connection, long id)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, kind, content, tags, importance, source,
                   created_at, updated_at, last_accessed_at, access_count
            FROM memories WHERE id = $id
            """;
        command.Parameters.AddWithValue("$id", id);

        return ReadAll(command).FirstOrDefault();
    }

    private static List<MemoryEntry> ReadAll(SqliteCommand command)
    {
        List<MemoryEntry> entries = new();

        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(new MemoryEntry
            {
                Id = reader.GetInt64(0),
                Kind = Enum.TryParse(reader.GetString(1), out MemoryKind kind) ? kind : MemoryKind.Fact,
                Content = reader.GetString(2),
                Tags = reader.GetString(3)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList(),
                Importance = reader.GetInt32(4),
                Source = reader.GetString(5),
                CreatedAt = FromDb(reader.GetString(6)),
                UpdatedAt = FromDb(reader.GetString(7)),
                LastAccessedAt = reader.IsDBNull(8) ? null : FromDb(reader.GetString(8)),
                AccessCount = reader.GetInt32(9)
            });
        }

        return entries;
    }

    private static void TouchAll(SqliteConnection connection, List<MemoryEntry> entries)
    {
        if (entries.Count == 0)
            return;

        string now = ToDb(DateTime.UtcNow);

        foreach (MemoryEntry entry in entries)
        {
            using SqliteCommand touch = connection.CreateCommand();
            touch.CommandText = """
                UPDATE memories SET last_accessed_at = $now, access_count = access_count + 1
                WHERE id = $id
                """;
            touch.Parameters.AddWithValue("$now", now);
            touch.Parameters.AddWithValue("$id", entry.Id);
            touch.ExecuteNonQuery();
        }
    }

    private static string ToDb(DateTime value) =>
        value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static DateTime FromDb(string value) =>
        DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
