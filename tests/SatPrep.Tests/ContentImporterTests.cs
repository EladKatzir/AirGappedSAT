using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SatPrep.Data;
using SatPrep.Services;

namespace SatPrep.Tests;

/// <summary>Imports the real bundled sample pack into an in-memory SQLite DB.</summary>
public class ContentImporterTests : IDisposable
{
    private readonly SqliteConnection _conn; // Keeps the in-memory DB alive for the test
    private readonly AppDbContext _db;       // Context under test

    /// <summary>Creates a fresh in-memory database with the taxonomy seeded.</summary>
    public ContentImporterTests()
    {
        _conn = new SqliteConnection("Data Source=:memory:");
        _conn.Open();                                                         // In-memory DB lives as long as the connection
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options);
        _db.Database.EnsureCreated();                                         // Build schema
        SatTaxonomy.EnsureSeeded(_db);                                        // 8 domains / 29 skills
    }

    [Fact]
    public void Taxonomy_has_8_domains_and_29_skills()
    {
        Assert.Equal(8, _db.Domains.Count());
        Assert.Equal(29, _db.Skills.Count());
    }

    [Fact]
    public void Sample_pack_imports_and_reimport_is_idempotent()
    {
        var importer = new ContentImporter(_db, NullLogger<ContentImporter>.Instance);
        var folder = Path.Combine(AppContext.BaseDirectory, "Content", "packs"); // Copied to output by SatPrep.csproj

        var first = importer.ImportFolder(folder);
        Assert.Empty(first.Errors);
        Assert.True(first.Added >= 5);

        var second = importer.ImportFolder(folder);
        Assert.Equal(0, second.Added);                                        // Nothing new
        Assert.Equal(first.Added, second.Updated);                            // Everything matched by ExternalId
        Assert.Equal(first.Added, _db.Questions.Count());                     // No duplicates
    }

    /// <summary>Closes the in-memory DB.</summary>
    public void Dispose()
    {
        _db.Dispose();
        _conn.Dispose();
    }
}
