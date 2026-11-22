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
        AppBootstrapper.InitializeData(app);     // Create DB, seed taxonomy, import content packs
        app.Run();                               // Start Kestrel and block until shutdown
    }
}

/// <summary>
/// Builds and configures the web host. Mirrors ShiftManager's hosting model:
/// Kestrel self-hosted, SQLite file DB, no external network calls, all static
/// assets served from wwwroot (no CDNs).
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
            ContentRootPath = AppContext.BaseDirectory  // Run correctly from START_HERE.bat or a Windows Service
        });

        builder.Services.AddRazorPages();                                                     // UI
        builder.Services.AddDbContext<AppDbContext>(o =>
            o.UseSqlite(builder.Configuration.GetConnectionString("Default")));               // SQLite file
        builder.Services.AddSingleton<MasteryService>();                                      // Pure, stateless
        builder.Services.AddScoped<ContentImporter>();                                        // Uses the DbContext

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");                                                // Friendly error page
        }

        app.UseStaticFiles();                                                                 // wwwroot (KaTeX, CSS, JS)
        app.UseRouting();
        app.UseAuthorization();
        app.MapRazorPages();
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));                       // For START_HERE.bat / monitoring

        return app;
    }

    /// <summary>Creates the database if needed, seeds the taxonomy and imports bundled content packs.</summary>
    /// <param name="app">The built application.</param>
    public static void InitializeData(WebApplication app)
    {
        using var scope = app.Services.CreateScope();                                         // Scoped services need a scope
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Database.EnsureCreated();                                                          // TODO: switch to Migrations before first real deployment
        SatTaxonomy.EnsureSeeded(db);                                                         // 8 domains / 29 skills

        var packs = Path.Combine(app.Environment.ContentRootPath, "Content", "packs");       // Bundled question packs
        scope.ServiceProvider.GetRequiredService<ContentImporter>().ImportFolder(packs);     // Idempotent import
    }
}
