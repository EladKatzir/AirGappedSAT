using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SatPrep.Data;
using SatPrep.Models;

namespace SatPrep.Pages.Account;

/// <summary>
/// "Who is studying?" screen. No passwords: pick an existing learner or type
/// a new name. A long-lived cookie remembers the choice on this PC.
/// </summary>
public class LoginModel : PageModel
{
    private readonly AppDbContext _db; // Database

    /// <param name="db">Database context.</param>
    public LoginModel(AppDbContext db) => _db = db;

    public List<AppUser> Learners { get; private set; } = new(); // Existing learners, most recent first
    [BindProperty] public string? Name { get; set; }             // Name typed or clicked
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; } // Where to go after signing in
    public string? Error { get; private set; }                   // Validation message

    /// <summary>Shows the learner list.</summary>
    public void OnGet() => LoadLearners();

    /// <summary>Signs in as the given name, creating the learner on first use.</summary>
    public async Task<IActionResult> OnPostAsync()
    {
        var name = (Name ?? "").Trim();                                        // Normalised name
        if (name.Length is < 1 or > 40)
        {
            Error = "Enter a name (1-40 characters).";
            LoadLearners();
            return Page();
        }

        var user = _db.Users.FirstOrDefault(u => u.UserName == name);          // Case-insensitive (NOCASE collation)
        if (user is null)
        {
            user = new AppUser { UserName = name };
            _db.Users.Add(user);                                               // New learner
        }
        user.LastSeenUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Cookie with the learner id and name
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName)
        }, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });            // Survives browser restarts

        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl! : "/");   // Never redirect off-site
    }

    /// <summary>Loads learners for the quick-pick buttons.</summary>
    private void LoadLearners()
        => Learners = _db.Users.OrderByDescending(u => u.LastSeenUtc).Take(20).ToList();
}
