using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SatPrep.Data;

namespace SatPrep.Services;

/// <summary>
/// Database safety helpers: automatic backup before schema upgrades and
/// on-demand consistent snapshots (used by the Content page and Backup-SatPrep.ps1).
/// </summary>
public static class DatabaseMaintenance
{
    /// <summary>Full path of the SQLite file the context uses.</summary>
    /// <param name="db">Database context.</param>
    public static string DbPath(AppDbContext db)
    {
        var builder = new SqliteConnectionStringBuilder(db.Database.GetConnectionString());
        return Path.GetFullPath(builder.DataSource, AppContext.BaseDirectory);  // Relative paths resolve next to the exe
    }

    /// <summary>Copies the database to backups/ if it exists and migrations are pending.</summary>
    /// <param name="db">Database context.</param>
    /// <param name="log">Logger.</param>
    public static void BackupBeforeMigrate(AppDbContext db, ILogger log)
    {
        var path = DbPath(db);
        if (!File.Exists(path)) return;                                         // Fresh install - nothing to protect
        if (!db.Database.GetPendingMigrations().Any()) return;                  // No schema change
        var target = Snapshot(db, "pre-upgrade");
        log.LogInformation("Database backed up before upgrade: {Path}", target);
    }

    /// <summary>
    /// Writes a consistent copy of the live database using SQLite VACUUM INTO
    /// (safe while the app is running). Returns the backup file path.
    /// </summary>
    /// <param name="db">Database context.</param>
    /// <param name="label">Text added to the file name.</param>
    public static string Snapshot(AppDbContext db, string label)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "backups");
        Directory.CreateDirectory(dir);
        var target = Path.Combine(dir, $"satprep-{label}-{DateTime.Now:yyyyMMdd-HHmmss}.db");
        using var conn = new SqliteConnection($"Data Source={DbPath(db)}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "VACUUM INTO $path";                                  // Consistent online backup
        cmd.Parameters.AddWithValue("$path", target);
        cmd.ExecuteNonQuery();
        return target;
    }
}
