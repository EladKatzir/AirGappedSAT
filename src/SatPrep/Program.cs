using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SatPrep.Data;
using SatPrep.Services;

namespace SatPrep;

/// <summary>
/// Application entry point. Kept thin: it only builds the host via
/// <see cref="AppBootstrapper"/> and runs it.
/// </summary>
public class Program
{
    /// <param name="args">Command line arguments (e.g. --urls http://0.0.0.0:5100).</param>
    public static void Main(string[] args)
    {
        var app = AppBootstrapper.Build(args);   // Configure services + pipeline
        AppBootstrapper.InitializeData(app);     // Migrate DB, seed taxonomy, import content
        app.Run();                               // Start Kestrel and block until shutdown
    }
}

/// <summary>
/// Builds and configures the web host. Mirrors ShiftManager's hosting model:
/// Kestrel self-hosted, SQLite file DB, no external network calls, all static
/// assets served from wwwroot (no CDNs). Runs as a console app or as a
/// Windows Service (installed by deploy/Install-SatPrep.ps1).
/// </summary>
public static class AppBootstrapper
{
    /// <summary>Registers services and the HTTP pipeline.</summary>
    /// <param name="args">Command line arguments passed through to the host.</param>
    public static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory  // Services start in System32; always resolve files next to the exe
        });

        builder.Host.UseWindowsService(o => o.ServiceName = "SatPrep"); // No-op when run from a console

        // Keep auth cookie keys next to the app so logins survive restarts / service account changes
        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "keys")))
            .SetApplicationName("SatPrep");

        builder.Services.AddRazorPages(o =>
        {
            o.Conventions.AuthorizeFolder("/");                      // Everything requires login...
            o.Conventions.AllowAnonymousToFolder("/Account");        // ...except the "enter your name" screen
        });

        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(o =>
            {
                o.LoginPath = "/Account/Login";
                o.LogoutPath = "/Account/Logout";
                o.Cookie.Name = "satprep.auth";
                o.Cookie.HttpOnly = true;                            // JS cannot read the cookie
                o.Cookie.SameSite = SameSiteMode.Lax;                // CSRF protection for normal navigation
                o.ExpireTimeSpan = TimeSpan.FromDays(365);           // Remember the learner on this PC
                o.SlidingExpiration = true;
            });

        // A Windows Service starts in System32, so a relative "Data Source=satprep.db" is anchored to the app folder
        var csb = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(builder.Configuration.GetConnectionString("Default"));
        if (!Path.IsPathRooted(csb.DataSource) && csb.DataSource != ":memory:")
            csb.DataSource = Path.Combine(AppContext.BaseDirectory, csb.DataSource);
        builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(csb.ToString()));       // SQLite file next to the exe

        // Application services
        builder.Services.AddSingleton<MasteryService>();             // Pure, stateless
        builder.Services.AddSingleton<ScoreEstimator>();             // Pure, stateless
        builder.Services.AddSingleton<LessonService>();              // Loads Markdown lessons from disk (cached)
        builder.Services.AddScoped<ContentImporter>();               // Imports question / vocab packs
        builder.Services.AddScoped<ProgressService>();               // Records answers, mastery, reviews
        builder.Services.AddScoped<PracticeSelector>();              // Picks the next practice question
        builder.Services.AddScoped<TestService>();                   // Builds, runs and scores practice tests
        builder.Services.AddScoped<StudyPlanner>();                  // Daily study plan
        builder.Services.AddScoped<StatsService>();                  // Dashboard / analytics queries

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");                                                // Friendly error page
        }

        app.UseStaticFiles();                                                                 // wwwroot (KaTeX, CSS, JS)
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapRazorPages();
        app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();      // Used by the installer

        return app;
    }

    /// <summary>Migrates the database, seeds the taxonomy and imports bundled content.</summary>
    /// <param name="app">The built application.</param>
    public static void InitializeData(WebApplication app)
    {
        using var scope = app.Services.CreateScope();                                         // Scoped services need a scope
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var log = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        DatabaseMaintenance.BackupBeforeMigrate(db, log);                                     // Copy satprep.db before schema changes
        db.Database.Migrate();                                                                // Create / upgrade schema
        SatTaxonomy.EnsureSeeded(db);                                                         // 8 domains / 29 skills

        var contentRoot = Path.Combine(app.Environment.ContentRootPath, "Content");          // Bundled content folder
        var importer = scope.ServiceProvider.GetRequiredService<ContentImporter>();
        importer.ImportFolder(Path.Combine(contentRoot, "packs"));                           // Questions (idempotent)
        importer.ImportVocab(Path.Combine(contentRoot, "vocab.json"));                        // Vocabulary (idempotent)
    }
}
