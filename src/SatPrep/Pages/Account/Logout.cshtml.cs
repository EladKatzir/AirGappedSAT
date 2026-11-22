using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SatPrep.Pages.Account;

/// <summary>"Switch learner": clears the cookie and returns to the name screen.</summary>
public class LogoutModel : PageModel
{
    /// <summary>GET just redirects (sign-out only happens on POST to avoid accidental logouts via links).</summary>
    public IActionResult OnGet() => Redirect("/Account/Login");

    /// <summary>Signs out and goes back to "Who is studying?".</summary>
    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); // Remove cookie
        return Redirect("/Account/Login");
    }
}
